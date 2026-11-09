namespace Transcriber.Services.Export;

/// <summary>Time formats shared by the transcript formatters.</summary>
internal static class TimeStamp
{
    /// <summary>"hh:mm:ss", and ".mmm" or ",mmm" after it when a separator is given.</summary>
    public static string Format(TimeSpan t, char? millisecondSeparator)
    {
        var whole = $"{(int)t.TotalHours:00}:{t.Minutes:00}:{t.Seconds:00}";
        return millisecondSeparator is { } separator ? $"{whole}{separator}{t.Milliseconds:000}" : whole;
    }
}
