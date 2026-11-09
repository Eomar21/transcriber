using Microsoft.Extensions.Options;
using Transcriber.Services.Configuration;
using Transcriber.Services.Interfaces;
using Transcriber.Services.Models;
using Whisper.net.Ggml;

namespace Transcriber.Services.Transcription;

/// <summary>
/// Stores models in app data (not the working directory) and downloads them atomically:
/// bytes go to a .part file that is renamed only when complete, so an interrupted download
/// is never mistaken for a valid model.
/// </summary>
internal sealed class ModelManager : IModelManager
{
    public string ModelsDirectory { get; }

    /// <summary>Read-only folder inside the app that holds models shipped with it (no download needed).</summary>
    public string BundledDirectory { get; }

    /// <param name="modelsDirectory">Overrides where downloaded models are kept.</param>
    /// <param name="bundledDirectory">Overrides where the models that are built into the app are.</param>
    public ModelManager(IOptions<ModelSettings> options, string? modelsDirectory = null, string? bundledDirectory = null)
    {
        var settings = options.Value;
        ModelsDirectory = modelsDirectory ?? settings.Directory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Transcriber", "models");
        BundledDirectory = bundledDirectory ?? settings.BundledDirectory ?? Path.Combine(AppContext.BaseDirectory, "models");
    }

    private string BundledPath(WhisperModel model) => Path.Combine(BundledDirectory, model.FileName);

    public bool IsBundled(WhisperModel model) => File.Exists(BundledPath(model));

    public bool IsDownloaded(WhisperModel model) =>
        IsBundled(model) || File.Exists(Path.Combine(ModelsDirectory, model.FileName));

    public async Task<string> EnsureModelAsync(WhisperModel model, IProgress<TranscriptionProgress>? progress = null, CancellationToken ct = default)
    {
        // A model shipped with the app always wins: works offline, no download, nothing to install.
        if (IsBundled(model)) return BundledPath(model);

        Directory.CreateDirectory(ModelsDirectory);
        var finalPath = Path.Combine(ModelsDirectory, model.FileName);
        if (File.Exists(finalPath)) return finalPath;

        var partPath = finalPath + ".part";
        try
        {
            using var source = await WhisperGgmlDownloader.Default.GetGgmlModelAsync(model.Type, model.Quantization, ct);
            await using (var target = new FileStream(partPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, useAsync: true))
            {
                var buffer = new byte[81920];
                long total = 0;
                int read;
                while ((read = await source.ReadAsync(buffer, ct)) > 0)
                {
                    await target.WriteAsync(buffer.AsMemory(0, read), ct);
                    total += read;
                    // The size is approximate (the stream has no length), so never report 100% early.
                    progress?.Report(new TranscriptionProgress($"Downloading {model.DisplayName}",
                        Math.Min(0.99, (double)total / model.ApproxBytes)));
                }
            }
            File.Move(partPath, finalPath, overwrite: true);
            progress?.Report(new TranscriptionProgress($"Downloading {model.DisplayName}", 1));
            return finalPath;
        }
        catch
        {
            // Cancelled or failed: leave no half-written file behind.
            try { File.Delete(partPath); } catch { /* best effort */ }
            throw;
        }
    }
}
