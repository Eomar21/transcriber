using Transcriber.Coach.Interfaces;

namespace Transcriber.Coach.Languages;

/// <summary>Takes the verbatim prompt from the language profile.</summary>
public sealed class ProfileVerbatimPromptProvider(ILanguageProfileProvider profiles) : IVerbatimPromptProvider
{
    public string? For(string? language) => profiles.Find(language)?.VerbatimPrompt;
}
