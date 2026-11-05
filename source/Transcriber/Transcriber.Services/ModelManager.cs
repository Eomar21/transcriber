using Whisper.net.Ggml;

/// <summary>A downloadable Whisper model. <see cref="Id"/> doubles as the file stem (ggml-{Id}.bin).</summary>
public record WhisperModel(string Id, string DisplayName, GgmlType Type, QuantizationType Quantization, long ApproxBytes)
{
    public string FileName => $"ggml-{Id}.bin";

    public static readonly WhisperModel Base = new("base", "Base (fast, lowest accuracy)", GgmlType.Base, QuantizationType.NoQuantization, 148_000_000);
    public static readonly WhisperModel Small = new("small", "Small (balanced)", GgmlType.Small, QuantizationType.NoQuantization, 488_000_000);
    public static readonly WhisperModel SmallQ5 = new("small-q5_1", "Small, compressed", GgmlType.Small, QuantizationType.Q5_1, 190_000_000);
    public static readonly WhisperModel MediumQ5 = new("medium-q5_0", "Medium, compressed (better)", GgmlType.Medium, QuantizationType.Q5_0, 539_000_000);
    public static readonly WhisperModel LargeV3TurboQ5 = new("large-v3-turbo-q5_0", "Large v3 turbo, compressed (best)", GgmlType.LargeV3Turbo, QuantizationType.Q5_0, 574_000_000);

    public static readonly IReadOnlyList<WhisperModel> All = [Base, Small, SmallQ5, MediumQ5, LargeV3TurboQ5];
}

public record TranscriptionProgress(string Stage, double Fraction);

public interface IModelManager
{
    string ModelsDirectory { get; }
    bool IsDownloaded(WhisperModel model);
    Task<string> EnsureModelAsync(WhisperModel model, IProgress<TranscriptionProgress>? progress = null, CancellationToken ct = default);
}

/// <summary>
/// Stores models in app data (not the working directory) and downloads them atomically:
/// bytes go to a .part file that is renamed only when complete, so an interrupted download
/// is never mistaken for a valid model.
/// </summary>
public class ModelManager : IModelManager
{
    public string ModelsDirectory { get; }

    public ModelManager(string? modelsDirectory = null)
    {
        ModelsDirectory = modelsDirectory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Transcriber", "models");
    }

    public bool IsDownloaded(WhisperModel model) => File.Exists(Path.Combine(ModelsDirectory, model.FileName));

    public async Task<string> EnsureModelAsync(WhisperModel model, IProgress<TranscriptionProgress>? progress = null, CancellationToken ct = default)
    {
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
