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

    /// <summary>
    /// Short pieces that follow each other go to Whisper together, until they are this long together. Whisper works on
    /// a window of 30 seconds, so a larger value has no use. 0 sends each piece alone.
    /// </summary>
    public double PackSeconds { get; set; }

    /// <summary>The silence that is put between two pieces that go to Whisper together.</summary>
    public double JoinSilenceSeconds { get; set; }
}
