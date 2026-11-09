using Transcriber.Coach.Models;

namespace Transcriber.Coach.Interfaces;

/// <summary>Writes the instructions and numbers that are sent to a feedback model.</summary>
public interface IFeedbackPromptBuilder
{
    string Build(string transcript, SpeechReport report);
}
