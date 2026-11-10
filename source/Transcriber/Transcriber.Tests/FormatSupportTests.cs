using System.Diagnostics;
using NAudio.Wave;

/// <summary>Real files in many formats go through the app's normal reading path and must come out as 16 kHz mono wav.</summary>
public class FormatSupportTests : IDisposable
{
    private readonly string _dir = Directory.CreateTempSubdirectory().FullName;
    private readonly List<string> _outputs = [];

    public void Dispose()
    {
        foreach (var o in _outputs) try { File.Delete(o); } catch { }
        Directory.Delete(_dir, true);
    }

    private static string Sample(string name) => Path.Combine(AppContext.BaseDirectory, "TestData", name);

    private static IFileReaderService Reader() => ServicesFixture.FileReader();

    private static async Task RunFfmpegAsync(params string[] args)
    {
        var psi = new ProcessStartInfo(ServicesFixture.FfmpegLocator.Locate()!)
        {
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        foreach (var a in new[] { "-y", "-loglevel", "error" }.Concat(args)) psi.ArgumentList.Add(a);
        using var p = Process.Start(psi)!;
        var err = p.StandardError.ReadToEndAsync();
        await p.WaitForExitAsync();
        Assert.True(p.ExitCode == 0, "could not create the test file: " + await err);
    }

    private async Task<string> EncodeAsync(string name, params string[] codecArgs)
    {
        var path = Path.Combine(_dir, name);
        await RunFfmpegAsync(["-i", Sample("en-02.mp3"), .. codecArgs, path]);
        return path;
    }

    // en-02.mp3 lasts about 17.2 seconds.
    private async Task AssertConvertsAsync(string path)
    {
        var wav = await Reader().GetAudioFileFromFileAsync(path);
        if (!string.Equals(wav, path, StringComparison.OrdinalIgnoreCase)) _outputs.Add(wav);

        using var reader = new WaveFileReader(wav);
        Assert.Equal(16000, reader.WaveFormat.SampleRate);
        Assert.Equal(1, reader.WaveFormat.Channels);
        Assert.Equal(16, reader.WaveFormat.BitsPerSample);
        Assert.InRange(reader.TotalTime.TotalSeconds, 16.5, 18.0);
    }

    // ---- the lists --------------------------------------------------------------------

    [Fact]
    public void Extension_lists_are_clean_and_cover_the_new_formats()
    {
        var all = ServicesFixture.Formats.SupportedExtensions;

        Assert.Equal(all.Count, all.Distinct().Count());
        Assert.All(all, e => Assert.Matches(@"^\.[a-z0-9]+$", e));
        foreach (var e in new[] { ".opus", ".wma", ".wmv", ".aiff", ".aif", ".caf", ".3gp", ".amr", ".ac3", ".mka", ".m4b", ".mpg", ".mpeg", ".flv" })
            Assert.Contains(e, all);
        // The original formats stay.
        foreach (var e in new[] { ".mp3", ".wav", ".m4a", ".flac", ".ogg", ".aac", ".mp4", ".mov", ".avi", ".mkv", ".webm" })
            Assert.Contains(e, all);
    }

    [Fact]
    public async Task An_unlisted_extension_that_is_not_media_says_it_could_not_be_read()
    {
        var path = Path.Combine(_dir, "notes.txt");
        File.WriteAllText(path, "hello");

        var e = await Assert.ThrowsAsync<MediaReadException>(() => Reader().GetAudioFileFromFileAsync(path));

        Assert.StartsWith("This file could not be read.", e.Message);
    }

    [Fact]
    public async Task An_unlisted_extension_with_real_audio_is_read_when_the_person_chooses_it()
    {
        var path = await EncodeAsync("voice.xyz", "-c:a", "libvorbis", "-f", "ogg");

        await AssertConvertsAsync(path);
    }

    [Fact]
    public async Task A_missing_file_says_it_was_not_found()
    {
        var e = await Assert.ThrowsAsync<MediaReadException>(
            () => Reader().GetAudioFileFromFileAsync(Path.Combine(_dir, "gone.mp4")));

        Assert.Equal("This file was not found. It may have been moved or deleted.", e.Message);
    }

    [Fact]
    public async Task An_empty_file_says_it_could_not_be_read()
    {
        var path = Path.Combine(_dir, "empty.dat");
        File.WriteAllBytes(path, []);

        var e = await Assert.ThrowsAsync<MediaReadException>(() => Reader().GetAudioFileFromFileAsync(path));

        Assert.StartsWith("This file could not be read.", e.Message);
    }

    // ---- new formats, through ffmpeg ---------------------------------------------------

    [Theory]
    [InlineData("t.opus", "-c:a", "libopus")]
    [InlineData("t.oga", "-c:a", "libvorbis")]
    [InlineData("t.wma", "-c:a", "wmav2")]
    [InlineData("t.aiff", "-c:a", "pcm_s16be")]
    [InlineData("t.aif", "-c:a", "pcm_s16be")]
    [InlineData("t.caf", "-c:a", "pcm_s16le")]
    [InlineData("t.3gp", "-c:a", "aac")]
    [InlineData("t.m4b", "-c:a", "aac", "-f", "ipod")]
    [InlineData("t.ac3", "-c:a", "ac3")]
    [InlineData("t.mka", "-c:a", "libvorbis")]
    [InlineData("t.mp2", "-c:a", "mp2")]
    [InlineData("t.wv", "-c:a", "wavpack")]
    [InlineData("t.au", "-c:a", "pcm_s16be")]
    [InlineData("t.flv", "-c:a", "libmp3lame", "-ar", "44100")]
    [InlineData("t.wmv", "-c:a", "wmav2", "-f", "asf")]
    [InlineData("t.mpg", "-c:a", "mp2")]
    [InlineData("t.amr", "-c:a", "libvo_amrwbenc", "-ar", "16000", "-ac", "1")]
    public async Task New_audio_formats_convert(string name, params string[] codecArgs)
    {
        await AssertConvertsAsync(await EncodeAsync(name, codecArgs));
    }

    // ---- wav and mp3 that the in-process decoder cannot read: ffmpeg takes over ------------

    [Theory]
    [InlineData("adpcm-ms.wav", "-c:a", "adpcm_ms")]
    [InlineData("adpcm-ima.wav", "-c:a", "adpcm_ima_wav")]
    [InlineData("alaw.wav", "-c:a", "pcm_alaw", "-ar", "8000")]
    [InlineData("mulaw.wav", "-c:a", "pcm_mulaw", "-ar", "8000")]
    [InlineData("mp3-in-wav.wav", "-c:a", "libmp3lame", "-ar", "44100")]
    [InlineData("surround.wav", "-c:a", "pcm_s16le", "-af", "pan=5.1|FL=c0|FR=c0|FC=c0|LFE=c0|BL=c0|BR=c0")]
    public async Task Files_the_in_process_decoder_rejects_fall_back_to_ffmpeg(string name, params string[] codecArgs)
    {
        var path = await EncodeAsync(name, codecArgs);

        // Prove the premise: the in-process decoder alone cannot read this file.
        await Assert.ThrowsAnyAsync<Exception>(() => ServicesFixture.AudioService.GetAudioFilePathAsync(path));

        await AssertConvertsAsync(path);
    }

    [Theory]
    [InlineData("plain.wav", "-c:a", "pcm_s24le")]
    [InlineData("plain.mp3", "-c:a", "libmp3lame")]
    [InlineData("layer2.mp3", "-c:a", "mp2", "-f", "mp2")]
    public async Task Ordinary_wav_and_mp3_still_convert(string name, params string[] codecArgs)
    {
        await AssertConvertsAsync(await EncodeAsync(name, codecArgs));
    }

    // ---- when the fallback applies -------------------------------------------------------

    private sealed class Audio(Func<Task<string>> run) : IAudioService
    {
        public int Calls { get; private set; }
        public Task<string> GetAudioFilePathAsync(string path, CancellationToken ct = default) { Calls++; return run(); }
    }

    private sealed class Video : IVideoReaderService
    {
        public int Calls { get; private set; }
        public Task<string> ConvertVideoToAudioAsync(string path, CancellationToken ct = default) { Calls++; return Task.FromResult("from-ffmpeg.wav"); }
    }

    [Fact]
    public async Task A_good_in_process_read_does_not_start_ffmpeg()
    {
        var audio = new Audio(() => Task.FromResult("in-process.wav"));
        var video = new Video();

        var result = await ServicesFixture.FileReader(audio, video).GetAudioFileFromFileAsync("a.mp3");

        Assert.Equal("in-process.wav", result);
        Assert.Equal(0, video.Calls);
    }

    [Fact]
    public async Task A_failed_in_process_read_is_retried_with_ffmpeg()
    {
        var audio = new Audio(() => throw new ArgumentException("Unsupported source encoding"));
        var video = new Video();

        var result = await ServicesFixture.FileReader(audio, video).GetAudioFileFromFileAsync("a.wav");

        Assert.Equal("from-ffmpeg.wav", result);
        Assert.Equal(1, audio.Calls);
        Assert.Equal(1, video.Calls);
    }

    [Fact]
    public async Task Cancelling_is_not_mistaken_for_a_decoder_failure()
    {
        var audio = new Audio(() => throw new OperationCanceledException());
        var video = new Video();

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => ServicesFixture.FileReader(audio, video).GetAudioFileFromFileAsync("a.mp3"));
        Assert.Equal(0, video.Calls);
    }

