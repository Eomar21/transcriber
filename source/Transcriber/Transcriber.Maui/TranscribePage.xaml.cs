using Transcriber.Maui.Services;
using Transcriber.ViewModels;

namespace Transcriber.Maui;

public partial class TranscribePage : ContentPage
{
    private const double MaxColumnWidth = 912;
    private const double SideMargin = 24;

    private readonly TranscribeViewModel _viewModel;

    // The page is only a view: all behaviour lives in the view model.
    public TranscribePage(TranscribeViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    // Layout only: keep the content column centred, at most 912 wide, and shrinking with the window.
    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);
        if (width > 0)
            Column.WidthRequest = Math.Max(0, Math.Min(MaxColumnWidth, width - 2 * SideMargin));
    }

    // Drag and drop: the platform specifics stay in DroppedFiles, the rules live in the view model.
    private void OnDragOver(object? sender, DragEventArgs e)
    {
        e.AcceptedOperation = _viewModel.IsBusy ? DataPackageOperation.None : DataPackageOperation.Copy;
        _viewModel.IsDragOver = !_viewModel.IsBusy;
    }

    private void OnDragLeave(object? sender, DragEventArgs e) => _viewModel.IsDragOver = false;

    private async void OnDrop(object? sender, DropEventArgs e)
    {
        try
        {
            _viewModel.AddFiles(await DroppedFiles.GetPathsAsync(e));
        }
        catch
        {
            _viewModel.IsDragOver = false;
        }
    }
}
