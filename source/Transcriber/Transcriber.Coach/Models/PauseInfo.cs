namespace Transcriber.Coach.Models;

/// <summary>A silence between two lines of speech.</summary>
public sealed record PauseInfo(TimeSpan Start, TimeSpan Length);
