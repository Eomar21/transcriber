using Transcriber.Coach.Interfaces;
using Transcriber.Coach.Models;

namespace Transcriber.Coach.Analysis;

/// <summary>
/// Builds a <see cref="SpeechReport"/> from a transcript. It only combines the work of the detectors, so a new
/// measurement or a new language does not change it.
/// </summary>
public sealed class SpeechAnalyzer(
    ITextTokenizer tokenizer,
    IFillerDetector fillers,
    IRepeatDetector repeats,
    IPauseDetector pauses,
    ILanguageProfileProvider profiles) : ISpeechAnalyzer
{
    public SpeechReport Analyze(IReadOnlyList<TranscriptSegment> segments, string? language = null, TimeSpan? pauseThreshold = null)
    {
        var lines = segments.Where(s => !string.IsNullOrWhiteSpace(s.Text)).OrderBy(s => s.Start).ToList();
        if (lines.Count == 0) return new SpeechReport();

        var profile = profiles.For(language);
        var words = 0;
        var speaking = TimeSpan.Zero;
        var stumbles = new List<RepeatInfo>();
        var fillerCounts = new Dictionary<string, int>();

        foreach (var line in lines)
        {
            var tokens = tokenizer.Tokenize(line.Text);
            words += tokens.Count;
            speaking += line.End > line.Start ? line.End - line.Start : TimeSpan.Zero;

            stumbles.AddRange(repeats.Find(line.Text, tokens, profile).Select(text => new RepeatInfo(line.Start, text)));
            foreach (var (filler, count) in fillers.Count(line.Text, tokens, profile))
                fillerCounts[filler] = fillerCounts.GetValueOrDefault(filler) + count;
        }

        return new SpeechReport
        {
            WordCount = words,
            Duration = lines[^1].End - lines[0].Start,
            SpeakingTime = speaking,
            WordsPerMinute = speaking.TotalMinutes > 0 ? words / speaking.TotalMinutes : 0,
            Pauses = pauses.Find(lines, pauseThreshold),
            Fillers = fillerCounts.OrderByDescending(kv => kv.Value).ThenBy(kv => kv.Key)
                .Select(kv => new FillerCount(kv.Key, kv.Value)).ToList(),
            Repeats = stumbles,
        };
    }
}
