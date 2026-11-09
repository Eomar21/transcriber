using Transcriber.Services.Models;

namespace Transcriber.Services.Interfaces;

/// <summary>Which languages the language pickers offer, and the hints that go with them.</summary>
public interface ILanguageCatalog
{
    /// <summary>The code that means "detect the language".</summary>
    string AutoCode { get; }

    /// <summary>"Auto-detect" first, then the languages offered for this model.</summary>
    IReadOnlyList<LanguageInfo> For(WhisperModel model);

    /// <summary>The languages that the Practice tab offers: those that its model handles well.</summary>
    IReadOnlyList<LanguageInfo> Practice { get; }

    /// <summary>A hint to show next to the language when a bigger model would be clearly better, otherwise null.</summary>
    string? Advice(string? code, WhisperModel model);

    /// <summary>A hint to show with a finished transcript when a bigger model would be clearly better, otherwise null.</summary>
    string? ResultNote(string? code, WhisperModel model);

    /// <summary>The hint under the Practice language picker, or null when the language is reliable there.</summary>
    string? PracticeAdvice(string? code);
}
