using Transcriber.Coach;
using Transcriber.ViewModels;

/// <summary>The practice tab: pick a recording, get it analysed, track progress, optionally ask Ollama.</summary>
public class PracticeViewModelTests : IDisposable
{
    private readonly string _dir = Directory.CreateTempSubdirectory().FullName;

    public void Dispose() => Directory.Delete(_dir, true);

    // ---- stand-ins ----

    private sealed class Reader(string dir, bool convert = true) : IFileReaderService
    {
        public List<string> Converted { get; } = [];
        public Exception? Fail { get; set; }

        public Task<string> GetAudioFileFromFileAsync(string filePath)
        {
            if (Fail is not null) throw Fail;
            if (!convert) return Task.FromResult(filePath);
            var wav = Path.Combine(dir, $"conv_{Converted.Count}.wav");
            File.WriteAllText(wav, "wav");
            Converted.Add(wav);
            return Task.FromResult(wav);
        }
    }

    private sealed class Whisper : IWhisperService
    {
        public string? LastRuntime => null;
        public TranscriptionOptions? Options { get; private set; }
        public List<TranscriptSegment> Segments { get; set; } = Talk(fillers: 6);
        public bool WaitForCancel { get; set; }

        public async Task<List<TranscriptSegment>> TranscribeAsync(string audioFilePath, TranscriptionOptions? options = null,
            IProgress<TranscriptionProgress>? progress = null, CancellationToken ct = default)
        {
            Options = options;
            progress?.Report(new TranscriptionProgress("Transcribing", 0.5));
            if (WaitForCancel) await Task.Delay(Timeout.Infinite, ct);
            return Segments;
        }
    }

    private sealed class Picker : IFilePickerService
    {
        public IReadOnlyList<string> Files { get; set; } = [];
        public Task<IReadOnlyList<string>> PickMediaFilesAsync() => Task.FromResult(Files);
    }

    private sealed class History : IPracticeHistory
    {
        public List<PracticeSession> Sessions { get; } = [];
        public IReadOnlyList<PracticeSession> Load() => Sessions.OrderBy(s => s.When).ToList();
        public void Add(PracticeSession session) => Sessions.Add(session);
        public void Delete(Guid id) => Sessions.RemoveAll(s => s.Id == id);
        public void Clear() => Sessions.Clear();
    }

    private sealed class Feedback : ICoachFeedback
    {
        public IReadOnlyList<string> Models { get; set; } = [];
        public string Answer { get; set; } = "Good job.";
        public bool Fail { get; set; }
        public (string Model, string Transcript)? Asked { get; private set; }

        public Task<IReadOnlyList<string>> ListModelsAsync(CancellationToken ct = default) => Task.FromResult(Models);

        public Task<string> GetFeedbackAsync(string model, string transcript, SpeechReport report, CancellationToken ct = default)
        {
            Asked = (model, transcript);
            return Fail ? throw new OllamaException("Could not reach Ollama. Is it running?") : Task.FromResult(Answer);
        }
    }

    /// <summary>About 60 seconds of talk at a comfortable pace, with some "um"s.</summary>
    private static List<TranscriptSegment> Talk(int fillers)
    {
        var words = string.Join(' ', Enumerable.Repeat("word", 12));
        var lines = new List<TranscriptSegment>();
        for (var i = 0; i < 12; i++)
        {
            var text = (i < fillers ? "um " : "") + words;
            lines.Add(new TranscriptSegment { Start = TimeSpan.FromSeconds(i * 5), End = TimeSpan.FromSeconds(i * 5 + 4.5), Text = text });
        }
        return lines;
    }

    private sealed class Recorder : IAudioRecorder
    {
        public bool IsRecording { get; private set; }
        public event EventHandler<RecordingProgress>? Progress;
        public string? Path { get; private set; }
        public RecordingException? Fail { get; set; }

        /// <summary>How much audio the fake "records": 16 kHz mono 16-bit is 32,000 bytes a second.</summary>
        public double Seconds { get; set; } = 5;

        public Task StartAsync(string wavPath)
        {
            if (Fail is not null) throw Fail;
            Path = wavPath;
            IsRecording = true;
            return Task.CompletedTask;
        }

        public void Stop()
        {
            if (!IsRecording) return;
            IsRecording = false;
            File.WriteAllBytes(Path!, new byte[44 + (int)(Seconds * 32_000)]);
        }

