using System.Diagnostics;
using NAudio.Wave;

public class FfmpegLocatorTests : IDisposable
{
    private readonly string _dir = Directory.CreateTempSubdirectory().FullName;

    public void Dispose() => Directory.Delete(_dir, true);

    [Fact]
    public void Returns_null_when_nothing_is_bundled() =>
        Assert.Null(FfmpegLocator.Locate(_dir));

    [Fact]
    public void Finds_the_bundled_binary_next_to_the_app()
    {
        var name = OperatingSystem.IsWindows() ? "ffmpeg.exe" : (System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture == System.Runtime.InteropServices.Architecture.Arm64 ? "ffmpeg-arm64" : "ffmpeg-x64");
        File.WriteAllText(Path.Combine(_dir, name), "fake");

        Assert.Equal(Path.Combine(_dir, name), FfmpegLocator.Locate(_dir));
    }

    [Fact]
    public void The_app_build_actually_contains_ffmpeg()
    {
        // Proves the build step bundled it: this runs from the test output folder.
        var path = FfmpegLocator.Locate();
        Assert.NotNull(path);
        Assert.True(new FileInfo(path!).Length > 10_000_000, "bundled ffmpeg looks truncated");
    }
}

/// <summary>Converts real video containers with the bundled ffmpeg through the app's normal file path.</summary>
public class VideoConversionTests : IDisposable
{
    private readonly string _dir = Directory.CreateTempSubdirectory().FullName;

    public void Dispose() => Directory.Delete(_dir, true);

    private static string Sample(string name) => Path.Combine(AppContext.BaseDirectory, "TestData", name);

    /// <summary>Wraps an audio sample into a tiny video file so we have real mp4/mkv input.</summary>
    internal static async Task<string> MakeVideoAsync(string dir, string audioPath, string extension)
    {
        var output = Path.Combine(dir, "clip" + extension);
        var psi = new ProcessStartInfo(FfmpegLocator.Locate()!)
        {
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        foreach (var a in new[] { "-y", "-f", "lavfi", "-i", "color=c=black:s=64x64:r=5", "-i", audioPath,
                                  "-c:v", "mpeg4", "-c:a", "aac", "-shortest", output })
            psi.ArgumentList.Add(a);

        using var p = Process.Start(psi)!;
        var err = p.StandardError.ReadToEndAsync();
        await p.WaitForExitAsync();
        Assert.True(p.ExitCode == 0, "could not create test video: " + await err);
        return output;
    }

    [Theory]
    [InlineData(".mp4")]
    [InlineData(".mkv")]
    public async Task Video_audio_is_extracted_as_16khz_mono_wav(string extension)
    {
        var video = await MakeVideoAsync(_dir, Sample("en-01.mp3"), extension);
        var reader = new FileReaderService(new AudioService(), new VideoReaderService());

        var wav = await reader.GetAudioFileFromFileAsync(video);
        try
        {
            using var wr = new WaveFileReader(wav);
            Assert.Equal(16000, wr.WaveFormat.SampleRate);
            Assert.Equal(1, wr.WaveFormat.Channels);
            Assert.Equal(16, wr.WaveFormat.BitsPerSample);
            Assert.InRange(wr.TotalTime.TotalSeconds, 8, 25);
            Assert.NotEqual(video, wav);
            Assert.True(File.Exists(video), "the source video must be left alone");
        }
        finally { File.Delete(wav); }
    }

    [Fact]
    public async Task A_corrupt_video_reports_that_it_cannot_be_read_and_leaves_no_temp_file()
    {
        var bad = Path.Combine(_dir, "broken.mp4");
        await File.WriteAllTextAsync(bad, "this is not a video");
        // A private temp folder, so other tests running in parallel cannot affect the count.
        var tempDir = Directory.CreateDirectory(Path.Combine(_dir, "tmp")).FullName;

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => new VideoReaderService(tempDirectory: tempDir).ConvertVideoToAudioAsync(bad));

        Assert.StartsWith("This file could not be read.", ex.Message);
        Assert.Empty(Directory.GetFiles(tempDir));
    }
}
