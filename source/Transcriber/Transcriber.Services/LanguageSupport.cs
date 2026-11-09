/// <summary>How much a Whisper model can be trusted for a language. Higher is a bigger, more accurate model.</summary>
public enum ModelTier
{
    Base = 0,
    Small = 1,
    Medium = 2,
    Large = 3,
}

/// <param name="ConfidentFrom">The smallest model tier for which the language is offered.</param>
/// <param name="AdvisedUntil">
/// From <paramref name="ConfidentFrom"/> up to this tier (exclusive) the language is offered with a hint to use a bigger model.
/// </param>
public sealed record LanguageInfo(string Code, string Name, ModelTier ConfidentFrom, ModelTier? AdvisedUntil = null);

/// <summary>
/// Which languages the language pickers offer. A language is offered only when the Whisper paper's FLEURS tests show
/// an error rate of about 12% or lower for the model in use, except Dutch, which is offered with a hint to use a bigger
/// model because it is the most used language that is just above that line with the bundled Small model.
/// </summary>
public static class LanguageSupport
{
    /// <summary>The model to suggest when a language is less accurate with the current one.</summary>
    public static readonly WhisperModel SuggestedModel = WhisperModel.MediumQ5;

    public static ModelTier TierOf(WhisperModel model) => model.Id switch
    {
        "small" or "small-q5_1" => ModelTier.Small,
        "medium-q5_0" => ModelTier.Medium,
        "large-v3-turbo-q5_0" => ModelTier.Large,
        _ => ModelTier.Base,
    };

    public static readonly IReadOnlyList<LanguageInfo> All =
    [
        new("en", "English", ModelTier.Base),
        new("es", "Spanish (Español)", ModelTier.Base),
        new("pt", "Portuguese (Português)", ModelTier.Small),
        new("it", "Italian (Italiano)", ModelTier.Small),
        new("de", "German (Deutsch)", ModelTier.Small),
        new("nl", "Dutch (Nederlands)", ModelTier.Small, AdvisedUntil: ModelTier.Medium),
        new("fr", "French (Français)", ModelTier.Medium),
        new("pl", "Polish (Polski)", ModelTier.Medium),
        new("tr", "Turkish (Türkçe)", ModelTier.Medium),
        new("id", "Indonesian (Bahasa Indonesia)", ModelTier.Medium),
        new("sv", "Swedish (Svenska)", ModelTier.Medium),
        new("uk", "Ukrainian (Українська)", ModelTier.Medium),
    ];

    /// <summary>"Auto-detect" first, then the languages offered for this model.</summary>
    public static IReadOnlyList<LanguageInfo> For(WhisperModel model)
    {
        var tier = TierOf(model);
        return [new("auto", "Auto-detect", ModelTier.Base), .. All.Where(l => tier >= l.ConfidentFrom)];
    }

    private static LanguageInfo? Find(string? code) => All.FirstOrDefault(l => l.Code == code);

    private static bool IsAdvised(string? code, WhisperModel model) =>
        Find(code) is { AdvisedUntil: { } until } && TierOf(model) < until;

    private static string ShortName(WhisperModel model) => TierOf(model).ToString();

    private static string LanguageName(string code) => Find(code)!.Name.Split(' ')[0];

    /// <summary>A hint to show next to the language when a bigger model would be clearly better, otherwise null.</summary>
    public static string? Advice(string? code, WhisperModel model) => !IsAdvised(code, model)
        ? null
        : $"{LanguageName(code!)} is less accurate with the {ShortName(model)} model. For better results, use the Medium model or a larger one.";

    /// <summary>A hint to show with a finished transcript when a bigger model would be clearly better, otherwise null.</summary>
    public static string? ResultNote(string? code, WhisperModel model) => !IsAdvised(code, model)
        ? null
        : $"This {LanguageName(code!)} transcript was made with the {ShortName(model)} model, which makes more mistakes in {LanguageName(code!)}. " +
          "Check important words, or use the Medium model for higher accuracy.";

    // ---- Practice ----

    /// <summary>
    /// Practice always runs the bundled Small model, so it offers the languages that model handles well. Each of these
    /// needs a language profile in the coach (filler words and a verbatim prompt). A test keeps the two in step.
    /// </summary>
    public static readonly IReadOnlyList<LanguageInfo> Practice = [.. All.Where(l => l.ConfidentFrom <= ModelTier.Small)];

    /// <summary>The hint under the Practice language picker, or null when the language is reliable.</summary>
    public static string? PracticeAdvice(string? code) => code == "nl"
        ? "Practice uses the built-in Small model. Dutch is less accurate with it, so counts of filler words and pauses can be off. English, Spanish, Portuguese, Italian and German are the most reliable."
        : null;
}
