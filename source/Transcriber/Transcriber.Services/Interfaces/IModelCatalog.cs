using Transcriber.Services.Models;

namespace Transcriber.Services.Interfaces;

/// <summary>The Whisper models that the app knows. The list and the roles come from the app settings.</summary>
public interface IModelCatalog
{
    IReadOnlyList<WhisperModel> All { get; }

    /// <summary>The model that is selected when the app starts.</summary>
    WhisperModel Default { get; }

    /// <summary>The model to suggest when a language is less accurate with the chosen one.</summary>
    WhisperModel Suggested { get; }

    /// <summary>The model that the Practice tab uses.</summary>
    WhisperModel Practice { get; }

    /// <returns>The model with this id, or null when the app does not know it.</returns>
    WhisperModel? Find(string id);
}
