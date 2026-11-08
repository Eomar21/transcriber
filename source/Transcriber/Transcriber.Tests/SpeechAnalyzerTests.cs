using Transcriber.Coach;

/// <summary>Pace, pauses, filler words and stumbles measured from a transcript.</summary>
public class SpeechAnalyzerTests
{
    private static TranscriptSegment Seg(double start, double end, string text) =>
        new() { Start = TimeSpan.FromSeconds(start), End = TimeSpan.FromSeconds(end), Text = text };

    [Fact]
    public void No_speech_gives_an_empty_report()
    {
        var report = SpeechAnalyzer.Analyze([]);

        Assert.Equal(0, report.WordCount);
        Assert.Equal(0, report.WordsPerMinute);
        Assert.Equal(0, report.FillersPer100Words);
    }

    [Fact]
    public void Words_are_counted_without_punctuation()
    {
        var report = SpeechAnalyzer.Analyze([Seg(0, 5, "Hello, world! It's a fine day.")]);

        Assert.Equal(6, report.WordCount);
    }

    [Fact]
    public void Pace_is_words_per_minute_of_speaking_time_so_pauses_do_not_slow_it()
    {
        // 30 words in 10 seconds of talking, with a 20 second gap in the middle.
        var fifteen = string.Join(' ', Enumerable.Repeat("word", 15));
        var report = SpeechAnalyzer.Analyze([Seg(0, 5, fifteen), Seg(25, 30, fifteen)]);

        Assert.Equal(30, report.WordCount);
        Assert.Equal(TimeSpan.FromSeconds(10), report.SpeakingTime);
        Assert.Equal(180, report.WordsPerMinute, precision: 1);
        Assert.Equal(TimeSpan.FromSeconds(30), report.Duration);
    }

    [Fact]
    public void A_gap_between_lines_over_the_threshold_is_a_pause()
    {
        var report = SpeechAnalyzer.Analyze([Seg(0, 3, "First part"), Seg(3.2, 6, "no pause"), Seg(8, 10, "after a pause")]);

        var pause = Assert.Single(report.Pauses);
        Assert.Equal(TimeSpan.FromSeconds(6), pause.Start);
        Assert.Equal(TimeSpan.FromSeconds(2), pause.Length);
        Assert.Equal(TimeSpan.FromSeconds(2), report.LongestPause);
    }

    [Fact]
    public void The_pause_threshold_can_be_changed()
    {
        var lines = new[] { Seg(0, 2, "one"), Seg(2.5, 4, "two") };

        Assert.Empty(SpeechAnalyzer.Analyze(lines).Pauses);
        Assert.Single(SpeechAnalyzer.Analyze(lines, pauseThreshold: TimeSpan.FromSeconds(0.4)).Pauses);
    }

    [Fact]
    public void Fillers_are_counted_and_ranked_ignoring_case_and_punctuation()
    {
        var report = SpeechAnalyzer.Analyze([Seg(0, 10, "Um, so I think, uh, that Um... you know, it works. Uh, yes.")], "en");

        Assert.Equal([new FillerCount("uh", 2), new FillerCount("um", 2), new FillerCount("you know", 1)],
            report.Fillers.OrderByDescending(f => f.Count).ThenBy(f => f.Filler));
        Assert.Equal(5, report.FillerTotal);
    }

    [Fact]
    public void Fillers_are_a_rate_per_hundred_words()
    {
        var text = "um " + string.Join(' ', Enumerable.Repeat("word", 19)); // 1 filler in 20 words

        Assert.Equal(5, SpeechAnalyzer.Analyze([Seg(0, 20, text)], "en").FillersPer100Words, precision: 3);
    }

    [Fact]
    public void Like_counts_only_when_set_apart_by_commas()
    {
        var plain = SpeechAnalyzer.Analyze([Seg(0, 5, "I like pizza and it looks like rain")], "en");
        var filler = SpeechAnalyzer.Analyze([Seg(0, 5, "It was, like, really big")], "en");

        Assert.DoesNotContain(plain.Fillers, f => f.Filler == "like");
        Assert.Contains(filler.Fillers, f => f.Filler == "like");
    }

    [Fact]
    public void A_word_that_is_part_of_a_longer_word_is_not_a_filler()
    {
        var report = SpeechAnalyzer.Analyze([Seg(0, 5, "The umbrella and the error were actually fine")], "en");

        Assert.DoesNotContain(report.Fillers, f => f.Filler is "um" or "er");
        Assert.Contains(report.Fillers, f => f.Filler == "actually");
    }

    [Theory]
    [InlineData("nl", "Dus, eh, ik denk eigenlijk dat het goed is", "eh")]
    [InlineData("es", "Pues, o sea, no sé", "o sea")]
    public void Other_languages_use_their_own_fillers(string language, string text, string expected)
    {
        var report = SpeechAnalyzer.Analyze([Seg(0, 5, text)], language);

        Assert.Contains(report.Fillers, f => f.Filler == expected);
    }

    [Fact]
    public void English_fillers_are_not_looked_for_in_other_languages()
    {
        var report = SpeechAnalyzer.Analyze([Seg(0, 5, "Het is actually gewoon een dag")], "nl");

        Assert.DoesNotContain(report.Fillers, f => f.Filler == "actually");
        Assert.Contains(report.Fillers, f => f.Filler == "gewoon");
    }

    [Fact]
    public void A_word_repeated_straight_away_is_a_stumble()
    {
        var report = SpeechAnalyzer.Analyze([Seg(0, 5, "I I think that the the plan is, is good")]);

        Assert.Equal(["i i", "the the", "is is"], report.Repeats.Select(r => r.Text));
    }

    [Fact]
    public void A_longer_run_is_one_stumble()
    {
        var report = SpeechAnalyzer.Analyze([Seg(0, 5, "we we we went home")]);

        Assert.Equal("we we we", Assert.Single(report.Repeats).Text);
    }

    [Fact]
    public void Repeating_for_emphasis_is_not_a_stumble()
    {
        var report = SpeechAnalyzer.Analyze([Seg(0, 5, "It was very very good and no no no it was 2 2 done")]);

        Assert.Empty(report.Repeats);
    }

    [Fact]
    public void A_stuttered_start_counts_once()
    {
        var report = SpeechAnalyzer.Analyze([Seg(0, 5, "I w-w-want to b-but I can't")]);

        Assert.Equal(["w-w-want", "b-but"], report.Repeats.Select(r => r.Text));
    }

    [Fact]
    public void A_hyphenated_repeat_of_a_whole_word_is_not_counted_twice()
    {
        var report = SpeechAnalyzer.Analyze([Seg(0, 5, "I-I think so")]);

        Assert.Single(report.Repeats);
    }

    [Fact]
    public void Lines_out_of_order_are_sorted_and_blank_lines_ignored()
    {
        var report = SpeechAnalyzer.Analyze([Seg(10, 12, "later"), Seg(5, 6, "  "), Seg(0, 2, "earlier")]);

        Assert.Equal(2, report.WordCount);
        Assert.Equal(TimeSpan.FromSeconds(12), report.Duration);
    }
}
