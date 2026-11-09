using Microsoft.Extensions.Options;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using NLayer.NAudioSupport;
using Transcriber.Services.Configuration;
using Transcriber.Services.Interfaces;

namespace Transcriber.Services.Media;

/// <summary>
/// Converts mp3 and wav input into the format Whisper requires: 16 kHz, mono, 16-bit PCM wav (the numbers are in the
/// app settings). It runs fully in the app with a managed mp3 decoder, so no ffmpeg is needed for these files.
/// </summary>
internal sealed class AudioService(IOptions<AudioSettings> options) : IAudioService
{
    private readonly AudioSettings _audio = options.Value;

    public Task<string> GetAudioFilePathAsync(string audioFilePath) => Task.Run(() => ConvertToWhisperWav(audioFilePath));

    private string ConvertToWhisperWav(string inputPath)
    {
        var ext = Path.GetExtension(inputPath).ToLowerInvariant();

        using WaveStream reader = ext switch
        {
            ".mp3" => new Mp3FileReaderBase(inputPath, wf => new Mp3FrameDecompressor(wf)),
            ".wav" => new WaveFileReader(inputPath),
            _ => throw new NotSupportedException($"Audio type '{ext}' is not supported."),
        };

        // Already in the right shape: use the file as is.
        var fmt = reader.WaveFormat;
        if (ext == ".wav" && fmt.Encoding == WaveFormatEncoding.Pcm
            && fmt.SampleRate == _audio.SampleRate && fmt.Channels == _audio.Channels && fmt.BitsPerSample == 16)
        {
            return inputPath;
        }

        ISampleProvider samples = reader.ToSampleProvider();
        if (samples.WaveFormat.Channels == 2)
        {
            // Downmix by averaging both channels instead of dropping one.
            samples = new StereoToMonoSampleProvider(samples) { LeftVolume = 0.5f, RightVolume = 0.5f };
        }
        else if (samples.WaveFormat.Channels > 2)
        {
            throw new NotSupportedException("Only mono and stereo audio are supported.");
        }

        if (samples.WaveFormat.SampleRate != _audio.SampleRate)
        {
            samples = new WdlResamplingSampleProvider(samples, _audio.SampleRate);
        }

        var outputPath = Path.Combine(Path.GetTempPath(), $"transcriber_{Guid.NewGuid():N}.wav");
        WaveFileWriter.CreateWaveFile16(outputPath, samples);
        return outputPath;
    }
}
