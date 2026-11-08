using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

public enum TranscriptFormat { Txt, Srt, Vtt, Json }

/// <summary>Serialises transcript segments to plain text, SubRip, WebVTT or JSON.</summary>
public static class TranscriptExporter
{
    public static string Extension(TranscriptFormat format) => format switch
    {
        TranscriptFormat.Srt => ".srt",
        TranscriptFormat.Vtt => ".vtt",
        TranscriptFormat.Json => ".json",
        _ => ".txt",
    };

    public static string Render(IReadOnlyList<TranscriptSegment> segments, TranscriptFormat format) => format switch
    {
        TranscriptFormat.Srt => ToSrt(segments),
        TranscriptFormat.Vtt => ToVtt(segments),
        TranscriptFormat.Json => ToJson(segments),
        _ => ToText(segments),
    };

    /// <summary>Writes "{audio name}_transcript.{ext}" next to the source file and returns its path.</summary>
    public static async Task<string> WriteAsync(IReadOnlyList<TranscriptSegment> segments, string sourceFilePath, TranscriptFormat format)
    {
        var directory = Path.GetDirectoryName(sourceFilePath);
        if (string.IsNullOrEmpty(directory)) directory = Directory.GetCurrentDirectory();
        var path = Path.Combine(directory, Path.GetFileNameWithoutExtension(sourceFilePath) + "_transcript" + Extension(format));
        await File.WriteAllTextAsync(path, Render(segments, format), new UTF8Encoding(false));
        return path;
    }

    /// <summary>The line text, led by "Speaker N: " when speakers were identified.</summary>
    private static string Labelled(TranscriptSegment s) =>
        s.Speaker is { } speaker ? SpeakerAssigner.Label(speaker) + ": " + s.Text : s.Text;

    public static string ToText(IReadOnlyList<TranscriptSegment> segments)
    {
        var sb = new StringBuilder();
        foreach (var s in segments)
            sb.Append('[').Append(Stamp(s.Start, ',', false)).Append(" -> ").Append(Stamp(s.End, ',', false)).Append("] ").AppendLine(Labelled(s));
        return sb.ToString();
    }

    public static string ToSrt(IReadOnlyList<TranscriptSegment> segments)
    {
        var sb = new StringBuilder();
        for (var i = 0; i < segments.Count; i++)
        {
            sb.AppendLine((i + 1).ToString(CultureInfo.InvariantCulture));
            sb.Append(Stamp(segments[i].Start, ',')).Append(" --> ").AppendLine(Stamp(segments[i].End, ','));
            sb.AppendLine(Labelled(segments[i])).AppendLine();
        }
        return sb.ToString();
    }

    public static string ToVtt(IReadOnlyList<TranscriptSegment> segments)
    {
        var sb = new StringBuilder("WEBVTT\n\n");
        foreach (var s in segments)
        {
            sb.Append(Stamp(s.Start, '.')).Append(" --> ").AppendLine(Stamp(s.End, '.'));
            sb.AppendLine(s.Speaker is { } speaker ? $"<v {SpeakerAssigner.Label(speaker)}>{s.Text}" : s.Text).AppendLine();
        }
        return sb.ToString();
    }

    public static string ToJson(IReadOnlyList<TranscriptSegment> segments)
    {
        var rows = segments.Select(s => new
        {
            start = Math.Round(s.Start.TotalSeconds, 3),
            end = Math.Round(s.End.TotalSeconds, 3),
            speaker = s.Speaker is { } n ? SpeakerAssigner.Label(n) : null,
            text = s.Text,
        });
        // Relaxed escaping keeps Arabic and accented text readable instead of \uXXXX sequences.
        return JsonSerializer.Serialize(rows, new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        });
    }

    private static string Stamp(TimeSpan t, char msSeparator, bool millis = true)
    {
        var whole = $"{(int)t.TotalHours:00}:{t.Minutes:00}:{t.Seconds:00}";
        return millis ? $"{whole}{msSeparator}{t.Milliseconds:000}" : whole;
    }
}
