using AudioToolbox;
using AVFoundation;
using Foundation;
using Transcriber.ViewModels;

namespace Transcriber.Maui.Services;

// The macOS recorder, built on AVFoundation. What both platforms share is in Services/MauiAudioRecorder.cs.
public sealed partial class MauiAudioRecorder
{
    private AVAudioRecorder? _recorder;
    private IDispatcherTimer? _timer;

    public async Task StartAsync(string wavPath)
    {
        Stop();

        var allowed = await AVCaptureDevice.RequestAccessForMediaTypeAsync(AVAuthorizationMediaType.Audio);
        if (!allowed)
            throw new RecordingException(
                "Microphone access is turned off for Transcriber. Allow it in System Settings > Privacy & Security > Microphone, then try again.");

        var settings = new AudioSettings
        {
            Format = AudioFormatType.LinearPCM,
            SampleRate = SampleRate,
            NumberChannels = 1,
            LinearPcmBitDepth = 16,
            LinearPcmFloat = false,
            LinearPcmBigEndian = false,
        };

        var recorder = AVAudioRecorder.Create(NSUrl.FromFilename(wavPath), settings, out var error);
        if (recorder is null || error is not null)
            throw new RecordingException("The microphone could not be opened. Check that it is connected and try again.");

        recorder.MeteringEnabled = true;
        if (!recorder.PrepareToRecord() || !recorder.Record())
            throw new RecordingException("The microphone could not be started. Check that it is connected and not in use by another app.");

        _recorder = recorder;
        IsRecording = true;

        _timer ??= Application.Current?.Dispatcher.CreateTimer();
        if (_timer is not null)
        {
            _timer.Interval = TimeSpan.FromMilliseconds(100);
            _timer.Tick -= OnTick;
            _timer.Tick += OnTick;
            _timer.Start();
        }
    }

    private void OnTick(object? sender, EventArgs e)
    {
        if (_recorder is not { Recording: true } recorder) return;
        recorder.UpdateMeters();
        // Peak power is in decibels (-160 to 0); convert to a 0 to 1 loudness.
        var level = Math.Pow(10, recorder.PeakPower(0) / 20.0);
        Progress?.Invoke(this, new RecordingProgress(TimeSpan.FromSeconds(recorder.CurrentTime), level));
    }

    public void Stop()
    {
        IsRecording = false;
        _timer?.Stop();
        if (_recorder is not null)
        {
            _recorder.Stop();
            _recorder.Dispose();
            _recorder = null;
        }
    }
}
