using Transcriber.ViewModels;
using Transcriber.ViewModels.Transcribe;

public class DragAndDropTests
{
    private sealed class Reader : IFileReaderService
    {
        public Task<string> GetAudioFileFromFileAsync(string filePath, CancellationToken ct = default) => Task.FromResult(filePath);
    }

    private sealed class Whisper : IWhisperService
    {
        public string? LastRuntime => null;
        public Task<List<TranscriptSegment>> TranscribeAsync(string audioFilePath, TranscriptionOptions? options = null,
            IProgress<TranscriptionProgress>? progress = null, CancellationToken ct = default) => Task.FromResult(new List<TranscriptSegment>());
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
        public LaunchResult Open(string path) => LaunchResult.Opened;
        public LaunchResult Reveal(string path) => LaunchResult.Opened;
    }

    private static TranscribeViewModel Create() => TranscribeFixture.Create(new Reader(), new Whisper(), new Picker(), new Clipboard(), new Launcher(), new FakeModels(), new FakeDialogs(), new FakePlayer());

    [Fact]
    public void Dropped_media_files_are_added()
    {
        var vm = Create();

        var added = vm.Files.AddFiles([@"C:\a\talk.mp3", @"C:\a\clip.MP4"]);

        Assert.Equal(2, added);
        Assert.Equal(2, vm.Files.SelectedFiles.Count);
        Assert.True(vm.Files.HasFiles);
        Assert.False(vm.Files.HasDropMessage);
    }

    [Fact]
    public void Dropping_only_unsupported_files_changes_nothing_and_explains_why()
    {
        var vm = Create();

        var added = vm.Files.AddFiles([@"C:\a\notes.pdf", @"C:\a\photo.jpg"]);

        Assert.Equal(0, added);
        Assert.False(vm.Files.HasFiles);
        Assert.Contains("can't be transcribed", vm.Files.DropMessage);
    }

    [Fact]
    public void A_mixed_drop_adds_the_supported_files_and_reports_the_rest()
    {
        var vm = Create();

        vm.Files.AddFiles([@"C:\a\talk.mp3", @"C:\a\notes.pdf"]);

        Assert.Single(vm.Files.SelectedFiles);
        Assert.Equal("Added 1 of 2 files. The rest were unsupported or already in the list.", vm.Files.DropMessage);
    }

    [Fact]
    public void Dropping_the_same_file_twice_does_not_duplicate_it()
    {
        var vm = Create();
        vm.Files.AddFiles([@"C:\a\talk.mp3"]);

        var added = vm.Files.AddFiles([@"C:\A\TALK.mp3"]);

        Assert.Equal(0, added);
        Assert.Single(vm.Files.SelectedFiles);
    }

    [Fact]
    public void Drops_are_appended_to_the_existing_list()
    {
        var vm = Create();
        vm.Files.AddFiles([@"C:\a\one.mp3"]);
        vm.Files.AddFiles([@"C:\a\two.wav"]);

        Assert.Equal(2, vm.Files.SelectedFiles.Count);
    }

    [Fact]
    public void A_drop_ends_the_drag_highlight()
    {
        var vm = Create();
        vm.Files.IsDragOver = true;

        vm.Files.AddFiles([@"C:\a\talk.mp3"]);

        Assert.False(vm.Files.IsDragOver);
    }

    [Fact]
    public void Clearing_the_files_clears_the_drop_message()
    {
        var vm = Create();
        vm.Files.AddFiles([@"C:\a\notes.pdf"]);
        Assert.True(vm.Files.HasDropMessage);

        vm.Files.ClearFilesCommand.Execute(null);

        Assert.False(vm.Files.HasDropMessage);
    }
}
