using Transcriber.Coach.Exceptions;
using Transcriber.Coach.Interfaces;
using Transcriber.Coach.Languages;
using Transcriber.Coach.Models;

/// <summary>The language profiles are data. These tests check the data files and the services that read them.</summary>
public class LanguageProfileTests
{
    private sealed class Source(params LanguageProfile[] profiles) : ILanguageProfileSource
    {
        public IReadOnlyList<LanguageProfile> Load() => profiles;
    }

    private static LanguageProfile Profile(string code, string? prompt = null) => new() { Code = code, Fillers = ["x"], VerbatimPrompt = prompt };

    [Fact]
    public void The_built_in_profiles_are_found_in_the_assembly()
    {
        var codes = new EmbeddedLanguageProfileSource().Load().Select(p => p.Code).Order().ToArray();

        Assert.Equal(["de", "en", "es", "it", "nl", "pt"], codes);
    }

    [Fact]
    public void Every_built_in_profile_is_complete_and_has_no_duplicates()
    {
        foreach (var p in new EmbeddedLanguageProfileSource().Load())
        {
            Assert.NotEmpty(p.Fillers);
            Assert.False(string.IsNullOrWhiteSpace(p.VerbatimPrompt), p.Code);
            Assert.All(p.Fillers.Concat(p.SetApartFillers).Concat(p.EmphasisRepeats), w => Assert.Equal(w.Trim(), w));
            Assert.Equal(p.Fillers.Count, p.Fillers.Distinct().Count());
            // A word is a filler either everywhere or only between commas, not both.
            Assert.Empty(p.Fillers.Intersect(p.SetApartFillers));
        }
    }

    [Fact]
    public void Lookup_ignores_case_and_returns_null_for_a_language_without_a_profile()
    {
        var provider = new LanguageProfileRegistry(new Source(Profile("en"), Profile("de")));

        Assert.Equal("de", provider.Find("DE")?.Code);
        Assert.Null(provider.Find("auto"));
        Assert.Null(provider.Find(null));
        Assert.Null(provider.Find(" "));
    }

    [Fact]
    public void The_default_profile_is_used_when_the_language_is_unknown()
    {
        var provider = new LanguageProfileRegistry(new Source(Profile("en"), Profile("de")));

        Assert.Equal("en", provider.For("auto").Code);
        Assert.Equal("en", provider.For(null).Code);
        Assert.Equal("de", provider.For("de").Code);
    }

    [Fact]
    public void A_new_language_needs_only_a_profile()
    {
        var provider = new LanguageProfileRegistry(new Source(Profile("en"), new LanguageProfile { Code = "fr", Fillers = ["euh"] }));
        var analyzer = new Transcriber.Coach.Analysis.SpeechAnalyzer(
            new Transcriber.Coach.Analysis.WordTokenizer(), new Transcriber.Coach.Analysis.FillerDetector(),
            new Transcriber.Coach.Analysis.RepeatDetector(), new Transcriber.Coach.Analysis.PauseDetector(), provider);

        var report = analyzer.Analyze([new TranscriptSegment { Start = TimeSpan.Zero, End = TimeSpan.FromSeconds(4), Text = "Alors, euh, je pense, euh, oui" }], "fr");

        Assert.Equal(2, report.Fillers.Single(f => f.Filler == "euh").Count);
    }

    [Fact]
    public void A_missing_default_profile_is_an_error()
    {
        Assert.Throws<LanguageProfileException>(() => new LanguageProfileRegistry(new Source(Profile("de"))));
    }

    [Fact]
    public void Two_profiles_for_one_language_are_an_error()
    {
        Assert.Throws<LanguageProfileException>(() => new LanguageProfileRegistry(new Source(Profile("en"), Profile("EN"))));
    }

    [Fact]
    public void The_verbatim_prompt_comes_from_the_profile_and_is_absent_for_auto()
    {
        var prompts = new ProfileVerbatimPromptProvider(new LanguageProfileRegistry(new Source(Profile("en", "Umm, hello."), Profile("de"))));

        Assert.Equal("Umm, hello.", prompts.For("en"));
        Assert.Null(prompts.For("de"));
        Assert.Null(prompts.For("auto"));
        Assert.Null(prompts.For(null));
    }

    [Theory]
    [InlineData("pt", "Eu acho que, né, é isso", "né")]
    [InlineData("pt", "Foi, tipo, enorme", "tipo")]
    [InlineData("it", "Allora, ehm, io penso di sì", "ehm")]
    [InlineData("it", "Era, tipo, enorme", "tipo")]
    [InlineData("de", "Ich denke, ähm, dass es geht", "ähm")]
    [InlineData("de", "Das ist sozusagen fertig", "sozusagen")]
    public void The_new_languages_find_their_own_fillers(string language, string text, string expected)
    {
        var report = CoachFixture.Analyzer.Analyze([new TranscriptSegment { Start = TimeSpan.Zero, End = TimeSpan.FromSeconds(5), Text = text }], language);

        Assert.Contains(report.Fillers, f => f.Filler == expected);
    }

    [Theory]
    [InlineData("pt", "Era um tipo de carro")]
    [InlineData("it", "Che tipo di pizza vuoi")]
    [InlineData("de", "Also Mal ehrlich ist es gut")]
    public void A_word_that_is_a_filler_only_between_commas_is_not_counted_in_plain_use(string language, string text)
    {
        var report = CoachFixture.Analyzer.Analyze([new TranscriptSegment { Start = TimeSpan.Zero, End = TimeSpan.FromSeconds(5), Text = text }], language);

        Assert.Empty(report.Fillers);
    }

    [Fact]
    public void A_repeated_emphasis_word_belongs_to_its_own_language()
    {
        var segment = new TranscriptSegment { Start = TimeSpan.Zero, End = TimeSpan.FromSeconds(5), Text = "Das ist sehr sehr gut und ja ja" };

        Assert.Empty(CoachFixture.Analyzer.Analyze([segment], "de").Repeats);
        Assert.NotEmpty(CoachFixture.Analyzer.Analyze([segment], "en").Repeats);
    }
}
