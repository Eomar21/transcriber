using NAudio.Wave;
using Transcriber.ViewModels;

namespace Transcriber.Maui.Services;

// The Windows recorder, built on NAudio (WinMM). What both platforms share is in Services/MauiAudioRecorder.cs.
public sealed partial class MauiAudioRecorder
{
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
}
