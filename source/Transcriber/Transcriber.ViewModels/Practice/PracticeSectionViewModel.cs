using CommunityToolkit.Mvvm.ComponentModel;

namespace Transcriber.ViewModels.Practice;

/// <summary>The base of a section of the Practice tab. It passes the shared state on, so a view can bind to it.</summary>
public abstract class PracticeSectionViewModel : ObservableObject
{
    protected PracticeSectionViewModel(PracticeState state)
    {
        State = state;
        state.PropertyChanged += (_, e) =>
        {
            OnPropertyChanged(e.PropertyName);
            OnStateChanged();
        };
    }

    protected PracticeState State { get; }

    public bool IsBusy => State.IsBusy;

    public bool IsRecording => State.IsRecording;

    public bool IsIdle => State.IsIdle;

    /// <summary>Called when a recording or an analysis starts or ends. A section overrides it to update its own commands.</summary>
    protected virtual void OnStateChanged() { }
}
