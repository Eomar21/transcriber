#if WINDOWS
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
#elif MACCATALYST
using Foundation;
using UIKit;
#endif

namespace Transcriber.Maui.Services;

/// <summary>Reads the file paths out of a drag and drop, which MAUI only exposes through platform types.</summary>
public static class DroppedFiles
{
    public static async Task<IReadOnlyList<string>> GetPathsAsync(DropEventArgs e)
    {
#if WINDOWS
        var args = e.PlatformArgs?.DragEventArgs;
        if (args is null || !args.DataView.Contains(StandardDataFormats.StorageItems)) return [];

        // Keep the drop open while the items are read asynchronously.
        var deferral = args.GetDeferral();
        try
        {
            var items = await args.DataView.GetStorageItemsAsync();
            return items.OfType<IStorageFile>().Select(f => f.Path).ToList();
        }
        finally
        {
            deferral.Complete();
        }
#elif MACCATALYST
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
#else
        await Task.CompletedTask;
        return [];
#endif
    }
}
