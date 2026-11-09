using Microsoft.Extensions.Options;
using Transcriber.Services.Configuration;
using Transcriber.Services.Interfaces;
using Transcriber.Services.Models;

namespace Transcriber.Services.Catalog;

/// <summary>
/// Which languages the language pickers offer. A language is offered only when the Whisper paper's FLEURS tests show
/// an error rate of about 12% or lower for the model in use. A language that is just above that line, such as Dutch,
/// is offered with a hint to use a bigger model. The list and the tiers are in the app settings.
/// </summary>
internal sealed class LanguageCatalog : ILanguageCatalog
{
    private readonly IReadOnlyList<LanguageInfo> _all;
    private readonly IModelCatalog _models;

    public LanguageCatalog(IOptions<LanguageSettings> options, IModelCatalog models)
    {
        _all = [.. options.Value.Catalog.Select(l => l.ToInfo())];
        _models = models;
        Practice = [.. _all.Where(l => _models.Practice.Tier >= l.ConfidentFrom)];
    }

    public string AutoCode => "auto";

    public IReadOnlyList<LanguageInfo> Practice { get; }

    public IReadOnlyList<LanguageInfo> For(WhisperModel model) =>
        [new(AutoCode, "Auto-detect", ModelTier.Base), .. _all.Where(l => model.Tier >= l.ConfidentFrom)];

    public string? Advice(string? code, WhisperModel model) => !IsAdvised(code, model)
        ? null
        : $"{ShortName(code!)} is less accurate with the {model.Tier} model. For better results, use the {_models.Suggested.Tier} model or a larger one.";

    public string? ResultNote(string? code, WhisperModel model) => !IsAdvised(code, model)
        ? null
        : $"This {ShortName(code!)} transcript was made with the {model.Tier} model, which makes more mistakes in {ShortName(code!)}. " +
          $"Check important words, or use the {_models.Suggested.Tier} model for higher accuracy.";

    public string? PracticeAdvice(string? code)
    {
        var practice = _models.Practice;
        if (!IsAdvised(code, practice)) return null;

        var reliable = Practice.Where(l => !IsAdvised(l.Code, practice)).Select(l => ShortName(l.Code)).ToList();
        return $"Practice uses the built-in {practice.Tier} model. {ShortName(code!)} is less accurate with it, " +
               $"so counts of filler words and pauses can be off. {JoinNames(reliable)} are the most reliable.";
    }

    private LanguageInfo? Find(string? code) => _all.FirstOrDefault(l => l.Code == code);

    private bool IsAdvised(string? code, WhisperModel model) =>
        Find(code) is { AdvisedUntil: { } until } && model.Tier < until;

    /// <summary>"Spanish (Español)" becomes "Spanish".</summary>
    private string ShortName(string code) => Find(code)!.Name.Split(' ')[0];

    private static string JoinNames(IReadOnlyList<string> names) => names.Count switch
    {
        0 => "No other language",
        1 => names[0],
        _ => string.Join(", ", names.Take(names.Count - 1)) + " and " + names[^1],
    };
}
