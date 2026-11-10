using Transcriber.Services.Interfaces;

namespace Transcriber.Services.Platform;

/// <summary>For systems where a program can open every path (macOS): the program gets the files as they are.</summary>
internal sealed class DirectToolFiles : IToolFileStager
{
    public StagedFiles Stage(string inputFile, IReadOnlyList<string> sharedFiles) => new(inputFile, sharedFiles);
}
