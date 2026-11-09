namespace Transcriber.Coach.Feedback;

/// <summary>Where the local Ollama service listens. The transcript is sent only to this address.</summary>
public sealed record OllamaOptions
{
    public const string DefaultAddress = "http://localhost:11434";

    public string BaseAddress { get; init; } = DefaultAddress;

    /// <summary>A large model on a slow computer can take minutes to answer.</summary>
    public TimeSpan Timeout { get; init; } = TimeSpan.FromMinutes(3);
}
