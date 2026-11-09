using Transcriber.Services.Models;

namespace Transcriber.Services.Configuration;

/// <summary>One entry of the language list in the app settings.</summary>
internal sealed class LanguageDefinition
{
    public string Code { get; set; } = "";

    public string Name { get; set; } = "";

    public ModelTier ConfidentFrom { get; set; }

    public ModelTier? AdvisedUntil { get; set; }

    public LanguageInfo ToInfo() => new(Code, Name, ConfidentFrom, AdvisedUntil);
}
