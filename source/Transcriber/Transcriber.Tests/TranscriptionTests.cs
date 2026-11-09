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

    [Fact]
    public async Task With_speech_detection_text_keeps_its_place_in_the_recording_and_silence_gives_nothing()
    {
        const int lead = 6; // seconds of silence in front of the speech
        var sample = await new AudioService().GetAudioFilePathAsync(Path.Combine(AppContext.BaseDirectory, "TestData", "en-01.mp3"));
        var padded = Path.Combine(Path.GetTempPath(), $"vad_{Guid.NewGuid():N}.wav");
        var silent = Path.Combine(Path.GetTempPath(), $"vad_{Guid.NewGuid():N}.wav");
        try
        {
            var format = new NAudio.Wave.WaveFormat(16000, 16, 1);
            using (var writer = new NAudio.Wave.WaveFileWriter(padded, format))
            {
                writer.Write(new byte[lead * format.AverageBytesPerSecond], 0, lead * format.AverageBytesPerSecond);
                using var reader = new NAudio.Wave.WaveFileReader(sample);
                reader.CopyTo(writer);
            }
            using (var writer = new NAudio.Wave.WaveFileWriter(silent, format))
                writer.Write(new byte[10 * format.AverageBytesPerSecond], 0, 10 * format.AverageBytesPerSecond);

            var whisper = new WhisperService(Models, new SileroSpeechDetector());
            var options = new TranscriptionOptions
            {
                Language = "en",
                Model = TestModel,
                UseGpu = Environment.GetEnvironmentVariable("TRANSCRIBER_TEST_CPU") != "1",
            };

            var segments = await whisper.TranscribeAsync(padded, options);
            output.WriteLine(string.Join(" | ", segments.Select(s => $"{s.Start.TotalSeconds:F1}-{s.End.TotalSeconds:F1} {s.Text}")));
            Assert.NotEmpty(segments);
            Assert.True(segments[0].Start >= TimeSpan.FromSeconds(lead - 1), $"first line starts at {segments[0].Start}");

            Assert.Empty(await whisper.TranscribeAsync(silent, options));
        }
        finally
        {
            File.Delete(sample);
            File.Delete(padded);
            File.Delete(silent);
        }
    }

    [Fact]
    public async Task Speakers_are_identified_in_a_two_person_conversation()
    {
        var wav = Path.Combine(AppContext.BaseDirectory, "TestData", "two-speakers-en.wav");
        var whisper = new WhisperService(Models, new SileroSpeechDetector(), new SherpaSpeakerDiarizer());

        var segments = await whisper.TranscribeAsync(wav, new TranscriptionOptions
        {
            Language = "en",
            Model = TestModel,
            UseGpu = Environment.GetEnvironmentVariable("TRANSCRIBER_TEST_CPU") != "1",
            IdentifySpeakers = true,
            SpeakerCount = 2,
        });

        output.WriteLine(string.Join("\n", segments.Select(s => $"{s.Start.TotalSeconds:F1}s Speaker {s.Speaker}: {s.Text}")));
        Assert.NotEmpty(segments);
        Assert.All(segments, s => Assert.NotNull(s.Speaker));
        Assert.Equal(2, segments.Select(s => s.Speaker).Distinct().Count());
    }

    [Fact]
    public async Task The_practice_tab_analyses_real_speech_end_to_end()
    {
        var dir = Directory.CreateTempSubdirectory().FullName;
        try
        {
            var sample = Path.Combine(dir, "talk.wav");
            File.Copy(Path.Combine(AppContext.BaseDirectory, "TestData", "two-speakers-en.wav"), sample);
            var historyFile = Path.Combine(dir, "history.json");
            var whisper = new WhisperService(Models, new SileroSpeechDetector());
            var vm = new Transcriber.ViewModels.PracticeViewModel(
                new FileReaderService(new AudioService(), new VideoReaderService()),
                new CpuOnly(whisper),
                new NoPicker(),
                new Transcriber.Coach.History.JsonPracticeHistory(historyFile),
                CoachFixture.Analyzer,
                CoachFixture.Advisor,
                CoachFixture.VerbatimPrompts,
                new NoFeedback(),
                new NoRecorder());
            vm.SetFile(sample);

            await vm.AnalyzeCommand.ExecuteAsync(null);

            output.WriteLine(string.Join(" | ", vm.Metrics.Select(m => $"{m.Label}={m.Value}")) + "\n" + vm.Transcript);
            Assert.True(vm.HasResult, vm.ErrorMessage);
            Assert.Contains("wpm", vm.Metrics[0].Value);
            var wpm = int.Parse(vm.Metrics[0].Value.Split(' ')[0]);
            Assert.InRange(wpm, 100, 300); // fast conversational speech, but never absurd
            Assert.NotEmpty(vm.Tips);
            Assert.Single(vm.Sessions);
            Assert.True(File.Exists(sample), "the user's own wav must be left alone");
        }
        finally { Directory.Delete(dir, true); }
    }

    // The practice tab asks for the GPU like the app does; CI runners have none, so force the CPU there.
    private sealed class CpuOnly(IWhisperService inner) : IWhisperService
    {
        public string? LastRuntime => inner.LastRuntime;

        public Task<List<TranscriptSegment>> TranscribeAsync(string audioFilePath, TranscriptionOptions? options = null,
            IProgress<TranscriptionProgress>? progress = null, CancellationToken ct = default) =>
            inner.TranscribeAsync(audioFilePath,
                (options ?? new TranscriptionOptions()) with { UseGpu = Environment.GetEnvironmentVariable("TRANSCRIBER_TEST_CPU") != "1" },
                progress, ct);
    }

    private sealed class NoPicker : Transcriber.ViewModels.IFilePickerService
    {
        public Task<IReadOnlyList<string>> PickMediaFilesAsync() => Task.FromResult<IReadOnlyList<string>>([]);
    }

    private sealed class NoRecorder : Transcriber.ViewModels.IAudioRecorder
    {
        public bool IsRecording => false;
        public event EventHandler<Transcriber.ViewModels.RecordingProgress>? Progress { add { } remove { } }
        public Task StartAsync(string wavPath) => throw new NotSupportedException();
        public void Stop() { }
    }

    private sealed class NoFeedback : Transcriber.Coach.Interfaces.ICoachFeedback
    {
        public Task<IReadOnlyList<string>> ListModelsAsync(CancellationToken ct = default) => Task.FromResult<IReadOnlyList<string>>([]);

        public Task<string> GetFeedbackAsync(string model, string transcript, Transcriber.Coach.Models.SpeechReport report, CancellationToken ct = default) =>
            throw new NotSupportedException();
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
