namespace Transcriber.ViewModels.Progress;

/// <summary>Chooses the look of the progress bar for a run.</summary>
public interface IProgressLookPicker
{
    /// <summary>Returns a look chosen at random. When there is more than one look, it is not the look of the last call.</summary>
    ProgressLook Pick();
}
