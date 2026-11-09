namespace Transcriber.Services.Models;

/// <summary>One line of a transcript.</summary>
public class TranscriptSegment
{
    public TimeSpan Start { get; set; }
    public TimeSpan End { get; set; }
    public string Text { get; set; } = string.Empty;

    /// <summary>Who is speaking (0-based), or null when speakers were not identified.</summary>
    public int? Speaker { get; set; }
}
