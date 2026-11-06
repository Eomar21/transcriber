using Transcriber.ViewModels;

namespace Transcriber.Maui;

public partial class TranscribePage : ContentPage
{
    private const double MaxColumnWidth = 912;
    private const double SideMargin = 24;

    // The page is only a view: all behaviour lives in the view model.
    public TranscribePage(TranscribeViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    // Layout only: keep the content column centred, at most 912 wide, and shrinking with the window.
    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);
        if (width > 0)
            Column.WidthRequest = Math.Max(0, Math.Min(MaxColumnWidth, width - 2 * SideMargin));
    }
}
