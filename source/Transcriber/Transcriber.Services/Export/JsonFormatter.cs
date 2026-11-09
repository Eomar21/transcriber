using System.Text.Encodings.Web;
using System.Text.Json;
using Transcriber.Services.Interfaces;
using Transcriber.Services.Models;
using Transcriber.Services.Speakers;

namespace Transcriber.Services.Export;

/// <summary>A list of segments with times in seconds, for programs and scripts.</summary>
internal sealed class JsonFormatter : ITranscriptFormatter
{
    // Relaxed escaping keeps Arabic and accented text readable instead of \uXXXX sequences.
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public TranscriptFormat Format => TranscriptFormat.Json;

    public string Extension => ".json";

    public string Render(IReadOnlyList<TranscriptSegment> segments)
    {
        var rows = segments.Select(s => new
        {
            start = Math.Round(s.Start.TotalSeconds, 3),
            end = Math.Round(s.End.TotalSeconds, 3),
            speaker = s.Speaker is { } n ? SpeakerLabel.Format(n) : null,
            text = s.Text,
        });
        return JsonSerializer.Serialize(rows, Options);
    }
}
