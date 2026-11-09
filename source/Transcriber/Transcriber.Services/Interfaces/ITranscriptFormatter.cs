using Transcriber.Services.Models;

namespace Transcriber.Services.Interfaces;

/// <summary>Writes a transcript in one file format. A new format is a new formatter, with no change to the exporter.</summary>
internal interface ITranscriptFormatter
{
    TranscriptFormat Format { get; }

    /// <summary>The file extension with a dot (".srt").</summary>
    string Extension { get; }

    string Render(IReadOnlyList<TranscriptSegment> segments);
}
