using Transcriber.ViewModels;
using Transcriber.Maui.Services;
using Transcriber.ViewModels.Transcribe;

namespace Transcriber.Maui;

public partial class TranscribePage : ContentPage
{
    private readonly TranscribeViewModel _viewModel;

    // The page is only a view: all behaviour lives in the view model.
    public TranscribePage(TranscribeViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;

        Controls.ScrollIntoView.When(_viewModel.Status, "ShowProgress", () => _viewModel.Status.ShowProgress, Scroller, ProgressView);
        // The view model says when the transcript is ready: after the finished bar was seen and hidden.
        _viewModel.Status.ResultsReady += async (_, _) => await Controls.ScrollIntoView.ToAsync(Scroller, ResultsCard, ScrollToPosition.Start);
        ConfigureFileDrop();
    }

    // Dropping files on the page is native on each platform: see Platforms/Windows and Platforms/MacCatalyst.
    partial void ConfigureFileDrop();
}
