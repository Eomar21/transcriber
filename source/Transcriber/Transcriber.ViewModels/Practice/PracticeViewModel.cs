using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Transcriber.Coach.Interfaces;
using Transcriber.Coach.Models;
using Transcriber.Services.Exceptions;
using Transcriber.Services.Interfaces;
using Transcriber.Services.Models;

namespace Transcriber.ViewModels.Practice;

/// <summary>
/// The practice tab: pick a recording of yourself, get it transcribed, and see how your pace, pauses, filler words
/// and stumbles compare with last time. It owns the sections of the tab and runs the analysis with them.
/// Each section has its own view model and does not know the others. This class connects them.
/// </summary>
public sealed partial class PracticeViewModel : ObservableObject
{
    private readonly PracticeState _state;
    private readonly IFileReaderService _fileReader;
    private readonly IWhisperService _whisper;
    private readonly IModelCatalog _modelCatalog;
    private readonly ISpeechAnalyzer _analyzer;
    private readonly ICoachAdvisor _advisor;
    private readonly IVerbatimPromptProvider _verbatimPrompts;
    private readonly Func<DateTimeOffset> _now;

    public PracticeViewModel(
        PracticeState state,
        SourceViewModel source,
        PracticeStatusViewModel status,
        AnalysisViewModel analysis,
        FeedbackViewModel feedback,
        HistoryViewModel history,
        IFileReaderService fileReader,
        IWhisperService whisper,
        IModelCatalog modelCatalog,
        ISpeechAnalyzer analyzer,
        ICoachAdvisor advisor,
        IVerbatimPromptProvider verbatimPrompts,
        Func<DateTimeOffset>? now = null)
    {
        _state = state;
        Source = source;
        Status = status;
        Analysis = analysis;
        Feedback = feedback;
        History = history;
        _fileReader = fileReader;
        _whisper = whisper;
        _modelCatalog = modelCatalog;
        _analyzer = analyzer;
        _advisor = advisor;
        _verbatimPrompts = verbatimPrompts;
        _now = now ?? (() => DateTimeOffset.Now);

        // The sections only raise events. What each event means for the others is decided here and only here.
        Source.SourceChanged += (_, _) => ClearResult();
        Source.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(SourceViewModel.HasFile)) AnalyzeCommand.NotifyCanExecuteChanged();
        };
        Microphone.Starting += (_, _) => Status.ErrorMessage = "";
        Microphone.Recorded += (_, r) => Source.UseRecording(r.Path, r.DisplayName);
        Microphone.Problem += (_, message) => Status.ErrorMessage = message;
        _state.PropertyChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(IsBusy));
            OnPropertyChanged(nameof(IsIdle));
            AnalyzeCommand.NotifyCanExecuteChanged();
        };
    }

    public SourceViewModel Source { get; }
    public MicrophoneViewModel Microphone => Source.Microphone;
    public PracticeStatusViewModel Status { get; }
    public AnalysisViewModel Analysis { get; }
    public FeedbackViewModel Feedback { get; }
    public HistoryViewModel History { get; }

    public bool IsBusy => _state.IsBusy;

    public bool IsIdle => _state.IsIdle;

    private bool CanAnalyze() => IsIdle && Source.HasFile;

    [RelayCommand(CanExecute = nameof(CanAnalyze), IncludeCancelCommand = true)]
    private async Task AnalyzeAsync(CancellationToken ct)
    {
        var filePath = Source.FilePath;
        if (filePath is null) return;

        ClearResult();
        _state.IsBusy = true;
        Status.Progress = 0;
        Status.StatusText = "Preparing audio";
        string? audio = null;
        try
        {
            audio = await _fileReader.GetAudioFileFromFileAsync(filePath);

            var language = Source.SelectedLanguage.Code;
            var progress = new Progress<TranscriptionProgress>(p =>
            {
                Status.StatusText = p.Stage;
                Status.Progress = Math.Clamp(p.Fraction, 0, 1);
            });

            var segments = await _whisper.TranscribeAsync(audio, new TranscriptionOptions
            {
                Language = language,
                Model = _modelCatalog.Practice,
                // Without this Whisper tidies away the very um's and stutters being measured.
                Prompt = _verbatimPrompts.For(language),
            }, progress, ct);

            var report = _analyzer.Analyze(segments, language);
            var previous = History.LastReport(language);
            Analysis.Show(
                report,
                string.Join(" ", segments.Select(s => s.Text.Trim())),
                BuildMetrics(report),
                _advisor.Advise(report, previous).Select(t => new TipItem(t.Kind, t.Title, t.Detail)).ToList());

            // Runs too short to judge would only distort the progress shown over time.
            if (report.WordCount >= _advisor.MinimumWords)
                History.Add(new PracticeSession(Guid.NewGuid(), _now(), Source.FileName, language, report));
        }
        catch (OperationCanceledException)
        {
            Status.ErrorMessage = "Stopped.";
        }
        catch (Exception e) when (e is IOException or MediaReadException or InvalidOperationException or NotSupportedException or UnauthorizedAccessException)
        {
            Status.ErrorMessage = $"Couldn't analyse this recording: {e.Message}";
        }
        finally
        {
            // A converted copy is ours to remove; a wav used in place belongs to the user.
            if (audio is not null && !string.Equals(audio, filePath, StringComparison.OrdinalIgnoreCase))
            {
                try { File.Delete(audio); } catch (IOException) { }
            }
            _state.IsBusy = false;
            Status.StatusText = "";
        }
    }

    /// <summary>A new file or a new analysis makes the result of the earlier one out of date.</summary>
    private void ClearResult()
    {
        Analysis.Clear();
        Feedback.Clear();
        Status.ErrorMessage = "";
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
}