        public void Tick(TimeSpan elapsed, double level) => Progress?.Invoke(this, new RecordingProgress(elapsed, level));
    }

    private readonly Whisper _whisper = new();
    private readonly Picker _picker = new();
    private readonly History _history = new();
    private readonly Feedback _feedback = new();
    private readonly Recorder _recorder = new();
    private Reader _reader = null!;
    private DateTimeOffset _clock = new(2026, 3, 1, 9, 0, 0, TimeSpan.Zero);

    private PracticeViewModel Create(bool convert = true)
    {
        _reader = new Reader(_dir, convert);
        return new PracticeViewModel(_reader, _whisper, _picker, _history, _feedback, _recorder, () => _clock += TimeSpan.FromDays(1), _dir);
    }

    private string MakeFile(string name = "talk.mp3")
    {
        var path = Path.Combine(_dir, name);
        File.WriteAllText(path, "media");
        return path;
    }

    private static Task Analyze(PracticeViewModel vm) => vm.AnalyzeCommand.ExecuteAsync(null);

    // ---- choosing a recording ----

    [Fact]
    public void Nothing_can_be_analysed_until_a_recording_is_chosen()
    {
        var vm = Create();

        Assert.False(vm.HasFile);
        Assert.False(vm.AnalyzeCommand.CanExecute(null));
    }

    [Fact]
    public async Task Choosing_a_recording_shows_its_name_and_enables_analysis()
    {
        var vm = Create();
        _picker.Files = [MakeFile("my talk.mp3")];

        await vm.ChooseFileCommand.ExecuteAsync(null);

        Assert.Equal("my talk.mp3", vm.FileName);
        Assert.True(vm.AnalyzeCommand.CanExecute(null));
    }

    [Fact]
    public async Task Cancelling_the_file_picker_changes_nothing()
    {
        var vm = Create();
        _picker.Files = [];

        await vm.ChooseFileCommand.ExecuteAsync(null);

        Assert.False(vm.HasFile);
    }

    [Fact]
    public void A_file_that_does_not_exist_is_ignored()
    {
        var vm = Create();

        vm.SetFile(Path.Combine(_dir, "missing.mp3"));

        Assert.False(vm.HasFile);
    }

    [Fact]
    public void Only_languages_with_filler_support_are_offered()
    {
        Assert.Equal(["en", "es", "nl"], Create().Languages.Select(l => l.Code));
    }

    // ---- analysis ----

    [Fact]
    public async Task Analysing_shows_numbers_advice_and_what_was_heard()
    {
        var vm = Create();
        vm.SetFile(MakeFile());

        await Analyze(vm);

        Assert.True(vm.HasResult);
        Assert.Equal(["Pace", "Filler words", "Pauses", "Stumbles", "Length"], vm.Metrics.Select(m => m.Label));
        Assert.Contains("wpm", vm.Metrics[0].Value);
        Assert.NotEmpty(vm.Tips);
        Assert.StartsWith("um word", vm.Transcript);
        Assert.False(vm.IsBusy);
        Assert.Equal("", vm.ErrorMessage);
    }

    [Fact]
    public async Task The_recording_is_transcribed_in_the_chosen_language_with_a_prompt_that_keeps_fillers()
    {
        var vm = Create();
        vm.SetFile(MakeFile());
        vm.SelectedLanguage = vm.Languages.First(l => l.Code == "nl");

        await Analyze(vm);

        Assert.Equal("nl", _whisper.Options!.Language);
        Assert.Equal(VerbatimPrompt.For("nl"), _whisper.Options.Prompt);
        Assert.Contains("eh", _whisper.Options.Prompt!);
    }

    [Fact]
    public async Task Dutch_fillers_are_counted_with_the_dutch_list()
    {
        _whisper.Segments = [new TranscriptSegment { Start = TimeSpan.Zero, End = TimeSpan.FromSeconds(20), Text = string.Join(' ', Enumerable.Repeat("eh dat is gewoon een woord", 6)) }];
        var vm = Create();
        vm.SetFile(MakeFile());
        vm.SelectedLanguage = vm.Languages.First(l => l.Code == "nl");

        await Analyze(vm);

        Assert.Contains("per 100 words", vm.Metrics[1].Hint);

    }

