using System.Runtime.InteropServices;
using Microsoft.Extensions.Options;
using Transcriber.Services.Configuration;
using Transcriber.Services.Interfaces;

namespace Transcriber.Services.Media;

/// <summary>Finds the ffmpeg that ships inside the app. The file names to look for are in the app settings.</summary>
internal sealed class FfmpegLocator(IOptions<MediaSettings> options, IExecutablePermission permission) : IFfmpegLocator
{
    private readonly FfmpegSettings _names = options.Value.Ffmpeg;

    public string? Locate(string? baseDirectory = null)
    {
        var dir = baseDirectory ?? AppContext.BaseDirectory;
        foreach (var name in CandidateFileNames())
        {
            var path = Path.Combine(dir, name);
            if (!File.Exists(path)) continue;

            permission.Ensure(path);
            return path;
        }
        return null;
    }

    // Windows ships one program. macOS ships both CPU flavours; the one that matches this machine is tried first,
    // because an Apple Silicon Mac without Rosetta cannot run the Intel one.
    private IEnumerable<string> CandidateFileNames()
    {
        if (OperatingSystem.IsWindows()) return _names.Windows;
        return RuntimeInformation.ProcessArchitecture == Architecture.Arm64 ? _names.MacArm64 : _names.MacIntel;
    }
}
