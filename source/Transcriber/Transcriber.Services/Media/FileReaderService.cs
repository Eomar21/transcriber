using Microsoft.Extensions.Options;
using Transcriber.Services.Configuration;
using Transcriber.Services.Interfaces;

namespace Transcriber.Services.Media;

/// <summary>
/// Chooses who reads a file. A file of a type in the "native" list is tried in the app first, which is fast and starts no
/// program. If the app cannot read it (for example an a-law wav, or an mp3 that is really MPEG layer 2), ffmpeg reads it
/// instead. Every other file goes to ffmpeg, which recognises a file by its content.
/// </summary>
internal sealed class FileReaderService : IFileReaderService
{
    private readonly IAudioService _audioService;
    private readonly IVideoReaderService _videoReaderService;
    private readonly HashSet<string> _native;

    public FileReaderService(IAudioService audioService, IVideoReaderService videoReaderService, IOptions<MediaSettings> options)
    {
        _audioService = audioService;
        _videoReaderService = videoReaderService;
        _native = new HashSet<string>(options.Value.NativeExtensions, StringComparer.OrdinalIgnoreCase);
    }

    public async Task<string> GetAudioFileFromFileAsync(string filePath, CancellationToken ct = default)
    {
        if (_native.Contains(Path.GetExtension(filePath)))
        {
            try
            {
                return await _audioService.GetAudioFilePathAsync(filePath, ct);
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                // Fall through: ffmpeg handles the formats that the in-process decoder does not.
            }
        }

        // A file with an unlisted extension only gets here when the person chose it on purpose ("Any file type"),
        // so ffmpeg gets to try, and a file that it cannot read gets a clear message.
        return await _videoReaderService.ConvertVideoToAudioAsync(filePath, ct);
    }
}
