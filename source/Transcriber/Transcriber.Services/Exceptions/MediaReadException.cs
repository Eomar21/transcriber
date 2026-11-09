using Transcriber.Services.Models;

namespace Transcriber.Services.Exceptions;

/// <summary>A file could not be turned into audio. The message is written for the person who chose the file.</summary>
public sealed class MediaReadException(MediaReadFailure reason, string message) : Exception(message)
{
    public MediaReadFailure Reason { get; } = reason;
}
