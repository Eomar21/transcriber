using SherpaOnnx;

/// <summary>Works out who speaks when in a 16 kHz mono wav.</summary>
public interface ISpeakerDiarizer
{
    /// <summary>True when the speaker models are available next to the app.</summary>
    bool IsAvailable { get; }

    /// <param name="speakerCount">How many people speak, or null to work it out from the audio.</param>
    Task<List<SpeakerTurn>> DiarizeAsync(string wavPath, int? speakerCount = null, CancellationToken ct = default);
}

/// <summary>
/// Speaker diarization with sherpa-onnx: a pyannote model finds where speech changes hands, a speaker-embedding
/// model describes each voice, and clustering groups the pieces by voice.
/// </summary>
public class SherpaSpeakerDiarizer : ISpeakerDiarizer
{
    public const string SegmentationModelFileName = "pyannote-segmentation-3.0.int8.onnx";
    public const string EmbeddingModelFileName = "3dspeaker-campplus-zh-en-16k.onnx";

    /// <summary>Clustering threshold when the number of speakers is unknown. Lower finds more speakers.</summary>
    public const float DefaultThreshold = 0.5f;

    private readonly string _segmentationPath;
    private readonly string _embeddingPath;

    public SherpaSpeakerDiarizer(string? modelDirectory = null)
    {
        var dir = modelDirectory ?? Path.Combine(AppContext.BaseDirectory, "models");
        _segmentationPath = Path.Combine(dir, SegmentationModelFileName);
        _embeddingPath = Path.Combine(dir, EmbeddingModelFileName);
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
            config.Clustering.Threshold = DefaultThreshold;
            config.MinDurationOn = 0.3f;
            config.MinDurationOff = 0.5f;

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
        using var reader = new NAudio.Wave.WaveFileReader(path);
        if (reader.WaveFormat.SampleRate != expectedRate || reader.WaveFormat.Channels != 1 || reader.WaveFormat.BitsPerSample != 16)
            throw new NotSupportedException($"Speaker detection needs {expectedRate} Hz mono 16-bit audio.");

        var bytes = new byte[reader.Length];
        var read = 0;
        while (read < bytes.Length)
        {
            var n = reader.Read(bytes, read, bytes.Length - read);
            if (n == 0) break;
            read += n;
        }

        var samples = new float[read / 2];
        for (var i = 0; i < samples.Length; i++)
            samples[i] = BitConverter.ToInt16(bytes, i * 2) / 32768f;
        return samples;
    }
}
