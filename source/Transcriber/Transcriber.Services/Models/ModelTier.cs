namespace Transcriber.Services.Models;

/// <summary>How much a Whisper model can be trusted for a language. Higher is a bigger, more accurate model.</summary>
public enum ModelTier
{
    Base = 0,
    Small = 1,
    Medium = 2,
    Large = 3,
}
