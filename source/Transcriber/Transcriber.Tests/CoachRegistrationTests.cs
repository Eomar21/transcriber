using Microsoft.Extensions.DependencyInjection;
using Transcriber.Coach.Advice.Rules;
using Transcriber.Coach.Extensions;
using Transcriber.Coach.Interfaces;
using Transcriber.Coach.Models;

/// <summary>The app builds the coach from its container. If a registration is missing, the Practice tab would fail at runtime.</summary>
public class CoachRegistrationTests
{
    private static ServiceProvider Build() =>
        new ServiceCollection().AddCoach(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".json"))
            .BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });

    [Fact]
    public void Every_coach_interface_can_be_resolved()
    {
        using var provider = Build();

        foreach (var type in new[]
        {
            typeof(ISpeechAnalyzer), typeof(ICoachAdvisor), typeof(IVerbatimPromptProvider), typeof(ILanguageProfileProvider),
            typeof(ILanguageProfileSource), typeof(ITextTokenizer), typeof(IFillerDetector), typeof(IRepeatDetector),
            typeof(IPauseDetector), typeof(IFeedbackPromptBuilder), typeof(ICoachFeedback), typeof(IPracticeHistory),
        })
            Assert.NotNull(provider.GetRequiredService(type));
    }

    [Fact]
    public void The_advice_rules_are_registered_in_the_order_of_the_tips()
    {
        using var provider = Build();

        var rules = provider.GetServices<IAdviceRule>().Select(r => r.GetType()).ToArray();

        Assert.Equal([typeof(PaceRule), typeof(FillerRule), typeof(PauseRule), typeof(FluencyRule), typeof(ProgressRule)], rules);
    }

    [Fact]
    public void The_resolved_coach_analyses_a_transcript_end_to_end()
    {
        using var provider = Build();
        var segment = new TranscriptSegment { Start = TimeSpan.Zero, End = TimeSpan.FromSeconds(10), Text = "Ähm, ich denke, äh, dass es geht" };

        var report = provider.GetRequiredService<ISpeechAnalyzer>().Analyze([segment], "de");

        Assert.Equal(2, report.FillerTotal);
        Assert.NotNull(provider.GetRequiredService<IVerbatimPromptProvider>().For("de"));
    }
}
