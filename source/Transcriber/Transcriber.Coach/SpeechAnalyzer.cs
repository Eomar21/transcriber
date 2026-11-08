using System.Text.RegularExpressions;

namespace Transcriber.Coach;

/// <summary>Measures pace, pauses, filler words and stumbles in a transcript. Pure: the same input always gives the same report.</summary>
public static partial class SpeechAnalyzer
{
    /// <summary>A gap between lines at least this long counts as a pause.</summary>
    public static readonly TimeSpan DefaultPauseThreshold = TimeSpan.FromSeconds(0.8);

    // Words that are fine to say twice for emphasis.
    private static readonly HashSet<string> EmphasisRepeats = ["very", "really", "so", "no", "bye", "many", "yes", "well", "ja", "nee", "muy"];

    public static SpeechReport Analyze(IReadOnlyList<TranscriptSegment> segments, string? language = null, TimeSpan? pauseThreshold = null)
    {
        var lines = segments.Where(s => !string.IsNullOrWhiteSpace(s.Text)).OrderBy(s => s.Start).ToList();
        if (lines.Count == 0) return new SpeechReport();

        var threshold = pauseThreshold ?? DefaultPauseThreshold;

        var words = 0;
        var speaking = TimeSpan.Zero;
        var pauses = new List<PauseInfo>();
        var repeats = new List<RepeatInfo>();
        var fillerCounts = new Dictionary<string, int>();
        var lexicon = FillerLexicon.For(language);

        for (var i = 0; i < lines.Count; i++)
        {
            var line = lines[i];
            var tokens = Tokenize(line.Text);
            words += tokens.Count;
            speaking += line.End > line.Start ? line.End - line.Start : TimeSpan.Zero;

            if (i > 0)
            {
                var gap = line.Start - lines[i - 1].End;
                if (gap >= threshold) pauses.Add(new PauseInfo(lines[i - 1].End, gap));
            }

            foreach (var repeat in FindRepeats(tokens)) repeats.Add(new RepeatInfo(line.Start, repeat));
            foreach (Match stutter in StutterStarts().Matches(line.Text)) repeats.Add(new RepeatInfo(line.Start, stutter.Value));

            CountFillers(line.Text, tokens, lexicon, language, fillerCounts);
        }

        var duration = lines[^1].End - lines[0].Start;
        return new SpeechReport
        {
            WordCount = words,
            Duration = duration,
            SpeakingTime = speaking,
            WordsPerMinute = speaking.TotalMinutes > 0 ? words / speaking.TotalMinutes : 0,
            Pauses = pauses,
            Fillers = fillerCounts.OrderByDescending(kv => kv.Value).ThenBy(kv => kv.Key).Select(kv => new FillerCount(kv.Key, kv.Value)).ToList(),
            Repeats = repeats,
        };
    }

    private static void CountFillers(string text, List<string> tokens, IReadOnlyList<string> lexicon, string? language, Dictionary<string, int> counts)
    {
        var normalized = " " + string.Join(' ', tokens) + " ";
        foreach (var filler in lexicon)
        {
            var needle = " " + filler + " ";
            var found = 0;
            for (var at = normalized.IndexOf(needle, StringComparison.Ordinal); at >= 0; at = normalized.IndexOf(needle, at + needle.Length - 1, StringComparison.Ordinal))
                found++;
            if (found > 0) counts[filler] = counts.GetValueOrDefault(filler) + found;
        }

        if (FillerLexicon.HasLikeFiller(language))
        {
            var like = SetApartLike().Matches(text).Count;
            if (like > 0) counts["like"] = counts.GetValueOrDefault("like") + like;
        }
    }

    private static IEnumerable<string> FindRepeats(List<string> tokens)
    {
        var i = 0;
        while (i < tokens.Count)
        {
            var run = 1;
            while (i + run < tokens.Count && tokens[i + run] == tokens[i]) run++;
            var countable = tokens[i].Length > 1 || tokens[i] is "i" or "a"; // lone letters belong to a hyphenated stutter
            if (run > 1 && countable && !EmphasisRepeats.Contains(tokens[i]) && !tokens[i].All(char.IsDigit))
                yield return string.Join(' ', Enumerable.Repeat(tokens[i], run));
            i += run;
        }
    }

    /// <summary>Lower-case words without punctuation. Apostrophes stay inside the word.</summary>
    private static List<string> Tokenize(string text) =>
        WordPattern().Matches(text).Select(m => m.Value.ToLowerInvariant()).ToList();

    [GeneratedRegex(@"[\p{L}\p{N}]+(?:['’][\p{L}]+)*", RegexOptions.CultureInvariant)]
    private static partial Regex WordPattern();

    // "w-w-what", "b-but": the same first letter repeated with hyphens before the word.
    [GeneratedRegex(@"\b(\p{L})(?:-\1)*-\1\p{L}+", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex StutterStarts();

    [GeneratedRegex(@"(?:,\s*like\b)|(?:\blike\s*,)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex SetApartLike();
}
