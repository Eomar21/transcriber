using Transcriber.ViewModels;

public class TranscribeViewModelTests : IDisposable
{
    private readonly string _dir = Directory.CreateTempSubdirectory().FullName;

    public void Dispose() => Directory.Delete(_dir, recursive: true);

    // ---- fakes -----------------------------------------------------------------------

    private sealed class FakeFileReader : IFileReaderService
    {
        public List<string> TempFiles { get; } = [];

        public Task<string> GetAudioFileFromFileAsync(string filePath)
        {
            // Mimic a conversion that produces a temp wav.
            var temp = Path.Combine(Path.GetDirectoryName(filePath)!, Guid.NewGuid().ToString("N") + ".wav");
            File.WriteAllText(temp, "wav");
            TempFiles.Add(temp);
            return Task.FromResult(temp);
        }
    }

    private sealed class FakeWhisper : IWhisperService
    {
        public string? LastRuntime => "Fake";
        public Func<string, CancellationToken, Task<List<TranscriptSegment>>> Handler { get; set; } = (_, _) =>
            Task.FromResult(new List<TranscriptSegment>
            {
                new() { Start = TimeSpan.Zero, End = TimeSpan.FromSeconds(2), Text = " Hello world " },
                new() { Start = TimeSpan.FromSeconds(2), End = TimeSpan.FromSeconds(4), Text = "Goodbye moon" },
            });

        public Task<List<TranscriptSegment>> TranscribeAsync(string audioFilePath, TranscriptionOptions? options = null,
            IProgress<TranscriptionProgress>? progress = null, CancellationToken ct = default) =>
            Handler(audioFilePath, ct);
    }

    private sealed class FakePicker : IFilePickerService
    {
        public IReadOnlyList<string> Result { get; set; } = [];
        public Task<IReadOnlyList<string>> PickMediaFilesAsync() => Task.FromResult(Result);
    }

    private sealed class FakeLauncher : IFileLauncher
    {
        public LaunchResult OpenResult { get; set; } = LaunchResult.Opened;
        public LaunchResult RevealResult { get; set; } = LaunchResult.Opened;
        public List<string> Opened { get; } = [];
        public List<string> Revealed { get; } = [];
        public LaunchResult Open(string path) { Opened.Add(path); return OpenResult; }
        public LaunchResult Reveal(string path) { Revealed.Add(path); return RevealResult; }
    }

    private sealed class FakeClipboard : IClipboardService
    {
        public string? Text { get; private set; }
        public Task SetTextAsync(string text) { Text = text; return Task.CompletedTask; }
    }

    private readonly FakeLauncher Launcher = new();

    private (TranscribeViewModel Vm, FakeFileReader Reader, FakeWhisper Whisper, FakePicker Picker, FakeClipboard Clipboard) Create()
    {
        var reader = new FakeFileReader();
        var whisper = new FakeWhisper();
        var picker = new FakePicker();
        var clipboard = new FakeClipboard();
        return (new TranscribeViewModel(reader, whisper, picker, clipboard, Launcher), reader, whisper, picker, clipboard);
    }

    private string MakeInput(string name = "talk.mp3")
    {
        var path = Path.Combine(_dir, name);
        File.WriteAllText(path, "audio");
        return path;
    }

    // ---- tests -----------------------------------------------------------------------

    [Fact]
    public void Starts_with_sensible_defaults_and_cannot_transcribe_without_files()
    {
        var (vm, _, _, _, _) = Create();

        Assert.Equal("auto", vm.SelectedLanguage.Code);
        Assert.Equal(WhisperModel.Small, vm.SelectedModel);
        Assert.Equal(TranscriptFormat.Txt, vm.SelectedFormat.Format);
        Assert.False(vm.HasFiles);
        Assert.False(vm.TranscribeCommand.CanExecute(null));
    }

    [Fact]
    public async Task Browse_sets_files_and_enables_transcribe()
    {
        var (vm, _, _, picker, _) = Create();
        picker.Result = [MakeInput("a.mp3"), MakeInput("b.mp3")];

        await vm.BrowseCommand.ExecuteAsync(null);

        Assert.Equal("2 files selected", vm.FilesSummary);
        Assert.True(vm.TranscribeCommand.CanExecute(null));
    }

    [Fact]
    public async Task Cancelling_the_picker_keeps_the_current_selection()
    {
        var (vm, _, _, picker, _) = Create();
        vm.AddFiles([MakeInput()]);
        picker.Result = [];

        await vm.BrowseCommand.ExecuteAsync(null);

        Assert.Equal("talk.mp3", vm.FilesSummary);
    }

