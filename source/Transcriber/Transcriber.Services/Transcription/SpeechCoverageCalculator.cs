using Microsoft.Extensions.Options;
using Transcriber.Services.Configuration;
using Transcriber.Services.Interfaces;
using Transcriber.Services.Models;

namespace Transcriber.Services.Transcription;

/// <summary>
/// Compares transcript lines with the speech spans. A line counts for a short time on each side of it as well, so the
/// normal pause between two sentences is not mistaken for missing speech. The limits are in the app settings.
/// </summary>
internal sealed class SpeechCoverageCalculator(IOptions<SpeechCoverageSettings> options) : ISpeechCoverageCalculator
{
    private readonly SpeechCoverageSettings _settings = options.Value;

    public SpeechCoverage Measure(IReadOnlyList<SpeechSpan> spans, IEnumerable<TranscriptSegment> segments, TimeSpan? bridge = null)
    {
        var reach = bridge ?? TimeSpan.FromSeconds(_settings.BridgeSeconds);
        var lines = segments.Select(s => (Start: s.Start - reach, End: s.End + reach)).OrderBy(l => l.Start).ToList();

        double speech = 0, text = 0;
        foreach (var span in spans)
        {
            speech += span.Length.TotalSeconds;

            var covered = TimeSpan.Zero;
            var cursor = span.Start;
            foreach (var line in lines)
            {
                var from = line.Start < cursor ? cursor : line.Start;
                var to = line.End > span.End ? span.End : line.End;
                if (to <= from) continue;
                covered += to - from;
                cursor = to;
            }
            text += covered.TotalSeconds;
        }

        var coverage = new SpeechCoverage(speech, text);
        return coverage with { LooksIncomplete = coverage.MissingSeconds >= _settings.MinMissingSeconds && coverage.Share < _settings.MinShare };
    }
}
