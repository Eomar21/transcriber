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

    /// <summary>Segments whose no-speech probability exceeds this are treated as silence.</summary>
    public float NoSpeechThreshold { get; init; } = 0.6f;
}

/// <summary>Language choices for the UI. "auto" lets Whisper detect.</summary>
public static class SupportedLanguages
{
    public static readonly IReadOnlyList<(string Code, string Name)> All =
    [
        ("auto", "Auto-detect"),
        ("en", "English"),
        ("nl", "Dutch (Nederlands)"),
        ("ar", "Arabic (العربية)"),
        ("es", "Spanish (Español)"),
        ("fr", "French"),
        ("de", "German"),
        ("it", "Italian"),
        ("pt", "Portuguese"),
        ("tr", "Turkish"),
        ("ru", "Russian"),
        ("zh", "Chinese"),
        ("ja", "Japanese"),
    ];
}
