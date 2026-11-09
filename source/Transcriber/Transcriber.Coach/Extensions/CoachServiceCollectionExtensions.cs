using Microsoft.Extensions.DependencyInjection;
using Transcriber.Coach.Advice;
using Transcriber.Coach.Advice.Rules;
using Transcriber.Coach.Analysis;
using Transcriber.Coach.Feedback;
using Transcriber.Coach.History;
using Transcriber.Coach.Interfaces;
using Transcriber.Coach.Languages;

namespace Transcriber.Coach.Extensions;

public static class CoachServiceCollectionExtensions
{
    /// <summary>Registers everything the Practice tab needs from the coach.</summary>
    /// <param name="historyPath">Where the practice history is saved. Null uses the app data folder.</param>
    /// <param name="ollama">Where Ollama listens. Null uses this computer.</param>
    public static IServiceCollection AddCoach(this IServiceCollection services, string? historyPath = null, OllamaOptions? ollama = null)
    {
        // Languages
        services.AddSingleton<ILanguageProfileSource, EmbeddedLanguageProfileSource>();
        services.AddSingleton<ILanguageProfileProvider, LanguageProfileRegistry>();
        services.AddSingleton<IVerbatimPromptProvider, ProfileVerbatimPromptProvider>();

        // Analysis
        services.AddSingleton<ITextTokenizer, WordTokenizer>();
        services.AddSingleton<IFillerDetector, FillerDetector>();
        services.AddSingleton<IRepeatDetector, RepeatDetector>();
        services.AddSingleton<IPauseDetector, PauseDetector>();
        services.AddSingleton<ISpeechAnalyzer, SpeechAnalyzer>();

        // Advice: the order of the rules is the order of the tips.
        services.AddSingleton<IAdviceRule, PaceRule>();
        services.AddSingleton<IAdviceRule, FillerRule>();
        services.AddSingleton<IAdviceRule, PauseRule>();
        services.AddSingleton<IAdviceRule, FluencyRule>();
        services.AddSingleton<IAdviceRule, ProgressRule>();
        services.AddSingleton<ICoachAdvisor, CoachAdvisor>();

        // Written feedback from a local model, and saved sessions.
        var options = ollama ?? new OllamaOptions();
        services.AddSingleton<IFeedbackPromptBuilder, OllamaPromptBuilder>();
        services.AddSingleton<ICoachFeedback>(sp => new OllamaCoach(
            new HttpClient { Timeout = options.Timeout }, sp.GetRequiredService<IFeedbackPromptBuilder>(), options));
        services.AddSingleton<IPracticeHistory>(_ => new JsonPracticeHistory(historyPath ?? JsonPracticeHistory.DefaultPath));

        return services;
    }
}
