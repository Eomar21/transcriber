using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Transcriber.Services.Interfaces;
using Transcriber.Services.Models;

namespace Transcriber.ViewModels.Transcribe;

/// <summary>
/// The Transcribe screen. It owns the sections of the screen and runs a transcription with them.
/// Each section has its own view model and does not know the others. This class connects them.
/// It has no dependency on any UI framework.
/// </summary>
public sealed partial class TranscribeViewModel : ObservableObject
{
    private readonly RunState _run;
    private readonly IFileReaderService _fileReader;
    private readonly IWhisperService _whisper;
    private readonly ITranscriptExporter _exporter;
    private readonly ILanguageCatalog _languageCatalog;
    private readonly IDialogService _dialogs;

    public TranscribeViewModel(
        RunState run,
        FilesViewModel files,
        OptionsViewModel options,
        RunStatusViewModel status,
        SavedTranscriptsViewModel saved,
        PlaybackViewModel playback,
        ResultsViewModel results,
        IFileReaderService fileReader,
        IWhisperService whisper,
        ITranscriptExporter exporter,
        ILanguageCatalog languageCatalog,
        IDialogService dialogs)
    {
        _run = run;
        Files = files;
        Options = options;
        Status = status;
        Saved = saved;
        Playback = playback;
        Results = results;
        _fileReader = fileReader;
        _whisper = whisper;
        _exporter = exporter;
        _languageCatalog = languageCatalog;
        _dialogs = dialogs;

        // The sections only raise events. What each event means for the others is decided here and only here.
        Files.FilesChanged += (_, _) => ClearResult();
        Files.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(FilesViewModel.HasFiles)) TranscribeCommand.NotifyCanExecuteChanged();
        };
        Results.SegmentPicked += (_, segment) => Playback.PlaySegment(segment);
        Playback.Problem += (_, message) => Saved.Report(message);
        _run.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName != nameof(RunState.IsBusy)) return;
            OnPropertyChanged(nameof(IsBusy));
            OnPropertyChanged(nameof(IsIdle));
            TranscribeCommand.NotifyCanExecuteChanged();
        };
    }

    public FilesViewModel Files { get; }
    public OptionsViewModel Options { get; }
    public RunStatusViewModel Status { get; }
    public SavedTranscriptsViewModel Saved { get; }
    public PlaybackViewModel Playback { get; }
    public ResultsViewModel Results { get; }

    public bool IsBusy => _run.IsBusy;

    public bool IsIdle => _run.IsIdle;

    // ---- Running -------------------------------------------------------------------------

    private bool CanTranscribe() => !IsBusy && Files.HasFiles;

    [RelayCommand(CanExecute = nameof(CanTranscribe), IncludeCancelCommand = true)]
    private async Task TranscribeAsync(CancellationToken ct)
    {
        var options = Options.ToTranscriptionOptions();

        // A model that is not on this computer yet is a large one-time download: ask first.
        var chosen = Options.SelectedModelOption;
        if (!chosen.IsInstalled)
        {
            var go = await _dialogs.ConfirmAsync(
                "Download model?",
                $"\"{chosen.Model.DisplayName}\" needs a one-time download of {chosen.SizeText}. " +
                "It is stored on this computer and works offline afterwards.",
                "Download", "Cancel");
            if (!go)
            {
                Status.SetResult(ResultKind.Info, "Download cancelled. Pick an installed model to transcribe without downloading.");
                return;
            }
        }

        var files = Files.SelectedFiles;
        var format = Options.SelectedFormat.Format;
        var rows = new List<SegmentItem>();
        var saved = new List<string>();
        var incomplete = new List<(string Name, SpeechCoverage Coverage)>();
        var failed = new List<(string Name, string Reason)>();
        var newPlayback = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        _run.IsBusy = true;
        ClearResult();
        Status.BeginProgress();
        Playback.Release();
        try
        {
            for (var i = 0; i < files.Count; i++)
            {
                var file = files[i];
                var prefix = files.Count > 1 ? $"File {i + 1}/{files.Count}: " : string.Empty;
                var progress = new Progress<TranscriptionProgress>(p =>
                {
                    Status.StatusText = prefix + p.Stage;
                    // One bar for the whole run: each file is an equal share.
                    Status.Progress = (i + Math.Clamp(p.Fraction, 0, 1)) / files.Count;
                });

                Status.StatusText = prefix + "Preparing audio";
                string audioPath;
                try
                {
                    audioPath = await _fileReader.GetAudioFileFromFileAsync(file, ct);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    // One file that cannot be read must not stop the others. It is reported when the run ends.
                    failed.Add((Path.GetFileName(file), ex.Message));
                    Status.Progress = (i + 1d) / files.Count;
                    continue;
                }
                var keepForPlayback = false;
                try
                {
                    var segments = await _whisper.TranscribeAsync(audioPath, options, progress, ct);
                    saved.Add(await _exporter.WriteAsync(segments, file, format));
                    if (_whisper.LastCoverage is { LooksIncomplete: true } coverage) incomplete.Add((Path.GetFileName(file), coverage));

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

            if (saved.Count == 0)
            {
                Status.Progress = 0; // no finish for a run that saved nothing
                // Nothing could be read. A single file gets its reason as the headline; several files get one line each.
                if (failed.Count == 1) Status.SetResult(ResultKind.Error, "Failed: " + failed[0].Reason);
                else
                {
                    Status.FailureNote = RunStatusViewModel.FailureMessage(failed);
                    Status.SetResult(ResultKind.Error, $"Failed: none of the {failed.Count} files could be read");
                }
                return;
            }

            Status.Progress = 1;
            Playback.SetSource(rows, newPlayback);
            newPlayback = [];
            Results.Show(rows);

            Status.FailureNote = failed.Count == 0 ? string.Empty
                : $"{failed.Count} of {files.Count} files were skipped.{Environment.NewLine}{RunStatusViewModel.FailureMessage(failed)}";
            Saved.SetFiles(saved);
            Status.ResultDetail = saved.Count == 1 ? saved[0] : Path.GetDirectoryName(saved[0]) ?? string.Empty;
            Status.RuntimeNote = _whisper.LastRuntime is { } r ? $"Processed with {r}" : string.Empty;
            Status.AccuracyNote = _languageCatalog.ResultNote(options.Language, options.Model) ?? string.Empty;
            Status.CompletenessNote = RunStatusViewModel.CompletenessMessage(incomplete, options.Language);
            Status.SetResult(ResultKind.Success, saved.Count == 1
                ? "Transcript saved"
                : $"{saved.Count} transcripts saved next to the source files");
        }
        catch (OperationCanceledException)
        {
            Status.SetResult(ResultKind.Info, "Cancelled.");
        }
        catch (Exception ex)
        {
            Status.SetResult(ResultKind.Error, "Failed: " + ex.Message);
        }
        finally
        {
            // A cancelled or failed run leaves converted files that will never be played.
            PlaybackViewModel.DeleteTempAudio(PlaybackViewModel.ConvertedCopies(newPlayback));
            Options.RefreshInstalledModels();
            _run.IsBusy = false;
            _ = Status.EndProgressAsync(RunStatusViewModel.FinishLinger);
            Status.StatusText = string.Empty;
        }
    }

    /// <summary>A new choice of files or a new run makes the outcome of the earlier run out of date.</summary>
    private void ClearResult()
    {
        Files.ClearMessage();
        Status.Clear();
        Saved.Clear();
    }
}
