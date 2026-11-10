using Transcriber.Services.Models;

namespace Transcriber.Services.Interfaces;

/// <summary>Decides which stretches of speech go to Whisper together in one call.</summary>
internal interface ISpeechPacker
{
    /// <param name="spans">The stretches of speech, in the order of the recording.</param>
    /// <returns>The chunks, in the order of the recording. Each stretch is in one chunk.</returns>
    List<SpeechChunk> Pack(IReadOnlyList<SpeechSpan> spans);
}
