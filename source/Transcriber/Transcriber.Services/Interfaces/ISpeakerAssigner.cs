using Transcriber.Services.Models;

namespace Transcriber.Services.Interfaces;

/// <summary>Puts speaker labels on transcript lines.</summary>
internal interface ISpeakerAssigner
{
    /// <returns>A copy of the segments with the speaker set where a speaker could be determined.</returns>
    List<TranscriptSegment> Assign(IReadOnlyList<TranscriptSegment> segments, IReadOnlyList<SpeakerTurn> turns);

    /// <summary>Hands short fragments that are not real speakers to the nearest real speaker in time.</summary>
    List<SpeakerTurn> MergeMinorSpeakers(IReadOnlyList<SpeakerTurn> turns);

    /// <summary>Renumbers the speakers in the order in which they first speak, so the numbers have no gaps.</summary>
    List<TranscriptSegment> RenumberByAppearance(IReadOnlyList<TranscriptSegment> segments);
}
