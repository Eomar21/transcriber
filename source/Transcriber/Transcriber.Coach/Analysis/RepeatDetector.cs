using System.Text.RegularExpressions;
using Transcriber.Coach.Interfaces;
using Transcriber.Coach.Models;

namespace Transcriber.Coach.Analysis;

/// <summary>Finds words said again at once ("the the") and stuttered starts ("w-w-want").</summary>
public sealed partial class RepeatDetector : IRepeatDetector
{
    public IReadOnlyList<string> Find(string text, IReadOnlyList<string> tokens, LanguageProfile profile)
    {
        var emphasis = new HashSet<string>(profile.EmphasisRepeats, StringComparer.OrdinalIgnoreCase);
        var found = FindRepeatedWords(tokens, emphasis).ToList();
        found.AddRange(StutterStarts().Matches(text).Select(m => m.Value));
        return found;
    }

    private static IEnumerable<string> FindRepeatedWords(IReadOnlyList<string> tokens, HashSet<string> emphasis)
    {
        var i = 0;
        while (i < tokens.Count)
        {
            var run = 1;
            while (i + run < tokens.Count && tokens[i + run] == tokens[i]) run++;
            var countable = tokens[i].Length > 1 || tokens[i] is "i" or "a"; // lone letters belong to a hyphenated stutter
            if (run > 1 && countable && !emphasis.Contains(tokens[i]) && !tokens[i].All(char.IsDigit))
                yield return string.Join(' ', Enumerable.Repeat(tokens[i], run));
            i += run;
        }
    }

    // "w-w-what", "b-but": the same first letter repeated with hyphens before the word.
    [GeneratedRegex(@"\b(\p{L})(?:-\1)*-\1\p{L}+", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex StutterStarts();
}
