using System.Text;
using Transcriber.Services.Interfaces;
using Transcriber.Services.Models;

namespace Transcriber.Services.Export;

/// <summary>Finds the formatter for a format and saves its text next to the source file.</summary>
internal sealed class TranscriptExporter(IEnumerable<ITranscriptFormatter> formatters) : ITranscriptExporter
{
    private readonly Dictionary<TranscriptFormat, ITranscriptFormatter> _formatters = formatters.ToDictionary(f => f.Format);

    public string Extension(TranscriptFormat format) => FormatterFor(format).Extension;

    public string Render(IReadOnlyList<TranscriptSegment> segments, TranscriptFormat format) => FormatterFor(format).Render(segments);

    public async Task<string> WriteAsync(IReadOnlyList<TranscriptSegment> segments, string sourceFilePath, TranscriptFormat format)
    {
        var directory = Path.GetDirectoryName(sourceFilePath);
        if (string.IsNullOrEmpty(directory)) directory = Directory.GetCurrentDirectory();
        var path = Path.Combine(directory, Path.GetFileNameWithoutExtension(sourceFilePath) + "_transcript" + Extension(format));
        await File.WriteAllTextAsync(path, Render(segments, format), new UTF8Encoding(false));
        return path;
    }

    private ITranscriptFormatter FormatterFor(TranscriptFormat format) =>
        _formatters.TryGetValue(format, out var formatter)
            ? formatter
            : throw new NotSupportedException($"There is no writer for the format '{format}'.");
}
