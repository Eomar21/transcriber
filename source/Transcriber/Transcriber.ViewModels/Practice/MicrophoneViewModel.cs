using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Transcriber.Coach.Exceptions;

namespace Transcriber.ViewModels.Practice;

/// <summary>Records the person with the microphone. It makes a file and tells the others. It does not know what happens to the file.</summary>
public sealed partial class MicrophoneViewModel : PracticeSectionViewModel
{
    /// <summary>A recording stops by itself after this long, so a forgotten recording cannot fill the disk.</summary>
    public static readonly TimeSpan MaxRecording = TimeSpan.FromMinutes(30);

    /// <summary>Anything shorter than this is treated as an accidental click.</summary>
    public static readonly TimeSpan MinRecording = TimeSpan.FromSeconds(1);

    private const string RecordingPrefix = "transcriber_practice_";
    private static readonly TimeSpan SilenceCheckAfter = TimeSpan.FromSeconds(3);
    private const double SilentLevel = 0.001;

    private readonly IAudioRecorder _recorder;
    private readonly Func<DateTimeOffset> _now;
    private readonly string _recordingsDirectory;
    private string? _recordingPath;
    private double _loudestLevel;

    public MicrophoneViewModel(IAudioRecorder recorder, PracticeState state, Func<DateTimeOffset>? now = null, string? recordingsDirectory = null) : base(state)
    {
        _recorder = recorder;
        _now = now ?? (() => DateTimeOffset.Now);
        _recordingsDirectory = recordingsDirectory ?? Path.GetTempPath();
        _recorder.Progress += OnRecordingProgress;
        DeleteOldRecordings();
    }

    /// <summary>A new recording is about to start, so a message about the last one is out of date.</summary>
    public event EventHandler? Starting;

    /// <summary>A recording is finished and long enough. The arguments are the file and a name to show.</summary>
    public event EventHandler<RecordedEventArgs>? Recorded;

    /// <summary>Something went wrong, or the recording ended early. The text is for the person.</summary>
    public event EventHandler<string>? Problem;

    [ObservableProperty] private string _recordingText = "";

    /// <summary>Loudness from 0 to 1, so it is obvious the microphone is picking something up.</summary>
    [ObservableProperty] private double _recordingLevel;

    /// <summary>
    /// True when nothing at all has come in for a few seconds. A blocked microphone usually records pure silence
    /// instead of failing, so this is the only sign the user gets.
    /// </summary>
    [ObservableProperty] private bool _microphoneSilent;

    private bool CanStartRecording() => !IsBusy && !IsRecording;

    [RelayCommand(CanExecute = nameof(CanStartRecording))]
    private async Task StartRecordingAsync()
    {
        Starting?.Invoke(this, EventArgs.Empty);
        var path = Path.Combine(_recordingsDirectory, $"{RecordingPrefix}{Guid.NewGuid():N}.wav");
        try
        {
            await _recorder.StartAsync(path);
        }
        catch (RecordingException e)
        {
            DeleteQuietly(path);
            Problem?.Invoke(this, e.Message);
            return;
        }

        _recordingPath = path;
        _loudestLevel = 0;
        MicrophoneSilent = false;
        RecordingLevel = 0;
        RecordingText = "Recording 0:00";
        State.IsRecording = true;
    }

    private bool CanStopRecording() => IsRecording;

    [RelayCommand(CanExecute = nameof(CanStopRecording))]
    private void StopRecording()
    {
        var path = _recordingPath;
        _recordingPath = null;
        _recorder.Stop();
        State.IsRecording = false;
        MicrophoneSilent = false;
        RecordingLevel = 0;
        if (path is null) return;

        var length = TryGetLength(path);
        // 16 kHz mono 16-bit is 32,000 bytes per second, plus a 44 byte header.
        if (length < 44 + (long)(MinRecording.TotalSeconds * 32_000))
        {
            DeleteQuietly(path);
            Problem?.Invoke(this, "That recording was too short. Hold on a little longer and try again.");
            return;
        }

        Recorded?.Invoke(this, new RecordedEventArgs(path, $"Recording {_now().LocalDateTime.ToString("d MMM HH:mm", CultureInfo.InvariantCulture)}"));
    }

    protected override void OnStateChanged()
    {
        StartRecordingCommand.NotifyCanExecuteChanged();
        StopRecordingCommand.NotifyCanExecuteChanged();
    }

    private void OnRecordingProgress(object? sender, RecordingProgress p)
    {
        if (!IsRecording) return;
        _loudestLevel = Math.Max(_loudestLevel, p.Level);
        MicrophoneSilent = p.Elapsed >= SilenceCheckAfter && _loudestLevel < SilentLevel;
        RecordingLevel = Math.Clamp(p.Level, 0, 1);
        RecordingText = $"Recording {(int)p.Elapsed.TotalMinutes}:{p.Elapsed.Seconds:00}";
        if (p.Elapsed >= MaxRecording)
        {
            StopRecording();
            Problem?.Invoke(this, $"Stopped after {(int)MaxRecording.TotalMinutes} minutes, the longest a single recording can be.");
        }
    }

    // Recordings the app made in an earlier session and never cleaned up (for example after a crash).
    private void DeleteOldRecordings()
    {
        try
        {
            foreach (var file in Directory.EnumerateFiles(_recordingsDirectory, RecordingPrefix + "*.wav"))
                if (File.GetLastWriteTimeUtc(file) < DateTime.UtcNow.AddDays(-1)) DeleteQuietly(file);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { }
    }

    private static long TryGetLength(string path)
    {
        try { return File.Exists(path) ? new FileInfo(path).Length : 0; }
        catch (IOException) { return 0; }
    }

    public static void DeleteQuietly(string path)
    {
        try { File.Delete(path); } catch (Exception e) when (e is IOException or UnauthorizedAccessException) { }
    }
}

/// <summary>A finished recording: where the file is and the name to show for it.</summary>
public sealed class RecordedEventArgs(string path, string displayName) : EventArgs
{
    public string Path { get; } = path;

    public string DisplayName { get; } = displayName;
}
