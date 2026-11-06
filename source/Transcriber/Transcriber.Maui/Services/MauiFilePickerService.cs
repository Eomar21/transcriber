using Transcriber.ViewModels;

namespace Transcriber.Maui.Services;

/// <summary>Native file dialog on Windows and macOS.</summary>
public class MauiFilePickerService : IFilePickerService
{
    public async Task<IReadOnlyList<string>> PickMediaFilesAsync()
    {
        var extensions = FileReaderService.SupportedExtensions.ToArray();
        var fileType = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
        {
            // WinUI wants ".mp3"; macOS wants bare extensions or UTIs.
            { DevicePlatform.WinUI, extensions },
            { DevicePlatform.MacCatalyst, extensions.Select(x => x.TrimStart('.')).ToArray() },
        });

        var result = await FilePicker.Default.PickMultipleAsync(new PickOptions
        {
            PickerTitle = "Select audio or video files",
            FileTypes = fileType,
        });

        return result?.Select(f => f.FullPath).ToList() ?? [];
    }
}
