namespace Transcriber.Coach.Models;

/// <summary>One piece of plain-language advice.</summary>
public sealed record Tip(TipKind Kind, string Title, string Detail);
