using Microsoft.Extensions.Options;
using Transcriber.Services.Configuration;
using Transcriber.Services.Interfaces;
using Transcriber.Services.Models;
using Transcriber.Services.Text;

namespace Transcriber.Services.Transcription;

/// <summary>
/// Cleans typical Whisper artefacts: empty or placeholder segments, stock phrases it invents over silence,
/// and the repetition loops where the same line is emitted over and over. The stock phrases are in the app settings.
/// </summary>
internal sealed class TranscriptFilter : ITranscriptFilter
{
    // Whole-segment matches only (after normalisation), so real speech containing these words is kept.
    private readonly HashSet<string> _stockHallucinations;

    public TranscriptFilter(IOptions<TranscriptionSettings> options)
    {
        _stockHallucinations = options.Value.Hallucinations.Select(TextMetrics.Normalize).ToHashSet(StringComparer.Ordinal);
    }

    public List<TranscriptSegment> Clean(IEnumerable<TranscriptSegment> segments)
    {
        var result = new List<TranscriptSegment>();
        string? previousKey = null;

        foreach (var seg in segments)
        {
            var text = seg.Text.Trim();
            var key = TextMetrics.Normalize(text);
            if (key.Length == 0 || _stockHallucinations.Contains(key)) continue;

            // Same line again right after itself: extend the previous segment instead of repeating.
            if (key == previousKey && result.Count > 0)
            {
                result[^1].End = seg.End;
                continue;
            }

            result.Add(new TranscriptSegment { Start = seg.Start, End = seg.End, Text = text });
            previousKey = key;
        }
        return result;
    }
}
