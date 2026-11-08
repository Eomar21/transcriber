#if WINDOWS
using NAudio.Wave;
#elif MACCATALYST
using AudioToolbox;
using AVFoundation;
using Foundation;
#endif
using Transcriber.ViewModels;

namespace Transcriber.Maui.Services;

/// <summary>
/// Records the microphone straight to a 16 kHz mono 16-bit wav, the format Whisper takes, so no conversion is needed
/// afterwards. Windows uses NAudio (WinMM); macOS uses AVFoundation.
/// </summary>
public sealed class MauiAudioRecorder : IAudioRecorder
{
    public const int SampleRate = 16000;

    public event EventHandler<RecordingProgress>? Progress;

    public bool IsRecording { get; private set; }

    // Progress comes from audio threads; the view model expects it on the UI thread.
    private void Report(TimeSpan elapsed, double level) =>
        MainThread.BeginInvokeOnMainThread(() => Progress?.Invoke(this, new RecordingProgress(elapsed, level)));

#if WINDOWS
    private WaveInEvent? _input;
    private WaveFileWriter? _writer;
    private readonly object _gate = new();

    public Task StartAsync(string wavPath)
    {
        Stop();

        if (WaveInEvent.DeviceCount == 0)
            throw new RecordingException("No microphone was found. Connect one and try again.");

        var format = new WaveFormat(SampleRate, 16, 1);
        try
        {
            _writer = new WaveFileWriter(wavPath, format);
            _input = new WaveInEvent { WaveFormat = format, BufferMilliseconds = 100 };
            _input.DataAvailable += OnData;
            _input.StartRecording();
        }
        catch (Exception e) when (e is NAudio.MmException or InvalidOperationException or IOException or UnauthorizedAccessException)
        {
            Stop();
            throw new RecordingException(
                "The microphone could not be opened. Check that it is connected and not in use by another app, and that Windows allows " +
                "desktop apps to use it (Settings > Privacy & security > Microphone).");
        }

        IsRecording = true;
        return Task.CompletedTask;
    }

    private void OnData(object? sender, WaveInEventArgs e)
    {
        TimeSpan elapsed;
        lock (_gate)
        {
            if (_writer is null) return;
            _writer.Write(e.Buffer, 0, e.BytesRecorded);
            elapsed = TimeSpan.FromSeconds((double)_writer.Length / _writer.WaveFormat.AverageBytesPerSecond);
        }

        var peak = 0;
        for (var i = 0; i + 1 < e.BytesRecorded; i += 2)
            peak = Math.Max(peak, Math.Abs((int)BitConverter.ToInt16(e.Buffer, i)));
        Report(elapsed, peak / 32768.0);
    }

    public void Stop()
    {
        IsRecording = false;
        var input = _input;
        _input = null;
        if (input is not null)
        {
            input.DataAvailable -= OnData;
            try { input.StopRecording(); } catch (Exception e) when (e is NAudio.MmException or InvalidOperationException) { }
            input.Dispose();
        }

        lock (_gate)
        {
            _writer?.Dispose(); // writes the final wav header
            _writer = null;
        }
    }
#elif MACCATALYST
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
#else
    public Task StartAsync(string wavPath) =>
        throw new RecordingException("Recording is not available on this platform.");

    public void Stop() => IsRecording = false;
#endif
}
