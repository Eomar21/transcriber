namespace Transcriber.Services.Models;

/// <summary>Where a run is: a short description of the stage and a share of the work (0 to 1).</summary>
public sealed record TranscriptionProgress(string Stage, double Fraction);
