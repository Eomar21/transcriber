namespace Transcriber.Coach.Models;

/// <summary>One saved practice run.</summary>
public sealed record PracticeSession(Guid Id, DateTimeOffset When, string Title, string Language, SpeechReport Report);
