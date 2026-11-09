namespace Transcriber.Services.Speakers;

/// <summary>How a speaker is named for people. The model counts from zero, people count from one.</summary>
public static class SpeakerLabel
{
    /// <summary>"Speaker 1", "Speaker 2", ...</summary>
    public static string Format(int speaker) => $"Speaker {speaker + 1}";
}
