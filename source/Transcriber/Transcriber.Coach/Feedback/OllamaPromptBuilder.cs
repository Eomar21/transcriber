using System.Globalization;
using System.Text;
using Transcriber.Coach.Interfaces;
using Transcriber.Coach.Models;

namespace Transcriber.Coach.Feedback;

/// <summary>Writes the instructions and the measurements that are sent to the model. Kept apart from the web call so it can be checked and tuned alone.</summary>
public sealed class OllamaPromptBuilder : IFeedbackPromptBuilder
{
    public string Build(string transcript, SpeechReport report)
    {
        var inv = CultureInfo.InvariantCulture;
        var sb = new StringBuilder();
        sb.AppendLine("You are a friendly, honest speaking coach. Read the transcript of a practice recording and the measurements below.");
        sb.AppendLine("Give feedback in the same language as the transcript: two things done well, up to three things to work on, and one exercise for the next attempt.");
        sb.AppendLine("Be specific, reference the speaker's actual words, keep it under 200 words, and do not repeat the numbers back.");
        sb.AppendLine();
        sb.AppendLine("Measurements:");
        sb.AppendLine(inv, $"- Pace: {report.WordsPerMinute:0} words per minute");
        sb.AppendLine(inv, $"- Filler words: {report.FillerTotal} ({report.FillersPer100Words:0.#} per 100 words){(report.Fillers.Count > 0 ? ", mostly " + string.Join(", ", report.Fillers.Take(3).Select(f => $"\"{f.Filler}\" x{f.Count}")) : "")}");
        sb.AppendLine(inv, $"- Pauses of 0.8 seconds or more: {report.Pauses.Count}, longest {report.LongestPause.TotalSeconds:0.#} seconds");
        sb.AppendLine(inv, $"- Repeated words or stumbled starts: {report.Repeats.Count}");
        sb.AppendLine();
        sb.AppendLine("Transcript:");
        sb.AppendLine(transcript);
        return sb.ToString();
    }
}
