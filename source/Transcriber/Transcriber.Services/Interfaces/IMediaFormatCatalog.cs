namespace Transcriber.Services.Interfaces;

/// <summary>The file types that the app accepts. The list comes from the app settings.</summary>
public interface IMediaFormatCatalog
{
    /// <summary>Extensions of audio files, in lower case with a dot (".mp3").</summary>
    IReadOnlyList<string> AudioExtensions { get; }

    IReadOnlyList<string> VideoExtensions { get; }

    /// <summary>Audio and video extensions together.</summary>
    IReadOnlyList<string> SupportedExtensions { get; }

    /// <summary>True when the extension of the file is in the list. The case of the extension does not matter.</summary>
    bool IsSupported(string path);
}
