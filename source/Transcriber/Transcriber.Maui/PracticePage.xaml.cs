using Transcriber.ViewModels.Practice;

namespace Transcriber.Maui;

public partial class PracticePage : ContentPage
{
    private readonly PracticeViewModel _viewModel;

    // The page is only a view: all behaviour lives in the view models of its sections.
    public PracticePage(PracticeViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
        Controls.ScrollIntoView.When(_viewModel.Status, "ShowProgress", () => _viewModel.Status.ShowProgress, Scroller, ProgressView);
        // The view model says when the result is ready: after the finished bar was seen and hidden.
        _viewModel.ResultsReady += async (_, _) => await Controls.ScrollIntoView.ToAsync(Scroller, ResultsView, ScrollToPosition.Start);
    }

    // Look for Ollama each time the tab is shown, so starting it later is picked up without restarting the app.
    protected override void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.Feedback.RefreshFeedbackModelsCommand.Execute(null);
    }
}
