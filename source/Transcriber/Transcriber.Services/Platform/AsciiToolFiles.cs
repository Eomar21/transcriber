using Transcriber.Services.Interfaces;

namespace Transcriber.Services.Platform;

/// <summary>
/// For Windows, where a command-line program often cannot open a path that has letters outside ASCII (for example a
/// user folder with an accent, or a recording with an Arabic name). A path that is plain ASCII goes to the program as
/// it is. If a path is not, the program starts in the staging folder and gets short relative names: a relative name
/// has no such letters, and the system resolves the folder itself.
/// </summary>
internal sealed class AsciiToolFiles : IToolFileStager
{
    private const string SharedFolder = "transcriber-tool-files";
    private const string InputPrefix = "transcriber_tool_";

    private readonly string _stagingDirectory;

    /// <param name="stagingDirectory">Where copies go. Defaults to the system temporary folder.</param>
    public AsciiToolFiles(string? stagingDirectory = null) =>
        _stagingDirectory = Path.GetFullPath(stagingDirectory ?? Path.GetTempPath());

    public StagedFiles Stage(string inputFile, IReadOnlyList<string> sharedFiles)
    {
        if (IsAscii(inputFile) && sharedFiles.All(IsAscii)) return new StagedFiles(inputFile, sharedFiles);

        Directory.CreateDirectory(_stagingDirectory);
        var (input, temporaryCopy) = StageInput(inputFile);
        try
        {
            var shared = sharedFiles.Select(StageShared).ToList();
            return new StagedFiles(input, shared, _stagingDirectory, () => DeleteQuietly(temporaryCopy));
        }
        catch
        {
            DeleteQuietly(temporaryCopy);
            throw;
        }
    }

    private (string Path, string? TemporaryCopy) StageInput(string inputFile)
    {
        if (IsAscii(inputFile)) return (inputFile, null);

        // A file that is already in the staging folder (a converted recording) needs no copy: its name is enough.
        var name = Path.GetFileName(inputFile);
        if (IsAscii(name) && IsInStagingDirectory(inputFile)) return (name, null);

        var copyName = InputPrefix + Guid.NewGuid().ToString("N") + AsciiExtension(inputFile);
        var copy = Path.Combine(_stagingDirectory, copyName);
        File.Copy(inputFile, copy, overwrite: true);
        return (copyName, copy);
    }

    private string StageShared(string file, int index)
    {
        if (IsAscii(file)) return file;

        var name = Path.GetFileName(file);
        if (!IsAscii(name)) name = $"file{index}{AsciiExtension(file)}";
        var folder = Path.Combine(_stagingDirectory, SharedFolder);
        var copy = Path.Combine(folder, name);

        // The copy of an earlier run is used again. A file of another size is another version, so it is replaced.
        if (!File.Exists(copy) || new FileInfo(copy).Length != new FileInfo(file).Length)
        {
            Directory.CreateDirectory(folder);
            File.Copy(file, copy, overwrite: true);
        }
        return Path.Combine(SharedFolder, name);
    }

    private bool IsInStagingDirectory(string file) =>
        string.Equals(
            Path.TrimEndingDirectorySeparator(Path.GetDirectoryName(Path.GetFullPath(file)) ?? string.Empty),
            Path.TrimEndingDirectorySeparator(_stagingDirectory),
            StringComparison.OrdinalIgnoreCase);

    private static string AsciiExtension(string file)
    {
        var extension = Path.GetExtension(file);
        return IsAscii(extension) ? extension : string.Empty;
    }

    private static bool IsAscii(string text) => text.All(char.IsAscii);

    private static void DeleteQuietly(string? path)
    {
        if (path is null) return;
        try { File.Delete(path); } catch (Exception e) when (e is IOException or UnauthorizedAccessException) { }
    }
}
