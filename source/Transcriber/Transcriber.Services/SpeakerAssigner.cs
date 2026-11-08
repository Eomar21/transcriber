/// <summary>One stretch of the recording where a single speaker talks (speaker numbers start at 0).</summary>
public readonly record struct SpeakerTurn(TimeSpan Start, TimeSpan End, int Speaker);

/// <summary>Labels transcript segments with the speaker who talks for most of each segment.</summary>
public static class SpeakerAssigner
{
    /// <returns>A copy of the segments with <see cref="TranscriptSegment.Speaker"/> set where a speaker could be determined.</returns>
    public static List<TranscriptSegment> Assign(IReadOnlyList<TranscriptSegment> segments, IReadOnlyList<SpeakerTurn> turns)
    {
        var result = new List<TranscriptSegment>(segments.Count);
        foreach (var segment in segments)
        {
            var overlap = new Dictionary<int, double>();
            foreach (var turn in turns)
            {
                var seconds = (Min(segment.End, turn.End) - Max(segment.Start, turn.Start)).TotalSeconds;
                if (seconds > 0) overlap[turn.Speaker] = overlap.GetValueOrDefault(turn.Speaker) + seconds;
            }

            int? speaker = overlap.Count == 0 ? null : overlap.MaxBy(kv => kv.Value).Key;
            result.Add(new TranscriptSegment { Start = segment.Start, End = segment.End, Text = segment.Text, Speaker = speaker });
        }
        return result;
    }

    /// <summary>"Speaker 1", "Speaker 2", ... (people count from one, the model from zero).</summary>
    public static string Label(int speaker) => $"Speaker {speaker + 1}";

    private static TimeSpan Min(TimeSpan a, TimeSpan b) => a < b ? a : b;
    private static TimeSpan Max(TimeSpan a, TimeSpan b) => a > b ? a : b;
}
