using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Transcriber.Coach;

namespace Transcriber.ViewModels;

/// <summary>One headline number in the practice results.</summary>
public sealed record MetricItem(string Label, string Value, string Hint);

/// <summary>One piece of advice, with a symbol that does not rely on colour alone.</summary>
public sealed record TipItem(TipKind Kind, string Title, string Detail)
{
    public string Symbol => Kind switch { TipKind.Good => "✓", TipKind.Improve => "!", _ => "i" };
}

/// <summary>A saved practice run as listed in the history.</summary>
public sealed record SessionItem(Guid Id, string Title, string When, string Summary);

/// <summary>
/// The practice tab: pick a recording of yourself, get it transcribed, and see how your pace, pauses, filler words
/// and stumbles compare with last time. Written feedback from a local Ollama model is optional.
/// </summary>
public sealed partial class PracticeViewModel : ObservableObject
{
    private readonly IFileReaderService _fileReader;
    private readonly IWhisperService _whisper;
    private readonly IFilePickerService _filePicker;
    private readonly IPracticeHistory _history;
    private readonly ICoachFeedback _feedback;
    private readonly IAudioRecorder _recorder;
    private readonly Func<DateTimeOffset> _now;
    private readonly string _recordingsDirectory;

    /// <summary>A recording stops by itself after this long, so a forgotten recording cannot fill the disk.</summary>
    public static readonly TimeSpan MaxRecording = TimeSpan.FromMinutes(30);

    /// <summary>Anything shorter than this is treated as an accidental click.</summary>
    public static readonly TimeSpan MinRecording = TimeSpan.FromSeconds(1);

    private const string RecordingPrefix = "transcriber_practice_";

    private string? _filePath;
    private string? _ownedRecording;
    private string _transcript = "";
    private SpeechReport? _report;

    public PracticeViewModel(
        IFileReaderService fileReader,
        IWhisperService whisper,
        IFilePickerService filePicker,
        IPracticeHistory history,
        ICoachFeedback feedback,
        IAudioRecorder recorder,
        Func<DateTimeOffset>? now = null,
        string? recordingsDirectory = null)
    {
        _fileReader = fileReader;
        _whisper = whisper;
        _filePicker = filePicker;
        _history = history;
        _feedback = feedback;
        _recorder = recorder;
        _now = now ?? (() => DateTimeOffset.Now);
        _recordingsDirectory = recordingsDirectory ?? Path.GetTempPath();
        _recorder.Progress += OnRecordingProgress;
        DeleteOldRecordings();

        // Only languages with their own filler words and a verbatim prompt are offered.
        Languages = SupportedLanguages.All
            .Where(l => l.Code is "en" or "nl" or "es")
            .Select(l => new LanguageOption(l.Code, l.Name))
            .ToList();
        _selectedLanguage = Languages[0];

        ReloadHistory();
    }

    // ---- Recording -----------------------------------------------------------------------

    public IReadOnlyList<LanguageOption> Languages { get; }

