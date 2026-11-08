using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Transcriber.ViewModels;

/// <summary>State and behaviour of the Transcribe screen. Has no dependency on any UI framework.</summary>
public partial class TranscribeViewModel : ObservableObject
{
    private readonly IFileReaderService _fileReader;
    private readonly IWhisperService _whisper;
    private readonly IFilePickerService _filePicker;
    private readonly IClipboardService _clipboard;
    private readonly IFileLauncher _launcher;
    private readonly IModelManager _modelManager;
    private readonly IDialogService _dialogs;
    private readonly IAudioPlayer _player;

    private List<SegmentItem> _allSegments = [];

    public TranscribeViewModel(
        IFileReaderService fileReader,
        IWhisperService whisper,
        IFilePickerService filePicker,
        IClipboardService clipboard,
        IFileLauncher launcher,
        IModelManager modelManager,
        IDialogService dialogs,
        IAudioPlayer player)
    {
        _fileReader = fileReader;
        _whisper = whisper;
        _filePicker = filePicker;
        _clipboard = clipboard;
        _launcher = launcher;
        _modelManager = modelManager;
        _dialogs = dialogs;
        _player = player;
        _player.StateChanged += (_, _) => UpdatePlaybackState();

        Languages = SupportedLanguages.All.Select(l => new LanguageOption(l.Code, l.Name)).ToList();
        Models = WhisperModel.All.Select(m => new ModelOption(m, modelManager)).ToList();
        Formats =
        [
            new(TranscriptFormat.Txt, "Text (.txt)"),
            new(TranscriptFormat.Srt, "Subtitles (.srt)"),
            new(TranscriptFormat.Vtt, "Web subtitles (.vtt)"),
            new(TranscriptFormat.Json, "Data (.json)"),
        ];

        _selectedLanguage = Languages[0];
        _selectedModelOption = Models.First(m => m.Model == WhisperModel.Small);
        _selectedFormat = Formats[0];
        _selectedSpeakerCount = SpeakerCounts[0];
    }

    // ---- Options -------------------------------------------------------------------------

    public IReadOnlyList<LanguageOption> Languages { get; }
    public IReadOnlyList<ModelOption> Models { get; }
    public IReadOnlyList<FormatOption> Formats { get; }

