namespace Transcriber.Services.Models;

/// <summary>Why a file could not be turned into audio.</summary>
public enum MediaReadFailure
{
    /// <summary>The file does not exist any more.</summary>
    FileNotFound,

    /// <summary>The file is a video, or an audio file, without any sound.</summary>
    NoAudioTrack,

    /// <summary>The file is damaged, empty, or not a media file.</summary>
    Unreadable,

    /// <summary>The converter that is part of the app could not be started.</summary>
    ConverterUnavailable,

    /// <summary>The converter failed for a reason that has no plain explanation.</summary>
    ConversionFailed,
}
