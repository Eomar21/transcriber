using Microsoft.Extensions.Options;
using Transcriber.Services.Configuration;
using Transcriber.Services.Interfaces;
using Transcriber.Services.Models;
using Whisper.net;

namespace Transcriber.Services.Transcription;

/// <summary>Finds where people are speaking in a 16 kHz mono wav using the Silero model that is built into the app.</summary>
internal sealed class SileroSpeechDetector : ISpeechDetector
{
    private readonly string _modelPath;

    public SileroSpeechDetector(IOptions<ModelSettings> options, string? modelDirectory = null)
    {
        var settings = options.Value;
        var directory = modelDirectory ?? settings.BundledDirectory ?? Path.Combine(AppContext.BaseDirectory, "models");
        _modelPath = Path.Combine(directory, settings.SpeechDetectionModelFile);
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
