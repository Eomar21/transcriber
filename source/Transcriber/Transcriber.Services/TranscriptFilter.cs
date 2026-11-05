/// <summary>
/// Cleans typical Whisper artefacts: empty or placeholder segments, stock phrases it invents over silence,
/// and the repetition loops where the same line is emitted over and over.
/// </summary>
public static class TranscriptFilter
{
    // Whole-segment matches only (after normalisation), so real speech containing these words is kept.
    private static readonly HashSet<string> StockHallucinations = new[]
    {
        "blank_audio", "music", "silence", "applause",
        "thanks for watching", "thank you for watching",
        "subtitles by the amara.org community", "ondertiteld door de amara.org gemeenschap",
        "subtítulos realizados por la comunidad de amara.org",
        "ترجمة نانسي قنقر", "اشتركوا في القناة",
    }.Select(TextMetrics.Normalize).ToHashSet(StringComparer.Ordinal);

    public static List<TranscriptSegment> Clean(IEnumerable<TranscriptSegment> segments)
    {
        var result = new List<TranscriptSegment>();
        string? previousKey = null;

        foreach (var seg in segments)
        {
            var text = seg.Text.Trim();
            var key = TextMetrics.Normalize(text);
            if (key.Length == 0 || StockHallucinations.Contains(key)) continue;

            // Same line again right after itself: extend the previous segment instead of repeating.
            if (key == previousKey && result.Count > 0)
            {
                result[^1].End = seg.End;
                continue;
            }

            result.Add(new TranscriptSegment { Start = seg.Start, End = seg.End, Text = text });
            previousKey = key;
        }
        return result;
    }
}
