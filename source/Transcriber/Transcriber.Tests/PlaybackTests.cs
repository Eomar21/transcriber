using Transcriber.ViewModels;

/// <summary>Click a transcript line to play the audio from that moment.</summary>
public class PlaybackTests : IDisposable
{
    private readonly string _dir = Directory.CreateTempSubdirectory().FullName;
    private readonly FakePlayer _player = new();

    public void Dispose() => Directory.Delete(_dir, true);

    /// <summary>Pretends to convert each file by writing a separate temp wav, like the real reader does.</summary>
    private sealed class Reader(string dir) : IFileReaderService
    {
        public List<string> Converted { get; } = [];

        public Task<string> GetAudioFileFromFileAsync(string filePath)
        {
            var wav = Path.Combine(dir, $"conv_{Path.GetFileNameWithoutExtension(filePath)}_{Converted.Count}.wav");
            File.WriteAllText(wav, "wav");
            Converted.Add(wav);
            return Task.FromResult(wav);
        }
    }

    private sealed class InPlaceReader : IFileReaderService
    {
        public Task<string> GetAudioFileFromFileAsync(string filePath) => Task.FromResult(filePath);
    }

    private sealed class Whisper : IWhisperService
    {
        public string? LastRuntime => null;

        public Task<List<TranscriptSegment>> TranscribeAsync(string audioFilePath, TranscriptionOptions? options = null,
            IProgress<TranscriptionProgress>? progress = null, CancellationToken ct = default) =>
            Task.FromResult(new List<TranscriptSegment>
            {
                new() { Start = TimeSpan.FromSeconds(0), End = TimeSpan.FromSeconds(5), Text = "First" },
                new() { Start = TimeSpan.FromSeconds(5), End = TimeSpan.FromSeconds(12), Text = "Second" },
            });
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

    private TranscribeViewModel Create(IFileReaderService reader) =>
        new(reader, new Whisper(), new Picker(), new Clipboard(), new Launcher(), new FakeModels(), new FakeDialogs(), _player);

    private async Task<(TranscribeViewModel Vm, Reader Reader)> RunAsync(params string[] names)
    {
        var reader = new Reader(_dir);
        var vm = Create(reader);
        foreach (var n in names) File.WriteAllText(Path.Combine(_dir, n), "media");
        vm.AddFiles(names.Select(n => Path.Combine(_dir, n)));
        await vm.TranscribeCommand.ExecuteAsync(null);
        return (vm, reader);
    }

    [Fact]
    public async Task Clicking_a_line_plays_the_converted_audio_from_that_lines_start()
    {
        var (vm, reader) = await RunAsync("talk.mp4");

        vm.PlaySegmentCommand.Execute(vm.VisibleSegments[1]);

        Assert.Equal(reader.Converted[0], _player.PlayedPath);
        Assert.Equal(TimeSpan.FromSeconds(5), _player.PlayedFrom);
        Assert.True(vm.IsPlaying);
        Assert.True(vm.HasPlayback);
        Assert.StartsWith("talk.mp4", vm.PlaybackText);
    }

    [Fact]
    public async Task Picking_a_line_plays_it_and_the_same_line_can_be_picked_again()
    {
        var (vm, _) = await RunAsync("talk.mp4");
        var first = vm.VisibleSegments[0];
        var second = vm.VisibleSegments[1];

        vm.SelectedSegment = second;
        Assert.Equal(second.Start, _player.PlayedFrom);
        Assert.Null(vm.SelectedSegment);

        vm.SelectedSegment = first;
        Assert.Equal(first.Start, _player.PlayedFrom);

        vm.SelectedSegment = first;
        Assert.Equal(first.Start, _player.PlayedFrom);
        Assert.Null(vm.SelectedSegment);
    }

    [Fact]
    public async Task In_a_batch_each_line_plays_its_own_file()
    {
        var (vm, reader) = await RunAsync("a.mp3", "b.mp3");
        var fromB = vm.VisibleSegments.First(s => s.Source == "b.mp3");

        vm.PlaySegmentCommand.Execute(fromB);

        Assert.Equal(reader.Converted[1], _player.PlayedPath);
    }

    [Fact]
    public async Task The_line_being_played_is_highlighted_and_follows_the_clock()
    {
        var (vm, _) = await RunAsync("talk.mp3");
        vm.PlaySegmentCommand.Execute(vm.VisibleSegments[0]);
        Assert.True(vm.VisibleSegments[0].IsActive);
        Assert.False(vm.VisibleSegments[1].IsActive);

        _player.Tick(TimeSpan.FromSeconds(6));

        Assert.False(vm.VisibleSegments[0].IsActive);
        Assert.True(vm.VisibleSegments[1].IsActive);
    }

    [Fact]
    public async Task Pause_resume_and_stop_work_and_stop_clears_the_highlight()
    {
        var (vm, _) = await RunAsync("talk.mp3");
        vm.PlaySegmentCommand.Execute(vm.VisibleSegments[0]);

        vm.TogglePlaybackCommand.Execute(null);
        Assert.False(vm.IsPlaying);
        vm.TogglePlaybackCommand.Execute(null);
        Assert.True(vm.IsPlaying);

        vm.StopPlaybackCommand.Execute(null);
        Assert.False(vm.IsPlaying);
        Assert.False(vm.HasPlayback);
        Assert.All(vm.VisibleSegments, s => Assert.False(s.IsActive));
    }

    [Fact]
    public async Task Converted_audio_is_kept_for_playback_and_removed_with_the_next_run()
    {
        var (vm, reader) = await RunAsync("talk.mp3");
        Assert.True(File.Exists(reader.Converted[0]));

        await vm.TranscribeCommand.ExecuteAsync(null);

        Assert.False(File.Exists(reader.Converted[0]));
        Assert.True(File.Exists(reader.Converted[1]));
    }

    [Fact]
    public async Task A_wav_used_in_place_is_never_deleted()
    {
        var own = Path.Combine(_dir, "own.wav");
        File.WriteAllText(own, "wav");
        var vm = Create(new InPlaceReader());
        vm.AddFiles([own]);

        await vm.TranscribeCommand.ExecuteAsync(null);
        await vm.TranscribeCommand.ExecuteAsync(null);

        Assert.True(File.Exists(own));
    }

    [Fact]
    public async Task Clicking_a_line_whose_files_are_gone_shows_a_message_and_plays_nothing()
    {
        var (vm, reader) = await RunAsync("talk.mp3");
        File.Delete(reader.Converted[0]);
        File.Delete(Path.Combine(_dir, "talk.mp3"));

        vm.PlaySegmentCommand.Execute(vm.VisibleSegments[0]);

        Assert.Null(_player.PlayedPath);
        Assert.Contains("Couldn't find talk.mp3", vm.ActionMessage);
    }
}
