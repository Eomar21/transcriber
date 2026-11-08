using Transcriber.Maui.Services;
using Transcriber.ViewModels;

namespace Transcriber.Maui;

public partial class TranscribePage : ContentPage
{
    private const double MaxColumnWidth = 912;
    private const double SideMargin = 24;

    private readonly TranscribeViewModel _viewModel;
#if WINDOWS
    private bool _dropHooked;
#endif

    // The page is only a view: all behaviour lives in the view model.
    public TranscribePage(TranscribeViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;

#if MACCATALYST
        var drop = new DropGestureRecognizer { AllowDrop = true };
        drop.DragOver += (_, e) =>
        {
            e.AcceptedOperation = _viewModel.IsBusy ? DataPackageOperation.None : DataPackageOperation.Copy;
            _viewModel.IsDragOver = !_viewModel.IsBusy;
        };
        drop.DragLeave += (_, _) => _viewModel.IsDragOver = false;
        drop.Drop += async (_, e) =>
        {
            try { _viewModel.AddFiles(await DroppedFiles.GetPathsAsync(e)); }
            catch { _viewModel.IsDragOver = false; }
        };
        Content.GestureRecognizers.Add(drop);
#endif
    }

    // Layout only: keep the content column centred, at most 912 wide, and shrinking with the window.
    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);
        if (width > 0)
            Column.WidthRequest = Math.Max(0, Math.Min(MaxColumnWidth, width - 2 * SideMargin));
    }

#if WINDOWS
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
            _viewModel.IsDragOver = accept;
        };
        page.DragLeave += (_, _) => _viewModel.IsDragOver = false;
        page.Drop += async (_, e) =>
        {
            // Keep the drop open while the items are read asynchronously.
            var deferral = e.GetDeferral();
            try { _viewModel.AddFiles(await DroppedFiles.GetPathsAsync(e.DataView)); }
            catch { _viewModel.IsDragOver = false; }
            finally { deferral.Complete(); }
        };
    }
#endif
}
