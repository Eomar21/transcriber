using Transcriber.Services.Models;

namespace Transcriber.Services.Interfaces;

/// <summary>Saves a transcript as a file.</summary>
public interface ITranscriptExporter
{
    string Extension(TranscriptFormat format);

    /// <summary>The transcript as text in the format.</summary>
    string Render(IReadOnlyList<TranscriptSegment> segments, TranscriptFormat format);

    /// <summary>Writes "{source name}_transcript.{extension}" next to the source file.</summary>
    /// <returns>The path of the new file.</returns>
    Task<string> WriteAsync(IReadOnlyList<TranscriptSegment> segments, string sourceFilePath, TranscriptFormat format);
}
