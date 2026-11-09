using System.Text;
using Transcriber.Services.Interfaces;
using Transcriber.Services.Models;
using Transcriber.Services.Speakers;

namespace Transcriber.Services.Export;

/// <summary>WebVTT subtitles. A speaker is written as the standard voice tag.</summary>
internal sealed class VttFormatter : ITranscriptFormatter
{
    public TranscriptFormat Format => TranscriptFormat.Vtt;

    public string Extension => ".vtt";

    public string Render(IReadOnlyList<TranscriptSegment> segments)
    {
        var sb = new StringBuilder("WEBVTT\n\n");
        foreach (var s in segments)
        {
            sb.Append(TimeStamp.Format(s.Start, '.')).Append(" --> ").AppendLine(TimeStamp.Format(s.End, '.'));
            sb.AppendLine(s.Speaker is { } speaker ? $"<v {SpeakerLabel.Format(speaker)}>{s.Text}" : s.Text).AppendLine();
        }
        return sb.ToString();
    }
}
