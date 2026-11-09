using System.Globalization;
using System.Text;
using Transcriber.Services.Interfaces;
using Transcriber.Services.Models;

namespace Transcriber.Services.Export;

/// <summary>SubRip subtitles.</summary>
internal sealed class SrtFormatter : ITranscriptFormatter
{
    public TranscriptFormat Format => TranscriptFormat.Srt;

    public string Extension => ".srt";

    public string Render(IReadOnlyList<TranscriptSegment> segments)
    {
        var sb = new StringBuilder();
        for (var i = 0; i < segments.Count; i++)
        {
            sb.AppendLine((i + 1).ToString(CultureInfo.InvariantCulture));
            sb.Append(TimeStamp.Format(segments[i].Start, ',')).Append(" --> ").AppendLine(TimeStamp.Format(segments[i].End, ','));
            sb.AppendLine(SegmentText.Labelled(segments[i])).AppendLine();
        }
        return sb.ToString();
    }
}
