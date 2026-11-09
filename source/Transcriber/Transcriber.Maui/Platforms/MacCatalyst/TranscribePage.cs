using Transcriber.Maui.Services;

namespace Transcriber.Maui;

// macOS file drops. The shared page code is in TranscribePage.xaml.cs.
public partial class TranscribePage
{
    partial void ConfigureFileDrop()
    {
        var drop = new DropGestureRecognizer { AllowDrop = true };
        drop.DragOver += (_, e) =>
        {
            e.AcceptedOperation = _viewModel.IsBusy ? DataPackageOperation.None : DataPackageOperation.Copy;
            _viewModel.Files.IsDragOver = !_viewModel.IsBusy;
        };
        drop.DragLeave += (_, _) => _viewModel.Files.IsDragOver = false;
        drop.Drop += async (_, e) =>
        {
            try { _viewModel.Files.AddFiles(await DroppedFiles.GetPathsAsync(e)); }
            catch { _viewModel.Files.IsDragOver = false; }
        };
        Content.GestureRecognizers.Add(drop);
    }
}
