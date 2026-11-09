namespace Transcriber.Services.Exceptions;

/// <summary>Speaker detection could not run or failed.</summary>
public sealed class SpeakerDetectionException(string message) : Exception(message);
