namespace Transcriber.Services.Models;

/// <summary>A stretch of the recording that contains speech.</summary>
internal readonly record struct SpeechSpan(TimeSpan Start, TimeSpan End)
{
    public TimeSpan Length => End - Start;
}
