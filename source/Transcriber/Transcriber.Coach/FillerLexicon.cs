namespace Transcriber.Coach;

/// <summary>Filler words and phrases per language. Matching ignores case and punctuation.</summary>
public static class FillerLexicon
{
    private static readonly string[] English =
        ["um", "umm", "uh", "uhh", "er", "erm", "ah", "hmm", "you know", "i mean", "sort of", "kind of", "basically", "literally", "actually"];

    private static readonly string[] Dutch =
        ["eh", "ehm", "uhm", "uh", "zeg maar", "eigenlijk", "gewoon", "nou", "weet je"];

    private static readonly string[] Spanish =
        ["eh", "em", "este", "o sea", "pues", "bueno", "como que"];

    /// <summary>The fillers to look for in a language ("auto" and unknown languages use English).</summary>
    public static IReadOnlyList<string> For(string? language) => (language ?? "en").ToLowerInvariant() switch
    {
        "nl" => Dutch,
        "es" => Spanish,
        _ => English,
    };

    /// <summary>
    /// "like" is a filler only when it is set apart ("it was, like, huge"), which transcripts show with commas.
    /// Counted separately because plain matching would flag every genuine use.
    /// </summary>
    public static bool HasLikeFiller(string? language) => (language ?? "en").ToLowerInvariant() is "en" or "auto";
}
