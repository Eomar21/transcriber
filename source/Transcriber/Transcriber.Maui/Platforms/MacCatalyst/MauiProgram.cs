using Microsoft.Extensions.DependencyInjection;
using Transcriber.Services.Extensions;

namespace Transcriber.Maui;

public static partial class MauiProgram
{
    // Speaker detection runs as the bundled sherpa-onnx program. A Mac Catalyst app cannot load the native library.
    private static partial void RegisterSpeakerDetection(IServiceCollection services) =>
        services.AddToolSpeakerDetection();

    // The native fields on macOS already share one shape.
    private static partial void ConfigureFieldShape() { }
}
