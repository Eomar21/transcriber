using CommunityToolkit.Mvvm.ComponentModel;

namespace Transcriber.ViewModels.Practice;

/// <summary>What the person sees about the work in progress: the progress bar and the message when something goes wrong.</summary>
public sealed partial class PracticeStatusViewModel(PracticeState state) : PracticeSectionViewModel(state)
{
    [ObservableProperty] private double _progress;
    [ObservableProperty] private string _statusText = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    private string _errorMessage = "";

    public bool HasError => ErrorMessage.Length > 0;
}
