using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Transcriber.Coach.Interfaces;
using Transcriber.Coach.Models;

namespace Transcriber.Coach.Analysis;

/// <summary>Counts the filler words of a language profile. The words come from the profile, so this class knows no language.</summary>
public sealed class FillerDetector : IFillerDetector
{
    private readonly ConcurrentDictionary<string, Regex> _setApart = new(StringComparer.Ordinal);

    public IReadOnlyDictionary<string, int> Count(string text, IReadOnlyList<string> tokens, LanguageProfile profile)
    {
        var counts = new Dictionary<string, int>();

        // Whole words and phrases, matched on the cleaned words so punctuation and case do not matter.
        var normalized = " " + string.Join(' ', tokens) + " ";
        foreach (var filler in profile.Fillers)
        {
            var found = CountPhrase(normalized, " " + filler.ToLowerInvariant() + " ");
            if (found > 0) counts[filler] = counts.GetValueOrDefault(filler) + found;
        }

        // Words that are a filler only between commas.
        foreach (var word in profile.SetApartFillers)
        {
            var found = SetApartPattern(word).Matches(text).Count;
            if (found > 0) counts[word] = counts.GetValueOrDefault(word) + found;
        }

        return counts;
    }

    private static int CountPhrase(string normalized, string needle)
    {
        var found = 0;
        // The next search starts on the closing space, so two fillers in a row share it.
        for (var at = normalized.IndexOf(needle, StringComparison.Ordinal); at >= 0;
             at = normalized.IndexOf(needle, at + needle.Length - 1, StringComparison.Ordinal))
            found++;
        return found;
    }

    private Regex SetApartPattern(string word) => _setApart.GetOrAdd(word, w =>
    {
        var escaped = Regex.Escape(w);
        return new Regex($@"(?:,\s*{escaped}\b)|(?:\b{escaped}\s*,)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    });
}
