using System.Drawing;

/// <summary>Drives the real Windows app through its UI Automation tree.</summary>
public class AppTests
{
    private static string Sample(string name) => Path.Combine(AppContext.BaseDirectory, "TestData", name);

    private static string CopyToTemp(string sample, out string dir)
    {
        dir = Directory.CreateTempSubdirectory("transcriber-ui-").FullName;
        var path = Path.Combine(dir, sample);
        File.Copy(Sample(sample), path);
        return path;
    }

    [UiFact]
    public void The_app_starts_with_nothing_selected_and_transcribing_disabled()
    {
        using var app = new AppDriver();

        Assert.Equal("Transcriber", app.Window.Title);
        Assert.True(app.Get("ChooseFilesButton").IsEnabled);
        Assert.False(app.Get("TranscribeButton").IsEnabled);
        Assert.Equal(string.Empty, app.TextOf("ResultMessage"));
    }

    [UiFact]
    public void A_file_passed_on_the_command_line_is_added_and_enables_transcribing()
    {
        var file = CopyToTemp("en-01.mp3", out var dir);
        try
        {
            using var app = new AppDriver(file);

            AppDriver.Wait(() => app.TextOf("FilesSummary").Contains("en-01.mp3"), TimeSpan.FromSeconds(10), "the file to appear");
            Assert.True(app.Get("TranscribeButton").IsEnabled);
        }
        finally { Directory.Delete(dir, true); }
    }

    [UiFact]
    public void Transcribing_a_file_saves_a_transcript_and_offers_to_open_it()
    {
        var file = CopyToTemp("en-01.mp3", out var dir);
        using var app = new AppDriver(file);
        try
        {
            app.Click("TranscribeButton");

            AppDriver.Wait(() => app.TextOf("ResultMessage") == "Transcript saved", TimeSpan.FromMinutes(5), "the transcript to be saved");

            var transcript = Path.Combine(dir, "en-01_transcript.txt");
            Assert.True(File.Exists(transcript));
            Assert.Contains("human", File.ReadAllText(transcript), StringComparison.OrdinalIgnoreCase);
            Assert.EndsWith("en-01_transcript.txt", app.TextOf("ResultDetail"));
            Assert.True(app.Get("OpenTranscriptButton").IsEnabled);
            Assert.True(app.Get("ShowInFolderButton").IsEnabled);
        }
        catch { app.SaveScreenshot(nameof(Transcribing_a_file_saves_a_transcript_and_offers_to_open_it)); throw; }
        finally { app.Dispose(); Directory.Delete(dir, true); }
    }

    [UiFact]
    public void Labelling_speakers_names_who_said_each_line_in_the_saved_transcript()
    {
        var file = CopyToTemp("two-speakers-en.wav", out var dir);
        using var app = new AppDriver(file);
        try
        {
            Assert.Null(app.Find("SpeakerCountPicker"));
            app.Get("IdentifySpeakersSwitch").Patterns.Toggle.Pattern.Toggle();
            AppDriver.Wait(() => app.Find("SpeakerCountPicker") is not null, TimeSpan.FromSeconds(10), "the speaker count picker");

            app.Click("TranscribeButton");
            AppDriver.Wait(() => app.TextOf("ResultMessage") == "Transcript saved", TimeSpan.FromMinutes(5), "the transcript to be saved");

            var saved = File.ReadAllText(Path.Combine(dir, "two-speakers-en_transcript.txt"));
            Assert.Contains("Speaker 1:", saved);
            Assert.Contains("Speaker 2:", saved);
        }
        catch { app.SaveScreenshot(nameof(Labelling_speakers_names_who_said_each_line_in_the_saved_transcript)); throw; }
        finally { app.Dispose(); Directory.Delete(dir, true); }
    }

    [UiFact]
    public void The_practice_tab_starts_empty_and_cannot_analyse_until_a_recording_is_chosen()
    {
        using var app = new AppDriver();

        app.SelectTab("Practice");

        AppDriver.Wait(() => app.Find("PracticeFileName") is not null, TimeSpan.FromSeconds(10), "the practice tab");
        Assert.Equal("No recording chosen", app.TextOf("PracticeFileName"));
        Assert.False(app.Get("AnalyzeButton").IsEnabled);
        Assert.True(app.Get("PracticeChooseButton").IsEnabled);
        // Recording is offered, but nothing is recording and no microphone is touched by this test.
        Assert.True(app.Get("RecordButton").IsEnabled);
        Assert.Null(app.Find("StopRecordingButton"));
        Assert.Null(app.Find("RecordingText"));
        Assert.Null(app.Find("PracticeTips")); // no results are shown before an analysis
    }

