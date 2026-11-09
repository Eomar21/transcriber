using Transcriber.Coach.Exceptions;
using Transcriber.Coach.Models;

namespace Transcriber.Coach.Interfaces;

/// <summary>Written feedback from a model, kept behind an interface so the app and its tests do not depend on Ollama itself.</summary>
public interface ICoachFeedback
{
    /// <summary>The models that can give feedback; empty when the service is not running.</summary>
    Task<IReadOnlyList<string>> ListModelsAsync(CancellationToken ct = default);

    /// <exception cref="OllamaException">The service could not be reached or gave no answer.</exception>
    Task<string> GetFeedbackAsync(string model, string transcript, SpeechReport report, CancellationToken ct = default);
}
