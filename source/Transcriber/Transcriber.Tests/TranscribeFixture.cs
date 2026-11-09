using Transcriber.ViewModels;
using Transcriber.ViewModels.Progress;
using Transcriber.ViewModels.Transcribe;

/// <summary>Builds the Transcribe screen the way the app does: the sections first, then the screen that connects them.</summary>
internal static class TranscribeFixture
{
    public static TranscribeViewModel Create(
        IFileReaderService reader,
        IWhisperService whisper,
        IFilePickerService picker,
        IClipboardService clipboard,
        IFileLauncher launcher,
        IModelManager modelManager,
        IDialogService dialogs,
        IAudioPlayer player)
    {
        var run = new RunState();
        var options = new OptionsViewModel(modelManager, ServicesFixture.Models, ServicesFixture.Languages, whisper, run);
        return new TranscribeViewModel(
            run,
            new FilesViewModel(picker, ServicesFixture.Formats, run),
            options,
            new RunStatusViewModel(options, new ProgressLookPicker(ProgressLooks.All), run),
            new SavedTranscriptsViewModel(launcher, run),
            new PlaybackViewModel(player),
            new ResultsViewModel(clipboard),
            reader, whisper, ServicesFixture.Exporter, ServicesFixture.Languages, dialogs);
    }
}
