using Microsoft.Extensions.DependencyInjection;

namespace Transcriber.Maui;

public static partial class MauiProgram
{
    // Windows loads the native sherpa-onnx library inside the app.
    private static partial void RegisterSpeakerDiarizer(IServiceCollection services) =>
        services.AddSingleton<ISpeakerDiarizer, SherpaSpeakerDiarizer>();
}
