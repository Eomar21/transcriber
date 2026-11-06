using System.Runtime.InteropServices;

/// <summary>Finds the ffmpeg that ships inside the app, so users never have to install it.</summary>
public static class FfmpegLocator
{
    /// <returns>Full path of the bundled ffmpeg, or null if this build does not contain one.</returns>
    public static string? Locate(string? baseDirectory = null)
    {
        var dir = baseDirectory ?? AppContext.BaseDirectory;
        foreach (var name in CandidateFileNames())
        {
            var path = Path.Combine(dir, name);
            if (!File.Exists(path)) continue;

            EnsureExecutable(path);
            return path;
        }
        return null;
    }

    // Windows ships ffmpeg.exe. macOS ships both CPU flavours; prefer the one matching this machine
    // (Apple Silicon Macs without Rosetta cannot run the Intel one).
    private static IEnumerable<string> CandidateFileNames()
    {
        if (OperatingSystem.IsWindows())
            return ["ffmpeg.exe"];

        return RuntimeInformation.ProcessArchitecture == Architecture.Arm64
            ? ["ffmpeg-arm64", "ffmpeg-x64", "ffmpeg"]
            : ["ffmpeg-x64", "ffmpeg-arm64", "ffmpeg"];
    }

    // App bundles and archives can drop the executable bit; restore it so the file can be started.
    private static void EnsureExecutable(string path)
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
