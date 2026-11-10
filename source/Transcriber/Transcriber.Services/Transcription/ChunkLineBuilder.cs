using Transcriber.Services.Interfaces;
using Transcriber.Services.Models;

namespace Transcriber.Services.Transcription;

/// <summary>
/// When stretches of speech go to Whisper together, Whisper can put the words of two stretches in one line. That line
/// would cover the pause between the stretches, and it could hold the words of two people. This builder cuts such a
/// line at each join. The times of the parts say on which side of a join a word is. The text itself is cut between
/// words, and no character is changed.
/// </summary>
internal sealed class ChunkLineBuilder : IChunkLineBuilder
{
    // When the parts do not add up to the text, a cut moves to the nearest space that is this close.
    private const int SearchForSpace = 12;

    public IReadOnlyList<TranscriptSegment> Build(SpeechChunk chunk, TimedText line, IReadOnlyList<TimedText> parts)
    {
        var whole = chunk.ToRecording(line.Start, line.End);
        var cuts = chunk.Pieces.Count > 1 ? FindCuts(chunk, line, parts) : [];
        if (cuts.Count == 0) return [Line(line.Text, whole.Start, whole.End)];

        var lines = new List<TranscriptSegment>(cuts.Count + 1);
        var from = 0;
        for (var i = 0; i <= cuts.Count; i++)
        {
            var isFirst = i == 0;
            var isLast = i == cuts.Count;
            var piece = chunk.Pieces[isFirst ? cuts[0].PieceBefore : cuts[i - 1].PieceAfter].Span;
            var to = isLast ? line.Text.Length : cuts[i].Position;
            var text = line.Text[from..to];
            from = to;
            if (string.IsNullOrWhiteSpace(text)) continue;

            // The first and the last line keep the time that Whisper gave. A cut is at the edge of a stretch.
            var start = isFirst ? Clamp(whole.Start, piece) : piece.Start;
            var end = isLast ? Clamp(whole.End, piece) : piece.End;
            lines.Add(Line(text, start, end > start ? end : start));
        }
        return lines;
    }

    /// <summary>The places in the text where the next word is in a later stretch of speech.</summary>
    private static List<(int Position, int PieceBefore, int PieceAfter)> FindCuts(SpeechChunk chunk, TimedText line, IReadOnlyList<TimedText> parts)
    {
        var cuts = new List<(int Position, int PieceBefore, int PieceAfter)>();
        var total = parts.Sum(p => p.Text.Length);
        if (total == 0) return cuts;

        // Usually the parts are the text. If they are not (a part can be half a character), the cut is by share.
        var exact = total == line.Text.Length;
        var seen = 0;
        var current = -1;
        foreach (var part in parts)
        {
            // A part with no time gives no information, and a mark such as a full stop stays with the word before it.
            // A part never goes back to an earlier stretch.
            var hasPlace = part.End > part.Start && part.Text.Any(char.IsLetterOrDigit);
            var piece = hasPlace ? Math.Max(current, chunk.PieceIndexAt(part.Start + (part.End - part.Start) / 2)) : current;
            if (current >= 0 && piece > current && seen > 0)
            {
                var position = exact ? seen : NearestSpace(line.Text, (int)Math.Round((double)seen / total * line.Text.Length));
                if (position > (cuts.Count > 0 ? cuts[^1].Position : 0) && position < line.Text.Length)
                    cuts.Add((position, current, piece));
            }
            if (piece >= 0) current = piece;
            seen += part.Text.Length;
        }
        return cuts;
    }

    private static int NearestSpace(string text, int position)
    {
        position = Math.Clamp(position, 0, text.Length);
        for (var distance = 0; distance <= SearchForSpace; distance++)
        {
            if (position + distance < text.Length && char.IsWhiteSpace(text[position + distance])) return position + distance;
            if (position - distance >= 0 && position - distance < text.Length && char.IsWhiteSpace(text[position - distance])) return position - distance;
        }
        // A language without spaces: cut between two characters, but not in the middle of a pair.
        return position > 0 && position < text.Length && char.IsLowSurrogate(text[position]) ? position - 1 : position;
    }

    private static TimeSpan Clamp(TimeSpan time, SpeechSpan span) => time < span.Start ? span.Start : time > span.End ? span.End : time;

    private static TranscriptSegment Line(string text, TimeSpan start, TimeSpan end) => new() { Start = start, End = end, Text = text };
}
