/// <summary>A program that the app starts must get paths that it can open. On Windows that means plain ASCII.</summary>
public class ToolFileStagerTests : IDisposable
{
    private readonly string _root = Directory.CreateTempSubdirectory("stager").FullName;

    public void Dispose() => Directory.Delete(_root, true);

    private string Folder(string name) => Directory.CreateDirectory(Path.Combine(_root, name)).FullName;

    private static string FileIn(string folder, string name, string content = "x")
    {
        var path = Path.Combine(folder, name);
        File.WriteAllText(path, content);
        return path;
    }

    [Fact]
    public void Plain_paths_go_to_the_program_as_they_are()
    {
        var staging = Folder("staging");
        var input = FileIn(Folder("in"), "talk.wav");
        var model = FileIn(Folder("models"), "a.onnx");

        using var staged = new AsciiToolFiles(staging).Stage(input, [model]);

        Assert.Equal(input, staged.InputFile);
        Assert.Equal([model], staged.SharedFiles);
        Assert.Null(staged.WorkingDirectory);
        Assert.Empty(Directory.GetFileSystemEntries(staging));
    }

    [Fact]
    public void An_input_with_other_letters_is_copied_and_the_copy_is_removed_afterwards()
    {
        var staging = Folder("staging");
        var input = FileIn(Folder("José"), "مقابلة.wav", "sound");
        var model = FileIn(Folder("models"), "a.onnx");

        string copy;
        using (var staged = new AsciiToolFiles(staging).Stage(input, [model]))
        {
            Assert.Equal(staging, staged.WorkingDirectory);
            Assert.All(staged.InputFile, c => Assert.True(char.IsAscii(c)));
            Assert.EndsWith(".wav", staged.InputFile);
            Assert.False(Path.IsPathRooted(staged.InputFile));
            copy = Path.Combine(staging, staged.InputFile);
            Assert.Equal("sound", File.ReadAllText(copy));
            Assert.Equal([model], staged.SharedFiles);
        }

        Assert.False(File.Exists(copy));
        Assert.True(File.Exists(input));
    }

    [Fact]
    public void An_input_that_is_already_in_the_staging_folder_is_not_copied()
    {
        var staging = Folder("Zoë");
        var input = FileIn(staging, "converted.wav");

        using (var staged = new AsciiToolFiles(staging).Stage(input, []))
        {
            Assert.Equal("converted.wav", staged.InputFile);
            Assert.Equal(staging, staged.WorkingDirectory);
        }

        Assert.True(File.Exists(input));
    }

    [Fact]
    public void Shared_files_with_other_letters_are_copied_once_and_kept_for_the_next_run()
    {
        var staging = Folder("staging");
        var input = FileIn(Folder("in"), "talk.wav");
        var model = FileIn(Folder("Müller"), "a.onnx", "model");
        var stager = new AsciiToolFiles(staging);

        string copy;
        using (var staged = stager.Stage(input, [model]))
        {
            Assert.Equal(input, staged.InputFile);
            Assert.False(Path.IsPathRooted(staged.SharedFiles[0]));
            Assert.All(staged.SharedFiles[0], c => Assert.True(char.IsAscii(c)));
            copy = Path.Combine(staging, staged.SharedFiles[0]);
            Assert.Equal("model", File.ReadAllText(copy));
        }
        Assert.True(File.Exists(copy));

        var written = File.GetLastWriteTimeUtc(copy);
        using (stager.Stage(input, [model])) { }
        Assert.Equal(written, File.GetLastWriteTimeUtc(copy));
    }

    [Fact]
    public void A_shared_file_of_another_size_replaces_the_old_copy()
    {
        var staging = Folder("staging");
        var input = FileIn(Folder("in"), "talk.wav");
        var model = FileIn(Folder("Müller"), "a.onnx", "old");
        var stager = new AsciiToolFiles(staging);
        using (stager.Stage(input, [model])) { }

        File.WriteAllText(model, "a newer model");
        using var staged = stager.Stage(input, [model]);

        Assert.Equal("a newer model", File.ReadAllText(Path.Combine(staging, staged.SharedFiles[0])));
    }

    [Fact]
    public void The_direct_stager_changes_nothing()
    {
        using var staged = new DirectToolFiles().Stage("/music/مقابلة.wav", ["/models/a.onnx"]);

        Assert.Equal("/music/مقابلة.wav", staged.InputFile);
        Assert.Equal(["/models/a.onnx"], staged.SharedFiles);
        Assert.Null(staged.WorkingDirectory);
    }

    [Fact]
    public async Task The_bundled_program_reads_a_recording_and_models_from_folders_with_other_letters()
    {
        if (!OperatingSystem.IsWindows()) return;

        var models = Folder("Modèles");
        var bundled = Path.Combine(AppContext.BaseDirectory, "models");
        foreach (var name in new[] { ServicesFixture.Settings.Speakers.SegmentationModelFile, ServicesFixture.Settings.Speakers.EmbeddingModelFile })
            File.Copy(Path.Combine(bundled, name), Path.Combine(models, name));
        var input = Path.Combine(Folder("مقابلة"), "مقابلة.wav");
        File.Copy(Path.Combine(AppContext.BaseDirectory, "TestData", "two-speakers-en.wav"), input);
        var tool = Path.Combine(AppContext.BaseDirectory, SherpaCliSpeakerDiarizer.ToolFileName(ServicesFixture.Settings.Speakers.ToolFileName));
        var staging = Folder("staging");

        var turns = await ServicesFixture.CliDiarizer(tool, models, new AsciiToolFiles(staging)).DiarizeAsync(input);

        Assert.Equal([0, 1], turns.Select(t => t.Speaker).Distinct().Order());
        Assert.Empty(Directory.GetFiles(staging));
    }
}
