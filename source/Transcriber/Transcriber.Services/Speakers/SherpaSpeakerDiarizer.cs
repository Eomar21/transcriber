using Microsoft.Extensions.Options;
using NAudio.Wave;
using SherpaOnnx;
using Transcriber.Services.Configuration;
using Transcriber.Services.Exceptions;
using Transcriber.Services.Interfaces;
using Transcriber.Services.Media;
using Transcriber.Services.Models;

namespace Transcriber.Services.Speakers;

/// <summary>
/// Speaker diarization with sherpa-onnx: a pyannote model finds where speech changes hands, a speaker-embedding
/// model describes each voice, and clustering groups the pieces by voice. This is what the Windows build uses.
/// </summary>
internal sealed class SherpaSpeakerDiarizer : ISpeakerDiarizer
{
    private readonly SpeakerSettings _settings;
    private readonly string _segmentationPath;
    private readonly string _embeddingPath;

    public SherpaSpeakerDiarizer(IOptions<SpeakerSettings> speakers, IOptions<ModelSettings> models, string? modelDirectory = null)
    {
        _settings = speakers.Value;
        var dir = modelDirectory ?? models.Value.BundledDirectory ?? Path.Combine(AppContext.BaseDirectory, "models");
        _segmentationPath = Path.Combine(dir, _settings.SegmentationModelFile);
        _embeddingPath = Path.Combine(dir, _settings.EmbeddingModelFile);
    }

    public bool IsAvailable => File.Exists(_segmentationPath) && File.Exists(_embeddingPath) && NativeLibraryLoads.Value;

    // Where the native library cannot be loaded (for example a platform it was not built for) the feature switches itself off.
    private static readonly Lazy<bool> NativeLibraryLoads = new(() =>
        System.Runtime.InteropServices.NativeLibrary.TryLoad("sherpa-onnx-c-api", typeof(OfflineSpeakerDiarization).Assembly, null, out _));

    public Task<List<SpeakerTurn>> DiarizeAsync(string wavPath, int? speakerCount = null, CancellationToken ct = default) =>
        Task.Run(() =>
        {
            ct.ThrowIfCancellationRequested();

            var config = new OfflineSpeakerDiarizationConfig();
            config.Segmentation.Pyannote.Model = _segmentationPath;
            config.Segmentation.NumThreads = Math.Max(1, Environment.ProcessorCount / 2);
            config.Embedding.Model = _embeddingPath;
            config.Embedding.NumThreads = config.Segmentation.NumThreads;
            config.Clustering.NumClusters = speakerCount ?? -1;
            config.Clustering.Threshold = _settings.ClusterThreshold;
            config.MinDurationOn = _settings.MinDurationOn;
            config.MinDurationOff = _settings.MinDurationOff;

            using var diarization = new OfflineSpeakerDiarization(config);
            var samples = ReadSamples(wavPath, diarization.SampleRate);
            ct.ThrowIfCancellationRequested();

            return diarization.Process(samples)
                .Select(s => new SpeakerTurn(TimeSpan.FromSeconds(s.Start), TimeSpan.FromSeconds(s.End), s.Speaker))
                .OrderBy(t => t.Start)
                .ToList();
        }, ct);

    private static float[] ReadSamples(string path, int expectedRate)
    {
        using (var reader = new WaveFileReader(path))
        {
            if (reader.WaveFormat.SampleRate != expectedRate || reader.WaveFormat.Channels != 1 || reader.WaveFormat.BitsPerSample != 16)
                throw new SpeakerDetectionException($"Speaker detection needs {expectedRate} Hz mono 16-bit audio.");
        }

        return WavSamples.Read(path);
    }
}
