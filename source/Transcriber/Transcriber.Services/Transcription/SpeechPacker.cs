using Microsoft.Extensions.Options;
using Transcriber.Services.Configuration;
using Transcriber.Services.Interfaces;
using Transcriber.Services.Models;

namespace Transcriber.Services.Transcription;

/// <summary>
/// Whisper always works on a window of 30 seconds. A short stretch of speech costs as much as a full window.
/// This packer puts short stretches that follow each other into one chunk, as long as the chunk stays in one window.
/// A conversation with many short remarks then needs far fewer windows. A stretch that is longer than the limit
/// stays alone. The limit and the silence between the stretches are in the app settings.
/// </summary>
internal sealed class SpeechPacker(IOptions<SpeechPlanningSettings> options) : ISpeechPacker
{
    // Whisper gives times in steps, and its end of a line is the start of the next speech. Twice the padding covers
    // the padding of the stretch and that step.
    private const double EdgeForEachPadding = 2;

    private readonly TimeSpan _limit = TimeSpan.FromSeconds(options.Value.PackSeconds);
    private readonly TimeSpan _joinSilence = TimeSpan.FromSeconds(options.Value.JoinSilenceSeconds);
    private readonly TimeSpan _edge = TimeSpan.FromSeconds(options.Value.PaddingSeconds * EdgeForEachPadding);

    public List<SpeechChunk> Pack(IReadOnlyList<SpeechSpan> spans)
    {
        var chunks = new List<SpeechChunk>();
        var group = new List<SpeechSpan>();
        var length = TimeSpan.Zero;

        foreach (var span in spans)
        {
            var joined = length + _joinSilence + span.Length;
            if (group.Count > 0 && joined > _limit)
            {
                chunks.Add(SpeechChunk.Of(group, _joinSilence, _edge));
                group = [];
            }
            length = group.Count == 0 ? span.Length : joined;
            group.Add(span);
        }

        if (group.Count > 0) chunks.Add(SpeechChunk.Of(group, _joinSilence, _edge));
        return chunks;
    }
}
