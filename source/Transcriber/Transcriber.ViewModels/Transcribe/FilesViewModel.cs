using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Transcriber.Services.Interfaces;

namespace Transcriber.ViewModels.Transcribe;

/// <summary>The files to transcribe: choosing them, dropping them and clearing the list.</summary>
public sealed partial class FilesViewModel : SectionViewModel
{
    private readonly IFilePickerService _picker;
    private readonly IMediaFormatCatalog _formats;

    public FilesViewModel(IFilePickerService picker, IMediaFormatCatalog formats, RunState run) : base(run)
    {
        _picker = picker;
        _formats = formats;
    }

    /// <summary>The list was changed by the person. Anything that shows the result of an earlier run is out of date.</summary>
    public event EventHandler? FilesChanged;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FilesSummary), nameof(HasFiles))]
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
            .Where(_formats.IsSupported)
            .Where(p => !SelectedFiles.Contains(p, StringComparer.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (supported.Count == 0)
        {
            if (dropped.Count > 0)
                DropMessage = "Those files can't be transcribed. Try mp3, wav, m4a, mp4, mov or another common audio or video file. To try a file with another extension, use Any file type.";
            return 0;
        }

        Replace([.. SelectedFiles, .. supported]);
        if (supported.Count < dropped.Count)
            DropMessage = $"Added {supported.Count} of {dropped.Count} files. The rest were unsupported or already in the list.";
        return supported.Count;
    }

    [RelayCommand]
    private async Task BrowseAsync()
    {
        if (IsBusy) return;
        var picked = await _picker.PickMediaFilesAsync();
        if (picked.Count > 0) Replace(picked);
    }

    /// <summary>For a file whose extension is not in the list. The file is tried, and a clear message shows if it cannot be read.</summary>
    [RelayCommand]
    private async Task BrowseAnyAsync()
    {
        if (IsBusy) return;
        var picked = await _picker.PickAnyFilesAsync();
        if (picked.Count > 0) Replace(picked);
    }

    [RelayCommand]
    private void ClearFiles()
    {
        if (IsBusy) return;
        Replace([]);
    }

    /// <summary>Removes the drop hint. A new run starts with a clean screen.</summary>
    public void ClearMessage() => DropMessage = string.Empty;

    private void Replace(IReadOnlyList<string> files)
    {
        SelectedFiles = files;
        DropMessage = string.Empty;
        FilesChanged?.Invoke(this, EventArgs.Empty);
    }
}