    [Fact]
    public void AddFiles_ignores_unsupported_types()
    {
        var (vm, _, _, _, _) = Create();

        vm.AddFiles([Path.Combine(_dir, "notes.txt"), Path.Combine(_dir, "photo.jpg")]);
        Assert.False(vm.HasFiles);

        vm.AddFiles([Path.Combine(_dir, "clip.MP4")]);
        Assert.Single(vm.SelectedFiles);
    }

    [Fact]
    public async Task Transcribe_produces_segments_saves_file_and_cleans_temp_audio()
    {
        var (vm, reader, _, _, _) = Create();
        var input = MakeInput();
        vm.AddFiles([input]);
        vm.SelectedFormat = vm.Formats.First(f => f.Format == TranscriptFormat.Srt);

        await vm.TranscribeCommand.ExecuteAsync(null);

        Assert.Equal(ResultKind.Success, vm.ResultKind);
        Assert.Equal("Transcript saved", vm.ResultMessage);
        Assert.EndsWith("talk_transcript.srt", vm.ResultDetail);
        Assert.True(File.Exists(Path.Combine(_dir, "talk_transcript.srt")));
        Assert.Equal(["Hello world", "Goodbye moon"], vm.VisibleSegments.Select(s => s.Text));
        Assert.True(vm.HasResults);
        Assert.False(vm.IsBusy);
        Assert.All(reader.TempFiles, t => Assert.False(File.Exists(t)));
        Assert.True(File.Exists(input), "the user's own input file must never be deleted");
    }

    [Fact]
    public async Task Search_filters_the_visible_segments_case_insensitively()
    {
        var (vm, _, _, _, _) = Create();
        vm.AddFiles([MakeInput()]);
        await vm.TranscribeCommand.ExecuteAsync(null);

        vm.SearchText = "MOON";
        Assert.Equal(["Goodbye moon"], vm.VisibleSegments.Select(s => s.Text));

        vm.SearchText = "";
        Assert.Equal(2, vm.VisibleSegments.Count);
    }

    [Fact]
    public async Task Multiple_files_are_all_transcribed_and_rows_name_their_source()
    {
        var (vm, _, _, _, _) = Create();
        vm.AddFiles([MakeInput("one.mp3"), MakeInput("two.mp3")]);

        await vm.TranscribeCommand.ExecuteAsync(null);

        Assert.Contains("2 transcripts saved", vm.ResultMessage);
        Assert.Equal(4, vm.VisibleSegments.Count);
        Assert.Contains(vm.VisibleSegments, s => s.Source == "two.mp3");
    }

    [Fact]
    public async Task A_failure_is_reported_and_the_screen_is_usable_again()
    {
        var (vm, _, whisper, _, _) = Create();
        whisper.Handler = (_, _) => throw new InvalidOperationException("boom");
        vm.AddFiles([MakeInput()]);

        await vm.TranscribeCommand.ExecuteAsync(null);

        Assert.Equal(ResultKind.Error, vm.ResultKind);
        Assert.Contains("boom", vm.ResultMessage);
        Assert.False(vm.IsBusy);
        Assert.True(vm.TranscribeCommand.CanExecute(null));
    }

    [Fact]
    public async Task Cancel_stops_a_running_transcription()
    {
        var (vm, _, whisper, _, _) = Create();
        var started = new TaskCompletionSource();
        whisper.Handler = async (_, ct) =>
        {
            started.SetResult();
            await Task.Delay(Timeout.Infinite, ct);
            return [];
        };
        vm.AddFiles([MakeInput()]);

        var run = vm.TranscribeCommand.ExecuteAsync(null);
        await started.Task;
        Assert.True(vm.IsBusy);
        Assert.False(vm.TranscribeCommand.CanExecute(null), "cannot start a second run while busy");

        vm.TranscribeCancelCommand.Execute(null);
        await run;

        Assert.Equal(ResultKind.Info, vm.ResultKind);
        Assert.Equal("Cancelled.", vm.ResultMessage);
        Assert.False(vm.IsBusy);
    }

    [Fact]
    public async Task CopyAll_puts_the_whole_transcript_on_the_clipboard()
    {
        var (vm, _, _, _, clipboard) = Create();
        vm.AddFiles([MakeInput()]);
        await vm.TranscribeCommand.ExecuteAsync(null);

        await vm.CopyAllCommand.ExecuteAsync(null);

        Assert.Equal($"Hello world{Environment.NewLine}Goodbye moon", clipboard.Text);
    }
}
