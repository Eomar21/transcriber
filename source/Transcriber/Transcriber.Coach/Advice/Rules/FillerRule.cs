using Transcriber.Coach.Interfaces;
using Transcriber.Coach.Models;

namespace Transcriber.Coach.Advice.Rules;

/// <summary>Advice about filler words such as "um" and "you know".</summary>
public sealed class FillerRule : IAdviceRule
{
    public IEnumerable<Tip> Evaluate(SpeechReport report, SpeechReport? previous)
    {
        var rate = report.FillersPer100Words;
        var top = string.Join(", ", report.Fillers.Take(2).Select(f => $"\"{f.Filler}\" ({f.Count})"));

        if (report.FillerTotal == 0 || rate <= 2)
            yield return new Tip(TipKind.Good, "Few filler words", report.FillerTotal == 0 ? "No filler words found." : $"{TipFormat.One(rate)} per 100 words. Nicely clean.");
        else if (rate <= 5)
            yield return new Tip(TipKind.Info, "Some filler words", $"{TipFormat.One(rate)} per 100 words, mostly {top}. A silent pause works better than a filler.");
        else
            yield return new Tip(TipKind.Improve, "Many filler words", $"{TipFormat.One(rate)} per 100 words, mostly {top}. Replace them with a short pause.");
    }
}
