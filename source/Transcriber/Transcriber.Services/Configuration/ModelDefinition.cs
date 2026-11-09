using Transcriber.Services.Models;
using Whisper.net.Ggml;

namespace Transcriber.Services.Configuration;

/// <summary>One entry of the model list in the app settings.</summary>
internal sealed class ModelDefinition
{
    public string Id { get; set; } = "";

    public string DisplayName { get; set; } = "";

    public GgmlType Type { get; set; }

    public QuantizationType Quantization { get; set; }

    public long ApproxBytes { get; set; }

    public ModelTier Tier { get; set; }

    public WhisperModel ToModel() => new(Id, DisplayName, Type, Quantization, ApproxBytes, Tier);
}
