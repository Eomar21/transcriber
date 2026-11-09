using Transcriber.Coach.Models;

namespace Transcriber.Coach.Interfaces;

/// <summary>Plain-language advice from a report. Rules only, so it works offline and gives the same answer every time.</summary>
public interface ICoachAdvisor
{
    /// <summary>Below this many words there is too little speech to say anything fair.</summary>
    int MinimumWords { get; }

    IReadOnlyList<Tip> Advise(SpeechReport report, SpeechReport? previous = null);
}
