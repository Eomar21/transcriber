using Whisper.net;
using Whisper.net.LibraryLoader;

public class WhisperService : IWhisperService
{
    private readonly IModelManager _modelManager;

    public WhisperService(IModelManager modelManager)
    {
        _modelManager = modelManager;
    }

    /// <summary>The native runtime that was actually loaded for the last run, e.g. "Vulkan" or "Cpu".</summary>
    public string? LastRuntime { get; private set; }

    public async Task<List<TranscriptSegment>> TranscribeAsync(
        string audioFilePath,
        TranscriptionOptions? options = null,
        IProgress<TranscriptionProgress>? progress = null,
        CancellationToken ct = default)
    {
        options ??= new TranscriptionOptions();

        // Don't download or load a model (and pin a native runtime) for a run that is already cancelled.
        ct.ThrowIfCancellationRequested();

        var modelPath = await _modelManager.EnsureModelAsync(options.Model, progress, ct);

        // Prefer the GPU, fall back to the CPU if no compatible runtime or driver is present.
        RuntimeOptions.RuntimeLibraryOrder = options.UseGpu
            ? [RuntimeLibrary.Vulkan, RuntimeLibrary.Cpu]
            : [RuntimeLibrary.Cpu];

        using var factory = WhisperFactory.FromPath(modelPath);
        LastRuntime = RuntimeOptions.LoadedLibrary?.ToString();

        var builder = factory.CreateBuilder()
            .WithLanguage(options.Language)
            .WithNoSpeechThreshold(options.NoSpeechThreshold)
            .WithProgressHandler(p => progress?.Report(new TranscriptionProgress($"Transcribing ({LastRuntime})", p / 100.0)));

        if (!string.IsNullOrWhiteSpace(options.Prompt))
            builder = builder.WithPrompt(options.Prompt);

        await using var processor = builder.Build();

        var segments = new List<TranscriptSegment>();
        await using var fileStream = File.OpenRead(audioFilePath);
        await foreach (var result in processor.ProcessAsync(fileStream, ct))
        {
            segments.Add(new TranscriptSegment { Start = result.Start, End = result.End, Text = result.Text });
        }

        progress?.Report(new TranscriptionProgress("Done", 1));
        return options.FilterHallucinations ? TranscriptFilter.Clean(segments) : segments;
    }
}
