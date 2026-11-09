using Transcriber.ViewModels.Practice;

namespace Transcriber.Maui;

public partial class PracticePage : ContentPage
{
    private const double MaxColumnWidth = 912;
    private const double SideMargin = 24;

    private readonly PracticeViewModel _viewModel;

    // The page is only a view: all behaviour lives in the view models of its sections.
    public PracticePage(PracticeViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    // Look for Ollama each time the tab is shown, so starting it later is picked up without restarting the app.
    protected override void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.Feedback.RefreshFeedbackModelsCommand.Execute(null);
    }

    // Layout only: keep the content column centred and no wider than 912.
    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);
        if (width > 0)
            Column.WidthRequest = Math.Max(0, Math.Min(MaxColumnWidth, width - 2 * SideMargin));
    }
}
