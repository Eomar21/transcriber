using Transcriber.ViewModels;
using Transcriber.ViewModels.Transcribe;

public class ModelPickerTests
{
    private sealed class Reader : IFileReaderService
    {
        public Task<string> GetAudioFileFromFileAsync(string filePath, CancellationToken ct = default) => Task.FromResult(filePath);
    }

    private sealed class Whisper : IWhisperService
    {
        public int Calls { get; private set; }
        public WhisperModel? LastModel { get; private set; }
        public string? LastRuntime => null;
        public Task<List<TranscriptSegment>> TranscribeAsync(string audioFilePath, TranscriptionOptions? options = null,
            IProgress<TranscriptionProgress>? progress = null, CancellationToken ct = default)
        {
            Calls++;
            LastModel = options?.Model;
            return Task.FromResult(new List<TranscriptSegment> { new() { End = TimeSpan.FromSeconds(1), Text = "Hi" } });
        }
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

    private readonly string _dir = Directory.CreateTempSubdirectory().FullName;
    private readonly FakeModels _models = new();
    private readonly FakeDialogs _dialogs = new();
    private readonly Whisper _whisper = new();

    private TranscribeViewModel Create()
    {
        var vm = TranscribeFixture.Create(new Reader(), _whisper, new Picker(), new Clipboard(), new Launcher(), _models, _dialogs, new FakePlayer());
        var file = Path.Combine(_dir, "talk.mp3");
        File.WriteAllText(file, "x");
        vm.Files.AddFiles([file]);
        return vm;
    }

    private ModelOption Option(TranscribeViewModel vm, WhisperModel model) => vm.Options.Models.First(m => m.Model == model);

    [Fact]
    public void Installed_models_are_labelled_installed_and_others_show_their_download_size()
    {
        _models.Missing.Add(TestModels.MediumQ5.Id);
        var vm = Create();

        Assert.Contains("installed", Option(vm, TestModels.Small).Label);
        Assert.Contains("539 MB download", Option(vm, TestModels.MediumQ5).Label);
    }

    [Fact]
    public void The_default_model_is_the_bundled_small_one_and_shows_no_download_hint()
    {
        var vm = Create();

        Assert.Equal(TestModels.Small, vm.Options.SelectedModel);
        Assert.False(vm.Options.HasModelHint);
    }

    [Fact]
    public void Picking_a_model_that_is_not_installed_shows_a_download_hint()
    {
        _models.Missing.Add(TestModels.MediumQ5.Id);
        var vm = Create();

        vm.Options.SelectedModelOption = Option(vm, TestModels.MediumQ5);

        Assert.True(vm.Options.HasModelHint);
        Assert.Contains("one-time download of 539 MB", vm.Options.ModelHint);
    }

    [Fact]
    public async Task Declining_the_download_stops_before_anything_runs()
    {
        _models.Missing.Add(TestModels.MediumQ5.Id);
        _dialogs.Answer = false;
        var vm = Create();
        vm.Options.SelectedModelOption = Option(vm, TestModels.MediumQ5);

        await vm.TranscribeCommand.ExecuteAsync(null);

        Assert.Equal(0, _whisper.Calls);
        Assert.Single(_dialogs.Messages);
        Assert.Equal(ResultKind.Info, vm.Status.ResultKind);
        Assert.False(vm.IsBusy);
    }

    [Fact]
    public async Task Accepting_the_download_runs_the_transcription_with_that_model()
    {
        _models.Missing.Add(TestModels.MediumQ5.Id);
        var vm = Create();
        vm.Options.SelectedModelOption = Option(vm, TestModels.MediumQ5);

        await vm.TranscribeCommand.ExecuteAsync(null);

        Assert.Equal(1, _whisper.Calls);
        Assert.Equal(TestModels.MediumQ5, _whisper.LastModel);
    }

    [Fact]
    public async Task An_installed_model_never_asks_for_confirmation()
    {
        var vm = Create();

        await vm.TranscribeCommand.ExecuteAsync(null);

        Assert.Empty(_dialogs.Messages);
        Assert.Equal(1, _whisper.Calls);
    }

    [Fact]
    public async Task After_a_download_the_model_is_labelled_installed_and_the_hint_disappears()
    {
        _models.Missing.Add(TestModels.MediumQ5.Id);
        var vm = Create();
        vm.Options.SelectedModelOption = Option(vm, TestModels.MediumQ5);
        Assert.True(vm.Options.HasModelHint);

        // The download finishes while transcribing.
        _models.Missing.Remove(TestModels.MediumQ5.Id);
        await vm.TranscribeCommand.ExecuteAsync(null);

        Assert.Contains("installed", Option(vm, TestModels.MediumQ5).Label);
        Assert.False(vm.Options.HasModelHint);
    }
}
