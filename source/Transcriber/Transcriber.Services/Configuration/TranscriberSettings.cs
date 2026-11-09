using Transcriber.Services.Exceptions;

namespace Transcriber.Services.Configuration;

/// <summary>All the settings of the services. The values are in appsettings.json, which is built into the app.</summary>
internal sealed class TranscriberSettings
{
    public AudioSettings Audio { get; set; } = new();

    public MediaSettings Media { get; set; } = new();

    public ModelSettings Models { get; set; } = new();

    public LanguageSettings Languages { get; set; } = new();

    public TranscriptionSettings Transcription { get; set; } = new();

    public SpeechPlanningSettings SpeechPlanning { get; set; } = new();

    public SpeechCoverageSettings SpeechCoverage { get; set; } = new();

    public SpeakerSettings Speakers { get; set; } = new();

    /// <summary>Stops the app at start with a clear message instead of failing later with a confusing one.</summary>
    /// <exception cref="SettingsException">A setting is missing or cannot work.</exception>
    public void Validate()
    {
        Require(Audio.SampleRate > 0, "Audio:SampleRate must be more than 0.");
        Require(Audio.Channels > 0, "Audio:Channels must be more than 0.");

        var extensions = Media.NativeExtensions.Concat(Media.FfmpegAudioExtensions).Concat(Media.FfmpegVideoExtensions).ToList();
        Require(extensions.Count > 0, "Media: no file extension is listed.");
        foreach (var e in extensions)
            Require(e.Length > 1 && e[0] == '.' && e == e.ToLowerInvariant(), $"Media: '{e}' must be a lower-case extension that starts with a dot.");
        Require(extensions.Distinct().Count() == extensions.Count, "Media: an extension is listed twice.");
        Require(Media.Ffmpeg.Windows.Count > 0 && Media.Ffmpeg.MacArm64.Count > 0 && Media.Ffmpeg.MacIntel.Count > 0,
            "Media:Ffmpeg needs file names for Windows and macOS.");

        Require(Models.Catalog.Count > 0, "Models:Catalog is empty.");
        Require(Models.Catalog.Select(m => m.Id).Distinct().Count() == Models.Catalog.Count, "Models:Catalog has two models with the same id.");
        Require(Models.Catalog.All(m => m.Id.Length > 0 && m.DisplayName.Length > 0 && m.ApproxBytes > 0),
            "Models:Catalog: each model needs an id, a name and a size.");
        foreach (var (name, id) in new[]
        {
            ("DefaultModelId", Models.DefaultModelId), ("SuggestedModelId", Models.SuggestedModelId), ("PracticeModelId", Models.PracticeModelId),
        })
            Require(Models.Catalog.Any(m => m.Id == id), $"Models:{name} '{id}' is not in Models:Catalog.");
        Require(Models.SpeechDetectionModelFile.Length > 0, "Models:SpeechDetectionModelFile is empty.");

        Require(Languages.Catalog.Count > 0, "Languages:Catalog is empty.");
        Require(Languages.Catalog.Select(l => l.Code).Distinct().Count() == Languages.Catalog.Count,
            "Languages:Catalog has two languages with the same code.");

        Require(Transcription.NoSpeechThreshold is > 0 and < 1, "Transcription:NoSpeechThreshold must be between 0 and 1.");

        Require(SpeechPlanning.PaddingSeconds >= 0 && SpeechPlanning.MergeGapSeconds >= 0 && SpeechPlanning.MaxChunkSeconds > 0,
            "SpeechPlanning: the times must be 0 or more, and MaxChunkSeconds more than 0.");
        Require(SpeechCoverage.MinMissingSeconds >= 0 && SpeechCoverage.MinShare is > 0 and <= 1 && SpeechCoverage.BridgeSeconds >= 0,
            "SpeechCoverage: a value is out of range.");

        Require(Speakers.SegmentationModelFile.Length > 0 && Speakers.EmbeddingModelFile.Length > 0 && Speakers.ToolFileName.Length > 0,
            "Speakers: a model or tool file name is empty.");
        Require(Speakers.ClusterThreshold > 0 && Speakers.MinorSpeakerShare is > 0 and < 1 && Speakers.MinorSpeakerMaxSeconds > 0,
            "Speakers: a value is out of range.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new SettingsException(message);
    }
}
