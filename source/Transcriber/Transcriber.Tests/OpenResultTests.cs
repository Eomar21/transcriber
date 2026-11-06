using Transcriber.ViewModels;

/// <summary>The "Open transcript" / "Show in folder" actions shown after a run finishes.</summary>
public class OpenResultTests : IDisposable
{
    private readonly string _dir = Directory.CreateTempSubdirectory().FullName;

    public void Dispose() => Directory.Delete(_dir, true);

    private sealed class Reader : IFileReaderService
    {
        public Task<string> GetAudioFileFromFileAsync(string filePath) => Task.FromResult(filePath);
    }

    private sealed class Whisper : IWhisperService
    {
        public string? LastRuntime => "Vulkan";
        public Task<List<TranscriptSegment>> TranscribeAsync(string audioFilePath, TranscriptionOptions? options = null,
            IProgress<TranscriptionProgress>? progress = null, CancellationToken ct = default) =>
            Task.FromResult(new List<TranscriptSegment> { new() { End = TimeSpan.FromSeconds(1), Text = "Hello" } });
    }

    private sealed class Picker : IFilePickerService
    {
        public Task<IReadOnlyList<string>> PickMediaFilesAsync() => Task.FromResult<IReadOnlyList<string>>([]);
    }

    private sealed class Clipboard : IClipboardService
    {
        public Task SetTextAsync(string text) => Task.CompletedTask;
    }

    private sealed class Launcher : IFileLauncher
    {
        public LaunchResult OpenResult { get; set; } = LaunchResult.Opened;
        public LaunchResult RevealResult { get; set; } = LaunchResult.Opened;
        public List<string> Opened { get; } = [];
        public List<string> Revealed { get; } = [];
        public LaunchResult Open(string path) { Opened.Add(path); return OpenResult; }
        public LaunchResult Reveal(string path) { Revealed.Add(path); return RevealResult; }
    }

    private readonly Launcher _launcher = new();

    private async Task<TranscribeViewModel> RunAsync(params string[] names)
    {
        var vm = new TranscribeViewModel(new Reader(), new Whisper(), new Picker(), new Clipboard(), _launcher);
        var files = names.Select(n => { var p = Path.Combine(_dir, n); File.WriteAllText(p, "x"); return p; }).ToList();
        vm.AddFiles(files);
        await vm.TranscribeCommand.ExecuteAsync(null);
        return vm;
    }

    [Fact]
    public async Task A_single_file_offers_both_actions_and_shows_path_and_runtime()
    {
        var vm = await RunAsync("talk.mp3");

        Assert.Equal("Transcript saved", vm.ResultMessage);
        Assert.Equal(Path.Combine(_dir, "talk_transcript.txt"), vm.ResultDetail);
        Assert.Equal("Processed with Vulkan", vm.RuntimeNote);
        Assert.True(vm.HasSavedFiles);
        Assert.True(vm.CanOpenTranscript);
        Assert.True(vm.OpenTranscriptCommand.CanExecute(null));
        Assert.True(vm.ShowInFolderCommand.CanExecute(null));
    }

    [Fact]
    public async Task A_batch_only_offers_show_in_folder_and_names_the_folder()
    {
        var vm = await RunAsync("a.mp3", "b.mp3", "c.mp3");

        Assert.Equal("3 transcripts saved next to the source files", vm.ResultMessage);
        Assert.Equal(_dir, vm.ResultDetail);
        Assert.False(vm.CanOpenTranscript);

        vm.ShowInFolderCommand.Execute(null);
        Assert.Equal([Path.Combine(_dir, "a_transcript.txt")], _launcher.Revealed);
        Assert.Empty(_launcher.Opened);
    }

    [Fact]
    public async Task Open_transcript_opens_the_saved_file()
    {
        var vm = await RunAsync("talk.mp3");

        vm.OpenTranscriptCommand.Execute(null);

        Assert.Equal([Path.Combine(_dir, "talk_transcript.txt")], _launcher.Opened);
        Assert.False(vm.HasActionMessage);
    }

    [Fact]
    public async Task With_no_app_for_the_file_type_it_falls_back_to_showing_the_folder()
    {
        _launcher.OpenResult = LaunchResult.NoHandler;
        var vm = await RunAsync("talk.mp3");
        vm.SelectedFormat = vm.Formats.First(f => f.Format == TranscriptFormat.Srt);

        vm.OpenTranscriptCommand.Execute(null);

        Assert.Single(_launcher.Revealed);
        Assert.Equal("No app is set to open .txt files. Showing it in the folder instead.", vm.ActionMessage);
        Assert.True(vm.ShowInFolderCommand.CanExecute(null), "the folder action stays available");
    }

    [Fact]
    public async Task A_moved_or_deleted_file_shows_a_message_and_disables_both_actions()
    {
        _launcher.OpenResult = LaunchResult.NotFound;
        var vm = await RunAsync("talk.mp3");

        vm.OpenTranscriptCommand.Execute(null);

        Assert.Equal("Couldn't find talk_transcript.txt. It may have been moved or deleted.", vm.ActionMessage);
        Assert.False(vm.OpenTranscriptCommand.CanExecute(null));
        Assert.False(vm.ShowInFolderCommand.CanExecute(null));
    }

    [Fact]
    public async Task A_missing_folder_shows_a_message_and_disables_both_actions()
    {
        _launcher.RevealResult = LaunchResult.NotFound;
        var vm = await RunAsync("talk.mp3");

        vm.ShowInFolderCommand.Execute(null);

        Assert.Equal("The folder is no longer available.", vm.ActionMessage);
        Assert.False(vm.OpenTranscriptCommand.CanExecute(null));
        Assert.False(vm.ShowInFolderCommand.CanExecute(null));
    }

    [Fact]
    public async Task Choosing_new_files_clears_the_previous_result_and_actions()
    {
        var vm = await RunAsync("talk.mp3");
        Assert.True(vm.HasSavedFiles);

        File.WriteAllText(Path.Combine(_dir, "next.mp3"), "x");
        vm.AddFiles([Path.Combine(_dir, "next.mp3")]);

        Assert.False(vm.HasSavedFiles);
        Assert.False(vm.CanOpenTranscript);
        Assert.Equal(string.Empty, vm.ResultDetail);
        Assert.False(vm.OpenTranscriptCommand.CanExecute(null));
    }
}
