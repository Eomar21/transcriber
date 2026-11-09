namespace Transcriber.Coach.Models;

/// <summary>How many times one filler word or phrase was said.</summary>
public sealed record FillerCount(string Filler, int Count);
