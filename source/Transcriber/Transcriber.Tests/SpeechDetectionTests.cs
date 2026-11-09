using NAudio.Wave;

/// <summary>Voice activity detection: the pure planning rules, and the bundled Silero model on real audio.</summary>
public class SpeechDetectionTests : IDisposable
{
    private readonly string _dir = Directory.CreateTempSubdirectory().FullName;

    public void Dispose() => Directory.Delete(_dir, true);

    private static SpeechSpan Span(double start, double end) => new(TimeSpan.FromSeconds(start), TimeSpan.FromSeconds(end));

    private static readonly TimeSpan Hour = TimeSpan.FromHours(1);

    [Fact]
    public void Regions_are_padded_on_both_sides()
    {
        var plan = ServicesFixture.Planner.Plan([Span(10, 12)], Hour, padding: TimeSpan.FromSeconds(0.5));

        Assert.Equal([Span(9.5, 12.5)], plan);
    }

    [Fact]
    public void Padding_never_leaves_the_recording()
    {
        var plan = ServicesFixture.Planner.Plan([Span(0.1, 9.9)], TimeSpan.FromSeconds(10), padding: TimeSpan.FromSeconds(0.5));

        Assert.Equal([Span(0, 10)], plan);
    }

    [Fact]
    public void Regions_separated_by_a_short_pause_are_merged_so_a_sentence_is_not_cut()
    {
        var plan = ServicesFixture.Planner.Plan([Span(0, 4), Span(5, 8)], Hour, padding: TimeSpan.Zero, mergeGap: TimeSpan.FromSeconds(1.5));

        Assert.Equal([Span(0, 8)], plan);
    }

    [Fact]
    public void Regions_separated_by_a_long_pause_stay_separate()
    {
        var plan = ServicesFixture.Planner.Plan([Span(0, 4), Span(20, 25)], Hour, padding: TimeSpan.Zero, mergeGap: TimeSpan.FromSeconds(1.5));

        Assert.Equal([Span(0, 4), Span(20, 25)], plan);
    }

    [Fact]
    public void Unordered_and_overlapping_regions_are_handled()
    {
        var plan = ServicesFixture.Planner.Plan([Span(6, 9), Span(0, 7)], Hour, padding: TimeSpan.Zero);

        Assert.Equal([Span(0, 9)], plan);
    }

    [Fact]
    public void Very_long_speech_is_split_into_chunks_of_at_most_the_limit()
    {
        var plan = ServicesFixture.Planner.Plan([Span(0, 25)], Hour, padding: TimeSpan.Zero, maxChunk: TimeSpan.FromSeconds(10));

        Assert.Equal([Span(0, 10), Span(10, 20), Span(20, 25)], plan);
    }

    [Fact]
    public void No_regions_means_nothing_to_transcribe()
    {
        Assert.Empty(ServicesFixture.Planner.Plan([], Hour));
    }

    /// <summary>Silence, then the sample, then silence, as one 16 kHz mono wav.</summary>
    private async Task<string> SpeechBetweenSilenceAsync(double silenceSeconds)
    {
        var sample = await ServicesFixture.AudioService.GetAudioFilePathAsync(Path.Combine(AppContext.BaseDirectory, "TestData", "en-01.mp3"));
        var path = Path.Combine(_dir, "padded.wav");
        var format = new WaveFormat(16000, 16, 1);
        var silence = new byte[(int)(silenceSeconds * format.AverageBytesPerSecond)];

        using (var writer = new WaveFileWriter(path, format))
        {
            writer.Write(silence, 0, silence.Length);
            using (var reader = new WaveFileReader(sample)) reader.CopyTo(writer);
            writer.Write(silence, 0, silence.Length);
        }
        File.Delete(sample);
        return path;
    }

    private static ISpeechDetector Detector() => ServicesFixture.SpeechDetector();

    [Fact]
    public void The_detection_model_ships_next_to_the_app()
    {
        Assert.True(Detector().IsAvailable);
    }

    [Fact]
    public async Task Speech_is_found_after_leading_silence()
    {
        var path = await SpeechBetweenSilenceAsync(4);

        var spans = await Detector().DetectAsync(path);

        Assert.NotEmpty(spans);
        Assert.True(spans[0].Start >= TimeSpan.FromSeconds(3), $"speech starts at {spans[0].Start}");
        Assert.True(spans[^1].End <= WaveDuration(path) - TimeSpan.FromSeconds(3), $"speech ends at {spans[^1].End}");
    }

    [Fact]
    public async Task Pure_silence_has_no_speech()
    {
        var path = Path.Combine(_dir, "silence.wav");
        using (var writer = new WaveFileWriter(path, new WaveFormat(16000, 16, 1)))
            writer.Write(new byte[16000 * 2 * 8], 0, 16000 * 2 * 8);

        Assert.Empty(await Detector().DetectAsync(path));
    }

    private static TimeSpan WaveDuration(string path)
    {
        using var reader = new WaveFileReader(path);
        return reader.TotalTime;
    }
}
