/// <summary>
/// How much of the speech that voice detection found ended up as text. A transcript that misses a large part of the speech
/// is a sign that something went wrong, for example Whisper stuck to one language in a recording that mixes two.
/// </summary>
/// <param name="SpeechSeconds">Seconds of speech found in the recording.</param>
/// <param name="TextSeconds">Seconds of that speech that are covered by transcript lines.</param>
public readonly record struct SpeechCoverage(double SpeechSeconds, double TextSeconds)
{
    /// <summary>At least this many seconds must be missing before the transcript is called incomplete.</summary>
    public const double MinMissingSeconds = 5;

    /// <summary>The share of speech that must be covered by text, or the transcript is called incomplete.</summary>
    public const double MinShare = 0.75;

    public double MissingSeconds => Math.Max(0, SpeechSeconds - TextSeconds);

    public double Share => SpeechSeconds <= 0 ? 1 : Math.Min(1, TextSeconds / SpeechSeconds);

    /// <summary>True when a clear part of the speech has no text.</summary>
    public bool LooksIncomplete => MissingSeconds >= MinMissingSeconds && Share < MinShare;

    /// <summary>
    /// Compares transcript lines with the speech spans. A line counts for the second on each side of it as well, so the
    /// normal pause between two sentences is not mistaken for missing speech.
    /// </summary>
    public static SpeechCoverage Measure(IReadOnlyList<SpeechSpan> spans, IEnumerable<TranscriptSegment> segments, TimeSpan? bridge = null)
    {
        var reach = bridge ?? TimeSpan.FromSeconds(1);
        var lines = segments.Select(s => (Start: s.Start - reach, End: s.End + reach)).OrderBy(l => l.Start).ToList();

        double speech = 0, text = 0;
        foreach (var span in spans)
        {
            speech += span.Length.TotalSeconds;

            var covered = TimeSpan.Zero;
            var cursor = span.Start;
            foreach (var line in lines)
            {
                var from = line.Start < cursor ? cursor : line.Start;
                var to = line.End > span.End ? span.End : line.End;
                if (to <= from) continue;
                covered += to - from;
                cursor = to;
            }
            text += covered.TotalSeconds;
        }
        return new SpeechCoverage(speech, text);
    }
}
