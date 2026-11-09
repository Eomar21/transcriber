using Transcriber.Services.Models;
using Transcriber.Coach.Models;

namespace Transcriber.Coach.Interfaces;

/// <summary>Finds the silences between lines of speech.</summary>
public interface IPauseDetector
{
    /// <summary>The length of silence that counts as a pause when no other length is given.</summary>
    TimeSpan DefaultThreshold { get; }

    /// <param name="orderedLines">Lines of speech, sorted by start time.</param>
    /// <param name="threshold">A gap at least this long is a pause. Null uses <see cref="DefaultThreshold"/>.</param>
    IReadOnlyList<PauseInfo> Find(IReadOnlyList<TranscriptSegment> orderedLines, TimeSpan? threshold = null);
}
