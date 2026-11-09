namespace Transcriber.Services.Configuration;

internal sealed class ModelSettings
{
    /// <summary>Where downloaded models are kept. Empty means the app data folder.</summary>
    public string? Directory { get; set; }

    /// <summary>Where the models that are built into the app are. Empty means the models folder next to the app.</summary>
    public string? BundledDirectory { get; set; }

    public string DefaultModelId { get; set; } = "";

    /// <summary>The model to suggest when a language is less accurate with the chosen one.</summary>
    public string SuggestedModelId { get; set; } = "";

    /// <summary>The model that the Practice tab always uses.</summary>
    public string PracticeModelId { get; set; } = "";

    public string SpeechDetectionModelFile { get; set; } = "";

    public List<ModelDefinition> Catalog { get; set; } = [];
}
