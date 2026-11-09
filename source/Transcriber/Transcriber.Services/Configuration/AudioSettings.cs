namespace Transcriber.Services.Configuration;

/// <summary>The audio format that Whisper needs.</summary>
internal sealed class AudioSettings
{
    public int SampleRate { get; set; }

    public int Channels { get; set; }
}
