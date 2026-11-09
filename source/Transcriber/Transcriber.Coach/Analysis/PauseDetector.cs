using Transcriber.Services.Models;
using Transcriber.Coach.Interfaces;
using Transcriber.Coach.Models;

namespace Transcriber.Coach.Analysis;

/// <summary>A gap between two lines of speech that is long enough to notice.</summary>
public sealed class PauseDetector : IPauseDetector
{
    public TimeSpan DefaultThreshold { get; } = TimeSpan.FromSeconds(0.8);

    public IReadOnlyList<PauseInfo> Find(IReadOnlyList<TranscriptSegment> orderedLines, TimeSpan? threshold = null)
    {
        var limit = threshold ?? DefaultThreshold;
        var pauses = new List<PauseInfo>();
        for (var i = 1; i < orderedLines.Count; i++)
        {
            var gap = orderedLines[i].Start - orderedLines[i - 1].End;
            if (gap >= limit) pauses.Add(new PauseInfo(orderedLines[i - 1].End, gap));
        }
        return pauses;
    }
}
