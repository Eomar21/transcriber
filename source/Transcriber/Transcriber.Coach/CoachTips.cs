using System.Globalization;

namespace Transcriber.Coach;

public enum TipKind { Good, Improve, Info }

public sealed record Tip(TipKind Kind, string Title, string Detail);

/// <summary>Plain-language advice from a report. Rules only, so it works offline and gives the same answer every time.</summary>
public static class CoachTips
{
    /// <summary>Below this many words there is too little speech to say anything fair.</summary>
    public const int MinimumWords = 25;

    public static IReadOnlyList<Tip> From(SpeechReport report, SpeechReport? previous = null)
    {
        if (report.WordCount < MinimumWords)
            return [new Tip(TipKind.Info, "Too short to judge", "Speak for at least 30 seconds so the numbers mean something.")];

        var tips = new List<Tip> { Pace(report), Fillers(report), Pauses(report), Fluency(report) };
        if (previous is { WordCount: >= MinimumWords }) tips.AddRange(Progress(report, previous));
        return tips;
    }

    private static string One(double value) => value.ToString("0.#", CultureInfo.InvariantCulture);

    private static Tip Pace(SpeechReport r)
    {
        var wpm = r.WordsPerMinute;
        var rounded = (int)Math.Round(wpm);
        return wpm switch
        {
            < 100 => new Tip(TipKind.Improve, "Pace is slow", $"{rounded} words per minute. Most listeners follow 120 to 160 comfortably, so try to keep your energy up."),
            < 110 => new Tip(TipKind.Info, "Pace is a little slow", $"{rounded} words per minute. A touch faster, around 120 to 160, usually sounds more confident."),
            <= 170 => new Tip(TipKind.Good, "Comfortable pace", $"{rounded} words per minute, right in the range that is easy to follow."),
            <= 190 => new Tip(TipKind.Info, "Pace is a little fast", $"{rounded} words per minute. Slow down slightly on key points so they land."),
            _ => new Tip(TipKind.Improve, "Pace is fast", $"{rounded} words per minute. Listeners will struggle to keep up, so breathe and slow down."),
        };
    }

    private static Tip Fillers(SpeechReport r)
    {
        var rate = r.FillersPer100Words;
        var top = string.Join(", ", r.Fillers.Take(2).Select(f => $"\"{f.Filler}\" ({f.Count})"));
        if (r.FillerTotal == 0 || rate <= 2)
            return new Tip(TipKind.Good, "Few filler words", r.FillerTotal == 0 ? "No filler words found." : $"{One(rate)} per 100 words. Nicely clean.");
        if (rate <= 5)
            return new Tip(TipKind.Info, "Some filler words", $"{One(rate)} per 100 words, mostly {top}. A silent pause works better than a filler.");
        return new Tip(TipKind.Improve, "Many filler words", $"{One(rate)} per 100 words, mostly {top}. Replace them with a short pause.");
    }

    private static Tip Pauses(SpeechReport r)
    {
        var perMinute = r.PausesPerMinute;
        if (r.Pauses.Count == 0)
            return new Tip(TipKind.Info, "No clear pauses", "Short pauses between ideas give listeners time to take them in.");
        if (perMinute > 6)
            return new Tip(TipKind.Improve, "Lots of hesitation", $"{r.Pauses.Count} pauses, about {One(perMinute)} a minute. Try to plan the next sentence while finishing this one.");

        var longest = r.LongestPause >= TimeSpan.FromSeconds(3) ? $" The longest was {One(r.LongestPause.TotalSeconds)} seconds." : "";
        return new Tip(TipKind.Good, "Pauses used well", $"{r.Pauses.Count} pauses, about {One(perMinute)} a minute.{longest}");
    }

    private static Tip Fluency(SpeechReport r)
    {
        var count = r.Repeats.Count;
        if (count == 0) return new Tip(TipKind.Good, "Smooth delivery", "No repeated words or stumbled starts.");
        var examples = string.Join(", ", r.Repeats.Take(3).Select(x => $"\"{x.Text}\""));
        return count >= 3
            ? new Tip(TipKind.Improve, "Repeated words", $"{count} stumbles, for example {examples}. Slow down a little and finish the thought before you speak it.")
            : new Tip(TipKind.Info, "A few stumbles", $"{count} repeated {(count == 1 ? "word" : "words")}: {examples}.");
    }

    private static IEnumerable<Tip> Progress(SpeechReport now, SpeechReport before)
    {
        var fillerChange = now.FillersPer100Words - before.FillersPer100Words;
        if (fillerChange <= -1)
            yield return new Tip(TipKind.Good, "Fewer fillers than last time", $"Down from {One(before.FillersPer100Words)} to {One(now.FillersPer100Words)} per 100 words.");
        else if (fillerChange >= 1)
            yield return new Tip(TipKind.Improve, "More fillers than last time", $"Up from {One(before.FillersPer100Words)} to {One(now.FillersPer100Words)} per 100 words.");

        var beforeGap = Math.Abs(before.WordsPerMinute - 140);
        var nowGap = Math.Abs(now.WordsPerMinute - 140);
        if (beforeGap - nowGap >= 10)
            yield return new Tip(TipKind.Good, "Pace moved toward the sweet spot", $"{(int)Math.Round(before.WordsPerMinute)} to {(int)Math.Round(now.WordsPerMinute)} words per minute.");
    }
}
