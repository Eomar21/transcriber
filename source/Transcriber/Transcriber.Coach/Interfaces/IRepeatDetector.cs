using Transcriber.Coach.Models;

namespace Transcriber.Coach.Interfaces;

/// <summary>Finds stumbles in one line of speech: words said again at once, and stuttered starts.</summary>
public interface IRepeatDetector
{
    /// <returns>The text of each stumble, in the order found.</returns>
    IReadOnlyList<string> Find(string text, IReadOnlyList<string> tokens, LanguageProfile profile);
}
