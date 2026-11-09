using CommunityToolkit.Mvvm.ComponentModel;

namespace Transcriber.ViewModels.Practice;

/// <summary>
/// What the sections of the Practice tab must agree on: is the microphone recording, and is an analysis running.
/// The sections do not know each other. They know this object.
/// </summary>
public sealed partial class PracticeState : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsIdle))]
    private bool _isBusy;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsIdle))]
    private bool _isRecording;

    /// <summary>Nothing is being analysed or recorded, so the recording and language can be changed.</summary>
    public bool IsIdle => !IsBusy && !IsRecording;
}
