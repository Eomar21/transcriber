using Transcriber.Coach.Models;

namespace Transcriber.Coach.Interfaces;

/// <summary>Finds the filler words in one line of speech.</summary>
public interface IFillerDetector
{
    /// <param name="text">The line as written, with its punctuation.</param>
    /// <param name="tokens">The words of the line.</param>
    /// <param name="profile">The language of the line.</param>
    /// <returns>How many times each filler was found. Fillers that were not found are left out.</returns>
    IReadOnlyDictionary<string, int> Count(string text, IReadOnlyList<string> tokens, LanguageProfile profile);
}
