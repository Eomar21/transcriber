/// <summary>
/// Automatic speaker detection can return sparse, out-of-order numbers and many tiny "speakers"; the transcript must still
/// read Speaker 1, Speaker 2, ... in the order people first speak.
/// </summary>
public class SpeakerNumberingTests
{
    private static TimeSpan S(double seconds) => TimeSpan.FromSeconds(seconds);
    private static SpeakerTurn Turn(double start, double end, int speaker) => new(S(start), S(end), speaker);
    private static TranscriptSegment Seg(double start, double end, int? speaker = null) =>
        new() { Start = S(start), End = S(end), Text = "text", Speaker = speaker };

    private static int?[] Speakers(IEnumerable<TranscriptSegment> segments) => segments.Select(s => s.Speaker).ToArray();

    // ---- numbering ----

    [Fact]
    public void Numbers_start_at_the_first_voice_and_have_no_gaps()
    {
        var result = SpeakerAssigner.RenumberByAppearance([Seg(0, 1, 4), Seg(1, 2, 14), Seg(2, 3, 4), Seg(3, 4, 47)]);

        Assert.Equal([0, 1, 0, 2], Speakers(result));
    }

    [Fact]
    public void Labels_follow_the_order_people_first_speak_not_the_models_numbers()
    {
        var result = SpeakerAssigner.RenumberByAppearance([Seg(0, 1, 9), Seg(1, 2, 2)]);

        Assert.Equal("Speaker 1", SpeakerAssigner.Label(result[0].Speaker!.Value));
        Assert.Equal("Speaker 2", SpeakerAssigner.Label(result[1].Speaker!.Value));
    }

    [Fact]
    public void Lines_without_a_speaker_stay_without_one_and_do_not_use_up_a_number()
    {
        var result = SpeakerAssigner.RenumberByAppearance([Seg(0, 1), Seg(1, 2, 7), Seg(2, 3)]);

        Assert.Equal([null, 0, null], Speakers(result));
    }

    [Fact]
    public void Renumbering_keeps_text_and_times_and_leaves_the_input_alone()
    {
        var input = new[] { new TranscriptSegment { Start = S(1), End = S(2), Text = "hello", Speaker = 5 } };

        var result = SpeakerAssigner.RenumberByAppearance(input);

        Assert.Equal("hello", result[0].Text);
        Assert.Equal(S(1), result[0].Start);
        Assert.Equal(5, input[0].Speaker);
    }

    // ---- fragments ----

    // The shape of a real recording: one long voice (cluster 4), a second real one (5), and many short fragments.
    private static List<SpeakerTurn> OverSplit() =>
    [
        Turn(51.0, 51.4, 1), Turn(51.4, 52.7, 2), Turn(52.6, 54.9, 3),
        Turn(55.1, 120, 4), Turn(120, 135, 5), Turn(135, 138, 7), Turn(138.3, 142, 18),
        Turn(142, 260, 4), Turn(260, 272, 5), Turn(272, 276, 30), Turn(276, 330, 4),
    ];

    [Fact]
    public void Short_fragments_are_folded_into_the_real_speakers()
    {
        var merged = SpeakerAssigner.MergeMinorSpeakers(OverSplit());

        Assert.Equal([4, 5], merged.Select(t => t.Speaker).Distinct().Order());
    }

    [Fact]
    public void A_fragment_goes_to_the_speaker_it_is_closest_to_in_time()
    {
        var merged = SpeakerAssigner.MergeMinorSpeakers(OverSplit());

        Assert.Equal(5, merged.Single(t => t.Speaker == 5 && t.Start == S(135)).Speaker);
        // 135 to 138 sits right after speaker 5's turn, 138.3 to 142 right before speaker 4's.
        Assert.Equal(5, merged.First(t => t.Start == S(135) && t.End == S(138)).Speaker);
        Assert.Equal(4, merged.First(t => t.Start == S(138.3)).Speaker);
    }

    [Fact]
    public void The_whole_pipeline_gives_speaker_1_and_2_for_an_over_split_recording()
    {
        var segments = new[] { Seg(56, 100), Seg(121, 130), Seg(140, 200), Seg(261, 270), Seg(280, 320) };

        var turns = SpeakerAssigner.MergeMinorSpeakers(OverSplit());
        var result = SpeakerAssigner.RenumberByAppearance(SpeakerAssigner.Assign(segments, turns));

        Assert.Equal([0, 1, 0, 1, 0], Speakers(result));
    }

    [Fact]
    public void The_first_line_is_speaker_1_even_when_it_is_spoken_by_a_fragment_cluster()
    {
        var turns = SpeakerAssigner.MergeMinorSpeakers(OverSplit());
        var result = SpeakerAssigner.RenumberByAppearance(SpeakerAssigner.Assign([Seg(51.5, 52.5)], turns));

        Assert.Equal("Speaker 1", SpeakerAssigner.Label(result[0].Speaker!.Value));
    }

    [Fact]
    public void A_short_but_genuine_second_voice_is_kept()
    {
        // 90 seconds of one voice and 25 of another is a real conversation, whatever the ids.
        var merged = SpeakerAssigner.MergeMinorSpeakers([Turn(0, 90, 3), Turn(90, 115, 11)]);

        Assert.Equal([3, 11], merged.Select(t => t.Speaker).Distinct().Order());
    }

    [Fact]
    public void Two_ordinary_voices_are_left_exactly_as_they_were()
    {
        var turns = new[] { Turn(0, 5, 0), Turn(5, 9, 1), Turn(9, 12, 0) };

        Assert.Equal(turns, SpeakerAssigner.MergeMinorSpeakers(turns));
    }

    [Fact]
    public void Many_tiny_clusters_and_no_clear_major_one_still_leave_at_least_one_speaker()
    {
        var turns = Enumerable.Range(0, 30).Select(i => Turn(i, i + 0.9, i)).ToArray();

        var merged = SpeakerAssigner.MergeMinorSpeakers(turns);

        Assert.Equal(30, merged.Count);
        Assert.Single(merged.Select(t => t.Speaker).Distinct());
    }

    [Fact]
    public void No_turns_give_no_turns() => Assert.Empty(SpeakerAssigner.MergeMinorSpeakers([]));
}
