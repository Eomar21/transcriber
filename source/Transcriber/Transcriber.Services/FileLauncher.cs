using System.ComponentModel;
using System.Diagnostics;

public enum LaunchResult
{
    Opened,
    /// <summary>The file or its folder no longer exists.</summary>
    NotFound,
    /// <summary>No application is registered for this file type.</summary>
    NoHandler,
    Failed,
}

/// <summary>Opens files and folders with the operating system's own tools.</summary>
public interface IFileLauncher
{
    /// <summary>Opens the file in its default application.</summary>
    LaunchResult Open(string path);

    /// <summary>Shows the file selected in Explorer (Windows) or Finder (macOS).</summary>
    LaunchResult Reveal(string path);
}

public class ShellFileLauncher : IFileLauncher
{
    // Windows reports this when a file type has no associated program.
    private const int ErrorNoAssociation = 1155;

    public LaunchResult Open(string path)
    {
        if (!File.Exists(path)) return LaunchResult.NotFound;

        try
        {
            if (OperatingSystem.IsWindows())
            {
                Process.Start(new ProcessStartInfo(path) { UseShellExecute = true })?.Dispose();
                return LaunchResult.Opened;
            }
            return RunMacOpen([path]) == 0 ? LaunchResult.Opened : LaunchResult.NoHandler;
        }
        catch (Win32Exception ex) when (ex.NativeErrorCode == ErrorNoAssociation)
        {
            return LaunchResult.NoHandler;
        }
        catch
        {
            return LaunchResult.Failed;
        }
    }

    public LaunchResult Reveal(string path)
    {
        var directory = Path.GetDirectoryName(path);
        if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory)) return LaunchResult.NotFound;

        try
        {
            if (OperatingSystem.IsWindows())
            {
                // Explorer returns a non-zero exit code even on success, so the code is not checked.
                var psi = new ProcessStartInfo("explorer.exe") { UseShellExecute = false };
                if (File.Exists(path)) psi.ArgumentList.Add($"/select,{path}");
                else psi.ArgumentList.Add(directory);
                Process.Start(psi)?.Dispose();
                return LaunchResult.Opened;
            }

            // -R reveals (selects) the file in Finder; fall back to opening the folder.
            var code = File.Exists(path) ? RunMacOpen(["-R", path]) : RunMacOpen([directory]);
            return code == 0 ? LaunchResult.Opened : LaunchResult.Failed;
        }
        catch
        {
            return LaunchResult.Failed;
        }
    }

    private static int RunMacOpen(IEnumerable<string> args)
    {
        var psi = new ProcessStartInfo("/usr/bin/open") { UseShellExecute = false, CreateNoWindow = true };
        foreach (var a in args) psi.ArgumentList.Add(a);
        using var p = Process.Start(psi)!;
        p.WaitForExit(5000);
        return p.HasExited ? p.ExitCode : 0;
    }
}
