public class FileReaderService : IFileReaderService
{
    private readonly IAudioService _audioService;
    private readonly IVideoReaderService _videoReaderService;

    public FileReaderService(IAudioService audioService, IVideoReaderService videoReaderService)
    {
        _audioService = audioService;
        _videoReaderService = videoReaderService;
    }

    public async Task<string> GetAudioFileFromFileAsync(string filePath)
    {
        var ext = Path.GetExtension(filePath).ToLowerInvariant();

        // Check if it's a video file.
        if (ext == ".mp4" || ext == ".mov" || ext == ".avi" || ext == ".mkv")
        {
            return await _videoReaderService.ConvertVideoToAudioAsync(filePath);
        }
        // Check if it's an audio file.
        else if (ext == ".mp3" || ext == ".wav")
        {
            return await _audioService.GetAudioFilePathAsync(filePath);
        }
        else
        {
            throw new NotSupportedException("File type not supported.");
        }
    }
}