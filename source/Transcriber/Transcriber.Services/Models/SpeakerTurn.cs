namespace Transcriber.Services.Models;

/// <summary>One stretch of the recording where a single speaker talks (speaker numbers start at 0).</summary>
internal readonly record struct SpeakerTurn(TimeSpan Start, TimeSpan End, int Speaker);
