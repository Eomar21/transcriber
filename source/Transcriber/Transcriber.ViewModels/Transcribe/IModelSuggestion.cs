using CommunityToolkit.Mvvm.Input;

namespace Transcriber.ViewModels.Transcribe;

/// <summary>
/// The offer to switch to a more accurate model. The options section owns the choice of model.
/// The result section shows the offer as a button, and it knows only this interface.
/// </summary>
public interface IModelSuggestion
{
    string UseSuggestedModelText { get; }

    IRelayCommand UseSuggestedModelCommand { get; }
}
