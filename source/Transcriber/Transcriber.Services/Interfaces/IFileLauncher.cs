using Transcriber.Services.Models;

namespace Transcriber.Services.Interfaces;

/// <summary>Opens files and folders with the operating system's own tools.</summary>
public interface IFileLauncher
{
    /// <summary>Opens the file in its default application.</summary>
    LaunchResult Open(string path);

    /// <summary>Shows the file selected in Explorer (Windows) or Finder (macOS).</summary>
    LaunchResult Reveal(string path);
}
