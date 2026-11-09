using Microsoft.Extensions.DependencyInjection;
using Transcriber.ViewModels.Practice;
using Transcriber.ViewModels.Progress;
using Transcriber.ViewModels.Transcribe;

namespace Transcriber.ViewModels.Extensions;

public static class ViewModelServiceCollectionExtensions
{
    /// <summary>
    /// Registers the Transcribe screen and its sections. The screen and the sections are singletons,
    /// so the screen keeps its state while the person switches tabs.
    /// </summary>
    public static IServiceCollection AddTranscribeScreen(this IServiceCollection services)
    {
        services.AddSingleton<IProgressLookPicker>(_ => new ProgressLookPicker(ProgressLooks.All));
        services.AddSingleton<RunState>();
        services.AddSingleton<FilesViewModel>();
        services.AddSingleton<OptionsViewModel>();
        services.AddSingleton<IModelSuggestion>(p => p.GetRequiredService<OptionsViewModel>());
        services.AddSingleton<RunStatusViewModel>();
        services.AddSingleton<SavedTranscriptsViewModel>();
        services.AddSingleton<PlaybackViewModel>();
        services.AddSingleton<ResultsViewModel>();
        services.AddSingleton<TranscribeViewModel>();
        return services;
    }

    /// <summary>
    /// Registers the Practice tab and its sections. The tab and the sections are singletons,
    /// so the tab keeps its state while the person switches tabs.
    /// </summary>
    public static IServiceCollection AddPracticeScreen(this IServiceCollection services)
    {
        services.AddSingleton<PracticeState>();
        services.AddSingleton<MicrophoneViewModel>();
        services.AddSingleton<SourceViewModel>();
        services.AddSingleton<PracticeStatusViewModel>();
        services.AddSingleton<AnalysisViewModel>();
        services.AddSingleton<FeedbackViewModel>();
        services.AddSingleton<HistoryViewModel>();
        services.AddSingleton<PracticeViewModel>();
        return services;
    }
}
