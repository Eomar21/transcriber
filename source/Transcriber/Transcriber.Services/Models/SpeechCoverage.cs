namespace Transcriber.Services.Models;

/// <summary>
/// How much of the speech that voice detection found ended up as text. A transcript that misses a large part of the speech
/// is a sign that something went wrong, for example Whisper stuck to one language in a recording that mixes two.
/// </summary>
/// <param name="SpeechSeconds">Seconds of speech found in the recording.</param>
/// <param name="TextSeconds">Seconds of that speech that are covered by transcript lines.</param>
public readonly record struct SpeechCoverage(double SpeechSeconds, double TextSeconds)
{
    public double MissingSeconds => Math.Max(0, SpeechSeconds - TextSeconds);

    public double Share => SpeechSeconds <= 0 ? 1 : Math.Min(1, TextSeconds / SpeechSeconds);

    /// <summary>True when a clear part of the speech has no text. The calculator decides this with the limits from the app settings.</summary>
    public bool LooksIncomplete { get; init; }
}
