/// <summary>Who spoke when: the pure labelling rules, and the bundled models on a real two-person conversation.</summary>
public class SpeakerDiarizationTests
{
    private static TimeSpan S(double seconds) => TimeSpan.FromSeconds(seconds);

    private static TranscriptSegment Seg(double start, double end) =>
        new() { Start = S(start), End = S(end), Text = "text" };

    private static SpeakerTurn Turn(double start, double end, int speaker) => new(S(start), S(end), speaker);

    [Fact]
    public void A_segment_gets_the_speaker_who_talks_during_it()
    {
        var result = ServicesFixture.Assigner.Assign([Seg(0, 4), Seg(5, 9)], [Turn(0, 4.5, 0), Turn(4.5, 10, 1)]);

        Assert.Equal([0, 1], result.Select(s => s.Speaker));
    }

    [Fact]
    public void When_two_speakers_overlap_a_segment_the_one_talking_longest_wins()
    {
        var result = ServicesFixture.Assigner.Assign([Seg(0, 10)], [Turn(0, 3, 0), Turn(3, 10, 1)]);

        Assert.Equal(1, result[0].Speaker);
    }

    [Fact]
    public void A_speaker_with_several_short_turns_in_one_segment_adds_them_up()
    {
        var result = ServicesFixture.Assigner.Assign([Seg(0, 10)], [Turn(0, 2, 0), Turn(2, 5, 1), Turn(5, 9, 0)]);

        Assert.Equal(0, result[0].Speaker);
    }

    [Fact]
    public void A_segment_with_no_overlapping_speaker_stays_unlabelled()
    {
        var result = ServicesFixture.Assigner.Assign([Seg(20, 25)], [Turn(0, 5, 0)]);

        Assert.Null(result[0].Speaker);
    }

    [Fact]
    public void Assigning_leaves_the_original_segments_untouched_and_keeps_their_text_and_times()
    {
        var original = new[] { new TranscriptSegment { Start = S(1), End = S(2), Text = "hello" } };

        var result = ServicesFixture.Assigner.Assign(original, [Turn(0, 5, 0)]);

        Assert.Null(original[0].Speaker);
        Assert.Equal(("hello", S(1), S(2)), (result[0].Text, result[0].Start, result[0].End));
    }

    [Fact]
    public void Speakers_are_numbered_from_one_for_people()
    {
        Assert.Equal("Speaker 1", SpeakerLabel.Format(0));
        Assert.Equal("Speaker 3", SpeakerLabel.Format(2));
    }

    private static readonly string Sample = Path.Combine(AppContext.BaseDirectory, "TestData", "two-speakers-en.wav");

    [Fact]
    public void The_speaker_models_ship_next_to_the_app()
    {
        Assert.True(ServicesFixture.Diarizer().IsAvailable);
    }

    [Fact]
    public async Task Two_people_talking_are_told_apart()
    {
        var turns = await ServicesFixture.Diarizer().DiarizeAsync(Sample);

        Assert.Equal([0, 1], turns.Select(t => t.Speaker).Distinct().Order());
        Assert.All(turns, t => Assert.True(t.End > t.Start));
        // They take turns, so the speaker changes more than once in the recording.
        var changes = turns.Zip(turns.Skip(1), (a, b) => a.Speaker != b.Speaker).Count(x => x);
        Assert.True(changes >= 2, $"only {changes} speaker changes");
    }

    [Fact]
    public async Task Naming_the_number_of_speakers_is_respected()
    {
        var turns = await ServicesFixture.Diarizer().DiarizeAsync(Sample, speakerCount: 1);

        Assert.Single(turns.Select(t => t.Speaker).Distinct());
    }

    [Fact]
    public async Task A_cancel_stops_the_speaker_step_at_once()
    {
        using var cancel = new CancellationTokenSource();
        var clock = System.Diagnostics.Stopwatch.StartNew();

        var run = ServicesFixture.Diarizer().DiarizeAsync(Sample, null, null, cancel.Token);
        cancel.CancelAfter(TimeSpan.FromMilliseconds(100));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => run);
        Assert.True(clock.Elapsed < TimeSpan.FromSeconds(2), $"the cancel took {clock.Elapsed.TotalSeconds:0.0} s");
    }
}
