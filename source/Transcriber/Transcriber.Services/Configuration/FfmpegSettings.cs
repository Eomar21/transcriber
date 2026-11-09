namespace Transcriber.Services.Configuration;

/// <summary>The file names of the ffmpeg program that is built into the app, in the order they are tried.</summary>
internal sealed class FfmpegSettings
{
    public List<string> Windows { get; set; } = [];

    /// <summary>On an Apple Silicon Mac. The Intel program cannot run there without Rosetta, so it comes last.</summary>
    public List<string> MacArm64 { get; set; } = [];

    public List<string> MacIntel { get; set; } = [];
}
