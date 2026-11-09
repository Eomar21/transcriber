using Transcriber.ViewModels;
using Transcriber.ViewModels.Transcribe;

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
            if (Path.GetFileName(filePath).StartsWith("bad", StringComparison.Ordinal))
                throw new InvalidOperationException("This file could not be read. It may be damaged, or it may not be an audio or video file.");

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
        public SpeechCoverage? Coverage { get; set; }
        public SpeechCoverage? LastCoverage => Coverage;
        /// <summary>Called before the handler, so a test can report progress the way the real service does.</summary>
        public Action<IProgress<TranscriptionProgress>?>? OnProgress { get; set; }
        public Func<string, CancellationToken, Task<List<TranscriptSegment>>> Handler { get; set; } = (_, _) =>
            Task.FromResult(new List<TranscriptSegment>
            {
                new() { Start = TimeSpan.Zero, End = TimeSpan.FromSeconds(2), Text = " Hello world " },
                new() { Start = TimeSpan.FromSeconds(2), End = TimeSpan.FromSeconds(4), Text = "Goodbye moon" },
            });

        public Task<List<TranscriptSegment>> TranscribeAsync(string audioFilePath, TranscriptionOptions? options = null,
            IProgress<TranscriptionProgress>? progress = null, CancellationToken ct = default) =>
            RunAsync(audioFilePath, progress, ct);

        private Task<List<TranscriptSegment>> RunAsync(string audioFilePath, IProgress<TranscriptionProgress>? progress, CancellationToken ct)
        {
            OnProgress?.Invoke(progress);
            return Handler(audioFilePath, ct);
        }
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
    private readonly FakeModels Models = new();
    private readonly FakeDialogs Dialogs = new();
    private readonly FakePlayer Player = new();

    private (TranscribeViewModel Vm, FakeFileReader Reader, FakeWhisper Whisper, FakePicker Picker, FakeClipboard Clipboard) Create()
    {
        var reader = new FakeFileReader();
        var whisper = new FakeWhisper();
        var picker = new FakePicker();
        var clipboard = new FakeClipboard();
        return (TranscribeFixture.Create(reader, whisper, picker, clipboard, Launcher, Models, Dialogs, Player), reader, whisper, picker, clipboard);
    }

    private string MakeInput(string name = "talk.mp3")
    {
        var path = Path.Combine(_dir, name);
        File.WriteAllText(path, "audio");
        return path;
    }

    // ---- tests -----------------------------------------------------------------------


    // ---- languages and accuracy hints --------------------------------------------------

    private static LanguageOption Pick(TranscribeViewModel vm, string code) => vm.Options.Languages.First(l => l.Code == code);

    [Fact]
    public void Language_list_follows_the_selected_model()
    {
        var (vm, _, _, _, _) = Create();
        Assert.DoesNotContain(vm.Options.Languages, l => l.Code == "fr");

        vm.Options.SelectedModelOption = vm.Options.Models.First(m => m.Model == TestModels.MediumQ5);
        Assert.Contains(vm.Options.Languages, l => l.Code == "fr");

        vm.Options.SelectedModelOption = vm.Options.Models.First(m => m.Model == TestModels.Small);
        Assert.DoesNotContain(vm.Options.Languages, l => l.Code == "fr");
    }

    [Fact]
    public void Language_choice_is_kept_when_the_new_model_still_offers_it_and_resets_when_not()
    {
        var (vm, _, _, _, _) = Create();
        vm.Options.SelectedModelOption = vm.Options.Models.First(m => m.Model == TestModels.MediumQ5);
        vm.Options.SelectedLanguage = Pick(vm, "fr");

        vm.Options.SelectedModelOption = vm.Options.Models.First(m => m.Model == TestModels.LargeV3TurboQ5);
        Assert.Equal("fr", vm.Options.SelectedLanguage.Code);

        vm.Options.SelectedModelOption = vm.Options.Models.First(m => m.Model == TestModels.Small);
        Assert.Equal("auto", vm.Options.SelectedLanguage.Code);
    }

    [Fact]
    public void Dutch_with_small_shows_advice_and_the_button_switches_to_medium()
    {
        var (vm, _, _, _, _) = Create();
        Assert.False(vm.Options.HasLanguageAdvice);

        vm.Options.SelectedLanguage = Pick(vm, "nl");
        Assert.True(vm.Options.HasLanguageAdvice);
        Assert.Contains("Medium", vm.Options.LanguageAdvice);
        Assert.Contains("better with Medium", vm.Options.SelectedLanguage.Label);

        vm.Options.UseSuggestedModelCommand.Execute(null);

        Assert.Equal(TestModels.MediumQ5, vm.Options.SelectedModel);
        Assert.Equal("nl", vm.Options.SelectedLanguage.Code);
        Assert.False(vm.Options.HasLanguageAdvice);
    }

    [Fact]
    public async Task Result_shows_accuracy_note_for_dutch_with_small_and_clears_on_next_run()
    {
        var (vm, _, _, picker, _) = Create();
        vm.Options.SelectedLanguage = Pick(vm, "nl");
        picker.Result = [MakeInput()];
        await vm.Files.BrowseCommand.ExecuteAsync(null);

        await vm.TranscribeCommand.ExecuteAsync(null);
        Assert.True(vm.Status.HasAccuracyNote);

        vm.Options.SelectedLanguage = Pick(vm, "en");
        await vm.TranscribeCommand.ExecuteAsync(null);
        Assert.False(vm.Status.HasAccuracyNote);
    }

    [Fact]
    public async Task Incomplete_transcript_shows_a_note_that_suggests_the_main_language_for_auto()
    {
        var (vm, _, whisper, picker, _) = Create();
        whisper.Coverage = new SpeechCoverage(SpeechSeconds: 30, TextSeconds: 12) { LooksIncomplete = true };
        picker.Result = [MakeInput()];
        await vm.Files.BrowseCommand.ExecuteAsync(null);

        await vm.TranscribeCommand.ExecuteAsync(null);

        Assert.True(vm.Status.HasCompletenessNote);
        Assert.Contains("18 of 30 seconds", vm.Status.CompletenessNote);
        Assert.Contains("main language", vm.Status.CompletenessNote);
    }

    [Fact]
    public async Task Complete_transcript_shows_no_completeness_note()
    {
        var (vm, _, whisper, picker, _) = Create();
        whisper.Coverage = new SpeechCoverage(30, 29);
        picker.Result = [MakeInput()];
        await vm.Files.BrowseCommand.ExecuteAsync(null);

        await vm.TranscribeCommand.ExecuteAsync(null);

        Assert.False(vm.Status.HasCompletenessNote);
    }
    [Fact]
    public void Starts_with_sensible_defaults_and_cannot_transcribe_without_files()
    {
        var (vm, _, _, _, _) = Create();

        Assert.Equal("auto", vm.Options.SelectedLanguage.Code);
        Assert.Equal(TestModels.Small, vm.Options.SelectedModel);
        Assert.Equal(TranscriptFormat.Txt, vm.Options.SelectedFormat.Format);
        Assert.False(vm.Files.HasFiles);
        Assert.False(vm.TranscribeCommand.CanExecute(null));
    }

    [Fact]
    public async Task Browse_sets_files_and_enables_transcribe()
    {
        var (vm, _, _, picker, _) = Create();
        picker.Result = [MakeInput("a.mp3"), MakeInput("b.mp3")];

        await vm.Files.BrowseCommand.ExecuteAsync(null);

        Assert.Equal("2 files selected", vm.Files.FilesSummary);
        Assert.True(vm.TranscribeCommand.CanExecute(null));
    }

    [Fact]
    public async Task Cancelling_the_picker_keeps_the_current_selection()
    {
        var (vm, _, _, picker, _) = Create();
        vm.Files.AddFiles([MakeInput()]);
        picker.Result = [];

        await vm.Files.BrowseCommand.ExecuteAsync(null);

        Assert.Equal("talk.mp3", vm.Files.FilesSummary);
    }

    [Fact]
    public void AddFiles_ignores_unsupported_types()
    {
        var (vm, _, _, _, _) = Create();

        vm.Files.AddFiles([Path.Combine(_dir, "notes.txt"), Path.Combine(_dir, "photo.jpg")]);
        Assert.False(vm.Files.HasFiles);

        vm.Files.AddFiles([Path.Combine(_dir, "clip.MP4")]);
        Assert.Single(vm.Files.SelectedFiles);
    }

    [Fact]
    public async Task Transcribe_produces_segments_saves_file_and_keeps_converted_audio_for_playback()
    {
        var (vm, reader, _, _, _) = Create();
        var input = MakeInput();
        vm.Files.AddFiles([input]);
        vm.Options.SelectedFormat = vm.Options.Formats.First(f => f.Format == TranscriptFormat.Srt);

        await vm.TranscribeCommand.ExecuteAsync(null);

        Assert.Equal(ResultKind.Success, vm.Status.ResultKind);
        Assert.Equal("Transcript saved", vm.Status.ResultMessage);
        Assert.EndsWith("talk_transcript.srt", vm.Status.ResultDetail);
        Assert.True(File.Exists(Path.Combine(_dir, "talk_transcript.srt")));
        Assert.Equal(["Hello world", "Goodbye moon"], vm.Results.VisibleSegments.Select(s => s.Text));
        Assert.True(vm.Results.HasResults);
        Assert.False(vm.IsBusy);
        Assert.All(reader.TempFiles, t => Assert.True(File.Exists(t), "kept so a clicked line can play it"));
        Assert.True(File.Exists(input), "the user's own input file must never be deleted");
    }

    // ---- files that cannot be read -----------------------------------------------------------

    private const string Unreadable = "This file could not be read. It may be damaged, or it may not be an audio or video file.";

    [Fact]
    public async Task A_single_unreadable_file_shows_its_reason_as_the_failure()
    {
        var (vm, _, _, picker, _) = Create();
        picker.Result = [MakeInput("bad.xyz")];
        await vm.Files.BrowseAnyCommand.ExecuteAsync(null);

        await vm.TranscribeCommand.ExecuteAsync(null);

        Assert.Equal(ResultKind.Error, vm.Status.ResultKind);
        Assert.Equal("Failed: " + Unreadable, vm.Status.ResultMessage);
        Assert.False(vm.Status.HasFailureNote);
        Assert.False(vm.IsBusy);
    }

    [Fact]
    public async Task One_unreadable_file_does_not_stop_the_others()
    {
        var (vm, _, _, _, _) = Create();
        vm.Files.AddFiles([MakeInput("one.mp3"), MakeInput("bad.mp3"), MakeInput("two.mp3")]);

        await vm.TranscribeCommand.ExecuteAsync(null);

        Assert.Equal(ResultKind.Success, vm.Status.ResultKind);
        Assert.Equal("2 transcripts saved next to the source files", vm.Status.ResultMessage);
        Assert.True(File.Exists(Path.Combine(_dir, "one_transcript.txt")));
        Assert.True(File.Exists(Path.Combine(_dir, "two_transcript.txt")));
        Assert.False(File.Exists(Path.Combine(_dir, "bad_transcript.txt")));
        Assert.True(vm.Status.HasFailureNote);
        Assert.Contains("1 of 3 files were skipped.", vm.Status.FailureNote);
        Assert.Contains("bad.mp3: " + Unreadable, vm.Status.FailureNote);
    }

    [Fact]
    public async Task When_no_file_can_be_read_each_one_is_listed_with_its_reason()
    {
        var (vm, _, _, _, _) = Create();
        vm.Files.AddFiles([MakeInput("bad1.mp3"), MakeInput("bad2.mp3")]);

        await vm.TranscribeCommand.ExecuteAsync(null);

        Assert.Equal(ResultKind.Error, vm.Status.ResultKind);
        Assert.Equal("Failed: none of the 2 files could be read", vm.Status.ResultMessage);
        Assert.Contains("bad1.mp3: " + Unreadable, vm.Status.FailureNote);
        Assert.Contains("bad2.mp3: " + Unreadable, vm.Status.FailureNote);
        Assert.False(vm.Results.HasResults);
    }

    [Fact]
    public async Task The_failure_note_is_cleared_for_the_next_run()
    {
        var (vm, _, _, _, _) = Create();
        vm.Files.AddFiles([MakeInput("one.mp3"), MakeInput("bad.mp3")]);
        await vm.TranscribeCommand.ExecuteAsync(null);
        Assert.True(vm.Status.HasFailureNote);

        vm.Files.ClearFilesCommand.Execute(null);

        Assert.False(vm.Status.HasFailureNote);
    }

    [Fact]
    public async Task Choosing_any_file_type_replaces_the_list_with_the_chosen_files()
    {
        var (vm, _, _, picker, _) = Create();
        picker.Result = [MakeInput("voice.xyz")];

        await vm.Files.BrowseAnyCommand.ExecuteAsync(null);

        Assert.Equal(["voice.xyz"], vm.Files.SelectedFiles.Select(Path.GetFileName));
    }

    [Fact]
    public async Task Search_filters_the_visible_segments_case_insensitively()
    {
        var (vm, _, _, _, _) = Create();
        vm.Files.AddFiles([MakeInput()]);
        await vm.TranscribeCommand.ExecuteAsync(null);

        vm.Results.SearchText = "MOON";
        Assert.Equal(["Goodbye moon"], vm.Results.VisibleSegments.Select(s => s.Text));

        vm.Results.SearchText = "";
        Assert.Equal(2, vm.Results.VisibleSegments.Count);
    }

    [Fact]
    public async Task Multiple_files_are_all_transcribed_and_rows_name_their_source()
    {
        var (vm, _, _, _, _) = Create();
        vm.Files.AddFiles([MakeInput("one.mp3"), MakeInput("two.mp3")]);

        await vm.TranscribeCommand.ExecuteAsync(null);

        Assert.Contains("2 transcripts saved", vm.Status.ResultMessage);
        Assert.Equal(4, vm.Results.VisibleSegments.Count);
        Assert.Contains(vm.Results.VisibleSegments, s => s.Source == "two.mp3");
    }

    [Fact]
    public async Task One_progress_bar_covers_all_the_files_of_a_run()
    {
        var (vm, _, whisper, _, _) = Create();
        var seen = new List<double>();
        vm.Status.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(RunStatusViewModel.Progress)) lock (seen) seen.Add(vm.Status.Progress);
        };
        whisper.OnProgress = progress => progress?.Report(new TranscriptionProgress("Transcribing", 0.5));
        whisper.Handler = async (_, _) =>
        {
            await Task.Delay(150); // lets the progress report arrive before the file ends
            return [new TranscriptSegment { Start = TimeSpan.Zero, End = TimeSpan.FromSeconds(1), Text = "Hello" }];
        };
        vm.Files.AddFiles([MakeInput("one.mp3"), MakeInput("two.mp3")]);

        await vm.TranscribeCommand.ExecuteAsync(null);

        double[] values;
        lock (seen) values = [.. seen];
        Assert.Contains(0.25, values); // half way through the first of two files
        Assert.Contains(0.75, values); // half way through the second
        Assert.Equal(1, values[^1]);
        Assert.Equal(values.OrderBy(v => v), values); // the bar never goes back during a run
    }

    [Fact]
    public async Task The_progress_bar_stays_for_a_moment_after_a_finished_run_and_goes_at_once_after_a_failure()
    {
        var (vm, _, whisper, _, _) = Create();
        vm.Files.AddFiles([MakeInput()]);

        await vm.TranscribeCommand.ExecuteAsync(null);
        Assert.True(vm.Status.ShowProgress); // the finish can be seen
        Assert.False(vm.IsBusy);

        whisper.Handler = (_, _) => throw new InvalidOperationException("boom");
        vm.Files.AddFiles([MakeInput("again.mp3")]);
        await vm.TranscribeCommand.ExecuteAsync(null);
        await Task.Delay(100);
        Assert.False(vm.Status.ShowProgress);
    }

    [Fact]
    public async Task A_failure_is_reported_and_the_screen_is_usable_again()
    {
        var (vm, _, whisper, _, _) = Create();
        whisper.Handler = (_, _) => throw new InvalidOperationException("boom");
        vm.Files.AddFiles([MakeInput()]);

        await vm.TranscribeCommand.ExecuteAsync(null);

        Assert.Equal(ResultKind.Error, vm.Status.ResultKind);
        Assert.Contains("boom", vm.Status.ResultMessage);
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
        vm.Files.AddFiles([MakeInput()]);

        var run = vm.TranscribeCommand.ExecuteAsync(null);
        await started.Task;
        Assert.True(vm.IsBusy);
        Assert.False(vm.TranscribeCommand.CanExecute(null), "cannot start a second run while busy");

        vm.TranscribeCancelCommand.Execute(null);
        await run;

        Assert.Equal(ResultKind.Info, vm.Status.ResultKind);
        Assert.Equal("Cancelled.", vm.Status.ResultMessage);
        Assert.False(vm.IsBusy);
    }

    [Fact]
    public async Task CopyAll_puts_the_whole_transcript_on_the_clipboard()
    {
        var (vm, _, _, _, clipboard) = Create();
        vm.Files.AddFiles([MakeInput()]);
        await vm.TranscribeCommand.ExecuteAsync(null);

        await vm.Results.CopyAllCommand.ExecuteAsync(null);

        Assert.Equal($"Hello world{Environment.NewLine}Goodbye moon", clipboard.Text);
    }
}
