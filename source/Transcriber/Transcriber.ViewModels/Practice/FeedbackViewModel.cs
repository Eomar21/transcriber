using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Transcriber.Coach.Exceptions;
using Transcriber.Coach.Interfaces;

namespace Transcriber.ViewModels.Practice;

/// <summary>Written feedback from a local Ollama model. It is optional and shows only when Ollama runs with a model.</summary>
public sealed partial class FeedbackViewModel : PracticeSectionViewModel
{
    private readonly ICoachFeedback _feedback;
    private readonly AnalysisViewModel _analysis;

    public FeedbackViewModel(ICoachFeedback feedback, AnalysisViewModel analysis, PracticeState state) : base(state)
    {
        _feedback = feedback;
        _analysis = analysis;
        // The button depends on a result being there.
        _analysis.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(AnalysisViewModel.HasResult)) GetFeedbackCommand.NotifyCanExecuteChanged();
        };
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanAskForFeedback))]
    [NotifyCanExecuteChangedFor(nameof(GetFeedbackCommand))]
    private IReadOnlyList<string> _feedbackModels = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(GetFeedbackCommand))]
    private string? _selectedFeedbackModel;

    [ObservableProperty] private string _feedbackText = "";
    [ObservableProperty] private string _feedbackError = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanAskForFeedback))]
    private bool _isGettingFeedback;

    /// <summary>True when Ollama is running with at least one model, so the option is only shown when it can work.</summary>
    public bool CanAskForFeedback => FeedbackModels.Count > 0;

    /// <summary>Looks for Ollama on this computer. Call when the tab is shown; it quietly finds nothing if Ollama is not running.</summary>
    [RelayCommand]
    private async Task RefreshFeedbackModelsAsync()
    {
        FeedbackModels = await _feedback.ListModelsAsync();
        if (SelectedFeedbackModel is null || !FeedbackModels.Contains(SelectedFeedbackModel))
            SelectedFeedbackModel = FeedbackModels.FirstOrDefault();
    }

    /// <summary>Removes the feedback of an earlier result.</summary>
    public void Clear()
    {
        FeedbackText = "";
        FeedbackError = "";
    }

    protected override void OnStateChanged() => GetFeedbackCommand.NotifyCanExecuteChanged();

    private bool CanGetFeedback() => _analysis.HasResult && !IsBusy && !IsGettingFeedback && SelectedFeedbackModel is not null;

    [RelayCommand(CanExecute = nameof(CanGetFeedback))]
    private async Task GetFeedbackAsync()
    {
        if (_analysis.Report is null || SelectedFeedbackModel is null) return;

        IsGettingFeedback = true;
        GetFeedbackCommand.NotifyCanExecuteChanged();
        FeedbackText = "";
        FeedbackError = "";
        try
        {
            FeedbackText = await _feedback.GetFeedbackAsync(SelectedFeedbackModel, _analysis.Transcript, _analysis.Report);
        }
        catch (OllamaException e)
        {
            FeedbackError = e.Message;
        }
        finally
        {
            IsGettingFeedback = false;
            GetFeedbackCommand.NotifyCanExecuteChanged();
        }
    }
}
