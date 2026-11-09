using Transcriber.Services.Models;

namespace Transcriber.Services.Interfaces;

/// <summary>Removes the typical Whisper artefacts: empty lines, stock phrases over silence, and repetition loops.</summary>
internal interface ITranscriptFilter
{
    List<TranscriptSegment> Clean(IEnumerable<TranscriptSegment> segments);
}
