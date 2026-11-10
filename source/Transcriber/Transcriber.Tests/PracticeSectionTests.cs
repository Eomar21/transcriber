using Microsoft.Extensions.DependencyInjection;
using Transcriber.Coach.Extensions;
using Transcriber.Coach.Interfaces;
using Transcriber.Coach.Models;
using Transcriber.Services.Extensions;
using Transcriber.ViewModels.Progress;
using Transcriber.ViewModels;
using Transcriber.ViewModels.Extensions;
using Transcriber.ViewModels.Practice;

/// <summary>Each section of the Practice tab works alone. It knows the shared state and nothing else.</summary>
public class PracticeSectionTests
{
    private sealed class NoPicker : IFilePickerService
    {
        public Task<IReadOnlyList<string>> PickMediaFilesAsync() => Task.FromResult<IReadOnlyList<string>>([]);
    }

    private sealed class NoFeedback : ICoachFeedback
    {
        public Task<IReadOnlyList<string>> ListModelsAsync(CancellationToken ct = default) => Task.FromResult<IReadOnlyList<string>>([]);
        public Task<string> GetFeedbackAsync(string model, string transcript, SpeechReport report, CancellationToken ct = default) => Task.FromResult("");
    }

    private sealed class SilentRecorder : IAudioRecorder
    {
        public bool IsRecording { get; private set; }
        public event EventHandler<RecordingProgress>? Progress;
        public Task StartAsync(string wavPath) { IsRecording = true; return Task.CompletedTask; }
        public void Stop() => IsRecording = false;
    }

    private static string TempFile(string name = "talk.wav")
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + name);
        File.WriteAllText(path, "x");
        return path;
    }

    [Fact]
    public void A_new_file_tells_the_tab_so_it_can_clear_the_old_result()
    {
        var state = new PracticeState();
        var source = new SourceViewModel(new NoPicker(), ServicesFixture.Languages, new MicrophoneViewModel(new SilentRecorder(), state, null, Path.GetTempPath()), state);
        var changes = 0;
        source.SourceChanged += (_, _) => changes++;
        var file = TempFile();
        try
        {
            source.SetFile(file);

            Assert.Equal(1, changes);
            Assert.True(source.HasFile);
            Assert.Equal(file, source.FilePath);
        }
        finally { File.Delete(file); }
    }

    [Fact]
    public void A_recording_that_is_replaced_is_removed_but_a_file_the_person_picked_stays()
    {
        var state = new PracticeState();
        var source = new SourceViewModel(new NoPicker(), ServicesFixture.Languages, new MicrophoneViewModel(new SilentRecorder(), state, null, Path.GetTempPath()), state);
        var first = TempFile("rec1.wav");
        var second = TempFile("rec2.wav");
        var picked = TempFile("mine.wav");
        try
        {
            source.UseRecording(first, "Recording 1");
            source.UseRecording(second, "Recording 2");
            Assert.False(File.Exists(first));

            source.SetFile(picked);
            Assert.False(File.Exists(second));
            source.UseRecording(first, "again");
            Assert.True(File.Exists(picked));
        }
        finally { File.Delete(first); File.Delete(second); File.Delete(picked); }
    }

    [Fact]
    public async Task A_recording_that_is_too_short_is_a_problem_and_not_a_file()
    {
        var state = new PracticeState();
        var microphone = new MicrophoneViewModel(new SilentRecorder(), state, null, Path.GetTempPath());
        string? problem = null;
        var recorded = 0;
        microphone.Problem += (_, message) => problem = message;
        microphone.Recorded += (_, _) => recorded++;

        await microphone.StartRecordingCommand.ExecuteAsync(null);
        Assert.True(state.IsRecording);
        microphone.StopRecordingCommand.Execute(null);

        Assert.Contains("too short", problem);
        Assert.Equal(0, recorded);
        Assert.False(state.IsRecording);
    }

    [Fact]
    public void Other_sections_see_that_a_recording_is_running()
    {
        var state = new PracticeState();
        var status = new PracticeStatusViewModel(state, new ProgressLookPicker(ProgressLooks.All));
        var history = new HistoryViewModel(new NoHistory());

        state.IsRecording = true;

        Assert.True(status.IsRecording);
        Assert.False(status.IsIdle);
        Assert.False(history.HasHistory);
    }

    private sealed class NoHistory : IPracticeHistory
    {
        public IReadOnlyList<PracticeSession> Load() => [];
        public void Add(PracticeSession session) { }
        public void Delete(Guid id) { }
        public void Clear() { }
    }

    [Fact]
    public void The_status_section_says_when_there_is_an_error_to_show()
    {
        var status = new PracticeStatusViewModel(new PracticeState(), new ProgressLookPicker(ProgressLooks.All));
        Assert.False(status.HasError);

        status.ErrorMessage = "Stopped.";

        Assert.True(status.HasError);
    }

    [Fact]
    public void The_container_builds_the_tab_with_one_instance_of_each_section()
    {
        var services = new ServiceCollection()
            .AddTranscriberServices(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".json"))
            .AddToolSpeakerDetection()
            .AddCoach(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".json"))
            .AddPracticeScreen()
            .AddSingleton<IFilePickerService, NoPicker>()
            .AddSingleton<IAudioRecorder, SilentRecorder>();
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });

        var tab = provider.GetRequiredService<PracticeViewModel>();

        Assert.Same(provider.GetRequiredService<SourceViewModel>(), tab.Source);
        Assert.Same(provider.GetRequiredService<MicrophoneViewModel>(), tab.Microphone);
        provider.GetRequiredService<PracticeState>().IsBusy = true;
        Assert.True(tab.IsBusy);
        Assert.True(tab.Source.IsBusy);
        Assert.True(tab.Feedback.IsBusy);
    }
}
