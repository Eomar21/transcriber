using Transcriber.Coach.Models;

namespace Transcriber.Coach.Interfaces;

/// <summary>One kind of advice, for example about pace. A new kind of advice is a new rule, not a change to an old one.</summary>
public interface IAdviceRule
{
    /// <param name="report">The recording that is being judged.</param>
    /// <param name="previous">The last comparable recording, or null when there is none.</param>
    /// <returns>Zero or more tips.</returns>
    IEnumerable<Tip> Evaluate(SpeechReport report, SpeechReport? previous);
}
