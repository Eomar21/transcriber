using Transcriber.Services.Interfaces;
using Transcriber.Services.Models;

namespace Transcriber.Services.Media;

/// <summary>Reads the pieces of a chunk from a 16-bit PCM wav and puts each piece at its place in the chunk.</summary>
internal sealed class WavSpeechChunkReader : ISpeechChunkReader
{
    public float[] Read(string wavPath, SpeechChunk chunk)
    {
        // One piece needs no assembly.
        if (chunk.Pieces.Count == 1) return WavSamples.Read(wavPath, chunk.Pieces[0].Span);

        var rate = WavSamples.SampleRate(wavPath);
        var samples = new float[(int)Math.Ceiling(chunk.Length.TotalSeconds * rate)];
        foreach (var piece in chunk.Pieces)
        {
            var audio = WavSamples.Read(wavPath, piece.Span);
            var at = Math.Min((int)Math.Round(piece.Offset.TotalSeconds * rate), samples.Length);
            audio.AsSpan(0, Math.Min(audio.Length, samples.Length - at)).CopyTo(samples.AsSpan(at));
        }
        // The samples between the pieces stay zero: that is the silence.
        return samples;
    }
}
