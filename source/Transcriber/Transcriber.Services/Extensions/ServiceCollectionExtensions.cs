using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Transcriber.Services.Catalog;
using Transcriber.Services.Configuration;
using Transcriber.Services.Export;
using Transcriber.Services.Interfaces;
using Transcriber.Services.Media;
using Transcriber.Services.Platform;
using Transcriber.Services.Speakers;
using Transcriber.Services.Transcription;

namespace Transcriber.Services.Extensions;

/// <summary>
/// The only way for an app to use the services. The classes are internal, so the app sees the public interfaces and
/// models only, and it gets them from the container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the services for reading media, transcribing, saving transcripts and the language and model lists.
    /// A speaker detector is added with <see cref="AddToolSpeakerDetection"/>.
    /// </summary>
    /// <param name="userSettingsPath">
    /// A file with changed settings. Null uses appsettings.user.json in the app data folder. A file that does not exist is ignored.
    /// </param>
    /// <exception cref="Exceptions.SettingsException">A setting cannot work. The app stops at start with a message that names it.</exception>
    public static IServiceCollection AddTranscriberServices(this IServiceCollection services, string? userSettingsPath = null)
    {
        var settings = SettingsLoader.Load(userSettingsPath ?? SettingsLoader.DefaultUserFile);

        // Settings. Each service asks only for the part that it needs.
        services.AddSingleton(Options.Create(settings.Audio));
        services.AddSingleton(Options.Create(settings.Media));
        services.AddSingleton(Options.Create(settings.Models));
        services.AddSingleton(Options.Create(settings.Languages));
        services.AddSingleton(Options.Create(settings.Transcription));
        services.AddSingleton(Options.Create(settings.SpeechPlanning));
        services.AddSingleton(Options.Create(settings.SpeechCoverage));
        services.AddSingleton(Options.Create(settings.Speakers));

        // Lists
        services.AddSingleton<IModelCatalog, ModelCatalog>();
        services.AddSingleton<ILanguageCatalog, LanguageCatalog>();
        services.AddSingleton<IMediaFormatCatalog, MediaFormatCatalog>();

        // Reading files
        services.AddSingleton<IExecutablePermission, ExecutablePermission>();
        // Windows command-line programs cannot open every path. macOS programs can.
        services.AddSingleton<IToolFileStager>(_ => OperatingSystem.IsWindows() ? new AsciiToolFiles() : new DirectToolFiles());
        services.AddSingleton<IFfmpegLocator, FfmpegLocator>();
        services.AddSingleton<IAudioService, AudioService>();
        services.AddSingleton<IVideoReaderService, VideoReaderService>();
        services.AddSingleton<IFileReaderService, FileReaderService>();

        // Transcribing
        services.AddSingleton<IModelManager, ModelManager>();
        services.AddSingleton<ISpeechDetector, SileroSpeechDetector>();
        services.AddSingleton<ISpeechPlanner, SpeechPlanner>();
        services.AddSingleton<ISpeechCoverageCalculator, SpeechCoverageCalculator>();
        services.AddSingleton<ITranscriptFilter, TranscriptFilter>();
        services.AddSingleton<ISpeakerAssigner, SpeakerAssigner>();
        services.AddSingleton<IWhisperService, WhisperService>();

        // Saving transcripts: one formatter for each format.
        services.AddSingleton<ITranscriptFormatter, TextFormatter>();
        services.AddSingleton<ITranscriptFormatter, SrtFormatter>();
        services.AddSingleton<ITranscriptFormatter, VttFormatter>();
        services.AddSingleton<ITranscriptFormatter, JsonFormatter>();
        services.AddSingleton<ITranscriptExporter, TranscriptExporter>();

        services.AddSingleton<IFileLauncher, ShellFileLauncher>();
        return services;
    }

    /// <summary>
    /// Speaker detection through the program that is built into the app. Windows and macOS use this.
    /// The app can stop a separate program at any moment, so a cancel has effect at once.
    /// </summary>
    public static IServiceCollection AddToolSpeakerDetection(this IServiceCollection services) =>
        services.AddSingleton<ISpeakerDiarizer, SherpaCliSpeakerDiarizer>();
}
