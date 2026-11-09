using WinDataPackageView = Windows.ApplicationModel.DataTransfer.DataPackageView;
using StandardDataFormats = Windows.ApplicationModel.DataTransfer.StandardDataFormats;
using Windows.Storage;

namespace Transcriber.Maui.Services;

/// <summary>Reads the file paths out of a drag and drop, which Windows only exposes through its own types.</summary>
public static class DroppedFiles
{
    public static bool HasFiles(WinDataPackageView view) => view.Contains(StandardDataFormats.StorageItems);

    public static async Task<IReadOnlyList<string>> GetPathsAsync(WinDataPackageView view)
    {
        if (!HasFiles(view)) return [];
        var items = await view.GetStorageItemsAsync();
        return items.OfType<IStorageFile>().Select(f => f.Path).ToList();
    }
}
