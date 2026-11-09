using Transcriber.ViewModels;

public class LanguageSupportTests
{
    private static string[] Codes(WhisperModel model) => ServicesFixture.Languages.For(model).Select(l => l.Code).ToArray();

    [Fact]
    public void Small_model_offers_auto_and_the_six_confident_languages()
    {
        Assert.Equal(["auto", "en", "es", "pt", "it", "de", "nl"], Codes(TestModels.Small));
        Assert.Equal(Codes(TestModels.Small), Codes(TestModels.SmallQ5));
    }

    [Fact]
    public void Base_model_offers_only_english_and_spanish()
    {
        Assert.Equal(["auto", "en", "es"], Codes(TestModels.Base));
    }

    [Fact]
    public void Larger_models_offer_more_languages()
    {
        Assert.Contains("fr", Codes(TestModels.MediumQ5));
        Assert.Contains("uk", Codes(TestModels.LargeV3TurboQ5));
        Assert.DoesNotContain("ja", Codes(TestModels.LargeV3TurboQ5));
        Assert.DoesNotContain("ru", Codes(TestModels.Small));
    }

    [Fact]
    public void Dutch_gets_advice_with_small_but_not_with_medium_or_larger()
    {
        Assert.Contains("Medium", ServicesFixture.Languages.Advice("nl", TestModels.Small)!);
        Assert.Null(ServicesFixture.Languages.Advice("nl", TestModels.MediumQ5));
        Assert.Null(ServicesFixture.Languages.Advice("nl", TestModels.LargeV3TurboQ5));
        Assert.Null(ServicesFixture.Languages.Advice("en", TestModels.Small));
        Assert.Null(ServicesFixture.Languages.Advice("auto", TestModels.Small));
    }

    [Fact]
    public void Result_note_follows_the_same_rule_as_the_advice()
    {
        Assert.NotNull(ServicesFixture.Languages.ResultNote("nl", TestModels.Small));
        Assert.Null(ServicesFixture.Languages.ResultNote("nl", TestModels.MediumQ5));
        Assert.Null(ServicesFixture.Languages.ResultNote("de", TestModels.Small));
    }

    [Fact]
    public void Practice_languages_all_have_filler_words_and_a_prompt()
    {
        foreach (var l in ServicesFixture.Languages.Practice)
        {
            var profile = CoachFixture.Profiles.Find(l.Code);
            Assert.NotNull(profile);
            Assert.NotEmpty(profile.Fillers);
            Assert.False(string.IsNullOrWhiteSpace(profile.VerbatimPrompt), $"{l.Code} needs a verbatim prompt");
        }
        // Practice runs the Small model, so it offers what the Small model offers.
        Assert.Equal(["en", "es", "pt", "it", "de", "nl"], ServicesFixture.Languages.Practice.Select(l => l.Code));
        Assert.NotNull(ServicesFixture.Languages.PracticeAdvice("nl"));
        Assert.Null(ServicesFixture.Languages.PracticeAdvice("en"));
    }
}

public class SpeechCoverageTests
{
    private static SpeechSpan Span(double a, double b) => new(TimeSpan.FromSeconds(a), TimeSpan.FromSeconds(b));
    private static TranscriptSegment Line(double a, double b) =>
        new() { Start = TimeSpan.FromSeconds(a), End = TimeSpan.FromSeconds(b), Text = "x" };

    [Fact]
    public void Full_text_is_complete()
    {
        var c = ServicesFixture.Coverage.Measure([Span(0, 20)], [Line(0, 10), Line(10, 20)]);
        Assert.False(c.LooksIncomplete);
        Assert.Equal(1, c.Share, 3);
    }

    [Fact]
    public void Normal_pause_between_sentences_is_not_missing_speech()
    {
        var c = ServicesFixture.Coverage.Measure([Span(0, 20)], [Line(0, 9), Line(10, 20)]);
        Assert.False(c.LooksIncomplete);
    }

    [Fact]
    public void A_long_stretch_without_text_is_incomplete()
    {
        var c = ServicesFixture.Coverage.Measure([Span(0, 30)], [Line(0, 18)]);
        Assert.True(c.LooksIncomplete);
        Assert.InRange(c.MissingSeconds, 10, 12);
    }

    [Fact]
    public void A_short_gap_in_a_long_recording_is_ignored()
    {
        var c = ServicesFixture.Coverage.Measure([Span(0, 120)], [Line(0, 112)]);
        Assert.False(c.LooksIncomplete);
    }

    [Fact]
    public void No_speech_means_nothing_to_warn_about()
    {
        Assert.False(ServicesFixture.Coverage.Measure([], []).LooksIncomplete);
    }
}
