using Transcriber.Coach.Models;

namespace Transcriber.ViewModels.Practice;

/// <summary>One headline number in the practice results.</summary>
public sealed record MetricItem(string Label, string Value, string Hint);

/// <summary>One piece of advice, with a symbol that does not rely on colour alone.</summary>
public sealed record TipItem(TipKind Kind, string Title, string Detail)
{
    public string Symbol => Kind switch { TipKind.Good => "✓", TipKind.Improve => "!", _ => "i" };
}

/// <summary>A saved practice run as listed in the history.</summary>
public sealed record SessionItem(Guid Id, string Title, string When, string Summary);
