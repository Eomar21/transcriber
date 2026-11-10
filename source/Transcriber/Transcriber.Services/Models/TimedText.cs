namespace Transcriber.Services.Models;

/// <summary>A text and its times, counted from the start of a chunk. It is a line of Whisper, or one small part of a line.</summary>
internal readonly record struct TimedText(string Text, TimeSpan Start, TimeSpan End);
