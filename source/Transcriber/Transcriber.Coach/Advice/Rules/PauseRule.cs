using Transcriber.Coach.Interfaces;
using Transcriber.Coach.Models;

namespace Transcriber.Coach.Advice.Rules;

/// <summary>Advice about silences between ideas.</summary>
public sealed class PauseRule : IAdviceRule
{
    public IEnumerable<Tip> Evaluate(SpeechReport report, SpeechReport? previous)
    {
        var perMinute = report.PausesPerMinute;
        if (report.Pauses.Count == 0)
        {
            yield return new Tip(TipKind.Info, "No clear pauses", "Short pauses between ideas give listeners time to take them in.");
            yield break;
        }

        if (perMinute > 6)
        {
            yield return new Tip(TipKind.Improve, "Lots of hesitation", $"{report.Pauses.Count} pauses, about {TipFormat.One(perMinute)} a minute. Try to plan the next sentence while finishing this one.");
            yield break;
        }

        var longest = report.LongestPause >= TimeSpan.FromSeconds(3) ? $" The longest was {TipFormat.One(report.LongestPause.TotalSeconds)} seconds." : "";
        yield return new Tip(TipKind.Good, "Pauses used well", $"{report.Pauses.Count} pauses, about {TipFormat.One(perMinute)} a minute.{longest}");
    }
}
