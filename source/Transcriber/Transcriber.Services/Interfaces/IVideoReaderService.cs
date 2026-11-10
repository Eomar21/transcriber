using Transcriber.Services.Exceptions;

namespace Transcriber.Services.Interfaces;

/// <summary>Converts any file that ffmpeg can read to the audio format that Whisper needs.</summary>
internal interface IVideoReaderService
{
    /// <exception cref="MediaReadException">The file cannot be read as audio.</exception>
    Task<string> ConvertVideoToAudioAsync(string videoFilePath, CancellationToken ct = default);
}
