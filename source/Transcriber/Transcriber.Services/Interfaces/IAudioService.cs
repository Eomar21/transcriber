namespace Transcriber.Services.Interfaces;

/// <summary>Converts mp3 and wav files to the audio format that Whisper needs, inside the app.</summary>
internal interface IAudioService
{
    Task<string> GetAudioFilePathAsync(string audioFilePath, CancellationToken ct = default);
}
