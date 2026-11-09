using CommunityToolkit.Mvvm.ComponentModel;

namespace Transcriber.ViewModels.Transcribe;

/// <summary>
/// The one fact that every part of the Transcribe screen needs to agree on: is a run in progress.
/// The sections do not know each other. They know this object.
/// </summary>
public sealed partial class RunState : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsIdle))]
    private bool _isBusy;

    public bool IsIdle => !IsBusy;
}
