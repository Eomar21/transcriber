using Microsoft.Extensions.Options;
using Transcriber.Services.Configuration;
using Transcriber.Services.Interfaces;

namespace Transcriber.Services.Catalog;

/// <summary>The file types from the app settings.</summary>
internal sealed class MediaFormatCatalog : IMediaFormatCatalog
{
    private readonly HashSet<string> _supported;

    public MediaFormatCatalog(IOptions<MediaSettings> options)
    {
        var media = options.Value;
        AudioExtensions = [.. media.NativeExtensions, .. media.FfmpegAudioExtensions];
        VideoExtensions = [.. media.FfmpegVideoExtensions];
        SupportedExtensions = [.. AudioExtensions, .. VideoExtensions];
        _supported = new HashSet<string>(SupportedExtensions, StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyList<string> AudioExtensions { get; }

    public IReadOnlyList<string> VideoExtensions { get; }

    public IReadOnlyList<string> SupportedExtensions { get; }

    public bool IsSupported(string path) => _supported.Contains(Path.GetExtension(path));
}
