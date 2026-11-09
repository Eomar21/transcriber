using Transcriber.ViewModels;

namespace Transcriber.Maui.Services;

/// <summary>
/// Records the microphone straight to a 16 kHz mono 16-bit wav, the format Whisper takes, so no conversion is needed
/// afterwards. This file holds what both platforms share. Windows uses NAudio (WinMM) in <c>Platforms/Windows/Services</c>;
/// macOS uses AVFoundation in <c>Platforms/MacCatalyst/Services</c>.
/// </summary>
public sealed partial class MauiAudioRecorder : IAudioRecorder
{
    public const int SampleRate = 16000;

    public event EventHandler<RecordingProgress>? Progress;

    public bool IsRecording { get; private set; }

    // Progress comes from audio threads; the view model expects it on the UI thread.
    private void Report(TimeSpan elapsed, double level) =>
        MainThread.BeginInvokeOnMainThread(() => Progress?.Invoke(this, new RecordingProgress(elapsed, level)));
}
