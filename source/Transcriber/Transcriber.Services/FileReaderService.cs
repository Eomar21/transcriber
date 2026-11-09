public class FileReaderService : IFileReaderService
{
    // mp3 and wav are tried in-process first, which is fast and starts no program. If the in-process decoder cannot
    // read a file (for example an a-law wav, or an mp3 that is really MPEG layer 2), ffmpeg reads it instead.
    private static readonly HashSet<string> NativeAudio = [".mp3", ".wav"];

    // Everything else goes to the ffmpeg that is bundled with the app. ffmpeg recognises the content of a file, so these
    // lists are only about what the file pickers offer and what a drop accepts.
    private static readonly HashSet<string> FfmpegAudio =
    [
        ".m4a", ".m4b", ".aac", ".flac", ".ogg", ".oga", ".opus", ".wma", ".aiff", ".aif", ".caf",
        ".amr", ".ac3", ".mka", ".mp2", ".wv", ".au",
    ];

    private static readonly HashSet<string> FfmpegVideo =
    [
        ".mp4", ".m4v", ".mov", ".3gp", ".avi", ".mkv", ".webm", ".wmv", ".mpg", ".mpeg", ".flv",
    ];

    public static readonly IReadOnlyList<string> AudioExtensions = [.. NativeAudio, .. FfmpegAudio];
    public static readonly IReadOnlyList<string> VideoExtensions = [.. FfmpegVideo];
    public static readonly IReadOnlyList<string> SupportedExtensions = [.. AudioExtensions, .. VideoExtensions];

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
        {
            try
            {
                return await _audioService.GetAudioFilePathAsync(filePath);
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                // Fall through: ffmpeg handles the formats the in-process decoder does not.
            }
            return await _videoReaderService.ConvertVideoToAudioAsync(filePath);
        }

        if (FfmpegAudio.Contains(ext) || FfmpegVideo.Contains(ext))
            return await _videoReaderService.ConvertVideoToAudioAsync(filePath);

        throw new NotSupportedException($"File type '{ext}' is not supported.");
    }
}
