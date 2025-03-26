// Service that deals with audio files.
public interface IAudioService
{
    Task<string> GetAudioFilePathAsync(string audioFilePath);
}