using Microsoft.Extensions.Options;
using NAudio.Wave;

/// <summary>Short stretches of speech go to Whisper together, and the times of each line come back as times in the recording.</summary>
public class SpeechPackingTests
{
    private static TimeSpan S(double seconds) => TimeSpan.FromSeconds(seconds);

    private static SpeechSpan Span(double start, double end) => new(S(start), S(end));

    private static SpeechPacker Packer(double packSeconds = 28, double joinSilence = 0.5) =>
        new(Options.Create(new SpeechPlanningSettings { PackSeconds = packSeconds, JoinSilenceSeconds = joinSilence }));

    private static List<SpeechSpan> Spans(SpeechChunk chunk) => chunk.Pieces.Select(p => p.Span).ToList();

    // ---- which stretches go together ------------------------------------------------------

    [Fact]
    public void Short_stretches_that_follow_each_other_go_into_one_chunk()
    {
        var chunks = Packer().Pack([Span(0, 5), Span(10, 14), Span(30, 36)]);

        var chunk = Assert.Single(chunks);
        Assert.Equal([Span(0, 5), Span(10, 14), Span(30, 36)], Spans(chunk));
        Assert.Equal(S(5 + 0.5 + 4 + 0.5 + 6), chunk.Length);
    }

    [Fact]
    public void A_chunk_is_closed_before_it_becomes_longer_than_the_limit()
    {
        var chunks = Packer().Pack([Span(0, 10), Span(20, 30), Span(40, 50), Span(60, 62)]);

        Assert.Equal(2, chunks.Count);
        Assert.Equal([Span(0, 10), Span(20, 30)], Spans(chunks[0]));
        Assert.Equal([Span(40, 50), Span(60, 62)], Spans(chunks[1]));
        Assert.All(chunks, c => Assert.True(c.Length <= S(28)));
    }

    [Fact]
    public void The_silence_between_the_stretches_counts_for_the_limit()
    {
        // 14 + 14 is 28, but the silence between them makes the chunk too long.
        Assert.Equal(2, Packer().Pack([Span(0, 14), Span(20, 34)]).Count);
        Assert.Single(Packer(joinSilence: 0).Pack([Span(0, 14), Span(20, 34)]));
    }

    [Fact]
    public void A_long_stretch_stays_alone_and_keeps_its_place_in_the_order()
    {
        var chunks = Packer().Pack([Span(0, 3), Span(10, 100), Span(110, 113), Span(120, 124)]);

        Assert.Equal(3, chunks.Count);
        Assert.Equal([Span(0, 3)], Spans(chunks[0]));
        Assert.Equal([Span(10, 100)], Spans(chunks[1]));
        Assert.Equal([Span(110, 113), Span(120, 124)], Spans(chunks[2]));
    }

    [Fact]
    public void A_limit_of_zero_sends_each_stretch_alone()
    {
        var chunks = Packer(packSeconds: 0).Pack([Span(0, 1), Span(2, 3), Span(4, 5)]);

        Assert.Equal(3, chunks.Count);
        Assert.All(chunks, c => Assert.Single(c.Pieces));
    }

    [Fact]
    public void Each_stretch_is_in_one_chunk_and_the_order_stays()
    {
        var spans = Enumerable.Range(0, 40).Select(i => Span(i * 10, i * 10 + 1 + i % 7)).ToList();

        var chunks = Packer().Pack(spans);

        Assert.Equal(spans, chunks.SelectMany(Spans));
        Assert.True(chunks.Count < spans.Count / 3, $"{chunks.Count} chunks for {spans.Count} stretches");
    }

    [Fact]
    public void No_speech_gives_no_chunks() => Assert.Empty(Packer().Pack([]));

    // ---- times in the recording -----------------------------------------------------------

    // Two pieces: 100..105 at chunk time 0..5, then 200..204 at chunk time 5.5..9.5.
    private static readonly SpeechChunk TwoPieces = SpeechChunk.Of([Span(100, 105), Span(200, 204)], S(0.5));

