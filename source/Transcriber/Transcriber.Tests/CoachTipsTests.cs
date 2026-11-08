using Transcriber.Coach;

/// <summary>The plain-language advice drawn from a report.</summary>
public class CoachTipsTests
{
    private static SpeechReport Report(int words = 200, double wpm = 140, int fillers = 0, int pauses = 3, double minutes = 1.5, int repeats = 0) =>
        new()
        {
            WordCount = words,
            WordsPerMinute = wpm,
            Duration = TimeSpan.FromMinutes(minutes),
            SpeakingTime = TimeSpan.FromMinutes(minutes),
            Fillers = fillers == 0 ? [] : [new FillerCount("um", fillers)],
            Pauses = Enumerable.Range(0, pauses).Select(i => new PauseInfo(TimeSpan.FromSeconds(i * 10), TimeSpan.FromSeconds(1))).ToList(),
            Repeats = Enumerable.Range(0, repeats).Select(_ => new RepeatInfo(TimeSpan.Zero, "the the")).ToList(),
        };

    private static Tip Find(IReadOnlyList<Tip> tips, string titleStart) => tips.Single(t => t.Title.StartsWith(titleStart));

    [Fact]
    public void A_very_short_recording_is_not_judged()
    {
        var tips = CoachTips.From(Report(words: 10));

        var tip = Assert.Single(tips);
        Assert.Equal(TipKind.Info, tip.Kind);
        Assert.Contains("Too short", tip.Title);
    }

    [Theory]
    [InlineData(80, TipKind.Improve, "Pace is slow")]
    [InlineData(105, TipKind.Info, "Pace is a little slow")]
    [InlineData(140, TipKind.Good, "Comfortable pace")]
    [InlineData(180, TipKind.Info, "Pace is a little fast")]
    [InlineData(210, TipKind.Improve, "Pace is fast")]
    public void Pace_advice_follows_the_words_per_minute(double wpm, TipKind kind, string title)
    {
        var tip = CoachTips.From(Report(wpm: wpm))[0]; // pace is always the first tip

        Assert.Equal(kind, tip.Kind);
        Assert.Equal(title, tip.Title);
    }

    [Theory]
    [InlineData(0, TipKind.Good)]
    [InlineData(3, TipKind.Good)]   // 1.5 per 100 words
    [InlineData(8, TipKind.Info)]   // 4 per 100
    [InlineData(20, TipKind.Improve)] // 10 per 100
    public void Filler_advice_follows_the_rate(int fillers, TipKind kind)
    {
        var tip = Find(CoachTips.From(Report(fillers: fillers)), fillers == 0 || kind == TipKind.Good ? "Few filler" : kind == TipKind.Info ? "Some filler" : "Many filler");

        Assert.Equal(kind, tip.Kind);
    }

    [Fact]
    public void Filler_advice_names_the_most_common_ones()
    {
        var report = Report(fillers: 20) with { Fillers = [new FillerCount("um", 12), new FillerCount("you know", 8)] };

        var tip = Find(CoachTips.From(report), "Many filler");

        Assert.Contains("\"um\" (12)", tip.Detail);
        Assert.Contains("\"you know\" (8)", tip.Detail);
    }

    [Fact]
    public void No_pauses_is_worth_a_note_and_too_many_is_hesitation()
    {
        Assert.Equal(TipKind.Info, Find(CoachTips.From(Report(pauses: 0)), "No clear pauses").Kind);
        Assert.Equal(TipKind.Improve, Find(CoachTips.From(Report(pauses: 15)), "Lots of hesitation").Kind);
        Assert.Equal(TipKind.Good, Find(CoachTips.From(Report(pauses: 3)), "Pauses used well").Kind);
    }

    [Fact]
    public void A_long_pause_is_called_out()
    {
        var report = Report(pauses: 1) with { Pauses = [new PauseInfo(TimeSpan.Zero, TimeSpan.FromSeconds(4.5))] };

        Assert.Contains("4.5 seconds", Find(CoachTips.From(report), "Pauses used well").Detail);
    }

    [Theory]
    [InlineData(0, TipKind.Good)]
    [InlineData(2, TipKind.Info)]
    [InlineData(5, TipKind.Improve)]
    public void Stumbles_are_graded_by_how_many(int repeats, TipKind kind)
    {
        var tips = CoachTips.From(Report(repeats: repeats));

        Assert.Equal(kind, tips.Single(t => t.Title is "Smooth delivery" or "A few stumbles" or "Repeated words").Kind);
    }

    [Fact]
    public void Progress_is_noticed_when_fillers_drop_and_when_they_rise()
    {
        var before = Report(fillers: 24);
        var better = Report(fillers: 4);
        var worse = Report(fillers: 24);

        Assert.Equal(TipKind.Good, Find(CoachTips.From(better, before), "Fewer fillers").Kind);
        Assert.Equal(TipKind.Improve, Find(CoachTips.From(worse, better), "More fillers").Kind);
    }

    [Fact]
    public void Small_changes_and_a_missing_or_tiny_previous_session_add_nothing()
    {
        var a = Report(fillers: 10);
        var b = Report(fillers: 11);

        Assert.DoesNotContain(CoachTips.From(b, a), t => t.Title.Contains("than last time"));
        Assert.DoesNotContain(CoachTips.From(a, null), t => t.Title.Contains("than last time"));
        Assert.DoesNotContain(CoachTips.From(a, Report(words: 5, fillers: 5)), t => t.Title.Contains("than last time"));
    }

    [Fact]
    public void Pace_moving_toward_the_comfortable_range_is_celebrated()
    {
        var tips = CoachTips.From(Report(wpm: 135), Report(wpm: 190));

        Assert.Contains(tips, t => t.Title.StartsWith("Pace moved"));
    }
}
