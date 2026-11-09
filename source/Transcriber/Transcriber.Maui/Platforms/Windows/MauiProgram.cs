using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Handlers;
using Transcriber.Services.Extensions;

namespace Transcriber.Maui;

public static partial class MauiProgram
{
    // Windows loads the native sherpa-onnx library inside the app.
    private static partial void RegisterSpeakerDetection(IServiceCollection services) =>
        services.AddNativeSpeakerDetection();

    // The native text box and drop-down list have different corners. This gives every field the same shape as the buttons.
    private static partial void ConfigureFieldShape()
    {
        var radius = new Microsoft.UI.Xaml.CornerRadius(8);
        EntryHandler.Mapper.AppendToMapping("FieldShape", (handler, _) => handler.PlatformView.CornerRadius = radius);
        PickerHandler.Mapper.AppendToMapping("FieldShape", (handler, _) => handler.PlatformView.CornerRadius = radius);
    }
}
