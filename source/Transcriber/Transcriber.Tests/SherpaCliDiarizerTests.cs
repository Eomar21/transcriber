public class SherpaCliDiarizerTests
{
    // What the tool prints, with the banner and timing lines it also writes around the turns.
    private const string RealisticOutput = """
        /Users/runner/work/sherpa-onnx/offline-speaker-diarization.cc:122 Started

        0.318 -- 6.865 speaker_00
        7.017 -- 10.747 speaker_01
        11.455 -- 13.632 speaker_01
        13.750 -- 15.001 speaker_00

        Duration : 17.8 s
        Elapsed seconds: 0.412 s
        Real time factor (RTF): 0.412 / 17.8 = 0.023
        """;

    [Fact]
    public void Turns_are_read_from_the_tools_output_and_everything_else_is_ignored()
    {
        var turns = SherpaCliSpeakerDiarizer.ParseOutput(RealisticOutput);

        Assert.Equal(4, turns.Count);
        Assert.Equal(TimeSpan.FromSeconds(0.318), turns[0].Start);
        Assert.Equal(TimeSpan.FromSeconds(6.865), turns[0].End);
        Assert.Equal([0, 1, 1, 0], turns.Select(t => t.Speaker));
    }

    [Fact]
    public void Turns_are_ordered_by_start_time()
    {
        var turns = SherpaCliSpeakerDiarizer.ParseOutput("5.0 -- 6.0 speaker_01\n1.0 -- 2.0 speaker_00\n");

        Assert.Equal([TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(5)], turns.Select(t => t.Start));
    }

    [Fact]
    public void Windows_line_endings_and_a_confidence_suffix_are_fine()
    {
        var turns = SherpaCliSpeakerDiarizer.ParseOutput("0.500 -- 2.250 speaker_02 confidence=0.910\r\n3 -- 4 speaker_10\r\n");

        Assert.Equal([2, 10], turns.Select(t => t.Speaker));
        Assert.Equal(TimeSpan.FromSeconds(3), turns[1].Start);
    }

    [Theory]
    [InlineData("")]
    [InlineData("Started\nDone!\n")]
    [InlineData("error: could not open model\n")]
    public void Output_without_turns_gives_an_empty_list(string text) =>
        Assert.Empty(SherpaCliSpeakerDiarizer.ParseOutput(text));

    [Fact]
    public void A_turn_that_ends_before_it_starts_is_dropped()
    {
        Assert.Empty(SherpaCliSpeakerDiarizer.ParseOutput("4.0 -- 4.0 speaker_00\n5.0 -- 3.0 speaker_01\n"));
    }

    [Fact]
    public void Numbers_are_read_the_same_in_any_culture()
    {
        var old = System.Globalization.CultureInfo.CurrentCulture;
        try
        {
            System.Globalization.CultureInfo.CurrentCulture = new System.Globalization.CultureInfo("nl-NL");
            var turns = SherpaCliSpeakerDiarizer.ParseOutput("1.500 -- 2.500 speaker_00");
            Assert.Equal(TimeSpan.FromSeconds(1.5), turns[0].Start);
        }
        finally { System.Globalization.CultureInfo.CurrentCulture = old; }
    }

    [Fact]
    public void Arguments_pass_models_the_speaker_count_and_the_wav_last()
    {
        var args = SherpaCliSpeakerDiarizer.BuildArguments("seg.onnx", "emb.onnx", "talk.wav", 3, 4);

        Assert.Contains("--segmentation.pyannote-model=seg.onnx", args);
        Assert.Contains("--embedding.model=emb.onnx", args);
        Assert.Contains("--clustering.num-clusters=3", args);
        Assert.Contains("--segmentation.num-threads=4", args);
        Assert.Equal("talk.wav", args[^1]);
    }

    [Fact]
    public void An_unknown_speaker_count_asks_the_tool_to_work_it_out()
    {
        var args = SherpaCliSpeakerDiarizer.BuildArguments("s", "e", "w.wav", null, 2);

        Assert.Contains("--clustering.num-clusters=-1", args);
        Assert.Contains("--clustering.cluster-threshold=0.5", args);
    }

    [Fact]
    public void Arguments_are_culture_independent()
    {
        var old = System.Globalization.CultureInfo.CurrentCulture;
        try
        {
            System.Globalization.CultureInfo.CurrentCulture = new System.Globalization.CultureInfo("nl-NL");
            Assert.Contains("--clustering.cluster-threshold=0.5", SherpaCliSpeakerDiarizer.BuildArguments("s", "e", "w.wav", null, 2));
        }
        finally { System.Globalization.CultureInfo.CurrentCulture = old; }
    }

    [Fact]
    public void Without_the_tool_or_the_models_the_feature_is_unavailable()
    {
        var dir = Directory.CreateTempSubdirectory().FullName;
        try
        {
            Assert.False(new SherpaCliSpeakerDiarizer(Path.Combine(dir, "missing"), dir).IsAvailable);

            var tool = Path.Combine(dir, "tool");
            File.WriteAllText(tool, "x");
            Assert.False(new SherpaCliSpeakerDiarizer(tool, dir).IsAvailable); // models still missing

            File.WriteAllText(Path.Combine(dir, SherpaSpeakerDiarizer.SegmentationModelFileName), "x");
            File.WriteAllText(Path.Combine(dir, SherpaSpeakerDiarizer.EmbeddingModelFileName), "x");
            Assert.True(new SherpaCliSpeakerDiarizer(tool, dir).IsAvailable);
        }
        finally { Directory.Delete(dir, true); }
    }

    [Fact]
    public async Task Asking_for_speakers_without_the_tool_is_a_clear_error()
    {
        var diarizer = new SherpaCliSpeakerDiarizer(Path.Combine(Path.GetTempPath(), "no-such-tool"), Path.GetTempPath());

        await Assert.ThrowsAsync<InvalidOperationException>(() => diarizer.DiarizeAsync("x.wav"));
    }
}
