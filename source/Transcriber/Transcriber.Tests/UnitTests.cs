using System.Text.Json;
using NAudio.Wave;

public class TextMetricsTests
{
    [Fact]
    public void Identical_text_has_zero_error() =>
        Assert.Equal(0, TextMetrics.WordErrorRate("Hello, world!", "hello world"));

    [Fact]
    public void One_wrong_word_out_of_four_is_25_percent() =>
        Assert.Equal(0.25, TextMetrics.WordErrorRate("a b c d", "a b x d"), 3);

    [Fact]
    public void Arabic_diacritics_and_alef_variants_are_ignored() =>
        Assert.Equal(0, TextMetrics.WordErrorRate("أحراراً", "احرارا"));

    [Fact]
    public void Latin_accents_are_ignored() =>
        Assert.Equal(0, TextMetrics.WordErrorRate("están", "estan"));
}

public class AudioServiceTests
{
    [Theory]
    [InlineData("en-01.mp3")]
    [InlineData("ar-01.mp3")]
    public async Task Mp3_is_converted_to_16khz_mono_16bit_wav(string file)
    {
        var input = Path.Combine(AppContext.BaseDirectory, "TestData", file);

        var output = await new AudioService().GetAudioFilePathAsync(input);
        try
        {
            Assert.EndsWith(".wav", output);
            using var reader = new WaveFileReader(output);
            Assert.Equal(16000, reader.WaveFormat.SampleRate);
            Assert.Equal(1, reader.WaveFormat.Channels);
            Assert.Equal(16, reader.WaveFormat.BitsPerSample);
            Assert.InRange(reader.TotalTime.TotalSeconds, 5, 40);
        }
        finally
        {
            File.Delete(output);
        }
    }

    [Fact]
    public async Task Unsupported_extension_throws()
    {
        await Assert.ThrowsAsync<NotSupportedException>(() => new AudioService().GetAudioFilePathAsync("x.flac"));
    }
}

public class TranscriptFilterTests
{
    private static TranscriptSegment Seg(double start, double end, string text) =>
        new() { Start = TimeSpan.FromSeconds(start), End = TimeSpan.FromSeconds(end), Text = text };

    [Fact]
    public void Drops_empty_and_placeholder_segments()
    {
        var cleaned = TranscriptFilter.Clean([Seg(0, 1, "  "), Seg(1, 2, "[BLANK_AUDIO]"), Seg(2, 3, "Hello there")]);
        Assert.Single(cleaned);
        Assert.Equal("Hello there", cleaned[0].Text);
    }

    [Fact]
    public void Drops_stock_silence_hallucinations_in_several_languages()
    {
        var cleaned = TranscriptFilter.Clean([
            Seg(0, 1, "Thanks for watching!"),
            Seg(1, 2, "Ondertiteld door de Amara.org gemeenschap"),
            Seg(2, 3, "اشتركوا في القناة"),
            Seg(3, 4, "Real speech")]);
        Assert.Equal(["Real speech"], cleaned.Select(s => s.Text));
    }

    [Fact]
    public void Keeps_real_speech_that_merely_contains_a_stock_phrase()
    {
        var cleaned = TranscriptFilter.Clean([Seg(0, 1, "Thanks for watching the kids while I was out")]);
        Assert.Single(cleaned);
    }

    [Fact]
    public void Collapses_repetition_loops_and_extends_the_end_time()
    {
        var cleaned = TranscriptFilter.Clean([Seg(0, 2, "Go home."), Seg(2, 4, "Go home!"), Seg(4, 6, "go home"), Seg(6, 8, "Different")]);
        Assert.Equal(2, cleaned.Count);
        Assert.Equal(TimeSpan.FromSeconds(6), cleaned[0].End);
    }
}

