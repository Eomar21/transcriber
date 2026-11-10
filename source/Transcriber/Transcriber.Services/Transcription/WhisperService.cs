using Microsoft.Extensions.Options;
using NAudio.Wave;
using Transcriber.Services.Configuration;
using Transcriber.Services.Interfaces;
using Transcriber.Services.Media;
using Transcriber.Services.Models;
using Whisper.net;
using Whisper.net.LibraryLoader;

namespace Transcriber.Services.Transcription;

/// <summary>
/// Transcribes a recording with Whisper. It first finds the speech (so silence is never transcribed), transcribes
/// each piece, cleans the result, and then labels the speakers when that is asked for.
/// </summary>
internal sealed class WhisperService : IWhisperService, IDisposable
{
    // Both tabs share one engine, and two runs at once would fight over the GPU and the loaded native library.
    private static readonly SemaphoreSlim OneRunAtATime = new(1, 1);

    private readonly IModelManager _modelManager;
    private readonly IModelCatalog _models;
    private readonly ISpeechPlanner _planner;
    private readonly ISpeechCoverageCalculator _coverage;
    private readonly ITranscriptFilter _filter;
    private readonly ISpeakerAssigner _speakerAssigner;
    private readonly TranscriptionSettings _settings;
    private readonly ISpeechDetector? _speechDetector;
    private readonly ISpeakerDiarizer? _speakerDiarizer;
    private readonly LoadedModelCache<(string Path, bool UseGpu), WhisperFactory> _loadedModel;

    public WhisperService(
        IModelManager modelManager,
        IModelCatalog models,
        ISpeechPlanner planner,
        ISpeechCoverageCalculator coverage,
        ITranscriptFilter filter,
        ISpeakerAssigner speakerAssigner,
        IOptions<TranscriptionSettings> settings,
        ISpeechDetector? speechDetector = null,
        ISpeakerDiarizer? speakerDiarizer = null)
    {
        _modelManager = modelManager;
        _models = models;
        _planner = planner;
        _coverage = coverage;
        _filter = filter;
        _speakerAssigner = speakerAssigner;
        _settings = settings.Value;
        _speechDetector = speechDetector;
        _speakerDiarizer = speakerDiarizer;
        _loadedModel = new LoadedModelCache<(string Path, bool UseGpu), WhisperFactory>(
            key => WhisperFactory.FromPath(key.Path, new WhisperFactoryOptions { UseGpu = key.UseGpu }),
            TimeSpan.FromMinutes(_settings.KeepModelLoadedMinutes));
    }

    /// <summary>The native runtime that was actually loaded for the last run, e.g. "Vulkan" or "Cpu".</summary>
    public string? LastRuntime { get; private set; }

    public SpeechCoverage? LastCoverage { get; private set; }

    public bool CanIdentifySpeakers => _speakerDiarizer is { IsAvailable: true };

    /// <summary>Releases the model that is kept in memory. The app does this when it closes.</summary>
    public void Dispose() => _loadedModel.Dispose();

    public async Task<List<TranscriptSegment>> TranscribeAsync(
        string audioFilePath,
        TranscriptionOptions? options = null,
        IProgress<TranscriptionProgress>? progress = null,
        CancellationToken ct = default)
    {
        await OneRunAtATime.WaitAsync(ct);
        // The model load, the audio read and the speech search are blocking work. They run on a worker thread, so the
        // caller (the window) stays free to draw and to take a cancel.
        try { return await Task.Run(() => TranscribeCoreAsync(audioFilePath, options, progress, ct), ct); }
        finally { OneRunAtATime.Release(); }
    }

    private async Task<List<TranscriptSegment>> TranscribeCoreAsync(
        string audioFilePath,
        TranscriptionOptions? options,
        IProgress<TranscriptionProgress>? progress,
        CancellationToken ct)
    {
        options ??= new TranscriptionOptions();
        LastCoverage = null;

        // Don't download or load a model (and pin a native runtime) for a run that is already cancelled.
        ct.ThrowIfCancellationRequested();

        var modelPath = await _modelManager.EnsureModelAsync(options.Model ?? _models.Default, progress, ct);

        // The native library loads one time for the life of the app, so its choice cannot follow the option of each
        // run. The library with graphics card support is always preferred (the plain one is the fallback when no
        // compatible driver is present), and each run says for itself whether it uses the graphics card.
        RuntimeOptions.RuntimeLibraryOrder = [RuntimeLibrary.Vulkan, RuntimeLibrary.Cpu];

        // The model of the last run stays in memory for some time. The next file of a batch, or the next run with the
        // same model, starts at once. Another model, or another choice for the graphics card, loads again.
        progress?.Report(new TranscriptionProgress("Loading the model", 0));
        var factory = _loadedModel.Take((modelPath, options.UseGpu));
        LastRuntime = (options.UseGpu ? RuntimeOptions.LoadedLibrary : RuntimeLibrary.Cpu)?.ToString();
        try { return await TranscribeWithModelAsync(factory, audioFilePath, options, progress, ct); }
        finally { _loadedModel.Return(); }
    }

    private async Task<List<TranscriptSegment>> TranscribeWithModelAsync(
        WhisperFactory factory,
        string audioFilePath,
        TranscriptionOptions options,
        IProgress<TranscriptionProgress>? progress,
        CancellationToken ct)
    {

        // Voice activity detection: only the stretches with speech go to Whisper. That is faster on recordings with
        // long pauses and stops Whisper inventing text over silence and noise.
        List<SpeechSpan>? spans = null;
        if (options.UseVad && _speechDetector is { IsAvailable: true })
        {
            progress?.Report(new TranscriptionProgress("Finding speech", 0));
            spans = _planner.Plan(await _speechDetector.DetectAsync(audioFilePath, ct), WavDuration(audioFilePath));
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
            .WithNoSpeechThreshold(options.NoSpeechThreshold ?? _settings.NoSpeechThreshold)
            .WithProgressHandler(p => progress?.Report(
                new TranscriptionProgress($"Transcribing ({LastRuntime})", chunkOffset + chunkShare * p / 100.0)));

        if (!string.IsNullOrWhiteSpace(options.Prompt))
            builder = builder.WithPrompt(options.Prompt);

        await using var processor = builder.Build();

        var segments = new List<TranscriptSegment>();
        if (spans is null)
        {
            await foreach (var result in processor.ProcessAsync(WavSamples.Read(audioFilePath), ct))
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

                var samples = WavSamples.Read(audioFilePath, span);
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

        var cleaned = options.FilterHallucinations ? _filter.Clean(segments) : segments;
        LastCoverage = spans is null ? null : _coverage.Measure(spans, cleaned);

        // Speaker labels: who talks when is worked out from the audio, then matched to each line by time.
        if (options.IdentifySpeakers && cleaned.Count > 0 && _speakerDiarizer is { IsAvailable: true })
        {
            progress?.Report(new TranscriptionProgress("Identifying speakers", 1));
            var turns = await _speakerDiarizer.DiarizeAsync(audioFilePath, options.SpeakerCount, ct);
            // With a count chosen by the user the clustering already produced exactly that many voices; otherwise
            // short fragments that are not real speakers are folded into the nearest real one.
            if (options.SpeakerCount is null) turns = _speakerAssigner.MergeMinorSpeakers(turns);
            cleaned = _speakerAssigner.RenumberByAppearance(_speakerAssigner.Assign(cleaned, turns));
        }

        progress?.Report(new TranscriptionProgress("Done", 1));
        return cleaned;
    }

    private static TimeSpan WavDuration(string path)
    {
        using var reader = new WaveFileReader(path);
        return reader.TotalTime;
    }
}
