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
    private readonly ISpeechPacker _packer;
    private readonly ISpeechChunkReader _chunkReader;
    private readonly IChunkLineBuilder _lineBuilder;
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
        ISpeechPacker packer,
        ISpeechChunkReader chunkReader,
        IChunkLineBuilder lineBuilder,
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
        _packer = packer;
        _chunkReader = chunkReader;
        _lineBuilder = lineBuilder;
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

        // With speaker labels, the last part of the bar is for the speaker step. Without this, the bar is full while
        // that step still works.
        var transcribeShare = options.IdentifySpeakers && _speakerDiarizer is { IsAvailable: true } ? 1 - SpeakerShare : 1.0;

        var chunkOffset = 0.0;  // share of the work finished before the current chunk
        var chunkShare = 1.0;   // share of the work the current chunk represents

        var builder = factory.CreateBuilder()
            .WithLanguage(options.Language)
            .WithNoSpeechThreshold(options.NoSpeechThreshold ?? _settings.NoSpeechThreshold)
            .WithThreads(_settings.Threads > 0 ? _settings.Threads : AutomaticThreads(Environment.ProcessorCount))
            .WithProgressHandler(p => progress?.Report(
                new TranscriptionProgress($"Transcribing ({LastRuntime})", transcribeShare * (chunkOffset + chunkShare * p / 100.0))));

        if (!string.IsNullOrWhiteSpace(options.Prompt))
            builder = builder.WithPrompt(options.Prompt);

        // One loop for each case. Without the speech search the whole recording is one chunk. With it, the packer
        // decides which stretches go to Whisper together, or each stretch goes alone when the run asks for that.
        var chunks = spans is null ? [SpeechChunk.Of(new SpeechSpan(TimeSpan.Zero, WavDuration(audioFilePath)))]
            : options.PackSpeech ? _packer.Pack(spans)
            : spans.Select(SpeechChunk.Of).ToList();

        // When stretches go to Whisper together, the time of each word is needed to cut a line at a join.
        if (chunks.Any(c => c.Pieces.Count > 1)) builder = builder.WithTokenTimestamps();

        await using var processor = builder.Build();

        var segments = new List<TranscriptSegment>();
        var total = chunks.Sum(c => c.Length.TotalSeconds);
        var done = 0.0;
        foreach (var chunk in chunks)
        {
            ct.ThrowIfCancellationRequested();
            chunkOffset = total > 0 ? done / total : 0;
            chunkShare = total > 0 ? chunk.Length.TotalSeconds / total : 1;

            // Whisper counts from the start of the chunk. The line builder gives the lines with times in the recording.
            await foreach (var result in processor.ProcessAsync(_chunkReader.Read(audioFilePath, chunk), ct))
                segments.AddRange(_lineBuilder.Build(chunk, new TimedText(result.Text, result.Start, result.End), Parts(result)));
            done += chunk.Length.TotalSeconds;
        }

        var cleaned = options.FilterHallucinations ? _filter.Clean(segments) : segments;
        LastCoverage = spans is null ? null : _coverage.Measure(spans, cleaned);

        // Speaker labels: who talks when is worked out from the audio, then matched to each line by time.
        if (options.IdentifySpeakers && cleaned.Count > 0 && _speakerDiarizer is { IsAvailable: true })
        {
            progress?.Report(new TranscriptionProgress("Identifying speakers", transcribeShare));
            var speakerProgress = new Relay(share => progress?.Report(
                new TranscriptionProgress("Identifying speakers", transcribeShare + (1 - transcribeShare) * share)));
            var turns = await _speakerDiarizer.DiarizeAsync(audioFilePath, options.SpeakerCount, speakerProgress, ct);
            // With a count chosen by the user the clustering already produced exactly that many voices; otherwise
            // short fragments that are not real speakers are folded into the nearest real one.
            if (options.SpeakerCount is null) turns = _speakerAssigner.MergeMinorSpeakers(turns);
            cleaned = _speakerAssigner.RenumberByAppearance(_speakerAssigner.Assign(cleaned, turns));
        }

        progress?.Report(new TranscriptionProgress("Done", 1));
        return cleaned;
    }

    /// <summary>
    /// How many threads Whisper uses on the processor when the settings give no number. Whisper itself stops at 4.
    /// Half of the logical processors is near the number of real cores on most computers, and more than that gives
    /// little. The result is never less than what Whisper uses by itself.
    /// </summary>
    public static int AutomaticThreads(int logicalProcessors)
    {
        var whisperDefault = Math.Clamp(logicalProcessors, 1, 4);
        return Math.Max(whisperDefault, Math.Min(MaxAutomaticThreads, logicalProcessors / 2));
    }

    private const int MaxAutomaticThreads = 16;

    /// <summary>The part of the progress bar that the speaker step gets when speaker labels are on.</summary>
    public const double SpeakerShare = 0.3;

    /// <summary>Passes a progress report on in the thread that makes it, so the reports keep their order.</summary>
    private sealed class Relay(Action<double> onReport) : IProgress<double>
    {
        public void Report(double value) => onReport(value);
    }

    /// <summary>The small parts of the text of a line, with their times. Marks of Whisper that are not text, such as "[_BEG_]", are left out.</summary>
    private static List<TimedText> Parts(SegmentData line) =>
    [
        .. line.Tokens
            .Where(t => !string.IsNullOrEmpty(t.Text) && !(t.Text.StartsWith("[_", StringComparison.Ordinal) && t.Text.EndsWith(']')))
            .Select(t => new TimedText(t.Text!, TimeSpan.FromMilliseconds(t.Start * 10), TimeSpan.FromMilliseconds(t.End * 10))),
    ];

    private static TimeSpan WavDuration(string path)
    {
        using var reader = new WaveFileReader(path);
        return reader.TotalTime;
    }
}
