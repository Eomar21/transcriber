using Transcriber.Maui.Services;

namespace Transcriber.Maui;

// Windows file drops. The shared page code is in TranscribePage.xaml.cs.
public partial class TranscribePage
{
    private bool _dropHooked;

    // Windows file drags only reach the native WinUI events, so hook them on the page itself.
    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();
        if (_dropHooked || Handler?.PlatformView is not Microsoft.UI.Xaml.UIElement page) return;
        _dropHooked = true;

        page.AllowDrop = true;
        page.DragOver += (_, e) =>
        {
            // Accept any drag while idle; the drop itself keeps only supported media files.
            var accept = !_viewModel.IsBusy;
            e.AcceptedOperation = accept ? Windows.ApplicationModel.DataTransfer.DataPackageOperation.Copy
                                         : Windows.ApplicationModel.DataTransfer.DataPackageOperation.None;
            if (accept) e.DragUIOverride.Caption = "Add to transcribe";
            _viewModel.Files.IsDragOver = accept;
        };
        page.DragLeave += (_, _) => _viewModel.Files.IsDragOver = false;
        page.Drop += async (_, e) =>
        {
            // Keep the drop open while the items are read asynchronously.
            var deferral = e.GetDeferral();
            try { _viewModel.Files.AddFiles(await DroppedFiles.GetPathsAsync(e.DataView)); }
            catch { _viewModel.Files.IsDragOver = false; }
            finally { deferral.Complete(); }
        };
    }
}