    [Fact]
    public async Task A_converted_copy_is_removed_but_a_file_used_in_place_is_kept()
    {
        var converting = Create();
        converting.SetFile(MakeFile("a.mp3"));
        await Analyze(converting);
        Assert.False(File.Exists(_reader.Converted[0]));

        var inPlace = Create(convert: false);
        var own = MakeFile("own.wav");
        inPlace.SetFile(own);
        await Analyze(inPlace);
        Assert.True(File.Exists(own));
    }

    [Fact]
    public async Task A_failure_is_shown_in_words_and_the_tab_is_usable_again()
    {
        var vm = Create();
        vm.SetFile(MakeFile());
        _reader.Fail = new IOException("disk gone");

        await Analyze(vm);

        Assert.Contains("disk gone", vm.ErrorMessage);
        Assert.False(vm.HasResult);
        Assert.False(vm.IsBusy);
        Assert.True(vm.AnalyzeCommand.CanExecute(null));
    }

    [Fact]
    public async Task Stopping_an_analysis_says_so_and_saves_nothing()
    {
        _whisper.WaitForCancel = true;
        var vm = Create();
        vm.SetFile(MakeFile());

        var run = Analyze(vm);
        await Task.Delay(50);
        vm.AnalyzeCancelCommand.Execute(null);
        await run;

        Assert.Equal("Stopped.", vm.ErrorMessage);
        Assert.False(vm.IsBusy);
        Assert.Empty(_history.Sessions);
    }

    [Fact]
    public async Task Choosing_another_recording_clears_the_previous_result()
    {
        var vm = Create();
        vm.SetFile(MakeFile("one.mp3"));
        await Analyze(vm);

        vm.SetFile(MakeFile("two.mp3"));

        Assert.False(vm.HasResult);
        Assert.Empty(vm.Metrics);
        Assert.Empty(vm.Tips);
        Assert.Equal("", vm.Transcript);
    }

    // ---- history and progress ----

    [Fact]
    public async Task A_finished_run_is_saved_to_the_history()
    {
        var vm = Create();
        vm.SetFile(MakeFile("talk.mp3"));

        await Analyze(vm);

        var saved = Assert.Single(_history.Sessions);
        Assert.Equal("talk.mp3", saved.Title);
        Assert.Equal("en", saved.Language);
        Assert.True(vm.HasHistory);
        Assert.Equal("talk.mp3", Assert.Single(vm.Sessions).Title);
    }

    [Fact]
    public async Task A_run_too_short_to_judge_is_not_saved()
    {
        _whisper.Segments = [new TranscriptSegment { Start = TimeSpan.Zero, End = TimeSpan.FromSeconds(3), Text = "Just a few words" }];
        var vm = Create();
        vm.SetFile(MakeFile());

        await Analyze(vm);

        Assert.True(vm.HasResult);
        Assert.Empty(_history.Sessions);
        Assert.Contains("Too short", vm.Tips[0].Title);
    }

    [Fact]
    public async Task The_second_run_is_compared_with_the_first()
    {
        var vm = Create();
        vm.SetFile(MakeFile());
        _whisper.Segments = Talk(fillers: 12);
        await Analyze(vm);

        _whisper.Segments = Talk(fillers: 1);
        await Analyze(vm);

        Assert.Contains(vm.Tips, t => t.Title == "Fewer fillers than last time");
    }

    [Fact]
    public async Task Only_a_run_in_the_same_language_is_used_for_comparison()
    {
        var vm = Create();
        vm.SetFile(MakeFile());
        _whisper.Segments = Talk(fillers: 12);
        await Analyze(vm);

        vm.SelectedLanguage = vm.Languages.First(l => l.Code == "nl");
        _whisper.Segments = Talk(fillers: 1);
        await Analyze(vm);

        Assert.DoesNotContain(vm.Tips, t => t.Title.Contains("than last time"));
    }

    [Fact]
    public async Task History_lists_the_newest_first_and_can_delete_one_or_all()
    {
        var vm = Create();
        vm.SetFile(MakeFile("first.mp3"));
        await Analyze(vm);
        vm.SetFile(MakeFile("second.mp3"));
        await Analyze(vm);

        Assert.Equal(["second.mp3", "first.mp3"], vm.Sessions.Select(s => s.Title));

        vm.DeleteSessionCommand.Execute(vm.Sessions[0]);
        Assert.Equal(["first.mp3"], vm.Sessions.Select(s => s.Title));

        vm.ClearHistoryCommand.Execute(null);
        Assert.False(vm.HasHistory);
        Assert.Empty(_history.Sessions);
    }

