/// <summary>A stretch of the recording that contains speech and is worth sending to Whisper.</summary>
public readonly record struct SpeechSpan(TimeSpan Start, TimeSpan End)
{
    public TimeSpan Length => End - Start;
}

/// <summary>
/// Turns the raw speech regions found by voice activity detection into the chunks that get transcribed:
/// pads each region, merges regions separated by a short pause (so a sentence is never cut in two),
/// and splits anything longer than Whisper handles comfortably.
/// </summary>
public static class SpeechPlanner
{
    public static readonly TimeSpan DefaultPadding = TimeSpan.FromMilliseconds(300);
    public static readonly TimeSpan DefaultMergeGap = TimeSpan.FromSeconds(1.5);
    public static readonly TimeSpan DefaultMaxChunk = TimeSpan.FromMinutes(5);

    public static List<SpeechSpan> Plan(
        IEnumerable<SpeechSpan> detected,
        TimeSpan total,
        TimeSpan? padding = null,
        TimeSpan? mergeGap = null,
        TimeSpan? maxChunk = null)
    {
        var pad = padding ?? DefaultPadding;
        var gap = mergeGap ?? DefaultMergeGap;
        var max = maxChunk ?? DefaultMaxChunk;

        var padded = detected
            .Where(s => s.End > s.Start)
            .Select(s => new SpeechSpan(Max(TimeSpan.Zero, s.Start - pad), Min(total, s.End + pad)))
            .OrderBy(s => s.Start)
            .ToList();

        var merged = new List<SpeechSpan>();
        foreach (var span in padded)
        {
            if (merged.Count > 0 && span.Start - merged[^1].End <= gap)
                merged[^1] = merged[^1] with { End = Max(merged[^1].End, span.End) };
            else
                merged.Add(span);
        }

        var result = new List<SpeechSpan>();
        foreach (var span in merged)
        {
            var start = span.Start;
            while (span.End - start > max)
            {
                result.Add(new SpeechSpan(start, start + max));
                start += max;
            }
            result.Add(new SpeechSpan(start, span.End));
        }
        return result;
    }

    private static TimeSpan Max(TimeSpan a, TimeSpan b) => a > b ? a : b;
    private static TimeSpan Min(TimeSpan a, TimeSpan b) => a < b ? a : b;
}
