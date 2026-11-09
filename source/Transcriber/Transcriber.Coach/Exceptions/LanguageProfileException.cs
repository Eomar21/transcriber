namespace Transcriber.Coach.Exceptions;

/// <summary>A language profile file is damaged, or the required profiles are missing. This is a defect in the app, not in the user's input.</summary>
public sealed class LanguageProfileException : Exception
{
    public LanguageProfileException(string message) : base(message) { }

    public LanguageProfileException(string message, Exception inner) : base(message, inner) { }
}
