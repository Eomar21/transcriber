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

    private List<SegmentItem> _allSegments = [];

    public TranscribeViewModel(
        IFileReaderService fileReader,
        IWhisperService whisper,
        IFilePickerService filePicker,
        IClipboardService clipboard)
    {
        _fileReader = fileReader;
        _whisper = whisper;
        _filePicker = filePicker;
        _clipboard = clipboard;

        Languages = SupportedLanguages.All.Select(l => new LanguageOption(l.Code, l.Name)).ToList();
        Models = WhisperModel.All;
        Formats =
        [
            new(TranscriptFormat.Txt, "Text (.txt)"),
            new(TranscriptFormat.Srt, "Subtitles (.srt)"),
            new(TranscriptFormat.Vtt, "Web subtitles (.vtt)"),
            new(TranscriptFormat.Json, "Data (.json)"),
        ];

        _selectedLanguage = Languages[0];
        _selectedModel = WhisperModel.Small;
        _selectedFormat = Formats[0];
    }

    // ---- Options -------------------------------------------------------------------------

    public IReadOnlyList<LanguageOption> Languages { get; }
    public IReadOnlyList<WhisperModel> Models { get; }
    public IReadOnlyList<FormatOption> Formats { get; }

    [ObservableProperty] private LanguageOption _selectedLanguage;
    [ObservableProperty] private WhisperModel _selectedModel;
    [ObservableProperty] private FormatOption _selectedFormat;
    [ObservableProperty] private string? _prompt;
    [ObservableProperty] private bool _useGpu = true;

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

    /// <summary>Adds files (for example from drag and drop), ignoring unsupported types.</summary>
    public void AddFiles(IEnumerable<string> paths)
    {
        if (IsBusy) return;
        var supported = paths
            .Where(p => FileReaderService.SupportedExtensions.Contains(Path.GetExtension(p).ToLowerInvariant()))
            .ToList();
        if (supported.Count == 0) return;

        SelectedFiles = [.. SelectedFiles, .. supported];
        ClearResult();
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

    private void ClearResult()
    {
        ResultMessage = string.Empty;
        ResultKind = ResultKind.None;
    }

    private void SetResult(ResultKind kind, string message)
    {
        ResultKind = kind;
        ResultMessage = message;
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
        };
        var files = SelectedFiles;
        var rows = new List<SegmentItem>();
        var saved = new List<string>();

        IsBusy = true;
        Progress = 0;
        ClearResult();
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
                try
                {
                    var segments = await _whisper.TranscribeAsync(audioPath, options, progress, ct);
                    saved.Add(await TranscriptExporter.WriteAsync(segments, file, SelectedFormat.Format));

                    var source = files.Count > 1 ? Path.GetFileName(file) : string.Empty;
                    rows.AddRange(segments.Select(s => new SegmentItem(s.Start, s.End, s.Text.Trim(), source)));
                }
                finally
                {
                    // Converted audio is a temp file; never delete the user's own input.
                    if (!string.Equals(audioPath, file, StringComparison.OrdinalIgnoreCase))
                        try { File.Delete(audioPath); } catch { /* best effort */ }
                }
            }

            _allSegments = rows;
            SearchText = string.Empty;
            RefreshVisibleSegments();

            var runtime = _whisper.LastRuntime is { } r ? $"  (runtime: {r})" : string.Empty;
            SetResult(ResultKind.Success, saved.Count == 1
                ? $"Transcript saved: {saved[0]}{runtime}"
                : $"{saved.Count} transcripts saved next to the source files{runtime}");
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
            IsBusy = false;
            StatusText = string.Empty;
        }
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
        await _clipboard.SetTextAsync(string.Join(Environment.NewLine, _allSegments.Select(s => s.Text)));
    }
}
