using Transcriber.Services.Models;

namespace Transcriber.Services.Interfaces;

/// <summary>Works out who speaks when in a 16 kHz mono wav.</summary>
internal interface ISpeakerDiarizer
{
    /// <summary>True when the speaker models are available next to the app.</summary>
    bool IsAvailable { get; }

    /// <param name="speakerCount">How many people speak, or null to work it out from the audio.</param>
    Task<List<SpeakerTurn>> DiarizeAsync(string wavPath, int? speakerCount = null, CancellationToken ct = default);
}
