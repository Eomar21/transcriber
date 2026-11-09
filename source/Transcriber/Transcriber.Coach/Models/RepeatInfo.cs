namespace Transcriber.Coach.Models;

/// <summary>A stumble: a word said again straight away ("I I think") or a stuttered start ("w-what").</summary>
public sealed record RepeatInfo(TimeSpan At, string Text);
