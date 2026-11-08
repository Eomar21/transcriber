using Whisper.net;
using Whisper.net.LibraryLoader;

public class WhisperService : IWhisperService
{
    private readonly IModelManager _modelManager;
    private readonly ISpeechDetector? _speechDetector;

    public WhisperService(IModelManager modelManager, ISpeechDetector? speechDetector = null)
    {
        _modelManager = modelManager;
        _speechDetector = speechDetector;
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

        // Voice activity detection: only the stretches with speech go to Whisper. That is faster on recordings with
        // long pauses and stops Whisper inventing text over silence and noise.
        List<SpeechSpan>? spans = null;
        if (options.UseVad && _speechDetector is { IsAvailable: true })
        {
            progress?.Report(new TranscriptionProgress("Finding speech", 0));
            spans = SpeechPlanner.Plan(await _speechDetector.DetectAsync(audioFilePath, ct), WavDuration(audioFilePath));
            if (spans.Count == 0)
            {
                progress?.Report(new TranscriptionProgress("Done", 1));
                return [];
            }
        }

        var chunkOffset = 0.0;  // share of the work finished before the current chunk
        var chunkShare = 1.0;   // share of the work the current chunk represents

        var builder = factory.CreateBuilder()
            .WithLanguage(options.Language)
            .WithNoSpeechThreshold(options.NoSpeechThreshold)
            .WithProgressHandler(p => progress?.Report(
                new TranscriptionProgress($"Transcribing ({LastRuntime})", chunkOffset + chunkShare * p / 100.0)));

        if (!string.IsNullOrWhiteSpace(options.Prompt))
            builder = builder.WithPrompt(options.Prompt);

        await using var processor = builder.Build();

        var segments = new List<TranscriptSegment>();
        if (spans is null)
        {
            await using var fileStream = File.OpenRead(audioFilePath);
            await foreach (var result in processor.ProcessAsync(fileStream, ct))
                segments.Add(new TranscriptSegment { Start = result.Start, End = result.End, Text = result.Text });
        }
        else
        {
            var totalSpeech = spans.Sum(s => s.Length.TotalSeconds);
            var done = 0.0;
            foreach (var span in spans)
            {
                ct.ThrowIfCancellationRequested();
                chunkOffset = done / totalSpeech;
                chunkShare = span.Length.TotalSeconds / totalSpeech;

                var samples = ReadSamples(audioFilePath, span);
                await foreach (var result in processor.ProcessAsync(samples, ct))
                {
                    segments.Add(new TranscriptSegment
                    {
                        Start = span.Start + result.Start,
                        End = span.Start + result.End,
                        Text = result.Text,
                    });
                }
                done += span.Length.TotalSeconds;
            }
        }

        progress?.Report(new TranscriptionProgress("Done", 1));
        return options.FilterHallucinations ? TranscriptFilter.Clean(segments) : segments;
    }

    private static TimeSpan WavDuration(string path)
    {
        using var reader = new NAudio.Wave.WaveFileReader(path);
        return reader.TotalTime;
    }

    /// <summary>Reads one stretch of a 16 kHz mono 16-bit wav as the float samples Whisper takes.</summary>
    private static float[] ReadSamples(string path, SpeechSpan span)
    {
        using var reader = new NAudio.Wave.WaveFileReader(path);
        var format = reader.WaveFormat;
        var bytesPerSecond = format.AverageBytesPerSecond;
        var start = (long)(span.Start.TotalSeconds * bytesPerSecond) / format.BlockAlign * format.BlockAlign;
        var length = (int)(span.Length.TotalSeconds * bytesPerSecond) / format.BlockAlign * format.BlockAlign;
        length = (int)Math.Min(length, Math.Max(0, reader.Length - start));

        reader.Position = start;
        var bytes = new byte[length];
        var read = 0;
        while (read < length)
        {
            var n = reader.Read(bytes, read, length - read);
            if (n == 0) break;
            read += n;
        }

        var samples = new float[read / 2];
        for (var i = 0; i < samples.Length; i++)
            samples[i] = BitConverter.ToInt16(bytes, i * 2) / 32768f;
        return samples;
    }
}
