using Transcriber.ViewModels;
using Transcriber.ViewModels.Transcribe;
using Transcriber.ViewModels.Practice;
using Transcriber.ViewModels.Progress;
using Xunit.Abstractions;

/// <summary>
/// After a run the page brings the result into view. This must happen only after the finished progress bar was
/// shown and hidden, and only for a run that has a result. The page scrolls when the view model raises the event,
/// so these rules are tested here, without a window.
/// </summary>
public class ResultRevealTests
{
    private sealed class NoSuggestion : IModelSuggestion
    {
        public string UseSuggestedModelText => "";
        public CommunityToolkit.Mvvm.Input.IRelayCommand UseSuggestedModelCommand { get; } = new CommunityToolkit.Mvvm.Input.RelayCommand(() => { });
    }

    private static RunStatusViewModel NewStatus() => new(new NoSuggestion(), new ProgressLookPicker(ProgressLooks.All), new RunState());

    /// <summary>Counts the events, and remembers if the bar was still shown at the moment of each one.</summary>
    private sealed class Watcher
    {
        public List<bool> BarWasShown { get; } = [];
        public int Count => BarWasShown.Count;

        public Watcher(RunStatusViewModel status) => status.ResultsReady += (_, _) => BarWasShown.Add(status.ShowProgress);
    }

    private static void FinishRun(RunStatusViewModel status, ResultKind kind, double progress)
    {
        status.BeginProgress();
        status.Progress = progress;
        status.SetResult(kind, "result");
    }

    [Fact]
    public async Task The_result_is_revealed_only_after_the_finished_bar_is_hidden()
    {
        var status = NewStatus();
        var watcher = new Watcher(status);
        FinishRun(status, ResultKind.Success, progress: 1);

        var ending = status.EndProgressAsync(TimeSpan.FromMilliseconds(300));

        // The bar is full and still on screen: the finish can be seen, so the page must not scroll away yet.
        Assert.True(status.ShowProgress);
        Assert.Equal(0, watcher.Count);

        await ending;

        Assert.False(status.ShowProgress);
        Assert.Equal([false], watcher.BarWasShown);
    }

    [Theory]
    [InlineData(ResultKind.Error)]
    [InlineData(ResultKind.Info)]   // a cancelled run
    [InlineData(ResultKind.None)]
    public async Task A_run_without_a_saved_result_does_not_scroll_the_page(ResultKind kind)
    {
        var status = NewStatus();
        var watcher = new Watcher(status);
        FinishRun(status, kind, progress: 0.4);

        await status.EndProgressAsync(TimeSpan.FromSeconds(30));

        Assert.False(status.ShowProgress);
        Assert.Equal(0, watcher.Count);
    }

    [Fact]
    public async Task A_run_that_ended_before_the_bar_was_full_reveals_the_result_without_waiting()
    {
        var status = NewStatus();
        var watcher = new Watcher(status);
        FinishRun(status, ResultKind.Success, progress: 0.5);

        await status.EndProgressAsync(TimeSpan.FromSeconds(30)); // returns at once: there is no finish to wait for

        Assert.Equal(1, watcher.Count);
    }

    [Fact]
    public async Task Each_successful_run_reveals_its_result_once()
    {
        var status = NewStatus();
        var watcher = new Watcher(status);

        for (var run = 1; run <= 3; run++)
        {
            FinishRun(status, ResultKind.Success, progress: 1);
            await status.EndProgressAsync(TimeSpan.FromMilliseconds(30));
            Assert.Equal(run, watcher.Count);
        }
    }

    [Fact]
    public async Task A_new_run_during_the_linger_cancels_the_reveal_of_the_old_result()
    {
        var status = NewStatus();
        var watcher = new Watcher(status);
        FinishRun(status, ResultKind.Success, progress: 1);

        var ending = status.EndProgressAsync(TimeSpan.FromMilliseconds(150));
        status.BeginProgress();
        await ending;

        Assert.True(status.ShowProgress);
        Assert.Equal(0, watcher.Count);
    }

    // ---- Practice ----------------------------------------------------------------------------

    [Fact]
    public async Task The_practice_bar_ends_in_the_same_order()
    {
        var status = new PracticeStatusViewModel(new PracticeState(), new ProgressLookPicker(ProgressLooks.All));
        var ended = new List<bool>();
        status.ProgressEnded += (_, _) => ended.Add(status.ShowProgress);
        status.BeginProgress();
        status.Progress = 1;

        var ending = status.EndProgressAsync(TimeSpan.FromMilliseconds(200));
        Assert.Empty(ended);
        await ending;

        Assert.Equal([false], ended);
    }

    [Fact]
    public async Task A_new_practice_analysis_during_the_linger_cancels_the_old_end()
    {
        var status = new PracticeStatusViewModel(new PracticeState(), new ProgressLookPicker(ProgressLooks.All));
        var ended = 0;
        status.ProgressEnded += (_, _) => ended++;
        status.BeginProgress();
        status.Progress = 1;

        var ending = status.EndProgressAsync(TimeSpan.FromMilliseconds(150));
        status.BeginProgress();
        await ending;

        Assert.Equal(0, ended);
    }
}

/// <summary>
/// The bar must not be full while work is left. A full bar that stays for many seconds looks like a frozen program, and
/// the page cannot know that the result is still on its way. This runs the real models on a recording with two voices.
/// </summary>
public class RunProgressContractTests(ITestOutputHelper output)
{
    private static readonly IModelManager Models = ServicesFixture.ModelManager(Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TranscriberTests"));

    private sealed class DirectProgress(Action<TranscriptionProgress> onReport) : IProgress<TranscriptionProgress>
    {
        public void Report(TranscriptionProgress value) => onReport(value);
    }

    [Fact]
    public async Task With_speaker_labels_the_bar_is_full_only_when_the_run_is_done()
    {
        var source = Path.Combine(AppContext.BaseDirectory, "TestData", "two-speakers-en.wav");
        var wav = await ServicesFixture.AudioService.GetAudioFilePathAsync(source);
        try
        {
            var reports = new List<TranscriptionProgress>();
            var whisper = ServicesFixture.Whisper(Models, diarizer: ServicesFixture.Diarizer());

            await whisper.TranscribeAsync(wav, new TranscriptionOptions
            {
                Language = "en",
                Model = TestModels.Small,
                IdentifySpeakers = true,
                UseGpu = Environment.GetEnvironmentVariable("TRANSCRIBER_TEST_CPU") != "1",
            }, new DirectProgress(reports.Add));

            foreach (var r in reports) output.WriteLine($"{r.Fraction,6:P1}  {r.Stage}");

            var speakerStage = reports.Where(r => r.Stage == "Identifying speakers").ToList();
            Assert.NotEmpty(speakerStage);

            // Nothing before the last report is full.
            Assert.Equal("Done", reports[^1].Stage);
            Assert.Equal(1, reports[^1].Fraction);
            Assert.All(reports.Take(reports.Count - 1), r => Assert.True(r.Fraction < 1, $"'{r.Stage}' reported a full bar before the end"));

            // The speaker step has its own part of the bar, after the transcription, and it only moves forward.
            Assert.All(speakerStage, r => Assert.InRange(r.Fraction, 1 - Transcriber.Services.Transcription.WhisperService.SpeakerShare - 1e-9, 1));
            Assert.Equal(speakerStage.Select(r => r.Fraction).Order(), speakerStage.Select(r => r.Fraction));
        }
        finally
        {
            // A wav file is used in place, and it is the shared test data: only a converted copy is removed.
            if (!string.Equals(wav, source, StringComparison.OrdinalIgnoreCase)) File.Delete(wav);
        }
    }
}
