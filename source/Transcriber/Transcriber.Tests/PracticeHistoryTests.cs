using Transcriber.Coach;

/// <summary>Practice sessions saved to a JSON file and read back.</summary>
public class PracticeHistoryTests : IDisposable
{
    private readonly string _dir = Directory.CreateTempSubdirectory().FullName;
    private string FilePath => Path.Combine(_dir, "nested", "history.json");

    public void Dispose() => Directory.Delete(_dir, true);

    private static PracticeSession Session(string title, int day) => new(
        Guid.NewGuid(), new DateTimeOffset(2026, 1, day, 10, 0, 0, TimeSpan.Zero), title, "en",
        new SpeechReport
        {
            WordCount = 120,
            WordsPerMinute = 142.5,
            Duration = TimeSpan.FromSeconds(50),
            SpeakingTime = TimeSpan.FromSeconds(45),
            Pauses = [new PauseInfo(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(1.5))],
            Fillers = [new FillerCount("um", 4)],
            Repeats = [new RepeatInfo(TimeSpan.FromSeconds(20), "the the")],
        });

    [Fact]
    public void A_missing_file_means_no_history()
    {
        Assert.Empty(new JsonPracticeHistory(FilePath).Load());
    }

    [Fact]
    public void A_session_survives_a_round_trip_with_its_whole_report()
    {
        var history = new JsonPracticeHistory(FilePath);
        var session = Session("Talk", 1);

        history.Add(session);
        var loaded = Assert.Single(new JsonPracticeHistory(FilePath).Load());

        Assert.Equal(session.Id, loaded.Id);
        Assert.Equal("Talk", loaded.Title);
        Assert.Equal(session.When, loaded.When);
        Assert.Equal(142.5, loaded.Report.WordsPerMinute);
        Assert.Equal(4, loaded.Report.FillerTotal);
        Assert.Equal("the the", Assert.Single(loaded.Report.Repeats).Text);
        Assert.Equal(TimeSpan.FromSeconds(1.5), loaded.Report.LongestPause);
    }

    [Fact]
    public void Sessions_come_back_oldest_first()
    {
        var history = new JsonPracticeHistory(FilePath);
        history.Add(Session("later", 9));
        history.Add(Session("earlier", 2));

        Assert.Equal(["earlier", "later"], history.Load().Select(s => s.Title));
    }

    [Fact]
    public void A_session_can_be_deleted_and_the_rest_stay()
    {
        var history = new JsonPracticeHistory(FilePath);
        var keep = Session("keep", 1);
        var drop = Session("drop", 2);
        history.Add(keep);
        history.Add(drop);

        history.Delete(drop.Id);

        Assert.Equal([keep.Id], history.Load().Select(s => s.Id));
    }

    [Fact]
    public void Clearing_removes_everything()
    {
        var history = new JsonPracticeHistory(FilePath);
        history.Add(Session("a", 1));

        history.Clear();

        Assert.Empty(history.Load());
    }

    [Fact]
    public void A_damaged_file_reads_as_empty_and_can_be_written_over()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
        File.WriteAllText(FilePath, "{ not json");
        var history = new JsonPracticeHistory(FilePath);

        Assert.Empty(history.Load());
        history.Add(Session("fresh", 1));

        Assert.Single(history.Load());
    }

    [Fact]
    public void Saving_leaves_no_temporary_file_behind()
    {
        new JsonPracticeHistory(FilePath).Add(Session("a", 1));

        Assert.Equal(["history.json"], Directory.GetFiles(Path.GetDirectoryName(FilePath)!).Select(Path.GetFileName));
    }
}
