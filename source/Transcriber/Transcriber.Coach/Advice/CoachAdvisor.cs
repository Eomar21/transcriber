using Transcriber.Coach.Interfaces;
using Transcriber.Coach.Models;

namespace Transcriber.Coach.Advice;

/// <summary>Asks each advice rule in turn and joins their tips. A recording with too few words gets one honest note instead.</summary>
public sealed class CoachAdvisor(IEnumerable<IAdviceRule> rules) : ICoachAdvisor
{
    public const int MinimumWordsForAdvice = 25;

    private readonly IReadOnlyList<IAdviceRule> _rules = rules.ToList();

    public int MinimumWords => MinimumWordsForAdvice;

    public IReadOnlyList<Tip> Advise(SpeechReport report, SpeechReport? previous = null)
    {
        if (report.WordCount < MinimumWords)
            return [new Tip(TipKind.Info, "Too short to judge", "Speak for at least 30 seconds so the numbers mean something.")];

        // A short earlier run would give a misleading comparison.
        var comparable = previous is { WordCount: >= MinimumWordsForAdvice } ? previous : null;
        return _rules.SelectMany(rule => rule.Evaluate(report, comparable)).ToList();
    }
}
