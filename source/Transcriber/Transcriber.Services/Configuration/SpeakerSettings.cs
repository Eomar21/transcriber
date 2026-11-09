namespace Transcriber.Services.Configuration;

/// <summary>Speaker detection: the model files, the tool for the Mac, and how voices are grouped.</summary>
internal sealed class SpeakerSettings
{
    public string SegmentationModelFile { get; set; } = "";

    public string EmbeddingModelFile { get; set; } = "";

    /// <summary>The program that the Mac build starts, because a Mac app cannot load the native library.</summary>
    public string ToolFileName { get; set; } = "";

    /// <summary>Clustering threshold when the number of speakers is unknown. Lower finds more speakers.</summary>
    public float ClusterThreshold { get; set; }

    public float MinDurationOn { get; set; }

    public float MinDurationOff { get; set; }

    /// <summary>A speaker with less talk time than this share of all speech (and under <see cref="MinorSpeakerMaxSeconds"/>) is treated as noise.</summary>
    public double MinorSpeakerShare { get; set; }

    public double MinorSpeakerMaxSeconds { get; set; }
}
