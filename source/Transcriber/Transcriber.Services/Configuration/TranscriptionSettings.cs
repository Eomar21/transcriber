namespace Transcriber.Services.Configuration;

internal sealed class TranscriptionSettings
{
    /// <summary>Segments whose no-speech probability is above this are treated as silence.</summary>
    public float NoSpeechThreshold { get; set; }

    /// <summary>
    /// Stock phrases that Whisper invents over silence. A line is removed only when the whole line is one of these,
    /// so real speech that contains the words is kept.
    /// </summary>
    public List<string> Hallucinations { get; set; } = [];
}
