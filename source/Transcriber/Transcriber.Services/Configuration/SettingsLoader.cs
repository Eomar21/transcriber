using Microsoft.Extensions.Configuration;
using Transcriber.Services.Exceptions;

namespace Transcriber.Services.Configuration;

/// <summary>
/// Reads the settings. The defaults are in appsettings.json, which is built into the app. A person can change a value by
/// putting it in a second file of the same form. Only the values that change need to be in that file.
/// </summary>
internal static class SettingsLoader
{
    private const string EmbeddedFile = "appsettings.json";

    public static string DefaultUserFile => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Transcriber", "appsettings.user.json");

    /// <param name="userFile">A file with changed values, or null to use only the defaults. A file that does not exist is ignored.</param>
    /// <exception cref="SettingsException">A file cannot be read, or a value cannot work.</exception>
    public static TranscriberSettings Load(string? userFile = null)
    {
        var assembly = typeof(SettingsLoader).Assembly;
        var resource = assembly.GetManifestResourceNames().Single(n => n.EndsWith("." + EmbeddedFile, StringComparison.Ordinal));

        try
        {
            var builder = new ConfigurationBuilder();
            builder.AddJsonStream(assembly.GetManifestResourceStream(resource)!);
            if (userFile is not null) builder.AddJsonFile(userFile, optional: true, reloadOnChange: false);

            var settings = builder.Build().Get<TranscriberSettings>() ?? new TranscriberSettings();
            settings.Validate();
            return settings;
        }
        catch (Exception e) when (e is InvalidDataException or FormatException or InvalidOperationException)
        {
            throw new SettingsException($"The settings cannot be read: {e.Message}", e);
        }
    }
}
