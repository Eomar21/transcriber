using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Transcriber.Services.Interfaces;
using Transcriber.Services.Models;

namespace Transcriber.ViewModels.Transcribe;

/// <summary>The transcripts that a run wrote, with the actions to open one or to show it in its folder.</summary>
public sealed partial class SavedTranscriptsViewModel : SectionViewModel
{
    private readonly IFileLauncher _launcher;
    private IReadOnlyList<string> _savedFiles = [];

    public SavedTranscriptsViewModel(IFileLauncher launcher, RunState run) : base(run) => _launcher = launcher;

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

    public void SetFiles(IReadOnlyList<string> files)
    {
        _savedFiles = files;
        SavedFilesMissing = false;
        ActionMessage = string.Empty;
        CanOpenTranscript = files.Count == 1;
        HasSavedFiles = files.Count > 0;
    }

    public void Clear() => SetFiles([]);

    /// <summary>Shows a warning under the buttons. Another section uses this to report a problem with a saved file.</summary>
    public void Report(string message) => ActionMessage = message;

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
                ActionMessage = $"Couldn't find {name}. It may have been moved or deleted.";
                SavedFilesMissing = true;
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
}
