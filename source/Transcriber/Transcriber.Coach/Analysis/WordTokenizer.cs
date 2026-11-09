using System.Text.RegularExpressions;
using Transcriber.Coach.Interfaces;

namespace Transcriber.Coach.Analysis;

/// <summary>Lower-case words without punctuation. Apostrophes stay inside the word.</summary>
public sealed partial class WordTokenizer : ITextTokenizer
{
    public IReadOnlyList<string> Tokenize(string text) =>
        WordPattern().Matches(text).Select(m => m.Value.ToLowerInvariant()).ToList();

    [GeneratedRegex(@"[\p{L}\p{N}]+(?:['’][\p{L}]+)*", RegexOptions.CultureInvariant)]
    private static partial Regex WordPattern();
}
