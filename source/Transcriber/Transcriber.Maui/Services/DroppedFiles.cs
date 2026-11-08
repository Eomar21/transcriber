#if WINDOWS
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
#elif MACCATALYST
using Foundation;
using UIKit;
#endif

namespace Transcriber.Maui.Services;

/// <summary>Reads the file paths out of a drag and drop, which the platforms only expose through their own types.</summary>
public static class DroppedFiles
{
#if WINDOWS
    public static bool HasFiles(DataPackageView view) => view.Contains(StandardDataFormats.StorageItems);

    public static async Task<IReadOnlyList<string>> GetPathsAsync(DataPackageView view)
    {
        if (!HasFiles(view)) return [];
        var items = await view.GetStorageItemsAsync();
        return items.OfType<IStorageFile>().Select(f => f.Path).ToList();
    }
#elif MACCATALYST
    public static async Task<IReadOnlyList<string>> GetPathsAsync(DropEventArgs e)
    {
        var session = e.PlatformArgs?.DropSession;
        if (session is null) return [];

        var pending = session.Items.Select(item =>
        {
            var source = new TaskCompletionSource<string?>();
            item.ItemProvider.LoadItem("public.file-url", null, (obj, _) =>
                source.TrySetResult((obj as NSUrl)?.Path));
            return source.Task;
        }).ToList();

        var paths = await Task.WhenAll(pending);
        return paths.Where(p => !string.IsNullOrEmpty(p)).Select(p => p!).ToList();
    }
#endif
}
