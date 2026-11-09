using Transcriber.Services.Interfaces;

namespace Transcriber.Services.Platform;

/// <summary>On macOS the executable bit of a file can be lost. Windows has no such bit, so nothing happens there.</summary>
internal sealed class ExecutablePermission : IExecutablePermission
{
    public void Ensure(string path)
    {
        if (OperatingSystem.IsWindows()) return;
        try
        {
            var mode = File.GetUnixFileMode(path);
            const UnixFileMode exec = UnixFileMode.UserExecute | UnixFileMode.GroupExecute | UnixFileMode.OtherExecute;
            if ((mode & exec) != exec) File.SetUnixFileMode(path, mode | exec);
        }
        catch
        {
            // If it cannot be changed, starting the process will report a clear error.
        }
    }
}
