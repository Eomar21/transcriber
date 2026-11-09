using Transcriber.Coach.Interfaces;
using Transcriber.Coach.Models;

namespace Transcriber.Coach.Advice.Rules;

/// <summary>Advice about the speed of speaking, in words per minute.</summary>
public sealed class PaceRule : IAdviceRule
{
    public IEnumerable<Tip> Evaluate(SpeechReport report, SpeechReport? previous)
    {
        var wpm = report.WordsPerMinute;
        var rounded = TipFormat.Whole(wpm);
        yield return wpm switch
        {
            < 100 => new Tip(TipKind.Improve, "Pace is slow", $"{rounded} words per minute. Most listeners follow 120 to 160 comfortably, so try to keep your energy up."),
            < 110 => new Tip(TipKind.Info, "Pace is a little slow", $"{rounded} words per minute. A touch faster, around 120 to 160, usually sounds more confident."),
            <= 170 => new Tip(TipKind.Good, "Comfortable pace", $"{rounded} words per minute, right in the range that is easy to follow."),
            <= 190 => new Tip(TipKind.Info, "Pace is a little fast", $"{rounded} words per minute. Slow down slightly on key points so they land."),
            _ => new Tip(TipKind.Improve, "Pace is fast", $"{rounded} words per minute. Listeners will struggle to keep up, so breathe and slow down."),
        };
    }
}