    [Fact]
    public void Saved_sessions_are_shown_when_the_tab_opens()
    {
        _history.Add(new PracticeSession(Guid.NewGuid(), _clock, "old.mp3", "en", new SpeechReport { WordCount = 100, WordsPerMinute = 150 }));

        var vm = Create();

        Assert.True(vm.HasHistory);
        Assert.Contains("150 wpm", vm.Sessions[0].Summary);
    }

    // ---- written feedback ----

    [Fact]
    public async Task Without_ollama_the_feedback_option_stays_hidden()
    {
        var vm = Create();

        await vm.RefreshFeedbackModelsCommand.ExecuteAsync(null);

        Assert.False(vm.CanAskForFeedback);
    }

    [Fact]
    public async Task With_ollama_the_first_model_is_selected_and_feedback_needs_a_result()
    {
        _feedback.Models = ["llama3.2:3b", "qwen2.5:7b"];
        var vm = Create();
        vm.SetFile(MakeFile());

        await vm.RefreshFeedbackModelsCommand.ExecuteAsync(null);

        Assert.True(vm.CanAskForFeedback);
        Assert.Equal("llama3.2:3b", vm.SelectedFeedbackModel);
        Assert.False(vm.GetFeedbackCommand.CanExecute(null));

        await Analyze(vm);
        Assert.True(vm.GetFeedbackCommand.CanExecute(null));
    }

    [Fact]
    public async Task Asking_for_feedback_shows_the_answer()
    {
        _feedback.Models = ["llama3.2:3b"];
        var vm = Create();
        vm.SetFile(MakeFile());
        await vm.RefreshFeedbackModelsCommand.ExecuteAsync(null);
        await Analyze(vm);

        await vm.GetFeedbackCommand.ExecuteAsync(null);

        Assert.Equal("Good job.", vm.FeedbackText);
        Assert.Equal("", vm.FeedbackError);
        Assert.Equal("llama3.2:3b", _feedback.Asked!.Value.Model);
        Assert.StartsWith("um word", _feedback.Asked.Value.Transcript);
        Assert.False(vm.IsGettingFeedback);
    }

    [Fact]
    public async Task A_feedback_failure_is_shown_without_breaking_the_results()
    {
        _feedback.Models = ["llama3.2:3b"];
        _feedback.Fail = true;
        var vm = Create();
        vm.SetFile(MakeFile());
        await vm.RefreshFeedbackModelsCommand.ExecuteAsync(null);
        await Analyze(vm);

        await vm.GetFeedbackCommand.ExecuteAsync(null);

        Assert.Contains("Is it running", vm.FeedbackError);
        Assert.Equal("", vm.FeedbackText);
        Assert.True(vm.HasResult);
        Assert.True(vm.GetFeedbackCommand.CanExecute(null));
    }

    [Fact]
    public async Task Feedback_is_cleared_when_a_new_recording_is_chosen()
    {
        _feedback.Models = ["m"];
        var vm = Create();
        vm.SetFile(MakeFile("one.mp3"));
        await vm.RefreshFeedbackModelsCommand.ExecuteAsync(null);
        await Analyze(vm);
        await vm.GetFeedbackCommand.ExecuteAsync(null);

        vm.SetFile(MakeFile("two.mp3"));

        Assert.Equal("", vm.FeedbackText);
    }

    // ---- microphone ----

    private async Task Record(PracticeViewModel vm, double seconds = 5)
    {
        _recorder.Seconds = seconds;
        await vm.StartRecordingCommand.ExecuteAsync(null);
        vm.StopRecordingCommand.Execute(null);
    }

    [Fact]
    public async Task Recording_starts_into_a_new_wav_and_locks_the_rest_of_the_tab()
    {
        var vm = Create();
        vm.SetFile(MakeFile());

        await vm.StartRecordingCommand.ExecuteAsync(null);

        Assert.True(vm.IsRecording);
        Assert.False(vm.IsIdle);
        Assert.False(vm.AnalyzeCommand.CanExecute(null));
        Assert.False(vm.StartRecordingCommand.CanExecute(null));
        Assert.True(vm.StopRecordingCommand.CanExecute(null));
        Assert.Equal(_dir, Path.GetDirectoryName(_recorder.Path));
        Assert.EndsWith(".wav", _recorder.Path);
        Assert.Equal("Recording 0:00", vm.RecordingText);
    }

