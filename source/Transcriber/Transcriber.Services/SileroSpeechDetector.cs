using Whisper.net;

/// <summary>Finds where people are speaking in a 16 kHz mono wav using the bundled Silero model.</summary>
public interface ISpeechDetector
{
    /// <summary>True when the detection model is available next to the app.</summary>
    bool IsAvailable { get; }

    Task<List<SpeechSpan>> DetectAsync(string wavPath, CancellationToken ct = default);
}

public class SileroSpeechDetector : ISpeechDetector
{
    public const string ModelFileName = "ggml-silero-v5.1.2.bin";

    private readonly string _modelPath;

    public SileroSpeechDetector(string? modelDirectory = null)
    {
        _modelPath = Path.Combine(modelDirectory ?? Path.Combine(AppContext.BaseDirectory, "models"), ModelFileName);
    }

    public bool IsAvailable => File.Exists(_modelPath);

    public async Task<List<SpeechSpan>> DetectAsync(string wavPath, CancellationToken ct = default)
    {
        using var factory = WhisperVadFactory.FromPath(_modelPath);
        await using var processor = factory.CreateBuilder()
            .WithUseGpu(false) // the model is tiny; the CPU is quick and avoids GPU setup for a few milliseconds of work
            .Build();

        var spans = new List<SpeechSpan>();
        await using var stream = File.OpenRead(wavPath);
        foreach (var segment in await processor.DetectSpeechAsync(stream, ct))
            spans.Add(new SpeechSpan(segment.Start, segment.End));
        return spans;
    }
}
