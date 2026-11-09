using Foundation;
using UIKit;

namespace Transcriber.Maui.Services;

/// <summary>Reads the file paths out of a drag and drop, which macOS only exposes through its own types.</summary>
public static class DroppedFiles
{
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
}
