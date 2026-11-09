using Transcriber.Coach.Interfaces;
using Transcriber.Coach.Models;

namespace Transcriber.Coach.Advice.Rules;

/// <summary>Compares a recording with the last comparable one. It says nothing when there is no earlier recording.</summary>
public sealed class ProgressRule : IAdviceRule
{
    /// <summary>The pace, in words per minute, that most listeners find easy to follow.</summary>
    private const double TargetPace = 140;

    public IEnumerable<Tip> Evaluate(SpeechReport report, SpeechReport? previous)
    {
        if (previous is null) yield break;

        var fillerChange = report.FillersPer100Words - previous.FillersPer100Words;
        if (fillerChange <= -1)
            yield return new Tip(TipKind.Good, "Fewer fillers than last time", $"Down from {TipFormat.One(previous.FillersPer100Words)} to {TipFormat.One(report.FillersPer100Words)} per 100 words.");
        else if (fillerChange >= 1)
            yield return new Tip(TipKind.Improve, "More fillers than last time", $"Up from {TipFormat.One(previous.FillersPer100Words)} to {TipFormat.One(report.FillersPer100Words)} per 100 words.");

        var beforeGap = Math.Abs(previous.WordsPerMinute - TargetPace);
        var nowGap = Math.Abs(report.WordsPerMinute - TargetPace);
        if (beforeGap - nowGap >= 10)
            yield return new Tip(TipKind.Good, "Pace moved toward the sweet spot", $"{TipFormat.Whole(previous.WordsPerMinute)} to {TipFormat.Whole(report.WordsPerMinute)} words per minute.");
    }
}
