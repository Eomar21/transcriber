using System.Globalization;

namespace Transcriber.Coach.Advice;

/// <summary>Number formats shared by the advice rules, so every tip writes numbers the same way.</summary>
internal static class TipFormat
{
    /// <summary>One decimal place at most, with a dot, whatever the computer's language is.</summary>
    public static string One(double value) => value.ToString("0.#", CultureInfo.InvariantCulture);

    public static int Whole(double value) => (int)Math.Round(value);
}
