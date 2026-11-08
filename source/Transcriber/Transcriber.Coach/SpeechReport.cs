namespace Transcriber.Coach;

/// <summary>A silence between two lines of speech.</summary>
public sealed record PauseInfo(TimeSpan Start, TimeSpan Length);

public sealed record FillerCount(string Filler, int Count);

/// <summary>A stumble: a word said again straight away ("I I think") or a stuttered start ("w-what").</summary>
public sealed record RepeatInfo(TimeSpan At, string Text);

/// <summary>What the analysis found in one recording.</summary>
public sealed record SpeechReport
{
    public int WordCount { get; init; }

    /// <summary>Time from the first word to the last.</summary>
    public TimeSpan Duration { get; init; }

    /// <summary>Time actually spent talking (pauses between lines excluded).</summary>
    public TimeSpan SpeakingTime { get; init; }

    /// <summary>Words per minute of speaking time, so pauses do not make a fast speaker look slow.</summary>
    public double WordsPerMinute { get; init; }

    /// <summary>Pauses that are long enough to notice.</summary>
    public IReadOnlyList<PauseInfo> Pauses { get; init; } = [];

    public TimeSpan LongestPause => Pauses.Count == 0 ? TimeSpan.Zero : Pauses.Max(p => p.Length);

    public IReadOnlyList<FillerCount> Fillers { get; init; } = [];

    public int FillerTotal => Fillers.Sum(f => f.Count);

    public double FillersPer100Words => WordCount == 0 ? 0 : FillerTotal * 100.0 / WordCount;

    public IReadOnlyList<RepeatInfo> Repeats { get; init; } = [];

    public double PausesPerMinute => Duration.TotalMinutes <= 0 ? 0 : Pauses.Count / Duration.TotalMinutes;
}