public class TranscriptExporterTests
{
    private static readonly List<TranscriptSegment> Segments =
    [
        new() { Start = TimeSpan.Zero, End = TimeSpan.FromMilliseconds(2500), Text = "Hello" },
        new() { Start = TimeSpan.FromSeconds(3661.5), End = TimeSpan.FromSeconds(3665), Text = "مرحبا" },
    ];

    [Fact]
    public void Srt_uses_numbered_cues_and_comma_milliseconds() =>
        Assert.Equal("1\r\n00:00:00,000 --> 00:00:02,500\r\nHello\r\n\r\n2\r\n01:01:01,500 --> 01:01:05,000\r\nمرحبا\r\n\r\n".Replace("\r\n", Environment.NewLine),
            TranscriptExporter.ToSrt(Segments));

    [Fact]
    public void Vtt_has_header_and_dot_milliseconds()
    {
        var vtt = TranscriptExporter.ToVtt(Segments);
        Assert.StartsWith("WEBVTT", vtt);
        Assert.Contains("00:00:00.000 --> 00:00:02.500", vtt);
    }

    [Fact]
    public void Json_keeps_arabic_readable_and_round_trips()
    {
        var json = TranscriptExporter.ToJson(Segments);
        Assert.Contains("مرحبا", json);
        using var doc = JsonDocument.Parse(json);
        Assert.Equal(2, doc.RootElement.GetArrayLength());
        Assert.Equal(3661.5, doc.RootElement[1].GetProperty("start").GetDouble());
    }

    [Fact]
    public async Task WriteAsync_creates_file_next_to_source_with_format_extension()
    {
        var dir = Directory.CreateTempSubdirectory().FullName;
        try
        {
            var path = await TranscriptExporter.WriteAsync(Segments, Path.Combine(dir, "talk.mp3"), TranscriptFormat.Srt);
            Assert.Equal(Path.Combine(dir, "talk_transcript.srt"), path);
            Assert.True(File.Exists(path));
        }
        finally { Directory.Delete(dir, true); }
    }
}

public class VideoReaderServiceTests
{
    [Fact]
    public async Task Missing_ffmpeg_gives_a_helpful_error()
    {
        var service = new VideoReaderService(Path.Combine(Path.GetTempPath(), "definitely-not-ffmpeg.exe"));
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.ConvertVideoToAudioAsync("x.mp4"));
        Assert.Contains("ffmpeg was not found", ex.Message);
    }
}

public class ModelManagerTests
{
    [Fact]
    public void Catalog_has_unique_ids_and_matching_file_names()
    {
        Assert.Equal(WhisperModel.All.Count, WhisperModel.All.Select(m => m.Id).Distinct().Count());
        Assert.All(WhisperModel.All, m => Assert.Equal($"ggml-{m.Id}.bin", m.FileName));
    }

    [Fact]
    public async Task Existing_model_is_reused_without_downloading_and_partial_files_are_ignored()
    {
        var dir = Directory.CreateTempSubdirectory().FullName;
        try
        {
            var manager = new ModelManager(dir);
            // A half-finished download must not count as an installed model.
            File.WriteAllText(Path.Combine(dir, WhisperModel.Base.FileName + ".part"), "partial");
            Assert.False(manager.IsDownloaded(WhisperModel.Base));

            File.WriteAllText(Path.Combine(dir, WhisperModel.Base.FileName), "fake");
            Assert.True(manager.IsDownloaded(WhisperModel.Base));
            Assert.Equal(Path.Combine(dir, WhisperModel.Base.FileName), await manager.EnsureModelAsync(WhisperModel.Base));
        }
        finally { Directory.Delete(dir, true); }
    }

    [Fact]
    public async Task Cancelled_download_leaves_no_files_behind()
    {
        var dir = Directory.CreateTempSubdirectory().FullName;
        try
        {
            using var cts = new CancellationTokenSource();
            cts.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                new ModelManager(dir).EnsureModelAsync(WhisperModel.Base, null, cts.Token));
            Assert.Empty(Directory.GetFiles(dir));
        }
        finally { Directory.Delete(dir, true); }
    }
}
