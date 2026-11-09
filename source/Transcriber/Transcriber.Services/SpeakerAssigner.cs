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

    /// <summary>A speaker with less talk time than this share of all speech (and under <see cref="MinorSpeakerMaxSeconds"/>) is treated as noise.</summary>
    public const double MinorSpeakerShare = 0.05;

    public const double MinorSpeakerMaxSeconds = 20;

    /// <summary>
    /// When the number of speakers is worked out automatically, clustering often splits one voice into many short
    /// "speakers" (a breath, a cough, a change of tone). This hands each such fragment to the nearest real speaker in time.
    /// A speaker is real when they talk for at least <see cref="MinorSpeakerShare"/> of all speech, or at least
    /// <see cref="MinorSpeakerMaxSeconds"/> seconds, so a short but genuine second voice is kept.
    /// </summary>
    public static List<SpeakerTurn> MergeMinorSpeakers(IReadOnlyList<SpeakerTurn> turns)
    {
        if (turns.Count == 0) return [];

        var talk = turns.GroupBy(t => t.Speaker).ToDictionary(g => g.Key, g => g.Sum(t => (t.End - t.Start).TotalSeconds));
        var total = talk.Values.Sum();
        var major = talk.Where(kv => kv.Value >= total * MinorSpeakerShare || kv.Value >= MinorSpeakerMaxSeconds).Select(kv => kv.Key).ToHashSet();
        if (major.Count == 0) major.Add(talk.MaxBy(kv => kv.Value).Key);
        if (major.Count == talk.Count) return [.. turns];

        var anchors = turns.Where(t => major.Contains(t.Speaker)).ToList();
        return turns.Select(t => major.Contains(t.Speaker) ? t : t with { Speaker = Nearest(t, anchors).Speaker }).ToList();
    }

    private static SpeakerTurn Nearest(SpeakerTurn turn, List<SpeakerTurn> anchors) =>
        anchors.MinBy(a => Math.Max(0, (Max(turn.Start, a.Start) - Min(turn.End, a.End)).TotalSeconds));

    /// <summary>
    /// Renumbers speakers in the order they first speak, so the first voice in the transcript is always speaker 0 (shown as
    /// "Speaker 1") and the numbers have no gaps, whatever numbers the model used.
    /// </summary>
    public static List<TranscriptSegment> RenumberByAppearance(IReadOnlyList<TranscriptSegment> segments)
    {
        var map = new Dictionary<int, int>();
        var result = new List<TranscriptSegment>(segments.Count);
        foreach (var s in segments.OrderBy(s => s.Start).ToList())
        {
            int? speaker = null;
            if (s.Speaker is int raw)
            {
                if (!map.TryGetValue(raw, out var mapped)) map[raw] = mapped = map.Count;
                speaker = mapped;
            }
            result.Add(new TranscriptSegment { Start = s.Start, End = s.End, Text = s.Text, Speaker = speaker });
        }
        return result;
    }

    /// <summary>"Speaker 1", "Speaker 2", ... (people count from one, the model from zero).</summary>
    public static string Label(int speaker) => $"Speaker {speaker + 1}";

    private static TimeSpan Min(TimeSpan a, TimeSpan b) => a < b ? a : b;
    private static TimeSpan Max(TimeSpan a, TimeSpan b) => a > b ? a : b;
}
