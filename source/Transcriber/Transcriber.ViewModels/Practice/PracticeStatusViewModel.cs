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

    /// <summary>Raised once for each analysis, after the bar was hidden. A new analysis that started in the meantime does not raise it.</summary>
    public event EventHandler? ProgressEnded;

    /// <summary>How long the bar stays after an analysis that finished.</summary>
    public static TimeSpan FinishLinger { get; } = TimeSpan.FromMilliseconds(3000);

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
        if (id != _runId) return; // a new analysis started: this end is out of date

        ShowProgress = false;
        ProgressEnded?.Invoke(this, EventArgs.Empty);
    }
    [ObservableProperty] private string _statusText = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    private string _errorMessage = "";

    public bool HasError => ErrorMessage.Length > 0;
}
