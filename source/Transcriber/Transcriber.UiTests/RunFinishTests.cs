using System.Diagnostics;
using System.Text.RegularExpressions;

/// <summary>
/// What a person sees at the end of a run, in the real app: the progress bar reaches the end and its finish can be
/// seen, then the page moves to the transcript. The window is made small, so the transcript is below the fold and the
/// page has to scroll. These tests give the same checks that were first done by hand, so a change of the layout or of the
/// timing that breaks them is found when the tests run.
/// </summary>
public class RunFinishTests
{
    /// <summary>The longest a full bar may stay before the result is there. The bar stays on purpose for a short time after the end (3 s).</summary>
    private static readonly TimeSpan FullBarToResult = TimeSpan.FromSeconds(6);

    private static string CopyToTemp(string sample, out string dir)
    {
        dir = Directory.CreateTempSubdirectory("transcriber-ui-").FullName;
        var path = Path.Combine(dir, sample);
        File.Copy(Path.Combine(AppContext.BaseDirectory, "TestData", sample), path);
        return path;
    }

    private static int? PercentOf(string name) =>
        Regex.Match(name, @"(\d+)\s*%") is { Success: true } m ? int.Parse(m.Groups[1].Value) : null;

    /// <summary>The moments of a run that matter to a person, measured from the click on Transcribe.</summary>
    private sealed class Timeline
    {
        public TimeSpan? BarFull, BarFullAndInView, ResultShown, TranscriptInView;
        public bool BarWasInViewWhenFull;
        public double ScrollWhenFull;
        public TimeSpan? BarScrolledAway;   // the first moment the page moved away from a bar that was full and still shown
        public bool BarStillInViewWhenTranscriptShown;
        public List<string> Log { get; } = [];
    }

    private static Timeline Watch(AppDriver app, Action start, TimeSpan timeout)
    {
        var t = new Timeline();
        var clock = Stopwatch.StartNew();
        start();

        while (clock.Elapsed < timeout)
        {
            try
            {
                var percent = PercentOf(app.TextOf("RunProgressPercent"));
                if (percent >= 100 && t.BarFull is null)
                {
                    t.BarFull = clock.Elapsed;
                    t.BarWasInViewWhenFull = app.IsInView("RunProgressBar");
                    t.ScrollWhenFull = app.PageScrollPercent();
                    t.Log.Add($"{clock.Elapsed.TotalSeconds,6:0.0}s  bar full, in view: {t.BarWasInViewWhenFull}");
                }

                // While the full bar is still shown, the page must stay where it is. A hidden card is not in the automation tree.
                if (t.BarFull is not null && t.BarScrolledAway is null && app.Find("RunProgressBar") is not null && Math.Abs(app.PageScrollPercent() - t.ScrollWhenFull) > 0.5)
                {
                    t.BarScrolledAway = clock.Elapsed;
                    t.Log.Add($"{clock.Elapsed.TotalSeconds,6:0.0}s  the page moved away from the full bar");
                }

                if (t.ResultShown is null && app.TextOf("ResultMessage") == "Transcript saved")
                {
                    t.ResultShown = clock.Elapsed;
                    t.Log.Add($"{clock.Elapsed.TotalSeconds,6:0.0}s  result message exists");
                }

                if (t.ResultShown is not null && app.IsInView("Segments"))
                {
                    t.TranscriptInView = clock.Elapsed;
                    t.BarStillInViewWhenTranscriptShown = app.Find("RunProgressBar") is not null && app.IsInView("RunProgressBar");
                    t.Log.Add($"{clock.Elapsed.TotalSeconds,6:0.0}s  transcript in view");
                    return t;
                }
            }
            catch (System.Runtime.InteropServices.COMException) { /* the app was too busy to answer one call */ }

            Thread.Sleep(100);
        }
        return t;
    }

    [UiFact]
    public void After_a_run_the_finish_of_the_bar_is_seen_and_then_the_transcript_comes_into_view()
    {
        var file = CopyToTemp("two-speakers-en.wav", out var dir);
        using var app = new AppDriver(file);
        try
        {
            app.Resize(1000, 640); // the transcript is below the fold: the page has to scroll to it
            AppDriver.Wait(() => app.Get("TranscribeButton").IsEnabled, TimeSpan.FromSeconds(10), "the file to be added");

            var timeline = Watch(app, () => app.Click("TranscribeButton"), TimeSpan.FromMinutes(5));
            var report = string.Join(Environment.NewLine, timeline.Log);

            Assert.True(timeline.BarFull is not null, "the bar never reached the end. " + report);
            Assert.True(timeline.BarWasInViewWhenFull, "the page moved away before the finish of the bar could be seen. " + report);
            Assert.True(timeline.BarScrolledAway is null, "the page moved away while the full bar was still shown, so its finish was not seen. " + report);
            Assert.True(timeline.TranscriptInView is not null, "the transcript never came into view. " + report);
            Assert.True(timeline.TranscriptInView > timeline.BarFull, "the page moved to the transcript before the bar was full. " + report);
            Assert.True(timeline.TranscriptInView - timeline.BarFull <= FullBarToResult,
                $"the bar was full for {(timeline.TranscriptInView - timeline.BarFull)!.Value.TotalSeconds:0.0} s before the transcript was shown. " + report);
        }
        catch { app.SaveScreenshot(nameof(After_a_run_the_finish_of_the_bar_is_seen_and_then_the_transcript_comes_into_view)); throw; }
        finally { app.Dispose(); Directory.Delete(dir, true); }
    }

    [UiFact]
    public void With_speaker_labels_a_full_bar_is_followed_by_the_result_without_a_long_wait()
    {
        var file = CopyToTemp("two-speakers-en.wav", out var dir);
        using var app = new AppDriver(file);
        try
        {
            app.Resize(1000, 640);
            app.Get("IdentifySpeakersSwitch").Patterns.Toggle.Pattern.Toggle();
            AppDriver.Wait(() => app.Find("SpeakerCountPicker") is not null, TimeSpan.FromSeconds(10), "the speaker count picker");
            AppDriver.Wait(() => app.Get("TranscribeButton").IsEnabled, TimeSpan.FromSeconds(10), "the file to be added");

            var timeline = Watch(app, () => app.Click("TranscribeButton"), TimeSpan.FromMinutes(5));
            var report = string.Join(Environment.NewLine, timeline.Log);

            Assert.True(timeline.BarFull is not null, "the bar never reached the end. " + report);
            Assert.True(timeline.ResultShown is not null, "the result never appeared. " + report);

            // The speaker step must not fill the bar and then keep working: a full bar means that the result is there.
            Assert.True(timeline.ResultShown - timeline.BarFull <= FullBarToResult,
                $"the bar was full for {(timeline.ResultShown - timeline.BarFull)!.Value.TotalSeconds:0.0} s before the result appeared. " + report);
            Assert.True(timeline.TranscriptInView is not null, "the transcript never came into view. " + report);
            Assert.True(timeline.BarWasInViewWhenFull, "the page moved away before the finish of the bar could be seen. " + report);
        }
        catch { app.SaveScreenshot(nameof(With_speaker_labels_a_full_bar_is_followed_by_the_result_without_a_long_wait)); throw; }
        finally { app.Dispose(); Directory.Delete(dir, true); }
    }
}
