namespace Transcriber.ViewModels;

/// <summary>Lets the user pick media files. Implemented per platform by the app, faked in tests.</summary>
public interface IFilePickerService
{
    /// <returns>Full paths of the chosen files, or an empty list if the user cancelled.</returns>
    Task<IReadOnlyList<string>> PickMediaFilesAsync();

    /// <summary>Like <see cref="PickMediaFilesAsync"/>, but the dialog shows files of every type.</summary>
    Task<IReadOnlyList<string>> PickAnyFilesAsync() => PickMediaFilesAsync();
}

/// <summary>Clipboard access, abstracted so view models never touch platform APIs.</summary>
public interface IClipboardService
{
    Task SetTextAsync(string text);
}

/// <summary>Plays audio so a transcript line can be clicked to hear it. Implemented per platform, faked in tests.</summary>
public interface IAudioPlayer
{
    /// <summary>Starts playing the file from the given position, replacing anything already playing.</summary>
    void Play(string path, TimeSpan from);
    void Pause();
    void Resume();
    void Stop();
    bool IsPlaying { get; }
    TimeSpan Position { get; }

    /// <summary>Raised on the UI thread when playback starts, pauses, ends, and a few times a second while playing.</summary>
    event EventHandler? StateChanged;
}

/// <summary>One row of the transcript list.</summary>
public sealed partial class SegmentItem : CommunityToolkit.Mvvm.ComponentModel.ObservableObject
{
    public SegmentItem(TimeSpan start, TimeSpan end, string text, string source = "", string filePath = "", int? speaker = null)
    {
        Start = start;
        End = end;
        Text = text;
        Source = source;
        FilePath = filePath;
        Speaker = speaker;
    }

    public TimeSpan Start { get; }
    public TimeSpan End { get; }
    public string Text { get; }

    /// <summary>File name shown in front of the time when several files were transcribed together.</summary>
    public string Source { get; }

    /// <summary>The media file this line came from, used to find the audio to play.</summary>
    public string FilePath { get; }

    /// <summary>Who is speaking (0-based), or null when speakers were not identified.</summary>
    public int? Speaker { get; }

    public bool HasSpeaker => Speaker is not null;

    public string SpeakerLabel => Speaker is { } s ? SpeakerAssigner.Label(s) : "";

    public string Time => (string.IsNullOrEmpty(Source) ? "" : Source + "  ") + Start.ToString(@"hh\:mm\:ss");

    /// <summary>True while this line is the one being played.</summary>
    [CommunityToolkit.Mvvm.ComponentModel.ObservableProperty] private bool _isActive;
}

/// <param name="Advice">A hint to show when this language is less accurate with the current setup, or null.</param>
/// <param name="Tag">A short note shown in the picker next to the name, or null.</param>
public sealed record LanguageOption(string Code, string Name, string? Advice = null, string? Tag = null)
{
    /// <summary>The text in the picker: the name, and the tag when there is one.</summary>
    public string Label => Tag is null ? Name : $"{Name} - {Tag}";
}

/// <summary>How many people speak in the recording; a null count means "work it out".</summary>
public sealed record SpeakerCountOption(string Label, int? Count);

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
