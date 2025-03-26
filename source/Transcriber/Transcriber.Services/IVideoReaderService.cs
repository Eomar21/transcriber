// Service that converts video files to .wav audio files.
public interface IVideoReaderService
{
    Task<string> ConvertVideoToAudioAsync(string videoFilePath);
}