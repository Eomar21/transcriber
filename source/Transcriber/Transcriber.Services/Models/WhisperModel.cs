using Whisper.net.Ggml;

namespace Transcriber.Services.Models;

/// <summary>A Whisper model that can be used for transcription. The list of models comes from the app settings.</summary>
/// <param name="Id">The model id. It is also the file stem: ggml-{Id}.bin.</param>
/// <param name="Tier">How accurate the model is, which decides the languages that it offers.</param>
public sealed record WhisperModel(string Id, string DisplayName, GgmlType Type, QuantizationType Quantization, long ApproxBytes, ModelTier Tier)
{
    public string FileName => $"ggml-{Id}.bin";

    // Pickers and lists show the friendly name without needing a display binding.
    public override string ToString() => DisplayName;
}
