using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using Microsoft.Extensions.Options;
using Transcriber.Services.Configuration;
using Transcriber.Services.Exceptions;
using Transcriber.Services.Interfaces;
using Transcriber.Services.Models;

namespace Transcriber.Services.Media;

/// <summary>Uses the ffmpeg that is built into the app to take the audio from a video, or to read an audio format that the app cannot read itself.</summary>
internal sealed class VideoReaderService : IVideoReaderService
{
    private const string CouldNotBeRead = "This file could not be read. It may be damaged, or it may not be an audio or video file.";

    private readonly string _ffmpegPath;
    private readonly string _tempDirectory;
    private readonly AudioSettings _audio;

    /// <param name="ffmpegPath">Path to ffmpeg. Defaults to the copy built into the app, then PATH (developer machines).</param>
    /// <param name="tempDirectory">Where the converted audio goes. Defaults to the system temporary folder.</param>
    public VideoReaderService(IFfmpegLocator locator, IOptions<AudioSettings> audio, string? ffmpegPath = null, string? tempDirectory = null)
    {
        _audio = audio.Value;
        _ffmpegPath = ffmpegPath ?? locator.Locate() ?? "ffmpeg";
        _tempDirectory = tempDirectory ?? Path.GetTempPath();
    }

    public async Task<string> ConvertVideoToAudioAsync(string videoFilePath)
    {
        if (!File.Exists(videoFilePath))
            throw new MediaReadException(MediaReadFailure.FileNotFound, "This file was not found. It may have been moved or deleted.");
        // ffmpeg reports an empty file as a file without audio, which would mislead.
        if (new FileInfo(videoFilePath).Length == 0)
            throw new MediaReadException(MediaReadFailure.Unreadable, CouldNotBeRead);

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
        // Drop video and write the audio format that Whisper wants.
        foreach (var arg in new[]
        {
            "-y", "-i", videoFilePath, "-vn",
            "-ac", _audio.Channels.ToString(CultureInfo.InvariantCulture),
            "-ar", _audio.SampleRate.ToString(CultureInfo.InvariantCulture),
            "-c:a", "pcm_s16le", outputFile,
        })
            startInfo.ArgumentList.Add(arg);

        Process process;
        try
        {
            process = Process.Start(startInfo)!;
        }
        catch (Win32Exception)
        {
            throw new MediaReadException(MediaReadFailure.ConverterUnavailable,
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
                var (reason, message) = Classify(process.ExitCode, errorText);
                throw new MediaReadException(reason, message);
            }
        }

        return outputFile;
    }

    /// <summary>Turns what ffmpeg printed into a message a person can act on.</summary>
    public static string DescribeFailure(int exitCode, string ffmpegOutput) => Classify(exitCode, ffmpegOutput).Message;

    private static (MediaReadFailure Reason, string Message) Classify(int exitCode, string ffmpegOutput)
    {
        // A video with no sound leaves nothing to write: ffmpeg says the output has no stream.
        if (Mentions(ffmpegOutput, "does not contain any stream") || Mentions(ffmpegOutput, "matches no streams"))
            return (MediaReadFailure.NoAudioTrack, "This file has no audio track, so there is nothing to transcribe.");

        // Damaged files, and files that only carry a media extension.
        var cannotOpen = Mentions(ffmpegOutput, "Error opening input")
            && !Mentions(ffmpegOutput, "No such file")
            && !Mentions(ffmpegOutput, "Permission denied");
        if (cannotOpen || Mentions(ffmpegOutput, "Invalid data found when processing input") || Mentions(ffmpegOutput, "moov atom not found"))
            return (MediaReadFailure.Unreadable, CouldNotBeRead);

        var tail = string.Join(Environment.NewLine, ffmpegOutput.Split('\n').TakeLast(3));
        return (MediaReadFailure.ConversionFailed, $"ffmpeg failed (exit code {exitCode}): {tail}");
    }

    private static bool Mentions(string text, string phrase) => text.Contains(phrase, StringComparison.OrdinalIgnoreCase);
}
