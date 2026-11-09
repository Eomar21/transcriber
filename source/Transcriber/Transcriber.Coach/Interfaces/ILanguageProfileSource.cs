using Transcriber.Coach.Models;

namespace Transcriber.Coach.Interfaces;

/// <summary>Where language profiles come from: files in the app, a folder, or a test.</summary>
public interface ILanguageProfileSource
{
    IReadOnlyList<LanguageProfile> Load();
}
