using Microsoft.Extensions.Options;
using Transcriber.Services.Catalog;
using Transcriber.Services.Configuration;
using Transcriber.Services.Export;
using Transcriber.Services.Interfaces;
using Transcriber.Services.Media;
using Transcriber.Services.Models;
using Transcriber.Services.Platform;
using Transcriber.Services.Speakers;
using Transcriber.Services.Transcription;

/// <summary>Builds the real services from the real default settings, the way the app does, so tests share one wiring.</summary>
internal static class ServicesFixture
{
    public static TranscriberSettings Settings { get; } = SettingsLoader.Load();

    private static IOptions<T> Opt<T>(T value) where T : class => Options.Create(value);

    // ---- lists ---------------------------------------------------------------------------

    public static IModelCatalog Models { get; } = new ModelCatalog(Opt(Settings.Models));

    public static ILanguageCatalog Languages { get; } = new LanguageCatalog(Opt(Settings.Languages), Models);

    public static IMediaFormatCatalog Formats { get; } = new MediaFormatCatalog(Opt(Settings.Media));

    // ---- media ---------------------------------------------------------------------------

    public static IExecutablePermission Permission { get; } = new ExecutablePermission();

    public static IFfmpegLocator FfmpegLocator { get; } = new FfmpegLocator(Opt(Settings.Media), Permission);

    public static IAudioService AudioService { get; } = new AudioService(Opt(Settings.Audio));

    public static IVideoReaderService VideoReader(string? ffmpegPath = null, string? tempDirectory = null) =>
        new VideoReaderService(FfmpegLocator, Opt(Settings.Audio), ffmpegPath, tempDirectory);

    public static IFileReaderService FileReader() => new FileReaderService(AudioService, VideoReader(), Opt(Settings.Media));

    public static IFileReaderService FileReader(IAudioService audio, IVideoReaderService video) =>
        new FileReaderService(audio, video, Opt(Settings.Media));

    // ---- transcription ---------------------------------------------------------------------

    public static IModelManager ModelManager(string? modelsDirectory = null, string? bundledDirectory = null) =>
        new ModelManager(Opt(Settings.Models), modelsDirectory, bundledDirectory);

    public static ISpeechDetector SpeechDetector(string? modelDirectory = null) => new SileroSpeechDetector(Opt(Settings.Models), modelDirectory);

    public static ISpeechPlanner Planner { get; } = new SpeechPlanner(Opt(Settings.SpeechPlanning));

    public static ISpeechCoverageCalculator Coverage { get; } = new SpeechCoverageCalculator(Opt(Settings.SpeechCoverage));

    public static ITranscriptFilter Filter { get; } = new TranscriptFilter(Opt(Settings.Transcription));

    public static ISpeakerAssigner Assigner { get; } = new SpeakerAssigner(Opt(Settings.Speakers));

    public static IWhisperService Whisper(IModelManager modelManager, ISpeechDetector? detector = null, ISpeakerDiarizer? diarizer = null) =>
        new WhisperService(modelManager, Models, Planner, Coverage, Filter, Assigner, Opt(ReleaseModelAtOnce), detector, diarizer);

    /// <summary>A service with the default settings: it keeps its model in memory after a run. The test must dispose it.</summary>
    public static WhisperService WhisperThatKeepsItsModel(IModelManager modelManager) =>
        new(modelManager, Models, Planner, Coverage, Filter, Assigner, Opt(Settings.Transcription), SpeechDetector());

    // Each test makes its own service. A service that keeps its model would hold many models in memory at the same time.
    private static TranscriptionSettings ReleaseModelAtOnce { get; } = new()
    {
        NoSpeechThreshold = Settings.Transcription.NoSpeechThreshold,
        Hallucinations = Settings.Transcription.Hallucinations,
        KeepModelLoadedMinutes = 0,
    };

    public static ISpeakerDiarizer Diarizer(string? modelDirectory = null) =>
        new SherpaSpeakerDiarizer(Opt(Settings.Speakers), Opt(Settings.Models), modelDirectory);

    public static SherpaCliSpeakerDiarizer CliDiarizer(string? toolPath = null, string? modelDirectory = null) =>
        new(Opt(Settings.Speakers), Opt(Settings.Models), Permission, toolPath, modelDirectory);

    // ---- saving ----------------------------------------------------------------------------

    public static ITranscriptExporter Exporter { get; } = new TranscriptExporter(
        [new TextFormatter(), new SrtFormatter(), new VttFormatter(), new JsonFormatter()]);
}

/// <summary>The models of the default settings, by the names that the tests use.</summary>
internal static class TestModels
{
    public static WhisperModel Base => ServicesFixture.Models.Find("base")!;
    public static WhisperModel Small => ServicesFixture.Models.Find("small")!;
    public static WhisperModel SmallQ5 => ServicesFixture.Models.Find("small-q5_1")!;
    public static WhisperModel MediumQ5 => ServicesFixture.Models.Find("medium-q5_0")!;
    public static WhisperModel LargeV3TurboQ5 => ServicesFixture.Models.Find("large-v3-turbo-q5_0")!;
}
