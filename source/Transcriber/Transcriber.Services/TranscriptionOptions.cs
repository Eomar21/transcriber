/// <summary>Per-run settings. Defaults favour accuracy on non-English languages.</summary>
public record TranscriptionOptions
{
    /// <summary>ISO code ("en", "nl", "ar", "es", ...) or "auto" to detect.</summary>
    public string Language { get; init; } = "auto";

    public WhisperModel Model { get; init; } = WhisperModel.Small;

    /// <summary>Optional vocabulary hint (names, jargon) that biases the decoder.</summary>
    public string? Prompt { get; init; }

    /// <summary>Try the GPU (Vulkan) first and fall back to the CPU automatically.</summary>
    public bool UseGpu { get; init; } = true;

    /// <summary>Drop silence hallucinations and collapse repetition loops.</summary>
    public bool FilterHallucinations { get; init; } = true;

    /// <summary>Find the speech first and transcribe only that, skipping silence and noise.</summary>
    public bool UseVad { get; init; } = true;

    /// <summary>Work out who is speaking and label each line. Takes extra time on long recordings.</summary>
    public bool IdentifySpeakers { get; init; }

    /// <summary>How many people speak, when known (improves accuracy). Null lets the app work it out.</summary>
    public int? SpeakerCount { get; init; }

    /// <summary>Segments whose no-speech probability exceeds this are treated as silence.</summary>
    public float NoSpeechThreshold { get; init; } = 0.6f;
}

