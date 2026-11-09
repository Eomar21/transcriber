using Transcriber.Coach.Advice;
using Transcriber.Coach.Advice.Rules;
using Transcriber.Coach.Analysis;
using Transcriber.Coach.Interfaces;
using Transcriber.Coach.Languages;

/// <summary>Builds the real coach services the way the app does, without a container, so tests share one wiring.</summary>
internal static class CoachFixture
{
    public static ILanguageProfileProvider Profiles { get; } = new LanguageProfileRegistry(new EmbeddedLanguageProfileSource());

    public static ISpeechAnalyzer Analyzer { get; } = new SpeechAnalyzer(
        new WordTokenizer(), new FillerDetector(), new RepeatDetector(), new PauseDetector(), Profiles);

    public static ICoachAdvisor Advisor { get; } = new CoachAdvisor(
        [new PaceRule(), new FillerRule(), new PauseRule(), new FluencyRule(), new ProgressRule()]);

    public static IVerbatimPromptProvider VerbatimPrompts { get; } = new ProfileVerbatimPromptProvider(Profiles);
}
