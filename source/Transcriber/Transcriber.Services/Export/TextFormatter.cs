using System.Text;
using Transcriber.Services.Interfaces;
using Transcriber.Services.Models;

namespace Transcriber.Services.Export;

/// <summary>Plain text: one line for each segment, with its start and end time.</summary>
internal sealed class TextFormatter : ITranscriptFormatter
{
    public TranscriptFormat Format => TranscriptFormat.Txt;

    public string Extension => ".txt";

    public string Render(IReadOnlyList<TranscriptSegment> segments)
    {
        var sb = new StringBuilder();
        foreach (var s in segments)
            sb.Append('[').Append(TimeStamp.Format(s.Start, null)).Append(" -> ").Append(TimeStamp.Format(s.End, null)).Append("] ")
              .AppendLine(SegmentText.Labelled(s));
        return sb.ToString();
    }
}
