namespace Transcriber.ViewModels;

/// <summary>Lets the user pick media files. Implemented per platform by the app, faked in tests.</summary>
public interface IFilePickerService
{
    /// <returns>Full paths of the chosen files, or an empty list if the user cancelled.</returns>
    Task<IReadOnlyList<string>> PickMediaFilesAsync();
}

/// <summary>Clipboard access, abstracted so view models never touch platform APIs.</summary>
public interface IClipboardService
{
    Task SetTextAsync(string text);
}

/// <summary>One row of the transcript list.</summary>
public sealed record SegmentItem(TimeSpan Start, TimeSpan End, string Text, string Source = "")
{
    public string Time => (string.IsNullOrEmpty(Source) ? "" : Source + "  ") + Start.ToString(@"hh\:mm\:ss");
}

public sealed record LanguageOption(string Code, string Name);

public sealed record FormatOption(TranscriptFormat Format, string Label);

public enum ResultKind { None, Success, Info, Error }