    // ---- clear errors --------------------------------------------------------------------

    [Fact]
    public async Task A_video_without_sound_says_it_has_no_audio_track()
    {
        var path = Path.Combine(_dir, "silent.mp4");
        await RunFfmpegAsync("-f", "lavfi", "-i", "color=c=black:s=64x64:r=5:d=2", "-c:v", "mpeg4", path);

        var e = await Assert.ThrowsAsync<MediaReadException>(() => Reader().GetAudioFileFromFileAsync(path));

        Assert.Equal("This file has no audio track, so there is nothing to transcribe.", e.Message);
    }

    [Theory]
    [InlineData("broken.mp4")]
    [InlineData("broken.wav")]
    [InlineData("broken.mp3")]
    [InlineData("broken.flac")]
    public async Task A_file_that_is_not_media_says_it_could_not_be_read(string name)
    {
        var path = Path.Combine(_dir, name);
        File.WriteAllBytes(path, Enumerable.Range(0, 4096).Select(i => (byte)(i * 31 % 251)).ToArray());

        var e = await Assert.ThrowsAsync<MediaReadException>(() => Reader().GetAudioFileFromFileAsync(path));

        Assert.StartsWith("This file could not be read.", e.Message);
    }

    [Theory]
    [InlineData("Output file #0 does not contain any stream", "no audio track")]
    [InlineData("Stream map 0:a matches no streams.", "no audio track")]
    [InlineData("x.mp4: Invalid data found when processing input", "could not be read")]
    [InlineData("moov atom not found", "could not be read")]
    [InlineData("Error opening input: Invalid argument", "could not be read")]
    [InlineData("x.mp4: No such file or directory\nError opening input", "exit code")]
    [InlineData("x.mp4: Permission denied\nError opening input", "exit code")]
    public void Failure_text_is_translated(string ffmpegOutput, string expected)
    {
        Assert.Contains(expected, VideoReaderService.DescribeFailure(1, ffmpegOutput));
    }

    [Fact]
    public void Unknown_failures_keep_the_ffmpeg_detail()
    {
        var message = VideoReaderService.DescribeFailure(7, "one\ntwo\nthree\nfour");

        Assert.Contains("exit code 7", message);
        Assert.Contains("four", message);
        Assert.DoesNotContain("one", message);
    }
}
