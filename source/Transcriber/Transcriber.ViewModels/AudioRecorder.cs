namespace Transcriber.ViewModels;

/// <summary>How far a recording has got, reported a few times a second while it runs.</summary>
/// <param name="Elapsed">Time recorded so far.</param>
/// <param name="Level">Current loudness from 0 (silent) to 1 (as loud as it gets), for a level meter.</param>
public readonly record struct RecordingProgress(TimeSpan Elapsed, double Level);

/// <summary>The microphone cannot be used: there is none, or the user has blocked access to it.</summary>
public sealed class RecordingException(string message) : Exception(message);

/// <summary>
/// Records the microphone to a 16 kHz mono 16-bit wav file, the format transcription takes directly.
/// Implemented per platform and faked in tests.
/// </summary>
public interface IAudioRecorder
{
    bool IsRecording { get; }

    /// <summary>Raised on the UI thread while recording.</summary>
    event EventHandler<RecordingProgress>? Progress;

    /// <exception cref="RecordingException">There is no microphone, or access to it is blocked.</exception>
    Task StartAsync(string wavPath);

    /// <summary>Finishes the file. Safe to call when nothing is recording.</summary>
    void Stop();
}
