using Microsoft.Extensions.DependencyInjection;
using Transcriber.Services.Extensions;
using Transcriber.ViewModels;
using Transcriber.ViewModels.Extensions;
using Transcriber.ViewModels.Transcribe;

/// <summary>Each section of the Transcribe screen works alone. It knows the run state and nothing else.</summary>
public class TranscribeSectionTests
{
    private sealed class NoPicker : IFilePickerService
    {
        public Task<IReadOnlyList<string>> PickMediaFilesAsync() => Task.FromResult<IReadOnlyList<string>>([]);
    }

    private sealed class Clipboard : IClipboardService
    {
        public string? Text { get; private set; }
        public Task SetTextAsync(string text) { Text = text; return Task.CompletedTask; }
    }

    private sealed class NoSuggestion : IModelSuggestion
    {
        public string UseSuggestedModelText => "Use the Medium model";
        public CommunityToolkit.Mvvm.Input.IRelayCommand UseSuggestedModelCommand { get; } = new CommunityToolkit.Mvvm.Input.RelayCommand(() => { });
    }

    private static SegmentItem Line(string text, string file = "a.mp3", double start = 0, double end = 2) =>
        new(TimeSpan.FromSeconds(start), TimeSpan.FromSeconds(end), text, string.Empty, file, null);

    [Fact]
    public void The_files_section_tells_the_screen_when_the_list_changes()
    {
        var files = new FilesViewModel(new NoPicker(), ServicesFixture.Formats, new RunState());
        var changes = 0;
        files.FilesChanged += (_, _) => changes++;

        files.AddFiles(["a.mp3", "b.wav"]);
        files.ClearFilesCommand.Execute(null);

        Assert.Equal(2, changes);
        Assert.False(files.HasFiles);
    }

    [Fact]
    public void A_busy_run_blocks_the_files_section_and_every_section_sees_the_same_state()
    {
        var run = new RunState();
        var files = new FilesViewModel(new NoPicker(), ServicesFixture.Formats, run);
        var status = new RunStatusViewModel(new NoSuggestion(), run);
        var seen = new List<string>();
        status.PropertyChanged += (_, e) => seen.Add(e.PropertyName!);

        run.IsBusy = true;

        Assert.Equal(0, files.AddFiles(["a.mp3"]));
        Assert.True(status.IsBusy);
        Assert.Contains(nameof(RunStatusViewModel.IsIdle), seen);
    }

    [Fact]
    public void Picking_a_line_raises_an_event_and_clears_the_pick_so_it_can_be_picked_again()
    {
        var results = new ResultsViewModel(new Clipboard());
        var line = Line("hello");
        results.Show([line]);
        var picked = new List<SegmentItem>();
        results.SegmentPicked += (_, s) => picked.Add(s);

        results.SelectedSegment = line;
        results.SelectedSegment = line;

        Assert.Equal([line, line], picked);
        Assert.Null(results.SelectedSegment);
    }

    [Fact]
    public async Task The_results_section_searches_and_copies_without_the_other_sections()
    {
        var clipboard = new Clipboard();
        var results = new ResultsViewModel(clipboard);
        results.Show([Line("good morning"), Line("good night")]);

        results.SearchText = "night";
        await results.CopyAllCommand.ExecuteAsync(null);

        Assert.Equal(["good night"], results.VisibleSegments.Select(s => s.Text));
        Assert.True(results.HasResults);
        Assert.Contains("good morning", clipboard.Text);
    }

    [Fact]
    public void Playing_a_line_whose_file_is_gone_reports_a_problem_and_does_not_start()
    {
        var player = new FakePlayer();
        var playback = new PlaybackViewModel(player);
        string? problem = null;
        playback.Problem += (_, message) => problem = message;

        playback.PlaySegment(Line("x", file: Path.Combine(Path.GetTempPath(), "gone-" + Guid.NewGuid().ToString("N") + ".mp3")));

        Assert.Contains("Couldn't find", problem);
        Assert.False(playback.HasPlayback);
        Assert.Null(player.PlayedPath);
    }

    [Fact]
    public void The_status_section_clears_the_outcome_of_an_earlier_run()
    {
        var status = new RunStatusViewModel(new NoSuggestion(), new RunState());
        status.SetResult(ResultKind.Success, "Transcript saved");
        status.FailureNote = "x";

        status.Clear();

        Assert.False(status.HasResultMessage);
        Assert.False(status.HasFailureNote);
        Assert.Equal(ResultKind.None, status.ResultKind);
    }

    [Fact]
    public void A_report_from_another_section_shows_under_the_saved_transcript_buttons()
    {
        var saved = new SavedTranscriptsViewModel(new Launcher(), new RunState());

        saved.Report("Couldn't find a.mp3 to play it.");

        Assert.True(saved.HasActionMessage);
    }

    private sealed class Launcher : IFileLauncher
    {
        public LaunchResult Open(string path) => LaunchResult.Opened;
        public LaunchResult Reveal(string path) => LaunchResult.Opened;
    }

    [Fact]
    public void The_container_builds_the_screen_with_one_instance_of_each_section()
    {
        var services = new ServiceCollection()
            .AddTranscriberServices(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".json"))
            .AddNativeSpeakerDetection()
            .AddTranscribeScreen()
            .AddSingleton<IFilePickerService, NoPicker>()
            .AddSingleton<IClipboardService, Clipboard>()
            .AddSingleton<IDialogService, FakeDialogs>()
            .AddSingleton<IAudioPlayer, FakePlayer>();
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });

        var screen = provider.GetRequiredService<TranscribeViewModel>();

        Assert.Same(provider.GetRequiredService<FilesViewModel>(), screen.Files);
        Assert.Same(provider.GetRequiredService<OptionsViewModel>(), screen.Options);
provider.GetRequiredService<RunState>().IsBusy = true;        Assert.True(screen.IsBusy);        Assert.True(screen.Files.IsBusy);        Assert.True(screen.Options.IsBusy);        Assert.True(screen.Status.IsBusy);
    }
}
