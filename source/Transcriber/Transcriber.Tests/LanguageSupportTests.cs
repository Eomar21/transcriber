using Transcriber.ViewModels;

public class LanguageSupportTests
{
    private static string[] Codes(WhisperModel model) => LanguageSupport.For(model).Select(l => l.Code).ToArray();

    [Fact]
    public void Small_model_offers_auto_and_the_six_confident_languages()
    {
        Assert.Equal(["auto", "en", "es", "pt", "it", "de", "nl"], Codes(WhisperModel.Small));
        Assert.Equal(Codes(WhisperModel.Small), Codes(WhisperModel.SmallQ5));
    }

    [Fact]
    public void Base_model_offers_only_english_and_spanish()
    {
        Assert.Equal(["auto", "en", "es"], Codes(WhisperModel.Base));
    }

    [Fact]
    public void Larger_models_offer_more_languages()
    {
        Assert.Contains("fr", Codes(WhisperModel.MediumQ5));
        Assert.Contains("uk", Codes(WhisperModel.LargeV3TurboQ5));
        Assert.DoesNotContain("ja", Codes(WhisperModel.LargeV3TurboQ5));
        Assert.DoesNotContain("ru", Codes(WhisperModel.Small));
    }

    [Fact]
    public void Dutch_gets_advice_with_small_but_not_with_medium_or_larger()
    {
        Assert.Contains("Medium", LanguageSupport.Advice("nl", WhisperModel.Small)!);
        Assert.Null(LanguageSupport.Advice("nl", WhisperModel.MediumQ5));
        Assert.Null(LanguageSupport.Advice("nl", WhisperModel.LargeV3TurboQ5));
        Assert.Null(LanguageSupport.Advice("en", WhisperModel.Small));
        Assert.Null(LanguageSupport.Advice("auto", WhisperModel.Small));
    }

    [Fact]
    public void Result_note_follows_the_same_rule_as_the_advice()
    {
        Assert.NotNull(LanguageSupport.ResultNote("nl", WhisperModel.Small));
        Assert.Null(LanguageSupport.ResultNote("nl", WhisperModel.MediumQ5));
        Assert.Null(LanguageSupport.ResultNote("de", WhisperModel.Small));
    }

    [Fact]
    public void Practice_languages_all_have_filler_words_and_a_prompt()
    {
        foreach (var l in LanguageSupport.Practice)
        {
            var profile = CoachFixture.Profiles.Find(l.Code);
            Assert.NotNull(profile);
            Assert.NotEmpty(profile.Fillers);
            Assert.False(string.IsNullOrWhiteSpace(profile.VerbatimPrompt), $"{l.Code} needs a verbatim prompt");
        }
        // Practice runs the Small model, so it offers what the Small model offers.
        Assert.Equal(["en", "es", "pt", "it", "de", "nl"], LanguageSupport.Practice.Select(l => l.Code));
        Assert.NotNull(LanguageSupport.PracticeAdvice("nl"));
        Assert.Null(LanguageSupport.PracticeAdvice("en"));
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
        var c = SpeechCoverage.Measure([Span(0, 20)], [Line(0, 10), Line(10, 20)]);
        Assert.False(c.LooksIncomplete);
        Assert.Equal(1, c.Share, 3);
    }

    [Fact]
    public void Normal_pause_between_sentences_is_not_missing_speech()
    {
        var c = SpeechCoverage.Measure([Span(0, 20)], [Line(0, 9), Line(10, 20)]);
        Assert.False(c.LooksIncomplete);
    }

    [Fact]
    public void A_long_stretch_without_text_is_incomplete()
    {
        var c = SpeechCoverage.Measure([Span(0, 30)], [Line(0, 18)]);
        Assert.True(c.LooksIncomplete);
        Assert.InRange(c.MissingSeconds, 10, 12);
    }

    [Fact]
    public void A_short_gap_in_a_long_recording_is_ignored()
    {
        var c = SpeechCoverage.Measure([Span(0, 120)], [Line(0, 112)]);
        Assert.False(c.LooksIncomplete);
    }

    [Fact]
    public void No_speech_means_nothing_to_warn_about()
    {
        Assert.False(SpeechCoverage.Measure([], []).LooksIncomplete);
    }
}
