namespace Transcriber.Coach.Exceptions;

/// <summary>The local Ollama service could not be reached, or it gave no usable answer.</summary>
public sealed class OllamaException(string message) : Exception(message);
