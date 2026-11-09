using CommunityToolkit.Mvvm.ComponentModel;

namespace Transcriber.ViewModels.Transcribe;

/// <summary>The base of a section of the Transcribe screen. It passes the busy state on, so a view can bind to <see cref="IsIdle"/>.</summary>
public abstract class SectionViewModel : ObservableObject
{
    protected SectionViewModel(RunState run)
    {
        Run = run;
        run.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName != nameof(RunState.IsBusy)) return;
            OnPropertyChanged(nameof(IsBusy));
            OnPropertyChanged(nameof(IsIdle));
            OnRunStateChanged();
        };
    }

    protected RunState Run { get; }

    public bool IsBusy => Run.IsBusy;

    public bool IsIdle => Run.IsIdle;

    /// <summary>Called when a run starts or ends. A section overrides it to update its own commands.</summary>
    protected virtual void OnRunStateChanged() { }
}
