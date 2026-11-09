using Microsoft.Extensions.DependencyInjection;
using Transcriber.ViewModels.Extensions;
using Transcriber.ViewModels.Progress;
using Transcriber.ViewModels.Transcribe;

/// <summary>Each run gets a random look for the progress bar. New looks are added to the catalog without other changes.</summary>
public class ProgressLookTests
{
    private static ProgressLook Look(string id) => new(id, id + ".png", 32, 8, 10, "#000000", "#FFFFFF");

    [Fact]
    public void The_picker_never_gives_the_same_look_twice_in_a_row()
    {
        var picker = new ProgressLookPicker([Look("a"), Look("b"), Look("c")], new Random(1));

        var previous = picker.Pick();
        for (var i = 0; i < 500; i++)
        {
            var next = picker.Pick();
            Assert.NotEqual(previous.Id, next.Id);
            previous = next;
        }
    }

    [Fact]
    public void The_picker_uses_every_look()
    {
        var picker = new ProgressLookPicker([Look("a"), Look("b"), Look("c"), Look("d")], new Random(2));

        var seen = Enumerable.Range(0, 200).Select(_ => picker.Pick().Id).ToHashSet();

        Assert.Equal(4, seen.Count);
    }

    [Fact]
    public void With_one_look_the_picker_repeats_it()
    {
        var picker = new ProgressLookPicker([Look("only")]);

        Assert.Equal("only", picker.Pick().Id);
        Assert.Equal("only", picker.Pick().Id);
    }

    [Fact]
    public void The_picker_needs_at_least_one_look() =>
        Assert.Throws<ArgumentException>(() => new ProgressLookPicker([]));

    [Fact]
    public void Each_run_starts_with_a_new_look()
    {
        var status = new RunStatusViewModel(new NoSuggestion(), new ProgressLookPicker([Look("a"), Look("b")], new Random(3)), new RunState());

        var seen = new List<string>();
        for (var i = 0; i < 6; i++)
        {
            status.BeginProgress();
            seen.Add(status.Look.Id);
        }

        Assert.All(seen.Zip(seen.Skip(1)), pair => Assert.NotEqual(pair.First, pair.Second));
    }

    [Fact]
    public void The_app_container_provides_the_picker_and_the_screen()
    {
        var services = new ServiceCollection();
        services.AddTranscribeScreen();

        Assert.Contains(services, d => d.ServiceType == typeof(IProgressLookPicker));
    }

    [Fact]
    public void Every_look_has_a_unique_id_valid_colours_and_a_sprite_of_the_right_size()
    {
        Assert.Equal(ProgressLooks.All.Count, ProgressLooks.All.Select(l => l.Id).Distinct().Count());
        Assert.Contains(ProgressLooks.Runner, ProgressLooks.All);

        var raw = FindRawFolder();
        foreach (var look in ProgressLooks.All)
        {
            Assert.Matches("^#[0-9A-Fa-f]{6}$", look.Fill);
            Assert.Matches("^#[0-9A-Fa-f]{6}$", look.FillDark);
            Assert.True(look.Cell > 0 && look.Frames > 0 && look.FramesPerSecond > 0, look.Id);

            var (width, height) = PngSize(Path.Combine(raw, look.Sprite));
            Assert.Equal(look.Cell * look.Frames, width);
            Assert.Equal(look.Cell, height);
        }
    }

    private sealed class NoSuggestion : IModelSuggestion
    {
        public string UseSuggestedModelText => string.Empty;
        public CommunityToolkit.Mvvm.Input.IRelayCommand UseSuggestedModelCommand { get; } = new CommunityToolkit.Mvvm.Input.RelayCommand(() => { });
    }

    private static string FindRawFolder()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            var candidate = Path.Combine(dir.FullName, "Transcriber.Maui", "Resources", "Raw");
            if (Directory.Exists(candidate)) return candidate;
        }
        throw new DirectoryNotFoundException("Resources/Raw not found above " + AppContext.BaseDirectory);
    }

    /// <summary>Reads the size from the PNG header: 8 bytes of signature, then the IHDR chunk with the width and the height.</summary>
    private static (int Width, int Height) PngSize(string path)
    {
        var bytes = new byte[24];
        using (var stream = File.OpenRead(path)) stream.ReadExactly(bytes);
        static int Be(byte[] b, int o) => (b[o] << 24) | (b[o + 1] << 16) | (b[o + 2] << 8) | b[o + 3];
        return (Be(bytes, 16), Be(bytes, 20));
    }
}