    [ObservableProperty] private LanguageOption _selectedLanguage;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasFile))]
    [NotifyCanExecuteChangedFor(nameof(AnalyzeCommand))]
    private string _fileName = "";

    public bool HasFile => FileName.Length > 0;

    [RelayCommand]
    private async Task ChooseFileAsync()
    {
        if (!IsIdle) return;
        var picked = await _filePicker.PickMediaFilesAsync();
        if (picked.Count > 0) SetFile(picked[0]);
    }

    /// <summary>Uses a file chosen elsewhere, for example dropped on the window or opened from the command line.</summary>
    public void SetFile(string path)
    {
        if (!IsIdle || !File.Exists(path)) return;
        UseFile(path, Path.GetFileName(path));
    }

    private void UseFile(string path, string displayName)
    {
        // A recording of ours is replaced by the next choice; a file the user picked is never touched.
        if (_ownedRecording is not null && !string.Equals(_ownedRecording, path, StringComparison.OrdinalIgnoreCase))
            DeleteQuietly(_ownedRecording);
        if (!string.Equals(_ownedRecording, path, StringComparison.OrdinalIgnoreCase)) _ownedRecording = null;

        _filePath = path;
        FileName = displayName;
        ClearResult();
    }

    private static void DeleteQuietly(string path)
    {
        try { File.Delete(path); } catch (Exception e) when (e is IOException or UnauthorizedAccessException) { }
    }

    // ---- Microphone ----------------------------------------------------------------------

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsIdle))]
    [NotifyCanExecuteChangedFor(nameof(AnalyzeCommand))]
    [NotifyCanExecuteChangedFor(nameof(StartRecordingCommand))]
    [NotifyCanExecuteChangedFor(nameof(StopRecordingCommand))]
    private bool _isRecording;

    [ObservableProperty] private string _recordingText = "";

    /// <summary>Loudness from 0 to 1, so it is obvious the microphone is picking something up.</summary>
    [ObservableProperty] private double _recordingLevel;

    private bool CanStartRecording() => !IsBusy && !IsRecording;

    [RelayCommand(CanExecute = nameof(CanStartRecording))]
    private async Task StartRecordingAsync()
    {
        ErrorMessage = "";
        var path = Path.Combine(_recordingsDirectory, $"{RecordingPrefix}{Guid.NewGuid():N}.wav");
        try
        {
            await _recorder.StartAsync(path);
        }
        catch (RecordingException e)
        {
            DeleteQuietly(path);
            ErrorMessage = e.Message;
            return;
        }

        _recordingPath = path;
        _loudestLevel = 0;
        MicrophoneSilent = false;
        RecordingLevel = 0;
        RecordingText = "Recording 0:00";
        IsRecording = true;
    }

    private string? _recordingPath;

    private bool CanStopRecording() => IsRecording;

    [RelayCommand(CanExecute = nameof(CanStopRecording))]
    private void StopRecording()
    {
        var path = _recordingPath;
        _recordingPath = null;
        _recorder.Stop();
        IsRecording = false;
        MicrophoneSilent = false;
        RecordingLevel = 0;
        if (path is null) return;

        var length = TryGetLength(path);
        // 16 kHz mono 16-bit is 32,000 bytes per second, plus a 44 byte header.
        if (length < 44 + (long)(MinRecording.TotalSeconds * 32_000))
        {
            DeleteQuietly(path);
            ErrorMessage = "That recording was too short. Hold on a little longer and try again.";
            return;
        }

        // The previous recording is only dropped once its replacement has turned out well.
        if (_ownedRecording is not null) DeleteQuietly(_ownedRecording);
        _ownedRecording = path;
        UseFile(path, $"Recording {_now().LocalDateTime.ToString("d MMM HH:mm", CultureInfo.InvariantCulture)}");
    }

    private static long TryGetLength(string path)
    {
        try { return File.Exists(path) ? new FileInfo(path).Length : 0; }
        catch (IOException) { return 0; }
    }

    /// <summary>
    /// True when nothing at all has come in for a few seconds. A blocked microphone usually records pure silence
    /// instead of failing, so this is the only sign the user gets.
    /// </summary>
    [ObservableProperty] private bool _microphoneSilent;

    private double _loudestLevel;

    private static readonly TimeSpan SilenceCheckAfter = TimeSpan.FromSeconds(3);
    private const double SilentLevel = 0.001;

    private void OnRecordingProgress(object? sender, RecordingProgress p)
    {
        if (!IsRecording) return;
        _loudestLevel = Math.Max(_loudestLevel, p.Level);
        MicrophoneSilent = p.Elapsed >= SilenceCheckAfter && _loudestLevel < SilentLevel;
        RecordingLevel = Math.Clamp(p.Level, 0, 1);
        RecordingText = $"Recording {(int)p.Elapsed.TotalMinutes}:{p.Elapsed.Seconds:00}";
        if (p.Elapsed >= MaxRecording)
        {
            StopRecording();
            ErrorMessage = $"Stopped after {(int)MaxRecording.TotalMinutes} minutes, the longest a single recording can be.";
        }
    }

    // Recordings the app made in an earlier session and never cleaned up (for example after a crash).
    private void DeleteOldRecordings()
    {
        try
        {
            foreach (var file in Directory.EnumerateFiles(_recordingsDirectory, RecordingPrefix + "*.wav"))
                if (File.GetLastWriteTimeUtc(file) < DateTime.UtcNow.AddDays(-1)) DeleteQuietly(file);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { }
    }

    // ---- Analysis ------------------------------------------------------------------------

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AnalyzeCommand))]
    [NotifyCanExecuteChangedFor(nameof(GetFeedbackCommand))]
    [NotifyCanExecuteChangedFor(nameof(StartRecordingCommand))]
    [NotifyPropertyChangedFor(nameof(IsIdle))]
    private bool _isBusy;

    /// <summary>Nothing is being analysed or recorded, so the recording and language can be changed.</summary>
    public bool IsIdle => !IsBusy && !IsRecording;

    [ObservableProperty] private double _progress;
    [ObservableProperty] private string _statusText = "";
    [ObservableProperty] private string _errorMessage = "";

    [ObservableProperty] private IReadOnlyList<MetricItem> _metrics = [];
    [ObservableProperty] private IReadOnlyList<TipItem> _tips = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(GetFeedbackCommand))]
    private bool _hasResult;

    /// <summary>What was heard, so the numbers can be checked against the words.</summary>
    public string Transcript => _transcript;

    private bool CanAnalyze() => IsIdle && HasFile;

    [RelayCommand(CanExecute = nameof(CanAnalyze), IncludeCancelCommand = true)]
    private async Task AnalyzeAsync(CancellationToken ct)
    {
        if (_filePath is null) return;

        ClearResult();
        IsBusy = true;
        Progress = 0;
        StatusText = "Preparing audio";
        string? audio = null;
        try
        {
            audio = await _fileReader.GetAudioFileFromFileAsync(_filePath);

            var language = SelectedLanguage.Code;
            var progress = new Progress<TranscriptionProgress>(p =>
            {
                StatusText = p.Stage;
                Progress = Math.Clamp(p.Fraction, 0, 1);
            });

            var segments = await _whisper.TranscribeAsync(audio, new TranscriptionOptions
            {
                Language = language,
                Model = WhisperModel.Small,
                // Without this Whisper tidies away the very um's and stutters being measured.
                Prompt = VerbatimPrompt.For(language),
            }, progress, ct);

            _report = SpeechAnalyzer.Analyze(segments, language);
            _transcript = string.Join(" ", segments.Select(s => s.Text.Trim()));
            OnPropertyChanged(nameof(Transcript));

            var previous = _history.Load().LastOrDefault(s => s.Language == language)?.Report;
            Metrics = BuildMetrics(_report);
            Tips = CoachTips.From(_report, previous).Select(t => new TipItem(t.Kind, t.Title, t.Detail)).ToList();
            HasResult = true;

            // Runs too short to judge would only distort the progress shown over time.
            if (_report.WordCount >= CoachTips.MinimumWords)
            {
                _history.Add(new PracticeSession(Guid.NewGuid(), _now(), FileName, language, _report));
                ReloadHistory();
            }
        }
        catch (OperationCanceledException)
        {
            ErrorMessage = "Stopped.";
        }
        catch (Exception e) when (e is IOException or InvalidOperationException or NotSupportedException or UnauthorizedAccessException)
        {
            ErrorMessage = $"Couldn't analyse this recording: {e.Message}";
        }
        finally
        {
            // A converted copy is ours to remove; a wav used in place belongs to the user.
            if (audio is not null && !string.Equals(audio, _filePath, StringComparison.OrdinalIgnoreCase))
            {
                try { File.Delete(audio); } catch (IOException) { }
            }
            IsBusy = false;
            StatusText = "";
        }
    }

    private void ClearResult()
    {
        _report = null;
        _transcript = "";
        OnPropertyChanged(nameof(Transcript));
        Metrics = [];
        Tips = [];
        HasResult = false;
        ErrorMessage = "";
        FeedbackText = "";
        FeedbackError = "";
    }

    private static IReadOnlyList<MetricItem> BuildMetrics(SpeechReport r)
    {
        var inv = CultureInfo.InvariantCulture;
        return
        [
            new("Pace", $"{r.WordsPerMinute.ToString("0", inv)} wpm", "Easy to follow: 120 to 160"),
            new("Filler words", r.FillersPer100Words.ToString("0.#", inv), r.FillerTotal == 0 ? "None found" : $"per 100 words ({r.FillerTotal} in total)"),
            new("Pauses", r.Pauses.Count.ToString(inv), r.Pauses.Count == 0 ? "None noticed" : $"Longest {r.LongestPause.TotalSeconds.ToString("0.#", inv)} s"),
            new("Stumbles", r.Repeats.Count.ToString(inv), "Repeated words or stuttered starts"),
            new("Length", $"{(int)r.Duration.TotalMinutes}:{r.Duration.Seconds:00}", $"{r.WordCount} words"),
        ];
    }

    // ---- History -------------------------------------------------------------------------

    [ObservableProperty] private IReadOnlyList<SessionItem> _sessions = [];
    [ObservableProperty] private bool _hasHistory;

    private void ReloadHistory()
    {
        Sessions = _history.Load()
            .OrderByDescending(s => s.When)
            .Select(s => new SessionItem(s.Id, s.Title, s.When.LocalDateTime.ToString("d MMM yyyy HH:mm", CultureInfo.InvariantCulture), Summarise(s.Report)))
            .ToList();
        HasHistory = Sessions.Count > 0;
    }

    private static string Summarise(SpeechReport r) =>
        $"{r.WordsPerMinute.ToString("0", CultureInfo.InvariantCulture)} wpm · {r.FillersPer100Words.ToString("0.#", CultureInfo.InvariantCulture)} fillers per 100 · {r.Repeats.Count} stumbles";

    [RelayCommand]
    private void DeleteSession(SessionItem? item)
    {
        if (item is null) return;
        _history.Delete(item.Id);
        ReloadHistory();
    }

    [RelayCommand]
    private void ClearHistory()
    {
        _history.Clear();
        ReloadHistory();
    }

    // ---- Written feedback (optional, needs Ollama) ---------------------------------------

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanAskForFeedback))]
    [NotifyCanExecuteChangedFor(nameof(GetFeedbackCommand))]
    private IReadOnlyList<string> _feedbackModels = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(GetFeedbackCommand))]
    private string? _selectedFeedbackModel;

    [ObservableProperty] private string _feedbackText = "";
    [ObservableProperty] private string _feedbackError = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanAskForFeedback))]
    private bool _isGettingFeedback;

    /// <summary>True when Ollama is running with at least one model, so the option is only shown when it can work.</summary>
    public bool CanAskForFeedback => FeedbackModels.Count > 0;

    /// <summary>Looks for Ollama on this computer. Call when the tab is shown; it quietly finds nothing if Ollama is not running.</summary>
    [RelayCommand]
    private async Task RefreshFeedbackModelsAsync()
    {
        FeedbackModels = await _feedback.ListModelsAsync();
        if (SelectedFeedbackModel is null || !FeedbackModels.Contains(SelectedFeedbackModel))
            SelectedFeedbackModel = FeedbackModels.FirstOrDefault();
    }

    private bool CanGetFeedback() => HasResult && !IsBusy && !IsGettingFeedback && SelectedFeedbackModel is not null;

    [RelayCommand(CanExecute = nameof(CanGetFeedback))]
    private async Task GetFeedbackAsync()
    {
        if (_report is null || SelectedFeedbackModel is null) return;

        IsGettingFeedback = true;
        GetFeedbackCommand.NotifyCanExecuteChanged();
        FeedbackText = "";
        FeedbackError = "";
        try
        {
            FeedbackText = await _feedback.GetFeedbackAsync(SelectedFeedbackModel, _transcript, _report);
        }
        catch (OllamaException e)
        {
            FeedbackError = e.Message;
        }
        finally
        {
            IsGettingFeedback = false;
            GetFeedbackCommand.NotifyCanExecuteChanged();
        }
    }
}
