using Transcriber.Services.Exceptions;

namespace Transcriber.Services.Interfaces;

/// <summary>Turns any supported audio or video file into the audio file that Whisper needs.</summary>
public interface IFileReaderService
{
    /// <returns>The path of a 16 kHz mono wav. It can be the input file itself, or a temporary copy that the caller removes.</returns>
    /// <exception cref="MediaReadException">The file cannot be read as audio.</exception>
    Task<string> GetAudioFileFromFileAsync(string filePath);
}