    [Fact]
    public async Task Choosing_a_file_is_ignored_while_recording()
    {
        var vm = Create();
        await vm.StartRecordingCommand.ExecuteAsync(null);
        _picker.Files = [MakeFile("other.mp3")];

        await vm.ChooseFileCommand.ExecuteAsync(null);
        vm.SetFile(MakeFile("dropped.mp3"));

        Assert.False(vm.HasFile);
    }

    [Fact]
    public async Task The_clock_and_level_follow_the_recording()
    {
        var vm = Create();
        await vm.StartRecordingCommand.ExecuteAsync(null);

        _recorder.Tick(TimeSpan.FromSeconds(65.4), 0.37);

        Assert.Equal("Recording 1:05", vm.RecordingText);
        Assert.Equal(0.37, vm.RecordingLevel, precision: 3);

        _recorder.Tick(TimeSpan.FromSeconds(66), 3.0);
        Assert.Equal(1, vm.RecordingLevel);
    }

    [Fact]
    public async Task Progress_after_stopping_changes_nothing()
    {
        var vm = Create();
        await Record(vm);

        _recorder.Tick(TimeSpan.FromSeconds(99), 0.9);

        Assert.Equal(0, vm.RecordingLevel);
        Assert.DoesNotContain("1:39", vm.RecordingText);
    }

    [Fact]
    public async Task Stopping_makes_the_recording_the_chosen_file_ready_to_analyse()
    {
        var vm = Create();

        await Record(vm, seconds: 5);

        Assert.False(vm.IsRecording);
        Assert.True(vm.HasFile);
        Assert.StartsWith("Recording ", vm.FileName);
        Assert.True(File.Exists(_recorder.Path));
        Assert.True(vm.AnalyzeCommand.CanExecute(null));
        Assert.True(vm.IsIdle);
    }

    [Fact]
    public async Task A_recording_can_be_analysed_and_is_kept_for_another_go()
    {
        var vm = Create(convert: false);
        await Record(vm);

        await Analyze(vm);

        Assert.True(vm.HasResult);
        Assert.True(File.Exists(_recorder.Path));
        Assert.StartsWith("Recording ", _history.Sessions[0].Title);

        await Analyze(vm); // for example after switching the language
        Assert.True(vm.HasResult);
    }

    [Fact]
    public async Task A_recording_that_is_too_short_is_discarded_with_a_hint()
    {
        var vm = Create();

        await Record(vm, seconds: 0.3);

        Assert.False(vm.HasFile);
        Assert.Contains("too short", vm.ErrorMessage);
        Assert.False(File.Exists(_recorder.Path));
        Assert.True(vm.StartRecordingCommand.CanExecute(null));
    }

    [Fact]
    public async Task No_microphone_is_explained_and_nothing_starts()
    {
        _recorder.Fail = new RecordingException("No microphone was found.");
        var vm = Create();

        await vm.StartRecordingCommand.ExecuteAsync(null);

        Assert.False(vm.IsRecording);
        Assert.Equal("No microphone was found.", vm.ErrorMessage);
        Assert.True(vm.StartRecordingCommand.CanExecute(null));
    }

    [Fact]
    public async Task Starting_again_clears_an_earlier_error()
    {
        var vm = Create();
        await Record(vm, seconds: 0.2);
        Assert.NotEqual("", vm.ErrorMessage);

        await vm.StartRecordingCommand.ExecuteAsync(null);

        Assert.Equal("", vm.ErrorMessage);
    }

    [Fact]
    public async Task A_new_recording_replaces_the_old_one_and_deletes_it()
    {
        var vm = Create();
        await Record(vm);
        var first = _recorder.Path!;

        await Record(vm);

        Assert.False(File.Exists(first));
        Assert.True(File.Exists(_recorder.Path));
        Assert.NotEqual(first, _recorder.Path);
    }

    [Fact]
    public async Task Choosing_another_file_deletes_the_recording()
    {
        var vm = Create();
        await Record(vm);
        var recording = _recorder.Path!;

        vm.SetFile(MakeFile("mine.mp3"));

        Assert.False(File.Exists(recording));
        Assert.Equal("mine.mp3", vm.FileName);
    }

