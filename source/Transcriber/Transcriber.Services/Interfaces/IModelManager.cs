using Transcriber.Services.Models;

namespace Transcriber.Services.Interfaces;

/// <summary>Finds the Whisper models on this computer and downloads the ones that are missing.</summary>
public interface IModelManager
{
    string ModelsDirectory { get; }

    /// <summary>True if the model is built into the app.</summary>
    bool IsBundled(WhisperModel model);

    bool IsDownloaded(WhisperModel model);

    /// <returns>The path of the model file. A model that is not on this computer is downloaded first.</returns>
    Task<string> EnsureModelAsync(WhisperModel model, IProgress<TranscriptionProgress>? progress = null, CancellationToken ct = default);
}
