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

/// <summary>Audio player for view model tests: records what was played and lets a test move the clock.</summary>
public sealed class FakePlayer : IAudioPlayer
{
    public string? PlayedPath { get; private set; }
    public TimeSpan PlayedFrom { get; private set; }
    public bool IsPlaying { get; private set; }
    public TimeSpan Position { get; set; }
    public event EventHandler? StateChanged;

    public void Play(string path, TimeSpan from) { PlayedPath = path; PlayedFrom = from; Position = from; IsPlaying = true; }
    public void Pause() => IsPlaying = false;
    public void Resume() => IsPlaying = true;
    public void Stop() { IsPlaying = false; PlayedPath = null; }

    /// <summary>Simulates the player's periodic update while playing.</summary>
    public void Tick(TimeSpan position) { Position = position; StateChanged?.Invoke(this, EventArgs.Empty); }
}
