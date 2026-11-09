namespace Transcriber.Coach.Interfaces;

/// <summary>Splits text into lower-case words without punctuation.</summary>
public interface ITextTokenizer
{
    IReadOnlyList<string> Tokenize(string text);
}
