using Microsoft.Extensions.Options;
using Transcriber.Services.Configuration;
using Transcriber.Services.Interfaces;
using Transcriber.Services.Models;

namespace Transcriber.Services.Transcription;

/// <summary>
/// Turns the raw speech regions found by voice activity detection into the chunks that get transcribed:
/// pads each region, merges regions separated by a short pause (so a sentence is never cut in two),
/// and splits anything longer than Whisper handles comfortably. The times are in the app settings.
/// </summary>
internal sealed class SpeechPlanner(IOptions<SpeechPlanningSettings> options) : ISpeechPlanner
{
    private readonly SpeechPlanningSettings _settings = options.Value;

    public List<SpeechSpan> Plan(IEnumerable<SpeechSpan> detected, TimeSpan total, TimeSpan? padding = null, TimeSpan? mergeGap = null, TimeSpan? maxChunk = null)
    {
        var pad = padding ?? TimeSpan.FromSeconds(_settings.PaddingSeconds);
        var gap = mergeGap ?? TimeSpan.FromSeconds(_settings.MergeGapSeconds);
        var max = maxChunk ?? TimeSpan.FromSeconds(_settings.MaxChunkSeconds);

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
