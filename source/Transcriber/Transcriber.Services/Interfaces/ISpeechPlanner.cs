using Transcriber.Services.Models;

namespace Transcriber.Services.Interfaces;

/// <summary>Turns the speech regions that voice detection found into the pieces that are sent to Whisper.</summary>
internal interface ISpeechPlanner
{
    /// <param name="detected">The regions of speech.</param>
    /// <param name="total">The length of the recording.</param>
    /// <param name="padding">Overrides the padding from the settings.</param>
    /// <param name="mergeGap">Overrides the merge gap from the settings.</param>
    /// <param name="maxChunk">Overrides the longest piece from the settings.</param>
    List<SpeechSpan> Plan(IEnumerable<SpeechSpan> detected, TimeSpan total, TimeSpan? padding = null, TimeSpan? mergeGap = null, TimeSpan? maxChunk = null);
}
