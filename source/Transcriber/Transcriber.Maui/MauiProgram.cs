using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Transcriber.Coach.Extensions;
using Transcriber.Services.Extensions;
using Transcriber.Maui.Services;
using Transcriber.ViewModels;

namespace Transcriber.Maui
{
    public static partial class MauiProgram
    {
        // Speaker detection runs differently on each platform: see Platforms/Windows and Platforms/MacCatalyst.
        private static partial void RegisterSpeakerDetection(IServiceCollection services);

        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            // Core services: reading media, transcribing, saving transcripts. The settings are in the services project.
            builder.Services.AddTranscriberServices();
            RegisterSpeakerDetection(builder.Services);

            // Platform services the view models depend on through interfaces.
            builder.Services.AddSingleton<IFilePickerService, MauiFilePickerService>();
            builder.Services.AddSingleton<IClipboardService, MauiClipboardService>();
            builder.Services.AddSingleton<IDialogService, MauiDialogService>();
            builder.Services.AddSingleton<IAudioPlayer, MauiAudioPlayer>();
            builder.Services.AddSingleton<IAudioRecorder, MauiAudioRecorder>();

            // View models are singletons so a screen keeps its state while you switch tabs.
            builder.Services.AddSingleton<TranscribeViewModel>();
            builder.Services.AddTransient<TranscribePage>();

            // Practice tab: history stays in the user's app data; written feedback is optional and local (Ollama).
            builder.Services.AddCoach();
            builder.Services.AddSingleton<PracticeViewModel>();
            builder.Services.AddTransient<PracticePage>();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
