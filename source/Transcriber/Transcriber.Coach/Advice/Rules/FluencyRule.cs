using Transcriber.Coach.Interfaces;
using Transcriber.Coach.Models;

namespace Transcriber.Coach.Advice.Rules;

/// <summary>Advice about repeated words and stumbled starts.</summary>
public sealed class FluencyRule : IAdviceRule
{
    public IEnumerable<Tip> Evaluate(SpeechReport report, SpeechReport? previous)
    {
        var count = report.Repeats.Count;
        if (count == 0)
        {
            yield return new Tip(TipKind.Good, "Smooth delivery", "No repeated words or stumbled starts.");
            yield break;
        }

        var examples = string.Join(", ", report.Repeats.Take(3).Select(x => $"\"{x.Text}\""));
        yield return count >= 3
            ? new Tip(TipKind.Improve, "Repeated words", $"{count} stumbles, for example {examples}. Slow down a little and finish the thought before you speak it.")
            : new Tip(TipKind.Info, "A few stumbles", $"{count} repeated {(count == 1 ? "word" : "words")}: {examples}.");
    }
}
