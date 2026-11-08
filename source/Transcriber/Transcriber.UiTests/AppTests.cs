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

            DragSource.DropFiles(target, TimeSpan.FromSeconds(60),
                whileOverTarget: () => app.SaveScreenshot("drag-over"),
                audio, notes);

            AppDriver.Wait(() => app.TextOf("FilesSummary").Contains("en-02.mp3"), TimeSpan.FromSeconds(15), "the dropped file to appear");
            Assert.True(app.Get("TranscribeButton").IsEnabled);
            AppDriver.Wait(() => app.TextOf("DropMessage").Contains("Added 1 of 2 files"), TimeSpan.FromSeconds(5), "the drop hint");
        }
        catch { app.SaveScreenshot(nameof(Dropping_files_on_the_window_adds_the_supported_ones_and_reports_the_rest)); throw; }
        finally { app.Dispose(); Directory.Delete(dir, true); }
    }
}
