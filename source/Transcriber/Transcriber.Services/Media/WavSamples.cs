using System.Runtime.InteropServices;
using NAudio.Wave;
using Transcriber.Services.Models;

namespace Transcriber.Services.Media;

/// <summary>
/// Reads a 16-bit PCM wav as the float samples that the speech models take. It reads the file in large blocks and
/// makes one array, so a long recording is read in a moment.
/// </summary>
internal static class WavSamples
{
    private const int FramesPerBlock = 16384;

    /// <param name="span">The stretch to read, or null for the whole file.</param>
    /// <returns>One sample for each moment in time. A file with more than one channel gives the average of its channels.</returns>
    /// <exception cref="InvalidDataException">The file is not 16-bit PCM.</exception>
    public static float[] Read(string path, SpeechSpan? span = null)
    {
        using var reader = new WaveFileReader(path);
        var format = reader.WaveFormat;
        if (format.Encoding != WaveFormatEncoding.Pcm || format.BitsPerSample != 16)
            throw new InvalidDataException("The audio must be a 16-bit PCM wav.");

        var frameBytes = format.BlockAlign;
        long start = 0;
        var length = reader.Length;
        if (span is { } s)
        {
            start = (long)(s.Start.TotalSeconds * format.AverageBytesPerSecond) / frameBytes * frameBytes;
            length = (long)(s.Length.TotalSeconds * format.AverageBytesPerSecond) / frameBytes * frameBytes;
        }
        length = Math.Min(length, Math.Max(0, reader.Length - start));

        reader.Position = start;
        var channels = format.Channels;
        var scale = 32768f * channels;
        var samples = new float[length / frameBytes];
        var buffer = new byte[FramesPerBlock * frameBytes];
        var filled = 0;
        while (filled < samples.Length)
        {
            var wanted = (int)Math.Min(buffer.Length, (long)(samples.Length - filled) * frameBytes);
            var frames = Fill(reader, buffer, wanted) / frameBytes;
            if (frames == 0) break;

            var values = MemoryMarshal.Cast<byte, short>(buffer.AsSpan(0, frames * frameBytes));
            for (var frame = 0; frame < frames; frame++)
            {
                var sum = 0;
                for (var channel = 0; channel < channels; channel++) sum += values[frame * channels + channel];
                samples[filled++] = sum / scale;
            }
        }

        // A file that is shorter than its header says gives the samples that are there.
        return filled == samples.Length ? samples : samples[..filled];
    }

    /// <summary>The number of samples for each second of the file.</summary>
    public static int SampleRate(string path)
    {
        using var reader = new WaveFileReader(path);
        return reader.WaveFormat.SampleRate;
    }

    private static int Fill(WaveFileReader reader, byte[] buffer, int wanted)
    {
        var read = 0;
        while (read < wanted)
        {
            var n = reader.Read(buffer, read, wanted - read);
            if (n == 0) break;
            read += n;
        }
        return read;
    }
}
