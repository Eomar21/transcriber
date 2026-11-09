namespace Transcriber.Services.Interfaces;

/// <summary>Finds the ffmpeg that is built into the app, so a person never has to install it.</summary>
internal interface IFfmpegLocator
{
    /// <returns>The full path of the ffmpeg program, or null if this build does not contain one.</returns>
    string? Locate(string? baseDirectory = null);
}