    [Theory]
    [InlineData(0, 2, 100, 102)]        // in the first piece
    [InlineData(6, 9, 200.5, 203.5)]    // in the second piece
    [InlineData(3, 7, 103, 201.5)]      // starts in the first, ends in the second
    [InlineData(5.2, 8, 200, 202.5)]    // a start in the silence moves to the next piece
    [InlineData(1, 5.3, 101, 105)]      // an end in the silence moves to the piece before it
    [InlineData(5, 7, 200, 201.5)]      // a start at the exact end of a piece is the next piece
    [InlineData(2, 5.5, 102, 105)]      // an end at the exact start of a piece is the piece before it
    [InlineData(8, 12, 202.5, 204)]     // an end after the audio stops at the end of the last piece
    public void A_time_in_the_chunk_becomes_a_time_in_the_recording(double start, double end, double expectedStart, double expectedEnd)
    {
        var time = TwoPieces.ToRecording(S(start), S(end));

        Assert.Equal(expectedStart, time.Start.TotalSeconds, 6);
        Assert.Equal(expectedEnd, time.End.TotalSeconds, 6);
    }

    [Fact]
    public void A_line_that_is_only_in_the_silence_gets_no_negative_length()
    {
        var time = TwoPieces.ToRecording(S(5.1), S(5.4));

        Assert.True(time.End >= time.Start);
    }

    [Fact]
    public void A_chunk_of_one_stretch_only_adds_the_start_of_the_stretch()
    {
        var time = SpeechChunk.Of(Span(60, 90)).ToRecording(S(1.5), S(4));

        Assert.Equal(Span(61.5, 64), time);
    }

    [Fact]
    public void A_chunk_needs_a_stretch() => Assert.Throws<ArgumentException>(() => SpeechChunk.Of([], S(0.5)));

    // The same two pieces, with 0.6 seconds at each end of a piece that has no speech.
    private static readonly SpeechChunk WithEdges = SpeechChunk.Of([Span(100, 105), Span(200, 204)], S(0.5), S(0.6));

    [Theory]
    [InlineData(3, 5.8, 103, 105)]        // Whisper ends the line when the next speech starts: the line ends with its own piece
    [InlineData(3, 6.1, 103, 105)]        // the last moment that still counts as the edge
    [InlineData(3, 6.5, 103, 201)]        // more than the edge: the line really continues in the next piece
    [InlineData(5.8, 6.0, 200.3, 200.5)]  // a short line at the start of a piece stays in that piece
    [InlineData(5.8, 8, 200.3, 202.5)]    // the start of a line is not moved
    public void An_end_that_is_a_moment_late_does_not_make_the_line_cover_the_pause(double start, double end, double expectedStart, double expectedEnd)
    {
        var time = WithEdges.ToRecording(S(start), S(end));

        Assert.Equal(expectedStart, time.Start.TotalSeconds, 6);
        Assert.Equal(expectedEnd, time.End.TotalSeconds, 6);
    }

    [Fact]
    public void The_packer_gives_its_chunks_an_edge_from_the_padding()
    {
        var packer = new SpeechPacker(Options.Create(new SpeechPlanningSettings { PackSeconds = 28, JoinSilenceSeconds = 0.5, PaddingSeconds = 0.3 }));

        var chunk = Assert.Single(packer.Pack([Span(100, 105), Span(200, 204)]));

        Assert.Equal(Span(103, 105), chunk.ToRecording(S(3), S(5.9)));
    }

    // ---- one line for each stretch --------------------------------------------------------

    private static TimedText Part(string text, double start, double end) => new(text, S(start), S(end));

    private static IReadOnlyList<TranscriptSegment> Lines(SpeechChunk chunk, double start, double end, params TimedText[] parts) =>
        new ChunkLineBuilder().Build(chunk, new TimedText(string.Concat(parts.Select(p => p.Text)), S(start), S(end)), parts);

    [Fact]
    public void A_line_in_one_stretch_stays_one_line()
    {
        var line = Assert.Single(Lines(TwoPieces, 1, 4, Part(" Good", 1, 2), Part(" morning", 2, 3.5), Part(".", 3.5, 4)));

        Assert.Equal((" Good morning.", S(101), S(104)), (line.Text, line.Start, line.End));
    }

    [Fact]
    public void A_line_with_the_words_of_two_stretches_becomes_two_lines()
    {
        var lines = Lines(TwoPieces, 1, 8,
            Part(" How", 1, 2), Part(" are", 2, 3), Part(" you", 3, 4), Part("?", 4, 4.5),
            Part(" Fine", 6, 7), Part(".", 7, 8));

        Assert.Equal([" How are you?", " Fine."], lines.Select(l => l.Text));
        // The first line ends with its stretch, and the second line starts with its stretch.
        Assert.Equal((S(101), S(105)), (lines[0].Start, lines[0].End));
        Assert.Equal((S(200), S(202.5)), (lines[1].Start, lines[1].End));
    }

