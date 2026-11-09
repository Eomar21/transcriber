using Transcriber.Coach.Models;

namespace Transcriber.Coach.Interfaces;

/// <summary>Looks up what the coach knows about a language.</summary>
public interface ILanguageProfileProvider
{
    /// <summary>The codes of the languages that have a profile.</summary>
    IReadOnlyCollection<string> Languages { get; }

    /// <returns>The profile for the language, or null when the coach has none ("auto" and unknown codes).</returns>
    LanguageProfile? Find(string? language);

    /// <returns>The profile for the language, or the default profile when the coach has none.</returns>
    LanguageProfile For(string? language);
}
