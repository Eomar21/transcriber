using Transcriber.Services.Models;

namespace Transcriber.Services.Interfaces;

/// <summary>Makes the audio of a chunk from the recording.</summary>
internal interface ISpeechChunkReader
{
    /// <returns>The samples of the chunk: each piece at its place, and silence between the pieces.</returns>
    float[] Read(string wavPath, SpeechChunk chunk);
}