    [ObservableProperty] private LanguageOption _selectedLanguage;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedModel), nameof(ModelHint), nameof(HasModelHint))]
    private ModelOption _selectedModelOption;

    public WhisperModel SelectedModel => SelectedModelOption.Model;

    /// <summary>Under the picker: tells the user a download is needed before the first run with this model.</summary>
    public string ModelHint => SelectedModelOption.IsInstalled
        ? string.Empty
        : $"Needs a one-time download of {SelectedModelOption.SizeText}. You will be asked before it starts.";

    public bool HasModelHint => ModelHint.Length > 0;
    [ObservableProperty] private FormatOption _selectedFormat;
    [ObservableProperty] private string? _prompt;
    [ObservableProperty] private bool _useGpu = true;

    // ---- Speakers -------------------------------------------------------------------------

    /// <summary>False where speaker detection cannot run, so the option is hidden instead of failing.</summary>
    public bool CanIdentifySpeakers => _whisper.CanIdentifySpeakers;

    [ObservableProperty] private bool _identifySpeakers;

    public IReadOnlyList<SpeakerCountOption> SpeakerCounts { get; } =
    [
        new("Detect automatically", null),
        new("2 speakers", 2),
        new("3 speakers", 3),
        new("4 speakers", 4),
        new("5 speakers", 5),
        new("6 speakers", 6),
    ];

    [ObservableProperty] private SpeakerCountOption _selectedSpeakerCount = null!;

    // ---- Input files ---------------------------------------------------------------------

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FilesSummary), nameof(HasFiles))]
    [NotifyCanExecuteChangedFor(nameof(TranscribeCommand))]
    private IReadOnlyList<string> _selectedFiles = [];

    public bool HasFiles => SelectedFiles.Count > 0;

    public string FilesSummary => SelectedFiles.Count switch
    {
        0 => "Choose audio or video files to transcribe",
        1 => Path.GetFileName(SelectedFiles[0]),
        var n => $"{n} files selected",
    };

    /// <summary>True while files are being dragged over the window, so the view can highlight the drop target.</summary>
    [ObservableProperty] private bool _isDragOver;

    /// <summary>Inline hint shown when a drop contained nothing usable.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasDropMessage))]
    private string _dropMessage = string.Empty;

    public bool HasDropMessage => DropMessage.Length > 0;

    /// <summary>
    /// Adds files (for example from drag and drop). Unsupported types, folders and files that are already
    /// in the list are skipped. Returns how many files were actually added.
    /// </summary>
    public int AddFiles(IEnumerable<string> paths)
    {
        IsDragOver = false;
        DropMessage = string.Empty;
        if (IsBusy) return 0;

        var dropped = paths.ToList();
        var supported = dropped
            .Where(p => FileReaderService.SupportedExtensions.Contains(Path.GetExtension(p).ToLowerInvariant()))
            .Where(p => !SelectedFiles.Contains(p, StringComparer.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (supported.Count == 0)
        {
            if (dropped.Count > 0)
                DropMessage = "Those files can't be transcribed. Try mp3, wav, mp4, mov, m4a or another audio or video file.";
            return 0;
        }

        SelectedFiles = [.. SelectedFiles, .. supported];
        ClearResult();
        if (supported.Count < dropped.Count)
            DropMessage = $"Added {supported.Count} of {dropped.Count} files. The rest were unsupported or already in the list.";
        return supported.Count;
    }

    [RelayCommand]
    private async Task BrowseAsync()
    {
        if (IsBusy) return;
        var picked = await _filePicker.PickMediaFilesAsync();
        if (picked.Count == 0) return;

        SelectedFiles = picked;
        ClearResult();
    }

    [RelayCommand]
    private void ClearFiles()
    {
        if (IsBusy) return;
        SelectedFiles = [];
        ClearResult();
    }

    // ---- Progress / result ---------------------------------------------------------------

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(TranscribeCommand))]
    [NotifyPropertyChangedFor(nameof(IsIdle))]
    private bool _isBusy;

    public bool IsIdle => !IsBusy;

    [ObservableProperty] private double _progress;
    [ObservableProperty] private string _statusText = string.Empty;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasResultMessage))]
    private string _resultMessage = string.Empty;

    public bool HasResultMessage => ResultMessage.Length > 0;
    [ObservableProperty] private ResultKind _resultKind;

    /// <summary>Secondary line under the result: the saved file's path (or folder, for several files).</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasResultDetail))]
    private string _resultDetail = string.Empty;

    public bool HasResultDetail => ResultDetail.Length > 0;

    /// <summary>Muted note about how the run was processed, e.g. "Processed with Vulkan".</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasRuntimeNote))]
    private string _runtimeNote = string.Empty;

    public bool HasRuntimeNote => RuntimeNote.Length > 0;

    private void ClearResult()
    {
        DropMessage = string.Empty;
        ResultMessage = string.Empty;
        ResultDetail = string.Empty;
        RuntimeNote = string.Empty;
        ResultKind = ResultKind.None;
        ClearSavedFiles();
    }

    private void SetResult(ResultKind kind, string message)
    {
        ResultKind = kind;
        ResultMessage = message;
    }

    // ---- Saved transcripts: open / show in folder ----------------------------------------

    private IReadOnlyList<string> _savedFiles = [];

    /// <summary>True once a run finished and wrote at least one transcript.</summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(OpenTranscriptCommand), nameof(ShowInFolderCommand))]
    private bool _hasSavedFiles;

    /// <summary>Opening a single file is offered; for a batch only "show in folder" makes sense.</summary>
    [ObservableProperty] private bool _canOpenTranscript;

    /// <summary>Set after a "not found" error so both buttons stay disabled.</summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(OpenTranscriptCommand), nameof(ShowInFolderCommand))]
    private bool _savedFilesMissing;

    /// <summary>Inline warning shown under the buttons, never a modal dialog.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasActionMessage))]
    private string _actionMessage = string.Empty;

    public bool HasActionMessage => ActionMessage.Length > 0;

    /// <summary>"Show in Finder" on macOS, "Show in folder" elsewhere.</summary>
    public string ShowInFolderLabel => OperatingSystem.IsMacOS() || OperatingSystem.IsMacCatalyst() ? "Show in Finder" : "Show in folder";

    private void SetSavedFiles(IReadOnlyList<string> files)
    {
        _savedFiles = files;
        SavedFilesMissing = false;
        ActionMessage = string.Empty;
        CanOpenTranscript = files.Count == 1;
        HasSavedFiles = files.Count > 0;
    }

    private void ClearSavedFiles() => SetSavedFiles([]);

    private bool CanUseSavedFiles() => HasSavedFiles && !SavedFilesMissing;

    [RelayCommand(CanExecute = nameof(CanUseSavedFiles))]
    private void OpenTranscript()
    {
        if (_savedFiles.Count != 1) return;
        var path = _savedFiles[0];
        var name = Path.GetFileName(path);

        switch (_launcher.Open(path))
        {
            case LaunchResult.Opened:
                ActionMessage = string.Empty;
                break;
            case LaunchResult.NotFound:
                MarkMissing(name);
                break;
            case LaunchResult.NoHandler:
                // Fall back to showing the file so the user can still get to it.
                _launcher.Reveal(path);
                ActionMessage = $"No app is set to open {Path.GetExtension(path)} files. Showing it in the folder instead.";
                break;
            default:
                ActionMessage = $"Couldn't open {name}.";
                break;
        }
    }

    [RelayCommand(CanExecute = nameof(CanUseSavedFiles))]
    private void ShowInFolder()
    {
        if (_savedFiles.Count == 0) return;
        var path = _savedFiles[0]; // for a batch, reveal the first file

        switch (_launcher.Reveal(path))
        {
            case LaunchResult.Opened:
                ActionMessage = string.Empty;
                break;
            case LaunchResult.NotFound:
                ActionMessage = "The folder is no longer available.";
                SavedFilesMissing = true;
                break;
            default:
                ActionMessage = "Couldn't open the folder.";
                break;
        }
    }

    private void MarkMissing(string name)
    {
        ActionMessage = $"Couldn't find {name}. It may have been moved or deleted.";
        SavedFilesMissing = true;
    }

    // ---- Transcription -------------------------------------------------------------------

    private bool CanTranscribe() => !IsBusy && HasFiles;

    [RelayCommand(CanExecute = nameof(CanTranscribe), IncludeCancelCommand = true)]
    private async Task TranscribeAsync(CancellationToken ct)
    {
        var options = new TranscriptionOptions
        {
            Language = SelectedLanguage.Code,
            Model = SelectedModel,
            Prompt = Prompt,
            UseGpu = UseGpu,
            IdentifySpeakers = IdentifySpeakers && CanIdentifySpeakers,
            SpeakerCount = SelectedSpeakerCount.Count,
        };
        // A model that is not on this computer yet is a large one-time download: ask first.
        var chosen = SelectedModelOption;
        if (!chosen.IsInstalled)
        {
            var go = await _dialogs.ConfirmAsync(
                "Download model?",
                $"\"{chosen.Model.DisplayName}\" needs a one-time download of {chosen.SizeText}. " +
                "It is stored on this computer and works offline afterwards.",
                "Download", "Cancel");
            if (!go)
            {
                SetResult(ResultKind.Info, "Download cancelled. Pick an installed model to transcribe without downloading.");
                return;
            }
        }

        var files = SelectedFiles;
        var rows = new List<SegmentItem>();
        var saved = new List<string>();
        var newPlayback = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        IsBusy = true;
        Progress = 0;
        ClearResult();
        ReleasePlayback();
        try
        {
            for (var i = 0; i < files.Count; i++)
            {
                var file = files[i];
                var prefix = files.Count > 1 ? $"File {i + 1}/{files.Count}: " : string.Empty;
                var progress = new Progress<TranscriptionProgress>(p =>
                {
                    StatusText = prefix + p.Stage;
                    Progress = Math.Clamp(p.Fraction, 0, 1);
                });

                StatusText = prefix + "Preparing audio";
                var audioPath = await _fileReader.GetAudioFileFromFileAsync(file);
                var keepForPlayback = false;
                try
                {
                    var segments = await _whisper.TranscribeAsync(audioPath, options, progress, ct);
                    saved.Add(await TranscriptExporter.WriteAsync(segments, file, SelectedFormat.Format));

                    var source = files.Count > 1 ? Path.GetFileName(file) : string.Empty;
                    rows.AddRange(segments.Select(s => new SegmentItem(s.Start, s.End, s.Text.Trim(), source, file, s.Speaker)));

                    // The converted audio is what plays when a line is clicked; it is removed with the next run.
                    newPlayback[file] = audioPath;
                    keepForPlayback = true;
                }
                finally
                {
                    // Converted audio is a temp file; never delete the user's own input.
                    if (!keepForPlayback && !string.Equals(audioPath, file, StringComparison.OrdinalIgnoreCase))
                        try { File.Delete(audioPath); } catch { /* best effort */ }
                }
            }

            _playbackFiles = newPlayback;
            newPlayback = [];
            _allSegments = rows;
            SearchText = string.Empty;
            RefreshVisibleSegments();

            SetSavedFiles(saved);
            ResultDetail = saved.Count == 1 ? saved[0] : Path.GetDirectoryName(saved[0]) ?? string.Empty;
            RuntimeNote = _whisper.LastRuntime is { } r ? $"Processed with {r}" : string.Empty;
            SetResult(ResultKind.Success, saved.Count == 1
                ? "Transcript saved"
                : $"{saved.Count} transcripts saved next to the source files");
        }
        catch (OperationCanceledException)
        {
            SetResult(ResultKind.Info, "Cancelled.");
        }
        catch (Exception ex)
        {
            SetResult(ResultKind.Error, "Failed: " + ex.Message);
        }
        finally
        {
            // A cancelled or failed run leaves converted files that will never be played.
            DeleteTempAudio(ConvertedCopies(newPlayback));
            foreach (var m in Models) m.Refresh();
            OnPropertyChanged(nameof(ModelHint));
            OnPropertyChanged(nameof(HasModelHint));
            IsBusy = false;
            StatusText = string.Empty;
        }
    }

    // ---- Playback: click a line to hear it -----------------------------------------------

    // Original file -> the converted audio that is played for it.
    private Dictionary<string, string> _playbackFiles = new(StringComparer.OrdinalIgnoreCase);
    private string? _playingFile;
    private SegmentItem? _activeSegment;

    [ObservableProperty] private bool _isPlaying;

    /// <summary>True once something has been played, so the small player bar is shown.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowPlayHint))]
    private bool _hasPlayback;

    /// <summary>Shows the "click a line" tip until something has been played.</summary>
    public bool ShowPlayHint => !HasPlayback;

    /// <summary>Player bar text, for example "talk.mp3  00:12".</summary>
    [ObservableProperty] private string _playbackText = string.Empty;

    [RelayCommand]
    private void PlaySegment(SegmentItem? segment)
    {
        if (segment is null) return;

        // Prefer the converted audio, fall back to the original file for formats the platform can play.
        if (!_playbackFiles.TryGetValue(segment.FilePath, out var path) || !File.Exists(path))
            path = segment.FilePath;
        if (!File.Exists(path))
        {
            ActionMessage = $"Couldn't find {Path.GetFileName(segment.FilePath)} to play it.";
            return;
        }

        _playingFile = segment.FilePath;
        HasPlayback = true;
        _player.Play(path, segment.Start);
        UpdatePlaybackState();
    }

    [RelayCommand]
    private void TogglePlayback()
    {
        if (_player.IsPlaying) _player.Pause(); else _player.Resume();
        UpdatePlaybackState();
    }

    [RelayCommand]
    private void StopPlayback()
    {
        _player.Stop();
        HasPlayback = false;
        UpdatePlaybackState();
    }

    private void UpdatePlaybackState()
    {
        IsPlaying = _player.IsPlaying;
        var position = _player.Position;
        PlaybackText = HasPlayback && _playingFile is not null
            ? $"{Path.GetFileName(_playingFile)}  {position:hh\\:mm\\:ss}"
            : string.Empty;

        // Highlight the line being spoken right now.
        var current = HasPlayback && _playingFile is not null
            ? _allSegments.FirstOrDefault(s =>
                string.Equals(s.FilePath, _playingFile, StringComparison.OrdinalIgnoreCase) &&
                position >= s.Start && position < s.End)
            : null;
        if (ReferenceEquals(current, _activeSegment)) return;
        if (_activeSegment is not null) _activeSegment.IsActive = false;
        _activeSegment = current;
        if (current is not null) current.IsActive = true;
    }

    private static IEnumerable<string> ConvertedCopies(Dictionary<string, string> files) =>
        files.Where(kv => !string.Equals(kv.Key, kv.Value, StringComparison.OrdinalIgnoreCase)).Select(kv => kv.Value).ToList();

    private static void DeleteTempAudio(IEnumerable<string> paths)
    {
        foreach (var p in paths)
            try { File.Delete(p); } catch { /* best effort */ }
    }

    private void ReleasePlayback()
    {
        _player.Stop();
        HasPlayback = false;
        UpdatePlaybackState();
        // Only converted copies are temp files; the user's own wav is used in place and must stay.
        DeleteTempAudio(ConvertedCopies(_playbackFiles));
        _playbackFiles.Clear();
    }

    // ---- Results -------------------------------------------------------------------------

    [ObservableProperty] private IReadOnlyList<SegmentItem> _visibleSegments = [];
    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private bool _hasResults;

    partial void OnSearchTextChanged(string value) => RefreshVisibleSegments();

    private void RefreshVisibleSegments()
    {
        var q = SearchText.Trim();
        VisibleSegments = q.Length == 0
            ? _allSegments
            : _allSegments.Where(s => s.Text.Contains(q, StringComparison.CurrentCultureIgnoreCase)).ToList();
        HasResults = _allSegments.Count > 0;
    }

    [RelayCommand]
    private async Task CopyAllAsync()
    {
        if (_allSegments.Count == 0) return;
        await _clipboard.SetTextAsync(string.Join(Environment.NewLine, _allSegments.Select(s => s.HasSpeaker ? $"{s.SpeakerLabel}: {s.Text}" : s.Text)));
    }
}
