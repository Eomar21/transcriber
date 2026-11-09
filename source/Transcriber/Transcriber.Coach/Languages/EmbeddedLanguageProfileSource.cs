using System.Reflection;
using System.Text.Json;
using Transcriber.Coach.Exceptions;
using Transcriber.Coach.Interfaces;
using Transcriber.Coach.Models;

namespace Transcriber.Coach.Languages;

/// <summary>Reads the language profiles that are built into the app as JSON files (the Profiles folder).</summary>
public sealed class EmbeddedLanguageProfileSource(Assembly? assembly = null) : ILanguageProfileSource
{
    private const string ResourceFolder = ".Languages.Profiles.";

    private static readonly JsonSerializerOptions Options = new() { PropertyNameCaseInsensitive = true };

    private readonly Assembly _assembly = assembly ?? typeof(EmbeddedLanguageProfileSource).Assembly;

    public IReadOnlyList<LanguageProfile> Load() => _assembly.GetManifestResourceNames()
        .Where(n => n.Contains(ResourceFolder, StringComparison.Ordinal) && n.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
        .OrderBy(n => n, StringComparer.Ordinal)
        .Select(Read)
        .ToList();

    private LanguageProfile Read(string resourceName)
    {
        try
        {
            using var stream = _assembly.GetManifestResourceStream(resourceName)!;
            return JsonSerializer.Deserialize<LanguageProfile>(stream, Options)
                ?? throw new LanguageProfileException($"The language profile '{resourceName}' is empty.");
        }
        catch (JsonException e)
        {
            throw new LanguageProfileException($"The language profile '{resourceName}' is not valid.", e);
        }
    }
}
