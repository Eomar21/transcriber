public interface IWhisperService
{
    /// <summary>The native runtime used by the last run ("Vulkan", "Cpu", ...), or null before the first run.</summary>
    string? LastRuntime { get; }

    /// <summary>How much of the speech found in the last run became text, or null when speech detection was not used.</summary>
    SpeechCoverage? LastCoverage => null;

    /// <summary>True when speakers can be identified on this computer (models and native library present).</summary>
    bool CanIdentifySpeakers => false;

    /// <param name="audioFilePath">16 kHz mono wav (use <see cref="IFileReaderService"/> to produce one).</param>
    Task<List<TranscriptSegment>> TranscribeAsync(
        string audioFilePath,
        TranscriptionOptions? options = null,
        IProgress<TranscriptionProgress>? progress = null,
        CancellationToken ct = default);
}
