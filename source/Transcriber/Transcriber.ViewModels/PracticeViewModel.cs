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
    private readonly Func<DateTimeOffset> _now;

    private string? _filePath;
    private string _transcript = "";
    private SpeechReport? _report;

    public PracticeViewModel(
        IFileReaderService fileReader,
        IWhisperService whisper,
        IFilePickerService filePicker,
        IPracticeHistory history,
        ICoachFeedback feedback,
        Func<DateTimeOffset>? now = null)
    {
        _fileReader = fileReader;
        _whisper = whisper;
        _filePicker = filePicker;
        _history = history;
        _feedback = feedback;
        _now = now ?? (() => DateTimeOffset.Now);

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
        if (IsBusy) return;
        var picked = await _filePicker.PickMediaFilesAsync();
        if (picked.Count > 0) SetFile(picked[0]);
    }

    /// <summary>Uses a file chosen elsewhere, for example dropped on the window or opened from the command line.</summary>
    public void SetFile(string path)
    {
        if (IsBusy || !File.Exists(path)) return;
        _filePath = path;
        FileName = Path.GetFileName(path);
        ClearResult();
    }

    // ---- Analysis ------------------------------------------------------------------------

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AnalyzeCommand))]
    [NotifyCanExecuteChangedFor(nameof(GetFeedbackCommand))]
    [NotifyPropertyChangedFor(nameof(IsIdle))]
    private bool _isBusy;

    public bool IsIdle => !IsBusy;

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

    private bool CanAnalyze() => !IsBusy && HasFile;

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