    [Fact]
    public async Task A_recording_never_deletes_a_file_the_user_chose()
    {
        var vm = Create();
        var mine = MakeFile("mine.mp3");
        vm.SetFile(mine);

        await Record(vm);

        Assert.True(File.Exists(mine));
    }

    [Fact]
    public async Task Recording_stops_by_itself_at_the_limit()
    {
        var vm = Create();
        await vm.StartRecordingCommand.ExecuteAsync(null);

        _recorder.Seconds = 5;
        _recorder.Tick(PracticeViewModel.MaxRecording, 0.5);

        Assert.False(vm.IsRecording);
        Assert.False(_recorder.IsRecording);
        Assert.True(vm.HasFile);
        Assert.Contains("30 minutes", vm.ErrorMessage);
    }

    [Fact]
    public async Task Recording_cannot_start_while_analysing()
    {
        _whisper.WaitForCancel = true;
        var vm = Create();
        vm.SetFile(MakeFile());

        var run = Analyze(vm);
        await Task.Delay(50);
        Assert.False(vm.StartRecordingCommand.CanExecute(null));

        vm.AnalyzeCancelCommand.Execute(null);
        await run;
        Assert.True(vm.StartRecordingCommand.CanExecute(null));
    }

    [Fact]
    public void Stopping_when_nothing_is_recording_is_not_offered()
    {
        Assert.False(Create().StopRecordingCommand.CanExecute(null));
    }

    [Fact]
    public void Leftover_recordings_from_earlier_sessions_are_cleaned_up_but_nothing_else()
    {
        var old = Path.Combine(_dir, "transcriber_practice_old.wav");
        var fresh = Path.Combine(_dir, "transcriber_practice_fresh.wav");
        var other = Path.Combine(_dir, "holiday.wav");
        foreach (var f in new[] { old, fresh, other }) File.WriteAllText(f, "x");
        File.SetLastWriteTimeUtc(old, DateTime.UtcNow.AddDays(-3));
        File.SetLastWriteTimeUtc(other, DateTime.UtcNow.AddDays(-30));

        Create();

        Assert.False(File.Exists(old));
        Assert.True(File.Exists(fresh));
        Assert.True(File.Exists(other));
    }

    [Fact]
    public async Task A_microphone_that_stays_silent_is_flagged_after_a_few_seconds()
    {
        var vm = Create();
        await vm.StartRecordingCommand.ExecuteAsync(null);

        _recorder.Tick(TimeSpan.FromSeconds(1), 0);
        Assert.False(vm.MicrophoneSilent); // too early to say

        _recorder.Tick(TimeSpan.FromSeconds(3.5), 0);
        Assert.True(vm.MicrophoneSilent);
    }

    [Fact]
    public async Task Any_sound_at_all_clears_the_silence_warning_for_good()
    {
        var vm = Create();
        await vm.StartRecordingCommand.ExecuteAsync(null);
        _recorder.Tick(TimeSpan.FromSeconds(3.5), 0);
        Assert.True(vm.MicrophoneSilent);

        _recorder.Tick(TimeSpan.FromSeconds(4), 0.02);
        _recorder.Tick(TimeSpan.FromSeconds(5), 0);

        Assert.False(vm.MicrophoneSilent);
    }

    [Fact]
    public async Task A_quiet_but_live_microphone_is_not_flagged()
    {
        var vm = Create();
        await vm.StartRecordingCommand.ExecuteAsync(null);

        _recorder.Tick(TimeSpan.FromSeconds(1), 0.004);
        _recorder.Tick(TimeSpan.FromSeconds(6), 0.002);

        Assert.False(vm.MicrophoneSilent);
    }

    [Fact]
    public async Task The_silence_warning_does_not_carry_over_to_the_next_recording()
    {
        var vm = Create();
        await vm.StartRecordingCommand.ExecuteAsync(null);
        _recorder.Tick(TimeSpan.FromSeconds(4), 0);
        Assert.True(vm.MicrophoneSilent);
        vm.StopRecordingCommand.Execute(null);
        Assert.False(vm.MicrophoneSilent);

        await vm.StartRecordingCommand.ExecuteAsync(null);
        _recorder.Tick(TimeSpan.FromSeconds(1), 0);

        Assert.False(vm.MicrophoneSilent);
    }
}
