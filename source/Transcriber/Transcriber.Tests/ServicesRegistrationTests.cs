using Microsoft.Extensions.DependencyInjection;
using Transcriber.Services.Extensions;

/// <summary>The app builds the services from its container. If a registration is missing, the app would fail at runtime.</summary>
public class ServicesRegistrationTests
{
    private static ServiceProvider Build(string? userFile = null) =>
        new ServiceCollection()
            .AddTranscriberServices(userFile ?? Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".json"))
            .AddToolSpeakerDetection()
            .BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });

    [Fact]
    public void Every_public_contract_can_be_resolved()
    {
        using var provider = Build();

        foreach (var type in new[]
        {
            typeof(IFileReaderService), typeof(IWhisperService), typeof(IModelManager), typeof(IModelCatalog), typeof(ILanguageCatalog),
            typeof(IMediaFormatCatalog), typeof(ITranscriptExporter), typeof(IFileLauncher),
        })
            Assert.NotNull(provider.GetRequiredService(type));
    }

    [Fact]
    public void Every_internal_service_can_be_resolved()
    {
        using var provider = Build();

        foreach (var type in new[]
        {
            typeof(IAudioService), typeof(IVideoReaderService), typeof(IFfmpegLocator), typeof(ISpeechDetector), typeof(ISpeechPlanner), typeof(ISpeechPacker), typeof(ISpeechChunkReader), typeof(IChunkLineBuilder),
            typeof(ISpeechCoverageCalculator), typeof(ITranscriptFilter), typeof(ISpeakerAssigner), typeof(ISpeakerDiarizer), typeof(IToolFileStager),
        })
            Assert.NotNull(provider.GetRequiredService(type));
    }

    [Fact]
    public void There_is_one_formatter_for_each_format_and_the_exporter_knows_all_of_them()
    {
        using var provider = Build();

        var formats = provider.GetServices<ITranscriptFormatter>().Select(f => f.Format).Order().ToArray();
        var exporter = provider.GetRequiredService<ITranscriptExporter>();

        Assert.Equal(Enum.GetValues<TranscriptFormat>().Order(), formats);
        Assert.All(formats, f => Assert.StartsWith(".", exporter.Extension(f)));
    }

    [Fact]
    public void Speaker_detection_runs_as_the_bundled_program()
    {
        using var provider = new ServiceCollection()
            .AddTranscriberServices(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".json"))
            .AddToolSpeakerDetection()
            .BuildServiceProvider();

        Assert.IsType<SherpaCliSpeakerDiarizer>(provider.GetRequiredService<ISpeakerDiarizer>());
    }

    [Fact]
    public void A_settings_file_in_the_user_folder_changes_the_services()
    {
        var file = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".json");
        File.WriteAllText(file, """{ "Models": { "DefaultModelId": "base" } }""");
        try
        {
            using var provider = Build(file);

            Assert.Equal("base", provider.GetRequiredService<IModelCatalog>().Default.Id);
        }
        finally { File.Delete(file); }
    }
}
