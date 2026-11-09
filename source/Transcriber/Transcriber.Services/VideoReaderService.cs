using System.ComponentModel;
using System.Diagnostics;

public class VideoReaderService : IVideoReaderService
{
    private const string CouldNotBeRead = "This file could not be read. It may be damaged, or it may not be an audio or video file.";

    private readonly string _ffmpegPath;
    private readonly string _tempDirectory;

    /// <param name="ffmpegPath">Path to ffmpeg. Defaults to the copy bundled with the app, then PATH (developer machines).</param>
    public VideoReaderService(string? ffmpegPath = null, string? tempDirectory = null)
    {
        _ffmpegPath = ffmpegPath ?? FfmpegLocator.Locate() ?? "ffmpeg";
        _tempDirectory = tempDirectory ?? Path.GetTempPath();
    }

    public async Task<string> ConvertVideoToAudioAsync(string videoFilePath)
    {
        if (!File.Exists(videoFilePath))
            throw new InvalidOperationException("This file was not found. It may have been moved or deleted.");
        // ffmpeg reports an empty file as a file without audio, which would mislead.
        if (new FileInfo(videoFilePath).Length == 0)
            throw new InvalidOperationException(CouldNotBeRead);

        // Temp file, so a video "talk.wav"-style name collision can never overwrite the user's files.
        var outputFile = Path.Combine(_tempDirectory, $"transcriber_{Guid.NewGuid():N}.wav");

        var startInfo = new ProcessStartInfo
        {
            FileName = _ffmpegPath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        // Drop video, mono, 16 kHz, 16-bit PCM: exactly what Whisper wants.
        foreach (var arg in new[] { "-y", "-i", videoFilePath, "-vn", "-ac", "1", "-ar", "16000", "-c:a", "pcm_s16le", outputFile })
            startInfo.ArgumentList.Add(arg);

        Process process;
        try
        {
            process = Process.Start(startInfo)!;
        }
        catch (Win32Exception)
        {
            throw new InvalidOperationException(
                "ffmpeg was not found. It is bundled with the app, so the installation looks incomplete: please reinstall the app.");
        }

        using (process)
        {
            // Read both streams so ffmpeg can never block on a full pipe buffer.
            var stdout = process.StandardOutput.ReadToEndAsync();
            var stderr = process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();
            await stdout;
            var errorText = await stderr;

            if (process.ExitCode != 0)
            {
                try { File.Delete(outputFile); } catch { /* best effort */ }
                throw new InvalidOperationException(DescribeFailure(process.ExitCode, errorText));
            }
        }

        return outputFile;
    }

    /// <summary>Turns what ffmpeg printed into a message a person can act on.</summary>
    public static string DescribeFailure(int exitCode, string ffmpegOutput)
    {
        // A video with no sound leaves nothing to write: ffmpeg says the output has no stream.
        if (ffmpegOutput.Contains("does not contain any stream", StringComparison.OrdinalIgnoreCase)
            || ffmpegOutput.Contains("matches no streams", StringComparison.OrdinalIgnoreCase))
            return "This file has no audio track, so there is nothing to transcribe.";

        // Damaged files, and files that only carry a media extension.
        var cannotOpen = ffmpegOutput.Contains("Error opening input", StringComparison.OrdinalIgnoreCase)
            && !ffmpegOutput.Contains("No such file", StringComparison.OrdinalIgnoreCase)
            && !ffmpegOutput.Contains("Permission denied", StringComparison.OrdinalIgnoreCase);
        if (cannotOpen
            || ffmpegOutput.Contains("Invalid data found when processing input", StringComparison.OrdinalIgnoreCase)
            || ffmpegOutput.Contains("moov atom not found", StringComparison.OrdinalIgnoreCase))
            return CouldNotBeRead;

        var tail = string.Join(Environment.NewLine, ffmpegOutput.Split('\n').TakeLast(3));
        return $"ffmpeg failed (exit code {exitCode}): {tail}";
    }
}
