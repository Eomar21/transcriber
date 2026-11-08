namespace Transcriber.Coach;

/// <summary>
/// Whisper is trained to tidy speech, so by default it drops "um", "uh" and stuttered words, which are exactly what a
/// speaking coach needs to see. Starting the decoder with a sample sentence full of fillers makes it keep them.
/// </summary>
public static class VerbatimPrompt
{
    /// <returns>A prompt for the language, or null where no prompt is available.</returns>
    public static string? For(string? language) => (language ?? "").ToLowerInvariant() switch
    {
        "en" => "Umm, let me think like, hmm... Okay, here's what I'm, like, thinking. I I think that, uh, it's w-working.",
        "nl" => "Eh, ehm, nou, laat me denken, zeg maar, eh... Oké, dit is wat ik, uhm, denk. Ik ik denk dat het, eh, werkt.",
        "es" => "Eh, pues, o sea, déjame pensar, este... Bueno, esto es lo que, eh, pienso. Yo yo creo que, em, funciona.",
        _ => null,
    };
}
