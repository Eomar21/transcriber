namespace Transcriber.Services.Configuration;

/// <summary>How the speech that is found in a recording is cut into pieces for Whisper.</summary>
internal sealed class SpeechPlanningSettings
{
    /// <summary>Extra time kept before and after each stretch of speech.</summary>
    public double PaddingSeconds { get; set; }

    /// <summary>Stretches that are closer together than this are joined, so a sentence is never cut in two.</summary>
    public double MergeGapSeconds { get; set; }

    /// <summary>A piece is never longer than this, because Whisper works best on shorter audio.</summary>
    public double MaxChunkSeconds { get; set; }
}
