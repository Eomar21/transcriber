namespace Transcriber.Services.Models;

/// <summary>Per-run choices. A value that is left empty comes from the app settings.</summary>
public record TranscriptionOptions
{
    /// <summary>ISO code ("en", "nl", "ar", "es", ...) or "auto" to detect.</summary>
    public string Language { get; init; } = "auto";

    /// <summary>The model to use. Null uses the default model from the app settings.</summary>
    public WhisperModel? Model { get; init; }

    /// <summary>Optional vocabulary hint (names, jargon) that biases the decoder.</summary>
    public string? Prompt { get; init; }

    /// <summary>Try the GPU (Vulkan) first and fall back to the CPU automatically.</summary>
    public bool UseGpu { get; init; } = true;

    /// <summary>Drop silence hallucinations and collapse repetition loops.</summary>
    public bool FilterHallucinations { get; init; } = true;

    /// <summary>Find the speech first and transcribe only that, skipping silence and noise.</summary>
    public bool UseVad { get; init; } = true;

    /// <summary>
    /// Send short stretches of speech to the model together. This is faster for recordings with many short remarks.
    /// A line can then cover a pause between two stretches. Turn it off when the pauses between the lines must be exact.
    /// </summary>
    public bool PackSpeech { get; init; } = true;

    /// <summary>Work out who is speaking and label each line. Takes extra time on long recordings.</summary>
    public bool IdentifySpeakers { get; init; }

    /// <summary>How many people speak, when known (improves accuracy). Null lets the app work it out.</summary>
    public int? SpeakerCount { get; init; }

    /// <summary>Segments whose no-speech probability exceeds this are treated as silence. Null uses the app settings.</summary>
    public float? NoSpeechThreshold { get; init; }
}