    [Fact]
    public void A_mark_that_comes_late_stays_with_the_word_before_it()
    {
        var lines = Lines(TwoPieces, 1, 8, Part(" Article", 1, 3), Part(" 1", 3, 4), Part(".", 5.6, 5.8), Part(" All", 6, 7));

        Assert.Equal([" Article 1.", " All"], lines.Select(l => l.Text));
    }

    [Fact]
    public void A_line_over_three_stretches_becomes_three_lines_in_the_order_of_the_recording()
    {
        var chunk = SpeechChunk.Of([Span(10, 12), Span(20, 22), Span(30, 32)], S(0.5));

        var lines = Lines(chunk, 0, 7, Part(" one", 0.5, 1.5), Part(" two", 3, 4), Part(" three", 5.5, 6.5));

        Assert.Equal([" one", " two", " three"], lines.Select(l => l.Text));
        Assert.Equal([S(10), S(20), S(30)], lines.Select(l => l.Start));
        Assert.Equal([S(12), S(22), S(32)], lines.Select(l => l.End));
    }

    [Fact]
    public void A_word_never_goes_back_to_an_earlier_stretch()
    {
        // The second word has a time in the first stretch again. It stays in the second stretch.
        var lines = Lines(TwoPieces, 1, 8, Part(" one", 1, 2), Part(" two", 6, 7), Part(" three", 3, 4));

        Assert.Equal([" one", " two three"], lines.Select(l => l.Text));
    }

    [Fact]
    public void Without_the_times_of_the_words_the_line_stays_whole()
    {
        var noTimes = Lines(TwoPieces, 1, 8, Part(" one", 0, 0), Part(" two", 0, 0));
        var noParts = new ChunkLineBuilder().Build(TwoPieces, new TimedText(" one two", S(1), S(8)), []);

        Assert.Equal(" one two", Assert.Single(noTimes).Text);
        Assert.Equal((S(101), S(202.5)), (Assert.Single(noParts).Start, noParts[0].End));
    }

    [Fact]
    public void When_the_parts_are_not_the_text_the_cut_moves_to_a_space_and_no_character_is_lost()
    {
        // The parts have other lengths than the text, as with a character that is in two parts.
        TimedText[] parts = [Part("ab", 1, 2), Part("cd", 2, 3), Part("efgh", 6, 7)];

        var lines = new ChunkLineBuilder().Build(TwoPieces, new TimedText(" مرحبا بكم جميعا", S(1), S(8)), parts);

        Assert.Equal(2, lines.Count);
        Assert.Equal(" مرحبا بكم جميعا", string.Concat(lines.Select(l => l.Text)));
        Assert.All(lines.Skip(1), l => Assert.StartsWith(" ", l.Text));
    }

    [Fact]
    public void A_chunk_of_one_stretch_is_never_cut()
    {
        var line = Assert.Single(Lines(SpeechChunk.Of(Span(60, 90)), 1, 9, Part(" one", 1, 2), Part(" two", 8, 9)));

        Assert.Equal((" one two", S(61), S(69)), (line.Text, line.Start, line.End));
    }

    // ---- the audio of a chunk -------------------------------------------------------------

    [Fact]
    public void The_audio_of_a_chunk_has_each_piece_at_its_place_and_silence_between()
    {
        // One second each of the levels 0.1, 0.2, 0.3 and 0.4, at 1000 samples for each second.
        var wav = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".wav");
        try
        {
            using (var writer = new WaveFileWriter(wav, new WaveFormat(1000, 16, 1)))
                for (var second = 1; second <= 4; second++)
                    for (var i = 0; i < 1000; i++) writer.WriteSample(second / 10f);

            var chunk = SpeechChunk.Of([Span(0, 1), Span(3, 4)], S(0.5));
            var samples = new WavSpeechChunkReader().Read(wav, chunk);

            Assert.Equal(2500, samples.Length);
            Assert.All(samples[..1000], s => Assert.Equal(0.1f, s, 3));
            Assert.All(samples[1000..1500], s => Assert.Equal(0f, s));
            Assert.All(samples[1500..], s => Assert.Equal(0.4f, s, 3));
        }
        finally { File.Delete(wav); }
    }
}
