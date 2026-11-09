using Transcriber.Coach.Exceptions;
using Transcriber.Coach.Interfaces;
using Transcriber.Coach.Models;

namespace Transcriber.Coach.Languages;

/// <summary>Holds the language profiles and finds one by language code.</summary>
public sealed class LanguageProfileRegistry : ILanguageProfileProvider
{
    /// <summary>The language whose profile is used when a recording has no known language, for example "auto".</summary>
    public const string DefaultLanguage = "en";

    private readonly Dictionary<string, LanguageProfile> _profiles;

    public LanguageProfileRegistry(ILanguageProfileSource source)
    {
        _profiles = new Dictionary<string, LanguageProfile>(StringComparer.OrdinalIgnoreCase);
        foreach (var profile in source.Load())
        {
            if (string.IsNullOrWhiteSpace(profile.Code))
                throw new LanguageProfileException("A language profile has no language code.");
            if (!_profiles.TryAdd(profile.Code, profile))
                throw new LanguageProfileException($"There are two language profiles for '{profile.Code}'.");
        }

        if (!_profiles.ContainsKey(DefaultLanguage))
            throw new LanguageProfileException($"The default language profile '{DefaultLanguage}' is missing.");
    }

    public IReadOnlyCollection<string> Languages => _profiles.Keys;

    public LanguageProfile? Find(string? language) =>
        string.IsNullOrWhiteSpace(language) ? null : _profiles.GetValueOrDefault(language);

    public LanguageProfile For(string? language) => Find(language) ?? _profiles[DefaultLanguage];
}
