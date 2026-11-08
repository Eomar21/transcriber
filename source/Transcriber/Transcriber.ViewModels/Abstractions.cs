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

/// <summary>Asks the user a yes/no question. Implemented per platform by the app, faked in tests.</summary>
public interface IDialogService
{
    /// <returns>True if the user chose <paramref name="accept"/>.</returns>
    Task<bool> ConfirmAsync(string title, string message, string accept, string cancel);
}

/// <summary>A Whisper model as shown in the picker, with whether it is ready to use or needs a download.</summary>
public sealed partial class ModelOption : CommunityToolkit.Mvvm.ComponentModel.ObservableObject
{
    private readonly IModelManager _manager;

    public ModelOption(WhisperModel model, IModelManager manager)
    {
        Model = model;
        _manager = manager;
    }

    public WhisperModel Model { get; }
    public bool IsInstalled => _manager.IsDownloaded(Model);
    public bool IsBundled => _manager.IsBundled(Model);

    public string SizeText => Model.ApproxBytes >= 1_000_000_000
        ? $"{Model.ApproxBytes / 1_000_000_000.0:0.0} GB"
        : $"{Model.ApproxBytes / 1_000_000} MB";

    /// <summary>Picker text: the name plus "Installed" or the download size.</summary>
    public string Label => IsInstalled
        ? $"{Model.DisplayName}  -  installed"
        : $"{Model.DisplayName}  -  {SizeText} download";

    /// <summary>Call after a download so the label and state refresh.</summary>
    public void Refresh()
    {
        OnPropertyChanged(nameof(IsInstalled));
        OnPropertyChanged(nameof(Label));
    }

    public override string ToString() => Label;
}
