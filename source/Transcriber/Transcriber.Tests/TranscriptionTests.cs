using System.Text.Json;
using Xunit.Abstractions;

/// <summary>
/// End-to-end: real audio -> converter -> Whisper -> compare with the reference transcript.
/// Needs the ggml-small model (~490 MB), downloaded once to %LOCALAPPDATA%\TranscriberTests.
/// </summary>
public class TranscriptionTests(ITestOutputHelper output)
{
    private record Sample(string File, string Language, string Transcript);

    // TRANSCRIBER_TEST_MODEL=<model id> runs the whole suite against another model (default: small).
    private static readonly WhisperModel TestModel =
        WhisperModel.All.FirstOrDefault(m => m.Id == Environment.GetEnvironmentVariable("TRANSCRIBER_TEST_MODEL")) ?? WhisperModel.Small;

    private static readonly ModelManager Models = new(Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TranscriberTests"));

    // Max accepted word error rate per language, set from measured results (en/es/ar score ~0%, nl 9-17%).
    // Dutch is looser because the small model genuinely mishears some Dutch words.
    private static double Threshold(string lang) => lang == "nl" ? 0.2 : 0.05;

    // With auto-detect there is one more thing that can go wrong, so allow some headroom.
    private static double AutoThreshold(string lang) => lang == "nl" ? 0.3 : 0.1;

    public static IEnumerable<object[]> Samples()
    {
        var json = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "TestData", "manifest.json"));
        var samples = JsonSerializer.Deserialize<List<Sample>>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
        return samples.Select(s => new object[] { s.File, s.Language, s.Transcript });
    }

    private async Task<(double Wer, string Actual, string? Runtime)> RunAsync(
        string file, string language, string reference, bool useGpu = true)
    {
        var wav = await new AudioService().GetAudioFilePathAsync(Path.Combine(AppContext.BaseDirectory, "TestData", file));
        try
        {
            var whisper = new WhisperService(Models);
            var segments = await whisper.TranscribeAsync(wav, new TranscriptionOptions
            {
                Language = language,
                Model = TestModel,
                // CI runners have no GPU: TRANSCRIBER_TEST_CPU=1 forces the CPU runtime so results are deterministic.
                UseGpu = useGpu && Environment.GetEnvironmentVariable("TRANSCRIBER_TEST_CPU") != "1",
            });
            var actual = string.Join(' ', segments.Select(s => s.Text));
            var wer = TextMetrics.WordErrorRate(reference, actual);
            output.WriteLine($"{file} [{language}] runtime={whisper.LastRuntime} WER={wer:P1}\n  ref: {reference}\n  got: {actual}");
            return (wer, actual, whisper.LastRuntime);
        }
        finally
        {
            File.Delete(wav);
        }
    }

    [Theory]
    [MemberData(nameof(Samples))]
    public async Task Transcript_matches_reference(string file, string language, string reference)
    {
        var (wer, actual, _) = await RunAsync(file, language, reference);
        Assert.True(wer <= Threshold(language),
            $"{file}: WER {wer:P0} > {Threshold(language):P0}\nexpected: {reference}\nactual:   {actual}");
    }

    [Theory]
    [MemberData(nameof(Samples))]
    public async Task Auto_detected_language_still_matches_reference(string file, string language, string reference)
    {
        var (wer, actual, _) = await RunAsync(file, "auto", reference);
        Assert.True(wer <= AutoThreshold(language),
            $"{file}: WER {wer:P0} > {AutoThreshold(language):P0} with auto-detect\nexpected: {reference}\nactual:   {actual}");
    }

    [Fact]
    public async Task Cpu_request_still_transcribes_and_reports_the_runtime_actually_used()
    {
        // Whisper.net loads its native library once per process, so if another test already loaded
        // Vulkan, a later UseGpu=false cannot switch back. The service must report what really ran.
        var (wer, _, runtime) = await RunAsync("en-01.mp3", "en", "Article 1. All human beings are born free and equal in dignity and rights. They are endowed with reason and conscience and should act towards one another in a spirit of brotherhood.", useGpu: false);
        Assert.True(wer <= 0.05);
        Assert.False(string.IsNullOrEmpty(runtime));
    }

    [Theory]
    [InlineData(".mp4")]
    [InlineData(".mkv")]
    public async Task A_video_file_transcribes_end_to_end_with_the_bundled_ffmpeg(string extension)
    {
        var dir = Directory.CreateTempSubdirectory().FullName;
        try
        {
            var video = await VideoConversionTests.MakeVideoAsync(dir, Path.Combine(AppContext.BaseDirectory, "TestData", "en-01.mp3"), extension);
            var wav = await new FileReaderService(new AudioService(), new VideoReaderService()).GetAudioFileFromFileAsync(video);
            try
            {
                var segments = await new WhisperService(Models).TranscribeAsync(wav, new TranscriptionOptions
                {
                    Language = "en",
                    Model = TestModel,
                    UseGpu = Environment.GetEnvironmentVariable("TRANSCRIBER_TEST_CPU") != "1",
                });
                var actual = string.Join(' ', segments.Select(s => s.Text));
                var wer = TextMetrics.WordErrorRate("Article 1. All human beings are born free and equal in dignity and rights. They are endowed with reason and conscience and should act towards one another in a spirit of brotherhood.", actual);
                output.WriteLine($"{extension} WER={wer:P1}\n  got: {actual}");
                Assert.True(wer <= 0.05, $"WER {wer:P0}: {actual}");
            }
            finally { File.Delete(wav); }
        }
        finally { Directory.Delete(dir, true); }
    }

    [Fact]
    public async Task Cancellation_stops_transcription()
    {
        var wav = await new AudioService().GetAudioFilePathAsync(Path.Combine(AppContext.BaseDirectory, "TestData", "en-01.mp3"));
        try
        {
            using var cts = new CancellationTokenSource();
            cts.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                new WhisperService(Models).TranscribeAsync(wav, new TranscriptionOptions { Model = WhisperModel.Small, UseGpu = false }, null, cts.Token));
        }
        finally
        {
            File.Delete(wav);
        }
    }
}
