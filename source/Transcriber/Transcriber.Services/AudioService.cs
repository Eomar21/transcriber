public class AudioService : IAudioService
{
    public Task<string> GetAudioFilePathAsync(string audioFilePath)
    {
        // If the file is mp3 or wav and the model accepts it,
        // just return the same file path.
        return Task.FromResult(audioFilePath);
    }
}