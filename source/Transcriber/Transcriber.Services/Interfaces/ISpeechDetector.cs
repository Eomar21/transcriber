using Transcriber.Services.Models;

namespace Transcriber.Services.Interfaces;

/// <summary>Finds where people are speaking in a 16 kHz mono wav.</summary>
internal interface ISpeechDetector
{
    /// <summary>True when the detection model is available next to the app.</summary>
    bool IsAvailable { get; }

    Task<List<SpeechSpan>> DetectAsync(string wavPath, CancellationToken ct = default);
}
