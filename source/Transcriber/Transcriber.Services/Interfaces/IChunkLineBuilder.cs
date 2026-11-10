using Transcriber.Services.Models;

namespace Transcriber.Services.Interfaces;

/// <summary>Changes a line that Whisper made for a chunk into lines of the transcript, with times in the recording.</summary>
internal interface IChunkLineBuilder
{
    /// <param name="chunk">The chunk that Whisper got.</param>
    /// <param name="line">The line, with times in the chunk.</param>
    /// <param name="parts">The small parts of the text of the line in sequence, each with its times in the chunk. Can be empty.</param>
    /// <returns>One line, or one line for each stretch of speech that the line covers.</returns>
    IReadOnlyList<TranscriptSegment> Build(SpeechChunk chunk, TimedText line, IReadOnlyList<TimedText> parts);
}
