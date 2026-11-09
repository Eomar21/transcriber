namespace Transcriber.Services.Configuration;

/// <summary>When a transcript is called incomplete.</summary>
internal sealed class SpeechCoverageSettings
{
    /// <summary>At least this many seconds must be missing before the transcript is called incomplete.</summary>
    public double MinMissingSeconds { get; set; }

    /// <summary>The share of speech that must be covered by text, or the transcript is called incomplete.</summary>
    public double MinShare { get; set; }

    /// <summary>A line counts for this many seconds on each side of it, so the pause between two sentences is not missing speech.</summary>
    public double BridgeSeconds { get; set; }
}
