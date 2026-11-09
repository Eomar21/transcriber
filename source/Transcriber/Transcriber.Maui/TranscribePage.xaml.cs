using Transcriber.Maui.Services;
using Transcriber.ViewModels.Transcribe;

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

        Controls.ScrollToProgress.When(_viewModel.Status, () => _viewModel.Status.ShowProgress, Scroller, ProgressView);
        ConfigureFileDrop();
    }

    // Layout only: keep the content column centred, at most 912 wide, and shrinking with the window.
    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);
        if (width > 0)
            Column.WidthRequest = Math.Max(0, Math.Min(MaxColumnWidth, width - 2 * SideMargin));
    }

    // Dropping files on the page is native on each platform: see Platforms/Windows and Platforms/MacCatalyst.
    partial void ConfigureFileDrop();
}
