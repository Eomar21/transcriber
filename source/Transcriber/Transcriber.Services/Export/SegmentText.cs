using Transcriber.Services.Models;
using Transcriber.Services.Speakers;

namespace Transcriber.Services.Export;

internal static class SegmentText
{
    /// <summary>The line text, led by "Speaker N: " when speakers were identified.</summary>
    public static string Labelled(TranscriptSegment s) =>
        s.Speaker is { } speaker ? SpeakerLabel.Format(speaker) + ": " + s.Text : s.Text;
}
