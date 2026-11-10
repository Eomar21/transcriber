namespace Transcriber.Services.Configuration;

internal sealed class TranscriptionSettings
{
    /// <summary>Segments whose no-speech probability is above this are treated as silence.</summary>
    public float NoSpeechThreshold { get; set; }

    /// <summary>
    /// How many minutes the model of the last run stays in memory when it is not used. The next run with the same
    /// model then starts without a load. Zero releases the model at the end of each file.
    /// </summary>
    public double KeepModelLoadedMinutes { get; set; }

    /// <summary>How many threads Whisper uses on the processor. Zero lets the app choose from the number of processors.</summary>
    public int Threads { get; set; }

    /// <summary>
    /// Stock phrases that Whisper invents over silence. A line is removed only when the whole line is one of these,
    /// so real speech that contains the words is kept.
    /// </summary>
    public List<string> Hallucinations { get; set; } = [];
}
