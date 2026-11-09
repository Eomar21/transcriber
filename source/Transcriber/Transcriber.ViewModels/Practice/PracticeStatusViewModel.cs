using CommunityToolkit.Mvvm.ComponentModel;
using Transcriber.ViewModels.Progress;

namespace Transcriber.ViewModels.Practice;

/// <summary>What the person sees about the work in progress: the progress bar and the message when something goes wrong.</summary>
public sealed partial class PracticeStatusViewModel(PracticeState state, IProgressLookPicker looks) : PracticeSectionViewModel(state)
{
    [ObservableProperty] private double _progress;

    /// <summary>The sprite and the theme of the progress bar. Each analysis gets a new random look.</summary>
    [ObservableProperty] private ProgressLook _look = looks.Pick();

    /// <summary>True while an analysis is in progress, and for a moment after one that reached 100 percent, so the finish can be seen.</summary>
    [ObservableProperty] private bool _showProgress;

    private int _runId;

    /// <summary>How long the bar stays after an analysis that finished.</summary>
    public static TimeSpan FinishLinger { get; } = TimeSpan.FromMilliseconds(2000);

    public void BeginProgress()
    {
        _runId++;
        Look = looks.Pick();
        Progress = 0;
        ShowProgress = true;
    }

    public async Task EndProgressAsync(TimeSpan linger)
    {
        var id = _runId;
        if (Progress >= 1 && linger > TimeSpan.Zero) await Task.Delay(linger);
        if (id == _runId) ShowProgress = false;
    }
    [ObservableProperty] private string _statusText = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    private string _errorMessage = "";

    public bool HasError => ErrorMessage.Length > 0;
}
