using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Transcriber.Services.Models;

namespace Transcriber.ViewModels.Transcribe;

/// <summary>What the person sees about a run: the progress bar, the outcome and the notes that go with the outcome.</summary>
public sealed partial class RunStatusViewModel : SectionViewModel
{
    private readonly IModelSuggestion _suggestion;

    public RunStatusViewModel(IModelSuggestion suggestion, RunState run) : base(run) => _suggestion = suggestion;

    // ---- Progress ------------------------------------------------------------------------

    [ObservableProperty] private double _progress;

    /// <summary>True while a run is in progress, and for a moment after a run that reached 100 percent, so the finish can be seen.</summary>
    [ObservableProperty] private bool _showProgress;

    private int _runId;

    /// <summary>How long the bar stays after a run that finished.</summary>
    public static TimeSpan FinishLinger { get; } = TimeSpan.FromMilliseconds(2000);

    public void BeginProgress()
    {
        _runId++;
        Progress = 0;
        ShowProgress = true;
    }

    public async Task EndProgressAsync(TimeSpan linger)
    {
        var id = _runId;
        if (Progress >= 1 && linger > TimeSpan.Zero) await Task.Delay(linger);
        if (id == _runId) ShowProgress = false;
    }
    [ObservableProperty] private string _statusText = string.Empty;

    // ---- Outcome -------------------------------------------------------------------------

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasResultMessage))]
    private string _resultMessage = string.Empty;

    public bool HasResultMessage => ResultMessage.Length > 0;

    [ObservableProperty] private ResultKind _resultKind;

    /// <summary>Secondary line under the result: the saved file's path (or folder, for several files).</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasResultDetail))]
    private string _resultDetail = string.Empty;

    public bool HasResultDetail => ResultDetail.Length > 0;

    /// <summary>Muted note about how the run was processed, e.g. "Processed with Vulkan".</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasRuntimeNote))]
    private string _runtimeNote = string.Empty;

    public bool HasRuntimeNote => RuntimeNote.Length > 0;

    /// <summary>Shown with a finished transcript when a bigger model would have been clearly more accurate for its language.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasAccuracyNote))]
    private string _accuracyNote = string.Empty;

    public bool HasAccuracyNote => AccuracyNote.Length > 0;

    /// <summary>Shown when a clear part of the speech in a recording has no text, for example when two languages are mixed.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasCompletenessNote))]
    private string _completenessNote = string.Empty;

    public bool HasCompletenessNote => CompletenessNote.Length > 0;

    /// <summary>Lists the files that could not be read, each with the reason, when other files in the run were transcribed.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasFailureNote))]
    private string _failureNote = string.Empty;

    public bool HasFailureNote => FailureNote.Length > 0;

    // The accuracy note offers a button for the suggested model. The options section owns that choice.
    public string UseSuggestedModelText => _suggestion.UseSuggestedModelText;

    public IRelayCommand UseSuggestedModelCommand => _suggestion.UseSuggestedModelCommand;

    public void SetResult(ResultKind kind, string message)
    {
        ResultKind = kind;
        ResultMessage = message;
    }

    /// <summary>Removes the outcome of an earlier run.</summary>
    public void Clear()
    {
        ResultMessage = string.Empty;
        ResultDetail = string.Empty;
        RuntimeNote = string.Empty;
        AccuracyNote = string.Empty;
        CompletenessNote = string.Empty;
        FailureNote = string.Empty;
        ResultKind = ResultKind.None;
    }

    // ---- Texts ---------------------------------------------------------------------------

    /// <summary>One line for each file that could not be read: its name and why.</summary>
    public static string FailureMessage(IReadOnlyList<(string Name, string Reason)> failed) =>
        string.Join(Environment.NewLine, failed.Select(f => $"{f.Name}: {f.Reason}"));

    /// <summary>The warning for recordings whose transcript misses speech, or an empty string when nothing is missing.</summary>
    public static string CompletenessMessage(IReadOnlyList<(string Name, SpeechCoverage Coverage)> incomplete, string language)
    {
        if (incomplete.Count == 0) return string.Empty;

        var what = incomplete.Count == 1
            ? $"About {incomplete[0].Coverage.MissingSeconds:0} of {incomplete[0].Coverage.SpeechSeconds:0} seconds of speech has no text."
            : $"Some speech has no text in {string.Join(", ", incomplete.Select(i => i.Name))}.";
        var why = language == "auto"
            ? "This can happen when a recording mixes languages. Select the main language instead of Auto-detect, or use a larger model, and transcribe again."
            : "This can happen when the audio is unclear or contains another language. Use a larger model or a cleaner recording, and transcribe again.";
        return $"Some speech may be missing. {what} {why}";
    }
}
