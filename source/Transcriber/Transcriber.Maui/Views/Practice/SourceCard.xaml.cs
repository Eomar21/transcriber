using System.ComponentModel;
using Transcriber.ViewModels.Practice;

namespace Transcriber.Maui.Views.Practice;

public partial class SourceCard : ContentView
{
    private const string PulseName = "RecordDotPulse";
    private MicrophoneViewModel? _microphone;

    public SourceCard() => InitializeComponent();

    protected override void OnBindingContextChanged()
    {
        base.OnBindingContextChanged();
        if (_microphone is not null) _microphone.PropertyChanged -= OnMicrophoneChanged;
        _microphone = (BindingContext as SourceViewModel)?.Microphone;
        if (_microphone is not null) _microphone.PropertyChanged += OnMicrophoneChanged;
        UpdatePulse();
    }

    private void OnMicrophoneChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MicrophoneViewModel.IsRecording)) Dispatcher.Dispatch(UpdatePulse);
    }

    // The dot is steady when idle and fades in and out about once a second while the microphone records.
    private void UpdatePulse()
    {
        RecordDot.AbortAnimation(PulseName);
        RecordDot.Opacity = 1;
        if (_microphone?.IsRecording != true) return;

        var pulse = new Animation(t => RecordDot.Opacity = 0.25 + 0.75 * (0.5 + 0.5 * Math.Cos(t * 2 * Math.PI)));
        pulse.Commit(RecordDot, PulseName, length: 1100, repeat: () => true);
    }
}
