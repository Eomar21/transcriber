using Microsoft.Extensions.Logging;

namespace Transcriber.Maui
{
    public static class MauiProgram
    {
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

            builder.Services.AddSingleton<IWhisperService, WhisperService>();
            builder.Services.AddSingleton<IVideoReaderService, VideoReaderService>();
            builder.Services.AddSingleton<IAudioService, AudioService>();
            builder.Services.AddSingleton<IFileReaderService, FileReaderService>();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
