using Transcriber.Services.Platform;

namespace Transcriber.Services.Interfaces;

/// <summary>
/// Makes files readable for a program that the app starts. Some programs cannot open every path that the system
/// allows. The stager gives the program a path that it can open, and it hides how.
/// </summary>
internal interface IToolFileStager
{
    /// <param name="inputFile">The file of this run. A copy that the stager makes is removed when the result is disposed.</param>
    /// <param name="sharedFiles">Files that each run uses, for example models. A copy that the stager makes stays for the next run.</param>
    /// <returns>The paths to give to the program, and the folder to start the program in. Dispose it when the program has ended.</returns>
    StagedFiles Stage(string inputFile, IReadOnlyList<string> sharedFiles);
}
