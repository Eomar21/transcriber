namespace Transcriber.Services.Interfaces;

/// <summary>Makes sure that a program that is built into the app can be started.</summary>
internal interface IExecutablePermission
{
    /// <summary>App bundles and archives can drop the executable bit. This restores it so the file can start.</summary>
    void Ensure(string path);
}
