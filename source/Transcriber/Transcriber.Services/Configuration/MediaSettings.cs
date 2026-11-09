namespace Transcriber.Services.Configuration;

/// <summary>Which files the app accepts, and who reads them.</summary>
internal sealed class MediaSettings
{
    /// <summary>Read in the app first, which is fast and starts no program. If that fails, ffmpeg reads the file.</summary>
    public List<string> NativeExtensions { get; set; } = [];

    public List<string> FfmpegAudioExtensions { get; set; } = [];

    public List<string> FfmpegVideoExtensions { get; set; } = [];

    public FfmpegSettings Ffmpeg { get; set; } = new();
}
