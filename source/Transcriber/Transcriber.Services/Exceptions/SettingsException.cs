namespace Transcriber.Services.Exceptions;

/// <summary>The app settings are missing a value or hold a value that cannot work. The message names the setting.</summary>
public sealed class SettingsException : Exception
{
    public SettingsException(string message) : base(message) { }

    public SettingsException(string message, Exception inner) : base(message, inner) { }
}
