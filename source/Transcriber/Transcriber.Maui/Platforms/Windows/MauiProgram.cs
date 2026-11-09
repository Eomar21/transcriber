using Microsoft.Extensions.DependencyInjection;
using Transcriber.Services.Extensions;

namespace Transcriber.Maui;

public static partial class MauiProgram
{
    // Windows loads the native sherpa-onnx library inside the app.
    private static partial void RegisterSpeakerDetection(IServiceCollection services) =>
        services.AddNativeSpeakerDetection();
}
