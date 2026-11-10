using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using Transcriber.Services.Configuration;
using Transcriber.Services.Exceptions;
using Transcriber.Services.Interfaces;
using Transcriber.Services.Models;

namespace Transcriber.Services.Speakers;

/// <summary>
/// Speaker diarization by running sherpa-onnx's own command-line tool, which ships inside the app.
/// This is what the Mac build uses: a Mac Catalyst app cannot load the native sherpa-onnx library, but it can start a
/// separate, statically linked macOS program, the same way it starts the bundled ffmpeg.
/// </summary>
internal sealed partial class SherpaCliSpeakerDiarizer : ISpeakerDiarizer
{
    private readonly SpeakerSettings _settings;
    private readonly string? _toolPath;
    private readonly string _segmentationPath;
    private readonly string _embeddingPath;

    /// <param name="toolPath">The tool to run; by default the one next to the app.</param>
    /// <param name="modelDirectory">Where the two speaker models are; by default the app's models folder.</param>
    public SherpaCliSpeakerDiarizer(IOptions<SpeakerSettings> speakers, IOptions<ModelSettings> models, IExecutablePermission permission, string? toolPath = null, string? modelDirectory = null)
    {
        _settings = speakers.Value;
        _toolPath = toolPath ?? Locate(permission);
        var dir = modelDirectory ?? models.Value.BundledDirectory ?? Path.Combine(AppContext.BaseDirectory, "models");
        _segmentationPath = Path.Combine(dir, _settings.SegmentationModelFile);
        _embeddingPath = Path.Combine(dir, _settings.EmbeddingModelFile);
    }

    public bool IsAvailable => _toolPath is not null && File.Exists(_toolPath) && File.Exists(_segmentationPath) && File.Exists(_embeddingPath);

    private string? Locate(IExecutablePermission permission)
    {
        var path = Path.Combine(AppContext.BaseDirectory, _settings.ToolFileName);
        if (!File.Exists(path)) return null;
        permission.Ensure(path);
        return path;
    }

    public async Task<List<SpeakerTurn>> DiarizeAsync(string wavPath, int? speakerCount = null, IProgress<double>? progress = null, CancellationToken ct = default)
    {
        if (!IsAvailable) throw new SpeakerDetectionException("The speaker detection tool is not part of this build.");

        using var process = new Process { StartInfo = CreateStartInfo(wavPath, speakerCount) };
        var output = new StringBuilder();
        var gate = new object();
        void Collect(object _, DataReceivedEventArgs e)
        {
            if (e.Data is null) return;
            if (ReadProgress(e.Data) is { } share) progress?.Report(share);
            lock (gate) output.AppendLine(e.Data);
        }
        process.OutputDataReceived += Collect;
        process.ErrorDataReceived += Collect;

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        try
        {
            await process.WaitForExitAsync(ct);
        }
        catch (OperationCanceledException)
        {
            try { process.Kill(entireProcessTree: true); } catch (InvalidOperationException) { }
            throw;
        }

        // Make sure the last lines have been delivered before they are read.
        process.WaitForExit();
        string text;
        lock (gate) text = output.ToString();

        var turns = ParseOutput(text);
        if (process.ExitCode != 0 && turns.Count == 0)
            throw new SpeakerDetectionException($"Speaker detection failed (exit code {process.ExitCode}). {Tail(text)}");
        return turns;
    }

    private ProcessStartInfo CreateStartInfo(string wavPath, int? speakerCount)
    {
        var info = new ProcessStartInfo(_toolPath!)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        foreach (var arg in BuildArguments(_segmentationPath, _embeddingPath, wavPath, speakerCount, Math.Max(1, Environment.ProcessorCount / 2)))
            info.ArgumentList.Add(arg);
        return info;
    }

    /// <summary>The same settings as the in-process diarizer, in the tool's command-line form.</summary>
    public List<string> BuildArguments(string segmentationModel, string embeddingModel, string wavPath, int? speakerCount, int threads) =>
    [
        $"--segmentation.pyannote-model={segmentationModel}",
        $"--segmentation.num-threads={threads.ToString(CultureInfo.InvariantCulture)}",
        $"--embedding.model={embeddingModel}",
        $"--embedding.num-threads={threads.ToString(CultureInfo.InvariantCulture)}",
        $"--clustering.num-clusters={(speakerCount ?? -1).ToString(CultureInfo.InvariantCulture)}",
        $"--clustering.cluster-threshold={_settings.ClusterThreshold.ToString(CultureInfo.InvariantCulture)}",
        $"--min-duration-on={_settings.MinDurationOn.ToString(CultureInfo.InvariantCulture)}",
        $"--min-duration-off={_settings.MinDurationOff.ToString(CultureInfo.InvariantCulture)}",
        wavPath,
    ];

    /// <summary>Reads the tool's lines of the form <c>0.318 -- 6.865 speaker_00</c>; everything else it prints is ignored.</summary>
    public static List<SpeakerTurn> ParseOutput(string text)
    {
        var turns = new List<SpeakerTurn>();
        foreach (Match m in TurnLine().Matches(text))
        {
            var start = double.Parse(m.Groups["start"].Value, CultureInfo.InvariantCulture);
            var end = double.Parse(m.Groups["end"].Value, CultureInfo.InvariantCulture);
            if (end <= start) continue;
            turns.Add(new SpeakerTurn(TimeSpan.FromSeconds(start), TimeSpan.FromSeconds(end), int.Parse(m.Groups["speaker"].Value, CultureInfo.InvariantCulture)));
        }
        return [.. turns.OrderBy(t => t.Start)];
    }

    [GeneratedRegex(@"^\s*(?<start>\d+(?:\.\d+)?)\s+--\s+(?<end>\d+(?:\.\d+)?)\s+speaker_(?<speaker>\d+)", RegexOptions.Multiline)]
    private static partial Regex TurnLine();

    /// <summary>Reads a line of the form <c>progress 97.87%</c> as a share from 0 to 1. Any other line gives null.</summary>
    public static double? ReadProgress(string line)
    {
        var m = ProgressLine().Match(line);
        return m.Success
            ? Math.Clamp(double.Parse(m.Groups["percent"].Value, CultureInfo.InvariantCulture) / 100, 0, 1)
            : null;
    }

    [GeneratedRegex(@"^\s*progress\s+(?<percent>\d+(?:\.\d+)?)%")]
    private static partial Regex ProgressLine();

    private static string Tail(string text)
    {
        var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return string.Join(" ", lines.TakeLast(3));
    }
}
