public class FileReaderService : IFileReaderService
{
    // mp3 and wav are decoded in-process; everything else needs ffmpeg.
    private static readonly HashSet<string> NativeAudio = [".mp3", ".wav"];
    private static readonly HashSet<string> FfmpegFormats = [".mp4", ".mov", ".avi", ".mkv", ".webm", ".m4a", ".flac", ".ogg", ".aac"];

    public static readonly IReadOnlyList<string> SupportedExtensions = [.. NativeAudio, .. FfmpegFormats];

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

        if (NativeAudio.Contains(ext))
            return await _audioService.GetAudioFilePathAsync(filePath);
        if (FfmpegFormats.Contains(ext))
            return await _videoReaderService.ConvertVideoToAudioAsync(filePath);

        throw new NotSupportedException($"File type '{ext}' is not supported.");
    }
}