    [UiFact]
    public void Switching_tabs_keeps_the_files_chosen_on_the_transcribe_tab()
    {
        var file = CopyToTemp("en-01.mp3", out var dir);
        try
        {
            using var app = new AppDriver(file);
            AppDriver.Wait(() => app.TextOf("FilesSummary").Contains("en-01.mp3"), TimeSpan.FromSeconds(10), "the file to appear");

            app.SelectTab("Practice");
            app.SelectTab("Transcribe");

            AppDriver.Wait(() => app.TextOf("FilesSummary").Contains("en-01.mp3"), TimeSpan.FromSeconds(10), "the file to still be listed");
            Assert.True(app.Get("TranscribeButton").IsEnabled);
        }
        finally { Directory.Delete(dir, true); }
    }

    [UiFact]
    public void Clicking_a_transcript_line_starts_playback_and_the_player_bar_can_stop_it()
    {
        var file = CopyToTemp("en-01.mp3", out var dir);
        using var app = new AppDriver(file);
        try
        {
            app.Click("TranscribeButton");
            AppDriver.Wait(() => app.TextOf("ResultMessage") == "Transcript saved", TimeSpan.FromMinutes(5), "the transcript to be saved");

            // Rows are named by their text (accessibility description), so find one by a word from the sample.

            var row = AppDriver_FindRow(app, "human beings");

            Thread.Sleep(500);
            Assert.Null(app.Find("PlaybackText"));
            // Selecting the row is what a click does; doing it through UI Automation keeps the test off the real mouse.
            row.Patterns.SelectionItem.Pattern.Select();

            AppDriver.Wait(() => app.TextOf("PlaybackText").StartsWith("en-01.mp3"), TimeSpan.FromSeconds(10), "the player bar to appear");
            app.Click("StopButton");
            AppDriver.Wait(() => app.Find("PlaybackText") is null, TimeSpan.FromSeconds(10), "the player bar to disappear");
        }
        catch { app.SaveScreenshot(nameof(Clicking_a_transcript_line_starts_playback_and_the_player_bar_can_stop_it)); throw; }
        finally { app.Dispose(); Directory.Delete(dir, true); }
    }

    private static FlaUI.Core.AutomationElements.AutomationElement AppDriver_FindRow(AppDriver app, string expectedText)
    {
        FlaUI.Core.AutomationElements.AutomationElement? row = null;
        AppDriver.Wait(() => (row = app.Find("Segments")?.FindFirstDescendant(cf => cf.ByControlType(FlaUI.Core.Definitions.ControlType.ListItem))) is not null,
            TimeSpan.FromSeconds(15), "a transcript row");
        Assert.Contains(expectedText, row!.Name, StringComparison.OrdinalIgnoreCase);
        return row;
    }

    [UiDragFact]
    public void Dropping_files_on_the_window_adds_the_supported_ones_and_reports_the_rest()
    {
        var audio = CopyToTemp("en-02.mp3", out var dir);
        var notes = Path.Combine(dir, "notes.txt");
        File.WriteAllText(notes, "not audio");
        using var app = new AppDriver();
        try
        {
            var box = app.Window.BoundingRectangle;
            var target = new Point(box.Left + box.Width / 2, box.Top + 280);

            var report = DragSource.DropFiles(target, TimeSpan.FromSeconds(60),
                whileOverTarget: () => { app.SaveScreenshot("drag-over"); AppDriver.SaveScreen("drag-over-screen"); },
                audio, notes);

            AppDriver.Wait(() => app.TextOf("FilesSummary").Contains("en-02.mp3"), TimeSpan.FromSeconds(15), $"the dropped file to appear ({report}, window={box})");
            Assert.True(app.Get("TranscribeButton").IsEnabled);
            AppDriver.Wait(() => app.TextOf("DropMessage").Contains("Added 1 of 2 files"), TimeSpan.FromSeconds(5), "the drop hint");
        }
        catch { app.SaveScreenshot(nameof(Dropping_files_on_the_window_adds_the_supported_ones_and_reports_the_rest)); throw; }
        finally { app.Dispose(); Directory.Delete(dir, true); }
    }
}
