using Transcriber.Services.Models;

namespace Transcriber.Services.Interfaces;

/// <summary>Measures how much of the speech in a recording became text.</summary>
internal interface ISpeechCoverageCalculator
{
    SpeechCoverage Measure(IReadOnlyList<SpeechSpan> spans, IEnumerable<TranscriptSegment> segments, TimeSpan? bridge = null);
}
