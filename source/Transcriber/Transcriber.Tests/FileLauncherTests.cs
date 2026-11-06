public class FileLauncherTests
{
    private readonly ShellFileLauncher _launcher = new();
    private static string Missing(string name) => Path.Combine(Path.GetTempPath(), "no-such-folder-" + Guid.NewGuid().ToString("N"), name);

    [Fact]
    public void Opening_a_missing_file_reports_not_found() =>
        Assert.Equal(LaunchResult.NotFound, _launcher.Open(Missing("talk_transcript.srt")));

    [Fact]
    public void Revealing_a_file_in_a_missing_folder_reports_not_found() =>
        Assert.Equal(LaunchResult.NotFound, _launcher.Reveal(Missing("talk_transcript.srt")));
}
