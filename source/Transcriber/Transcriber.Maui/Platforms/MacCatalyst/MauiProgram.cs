using Microsoft.Extensions.DependencyInjection;

namespace Transcriber.Maui;

public static partial class MauiProgram
{
    // Mac Catalyst cannot load the native sherpa-onnx library, so run its bundled command-line tool instead.
    private static partial void RegisterSpeakerDiarizer(IServiceCollection services) =>
        services.AddSingleton<ISpeakerDiarizer, SherpaCliSpeakerDiarizer>();
}
