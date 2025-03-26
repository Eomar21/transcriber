using System.Diagnostics;

public class VideoReaderService : IVideoReaderService
{
    public async Task<string> ConvertVideoToAudioAsync(string videoFilePath)
    {
        // Change the file extension to .wav for the output.
        var outputFile = Path.ChangeExtension(videoFilePath, ".wav");

        // Use ffmpeg to convert the video to audio.
        var processInfo = new ProcessStartInfo
        {
            FileName = "ffmpeg",
            Arguments = $"-i \"{videoFilePath}\" -ar 16000 \"{outputFile}\" -y",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        using var process = Process.Start(processInfo);
        await process.WaitForExitAsync();

        return outputFile;
    }
}