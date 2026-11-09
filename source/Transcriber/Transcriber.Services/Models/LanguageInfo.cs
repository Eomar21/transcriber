namespace Transcriber.Services.Models;

/// <param name="ConfidentFrom">The smallest model tier for which the language is offered.</param>
/// <param name="AdvisedUntil">
/// From <paramref name="ConfidentFrom"/> up to this tier (exclusive) the language is offered with a hint to use a bigger model.
/// </param>
public sealed record LanguageInfo(string Code, string Name, ModelTier ConfidentFrom, ModelTier? AdvisedUntil = null);
