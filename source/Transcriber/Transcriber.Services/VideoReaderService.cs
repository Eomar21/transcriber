using System.ComponentModel;
using System.Diagnostics;

public class VideoReaderService : IVideoReaderService
{
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
                var tail = string.Join(Environment.NewLine, errorText.Split('\n').TakeLast(3));
                throw new InvalidOperationException($"ffmpeg failed (exit code {process.ExitCode}): {tail}");
            }
        }

        return outputFile;
    }
}
