namespace Transcriber.Coach.Interfaces;

/// <summary>Gives the sample sentence that makes Whisper keep fillers and stutters instead of tidying them away.</summary>
public interface IVerbatimPromptProvider
{
    /// <returns>A prompt for the language, or null where no prompt is available.</returns>
    string? For(string? language);
}
