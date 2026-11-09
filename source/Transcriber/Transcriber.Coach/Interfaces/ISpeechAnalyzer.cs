using Transcriber.Coach.Models;

namespace Transcriber.Coach.Interfaces;

/// <summary>Measures pace, pauses, filler words and stumbles in a transcript. The same input always gives the same report.</summary>
public interface ISpeechAnalyzer
{
    /// <param name="segments">The transcript lines, in any order. Empty lines are ignored.</param>
    /// <param name="language">The language of the speech. An unknown language uses the default profile.</param>
    /// <param name="pauseThreshold">A gap between lines at least this long counts as a pause. Null uses the default.</param>
    SpeechReport Analyze(IReadOnlyList<TranscriptSegment> segments, string? language = null, TimeSpan? pauseThreshold = null);
}
