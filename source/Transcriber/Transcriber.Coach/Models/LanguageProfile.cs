namespace Transcriber.Coach.Models;

/// <summary>
/// Everything the coach knows about speaking in one language. It is data, not code: a language is added by adding a
/// profile file, with no change to the analysis.
/// </summary>
public sealed record LanguageProfile
{
    /// <summary>The Whisper language code, for example "en".</summary>
    public required string Code { get; init; }

    /// <summary>Filler words and phrases. Matching ignores case and punctuation.</summary>
    public IReadOnlyList<string> Fillers { get; init; } = [];

    /// <summary>
    /// Words that are fillers only when set apart by commas ("it was, like, huge"). Plain matching would flag every
    /// genuine use of them, so they are counted separately.
    /// </summary>
    public IReadOnlyList<string> SetApartFillers { get; init; } = [];

    /// <summary>Words that are fine to say twice for emphasis, so they are not counted as a stumble.</summary>
    public IReadOnlyList<string> EmphasisRepeats { get; init; } = [];

    /// <summary>
    /// Whisper is trained to tidy speech, so it drops "um" and stuttered words, which is what a speaking coach needs to
    /// see. Starting the decoder with a sample sentence full of fillers makes it keep them.
    /// </summary>
    public string? VerbatimPrompt { get; init; }
}
