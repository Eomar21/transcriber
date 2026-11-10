namespace Transcriber.Services.Models;

/// <summary>A stretch of the recording, and the time at which it starts in the audio of its chunk.</summary>
internal readonly record struct SpeechPiece(SpeechSpan Span, TimeSpan Offset)
{
    /// <summary>The time in the chunk at which this piece ends.</summary>
    public TimeSpan EndOffset => Offset + Span.Length;
}

/// <summary>
/// The audio that goes to Whisper in one call: one stretch of speech, or some short stretches in sequence with a
/// short silence between them. Whisper gives times that count from the start of the chunk. The chunk knows where each
/// piece is, so it changes those times back to times in the recording.
/// </summary>
internal sealed class SpeechChunk
{
    private readonly TimeSpan _edge;

    private SpeechChunk(IReadOnlyList<SpeechPiece> pieces, TimeSpan edge)
    {
        Pieces = pieces;
        _edge = edge;
    }

    /// <summary>The pieces in the order of the recording. There is always one piece or more.</summary>
    public IReadOnlyList<SpeechPiece> Pieces { get; }

    /// <summary>The length of the audio of the chunk, with the silence between the pieces.</summary>
    public TimeSpan Length => Pieces[^1].EndOffset;

    /// <param name="spans">The stretches, in the order of the recording. One or more.</param>
    /// <param name="joinSilence">The silence between two stretches.</param>
    /// <param name="edge">
    /// The time at the start and at the end of each stretch that has no speech (the padding). Whisper puts the end of
    /// a line at the moment the next speech starts, which is a little after the start of the next stretch. A time
    /// that is this close to a join belongs to the other side of the join.
    /// </param>
    public static SpeechChunk Of(IReadOnlyList<SpeechSpan> spans, TimeSpan joinSilence, TimeSpan edge = default)
    {
        if (spans.Count == 0) throw new ArgumentException("A chunk has one stretch of speech or more.", nameof(spans));

        var pieces = new List<SpeechPiece>(spans.Count);
        var offset = TimeSpan.Zero;
        foreach (var span in spans)
        {
            pieces.Add(new SpeechPiece(span, offset));
            offset += span.Length + joinSilence;
        }
        return new SpeechChunk(pieces, edge);
    }

    public static SpeechChunk Of(SpeechSpan span) => Of([span], TimeSpan.Zero);

    /// <summary>
    /// Changes the times of a line from chunk time to recording time. A start in the silence between two pieces moves
    /// to the start of the next piece. An end in that silence moves to the end of the piece before it. Thus a line
    /// never starts or ends in a part of the recording that was not sent to Whisper, and a line does not cover a
    /// pause because its end is a moment late.
    /// </summary>
    public SpeechSpan ToRecording(TimeSpan start, TimeSpan end)
    {
        var from = StartInRecording(start);
        var to = EndInRecording(end, start);
        return new SpeechSpan(from, to > from ? to : from);
    }

    /// <summary>The number of the piece that plays at a time in the chunk. A time in the silence after a piece counts for that piece.</summary>
    public int PieceIndexAt(TimeSpan time)
    {
        var index = 0;
        while (index + 1 < Pieces.Count && Pieces[index + 1].Offset <= time) index++;
        return index;
    }

    private TimeSpan StartInRecording(TimeSpan time)
    {
        // The last piece that starts at or before this time.
        var index = 0;
        while (index + 1 < Pieces.Count && Pieces[index + 1].Offset <= time) index++;

        var piece = Pieces[index];
        if (time < piece.EndOffset) return piece.Span.Start + (time - piece.Offset);
        // In the silence after the piece: the line starts with the next piece.
        return index + 1 < Pieces.Count ? Pieces[index + 1].Span.Start : piece.Span.End;
    }

    private TimeSpan EndInRecording(TimeSpan time, TimeSpan lineStart)
    {
        // The last piece that starts before this time: an end at the exact start of a piece belongs to the piece before it.
        var index = 0;
        while (index + 1 < Pieces.Count && Pieces[index + 1].Offset < time) index++;

        var piece = Pieces[index];
        // A line that comes from an earlier piece and ends in the first moment of this piece ends with the piece before it.
        var comesFromEarlierPiece = index > 0 && lineStart < piece.Offset;
        if (comesFromEarlierPiece && time <= piece.Offset + Min(_edge, piece.Span.Length / 2)) return Pieces[index - 1].Span.End;
        if (time <= piece.Offset) return piece.Span.Start;
        return time < piece.EndOffset ? piece.Span.Start + (time - piece.Offset) : piece.Span.End;
    }

    private static TimeSpan Min(TimeSpan a, TimeSpan b) => a < b ? a : b;
}
