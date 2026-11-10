namespace Transcriber.Services.Platform;

/// <summary>The paths that a program gets for one run. Dispose it when the program has ended, so temporary copies are removed.</summary>
internal sealed class StagedFiles : IDisposable
{
    private readonly Action? _cleanUp;

    public StagedFiles(string inputFile, IReadOnlyList<string> sharedFiles, string? workingDirectory = null, Action? cleanUp = null)
    {
        InputFile = inputFile;
        SharedFiles = sharedFiles;
        WorkingDirectory = workingDirectory;
        _cleanUp = cleanUp;
    }

    /// <summary>The path of the input file, as the program must get it.</summary>
    public string InputFile { get; }

    /// <summary>The paths of the shared files, in the order in which they were given.</summary>
    public IReadOnlyList<string> SharedFiles { get; }

    /// <summary>The folder to start the program in, or null when any folder is good. Relative paths start from this folder.</summary>
    public string? WorkingDirectory { get; }

    public void Dispose() => _cleanUp?.Invoke();
}
