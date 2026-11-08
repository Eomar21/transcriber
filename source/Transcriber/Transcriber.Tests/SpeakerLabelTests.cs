using System.Text.Json;
using Transcriber.ViewModels;

/// <summary>Speaker labels in the exports and in the transcript list.</summary>
public class SpeakerLabelTests : IDisposable
{
    private readonly string _dir = Directory.CreateTempSubdirectory().FullName;

    public void Dispose() => Directory.Delete(_dir, true);

    private static List<TranscriptSegment> Labelled() =>
    [
        new() { Start = TimeSpan.FromSeconds(0), End = TimeSpan.FromSeconds(2), Text = "Hello there", Speaker = 0 },
        new() { Start = TimeSpan.FromSeconds(2), End = TimeSpan.FromSeconds(4), Text = "Hi", Speaker = 1 },
        new() { Start = TimeSpan.FromSeconds(4), End = TimeSpan.FromSeconds(5), Text = "No label" },
    ];

    [Fact]
    public void Text_export_leads_each_labelled_line_with_the_speaker()
    {
        var text = TranscriptExporter.ToText(Labelled());

        Assert.Contains("] Speaker 1: Hello there", text);
        Assert.Contains("] Speaker 2: Hi", text);
        Assert.Contains("] No label", text);
    }

    [Fact]
    public void Srt_export_leads_each_labelled_line_with_the_speaker()
    {
        Assert.Contains("Speaker 2: Hi", TranscriptExporter.ToSrt(Labelled()));
    }

    [Fact]
    public void Vtt_export_uses_voice_tags()
    {
        var vtt = TranscriptExporter.ToVtt(Labelled());

        Assert.Contains("<v Speaker 1>Hello there", vtt);
        Assert.DoesNotContain("<v >", vtt);
    }

    [Fact]
    public void Json_export_has_a_speaker_field_only_for_labelled_lines()
    {
        var rows = JsonDocument.Parse(TranscriptExporter.ToJson(Labelled())).RootElement;

        Assert.Equal("Speaker 1", rows[0].GetProperty("speaker").GetString());
        Assert.Equal(JsonValueKind.Null, rows[2].GetProperty("speaker").ValueKind);
    }

    [Fact]
    public void Exports_without_speakers_are_unchanged()
    {
        var plain = new List<TranscriptSegment> { new() { Start = TimeSpan.Zero, End = TimeSpan.FromSeconds(1), Text = "Hi" } };

        Assert.Equal("[00:00:00 -> 00:00:01] Hi" + Environment.NewLine, TranscriptExporter.ToText(plain));
    }

    // ---- view model ----

    private sealed class Reader : IFileReaderService
    {
        public Task<string> GetAudioFileFromFileAsync(string filePath) => Task.FromResult(filePath);
    }

    private sealed class Whisper(bool canIdentify) : IWhisperService
    {
        public string? LastRuntime => null;
        public bool CanIdentifySpeakers => canIdentify;
        public TranscriptionOptions? Options { get; private set; }

        public Task<List<TranscriptSegment>> TranscribeAsync(string audioFilePath, TranscriptionOptions? options = null,
            IProgress<TranscriptionProgress>? progress = null, CancellationToken ct = default)
        {
            Options = options;
            return Task.FromResult(new List<TranscriptSegment>
            {
                new() { Start = TimeSpan.Zero, End = TimeSpan.FromSeconds(3), Text = "First", Speaker = options?.IdentifySpeakers == true ? 0 : null },
                new() { Start = TimeSpan.FromSeconds(3), End = TimeSpan.FromSeconds(6), Text = "Second", Speaker = options?.IdentifySpeakers == true ? 1 : null },
            });
        }
    }

    private sealed class Picker : IFilePickerService
    {
        public Task<IReadOnlyList<string>> PickMediaFilesAsync() => Task.FromResult<IReadOnlyList<string>>([]);
    }

    private sealed class Clip : IClipboardService
    {
        public string? Text { get; private set; }
        public Task SetTextAsync(string text) { Text = text; return Task.CompletedTask; }
    }

    private sealed class Launcher : IFileLauncher
    {
        public LaunchResult Open(string path) => LaunchResult.Opened;
        public LaunchResult Reveal(string path) => LaunchResult.Opened;
    }

    private (TranscribeViewModel Vm, Whisper Whisper, Clip Clipboard) Create(bool canIdentify)
    {
        var whisper = new Whisper(canIdentify);
        var clip = new Clip();
        var vm = new TranscribeViewModel(new Reader(), whisper, new Picker(), clip, new Launcher(), new FakeModels(), new FakeDialogs(), new FakePlayer());
        var file = Path.Combine(_dir, "talk.mp3");
        File.WriteAllText(file, "x");
        vm.AddFiles([file]);
        return (vm, whisper, clip);
    }

    [Fact]
    public async Task Turning_it_on_asks_for_speakers_and_labels_the_lines()
    {
        var (vm, whisper, _) = Create(canIdentify: true);
        vm.IdentifySpeakers = true;

        await vm.TranscribeCommand.ExecuteAsync(null);

        Assert.True(whisper.Options!.IdentifySpeakers);
        Assert.Equal(["Speaker 1", "Speaker 2"], vm.VisibleSegments.Select(s => s.SpeakerLabel));
        Assert.All(vm.VisibleSegments, s => Assert.True(s.HasSpeaker));
    }

    [Fact]
    public async Task By_default_no_speakers_are_identified()
    {
        var (vm, whisper, _) = Create(canIdentify: true);

        await vm.TranscribeCommand.ExecuteAsync(null);

        Assert.False(whisper.Options!.IdentifySpeakers);
        Assert.All(vm.VisibleSegments, s => Assert.False(s.HasSpeaker));
    }

    [Fact]
    public async Task A_chosen_speaker_count_is_passed_on_and_automatic_means_unknown()
    {
        var (vm, whisper, _) = Create(canIdentify: true);
        vm.IdentifySpeakers = true;

        await vm.TranscribeCommand.ExecuteAsync(null);
        Assert.Null(whisper.Options!.SpeakerCount);

        vm.SelectedSpeakerCount = vm.SpeakerCounts.First(c => c.Count == 3);
        await vm.TranscribeCommand.ExecuteAsync(null);
        Assert.Equal(3, whisper.Options!.SpeakerCount);
    }

    [Fact]
    public async Task Where_speakers_cannot_be_identified_the_option_is_hidden_and_never_requested()
    {
        var (vm, whisper, _) = Create(canIdentify: false);
        vm.IdentifySpeakers = true;

        await vm.TranscribeCommand.ExecuteAsync(null);

        Assert.False(vm.CanIdentifySpeakers);
        Assert.False(whisper.Options!.IdentifySpeakers);
    }

    [Fact]
    public async Task Copy_all_includes_the_speaker_names()
    {
        var (vm, _, clip) = Create(canIdentify: true);
        vm.IdentifySpeakers = true;
        await vm.TranscribeCommand.ExecuteAsync(null);

        await vm.CopyAllCommand.ExecuteAsync(null);

        Assert.Equal("Speaker 1: First" + Environment.NewLine + "Speaker 2: Second", clip.Text);
    }

    [Fact]
    public async Task Searching_still_matches_the_spoken_text_only()
    {
        var (vm, _, _) = Create(canIdentify: true);
        vm.IdentifySpeakers = true;
        await vm.TranscribeCommand.ExecuteAsync(null);

        vm.SearchText = "Speaker";

        Assert.Empty(vm.VisibleSegments);
    }
}
