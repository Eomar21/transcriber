using Microsoft.Extensions.DependencyInjection;
using Transcriber.Services.Extensions;

namespace Transcriber.Maui;

public static partial class MauiProgram
{
    // Mac Catalyst cannot load the native sherpa-onnx library, so run its bundled command-line tool instead.
    private static partial void RegisterSpeakerDetection(IServiceCollection services) =>
        services.AddToolSpeakerDetection();

    // The native fields on macOS already share one shape.
    private static partial void ConfigureFieldShape() { }
}
