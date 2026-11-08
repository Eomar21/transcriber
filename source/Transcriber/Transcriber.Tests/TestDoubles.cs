using Transcriber.ViewModels;

/// <summary>Model manager for view model tests: everything counts as installed unless a test says otherwise.</summary>
public sealed class FakeModels : IModelManager
{
    public string ModelsDirectory => "models";
    public HashSet<string> Missing { get; } = [];
    public bool IsBundled(WhisperModel model) => !Missing.Contains(model.Id);
    public bool IsDownloaded(WhisperModel model) => !Missing.Contains(model.Id);
    public Task<string> EnsureModelAsync(WhisperModel model, IProgress<TranscriptionProgress>? progress = null, CancellationToken ct = default) =>
        Task.FromResult(model.FileName);
}

public sealed class FakeDialogs : IDialogService
{
    public bool Answer { get; set; } = true;
    public List<string> Messages { get; } = [];

    public Task<bool> ConfirmAsync(string title, string message, string accept, string cancel)
    {
        Messages.Add(message);
        return Task.FromResult(Answer);
    }
}
