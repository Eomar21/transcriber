using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Transcriber.Services.Interfaces;

namespace Transcriber.ViewModels.Practice;

/// <summary>
/// What is analysed: a file or a recording, and the language that is spoken in it.
/// A recording from the microphone is a file that the app made, so the app removes it when it is replaced.
/// </summary>
public sealed partial class SourceViewModel : PracticeSectionViewModel
{
    private readonly IFilePickerService _picker;
    private string? _ownedRecording;

    public SourceViewModel(IFilePickerService picker, ILanguageCatalog languages, MicrophoneViewModel microphone, PracticeState state) : base(state)
    {
        _picker = picker;
        Microphone = microphone;

        // Practice offers the languages that its model handles well. A language that is less accurate with it carries a hint.
        Languages = languages.Practice
            .Select(l =>
            {
                var advice = languages.PracticeAdvice(l.Code);
                return new LanguageOption(l.Code, l.Name, advice, advice is null ? null : "less accurate");
            })
            .ToList();
        _selectedLanguage = Languages[0];
    }

    /// <summary>The microphone is the other way to give a recording. The view shows its button next to the file button.</summary>
    public MicrophoneViewModel Microphone { get; }

    /// <summary>A different file or recording is now in use. The result of an earlier analysis is out of date.</summary>
    public event EventHandler? SourceChanged;

    public IReadOnlyList<LanguageOption> Languages { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LanguageAdvice), nameof(HasLanguageAdvice))]
    private LanguageOption _selectedLanguage;

    /// <summary>Under the language picker: says when the chosen language is less reliable in Practice.</summary>
    public string LanguageAdvice => SelectedLanguage?.Advice ?? string.Empty;

    public bool HasLanguageAdvice => LanguageAdvice.Length > 0;

    /// <summary>The full path of the file to analyse, or null when nothing is chosen.</summary>
    public string? FilePath { get; private set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasFile))]
    private string _fileName = "";

    public bool HasFile => FileName.Length > 0;

    [RelayCommand]
    private async Task ChooseFileAsync()
    {
        if (!IsIdle) return;
        var picked = await _picker.PickMediaFilesAsync();
        if (picked.Count > 0) SetFile(picked[0]);
    }

    /// <summary>Uses a file chosen elsewhere, for example dropped on the window or opened from the command line.</summary>
    public void SetFile(string path)
    {
        if (!IsIdle || !File.Exists(path)) return;
        Use(path, Path.GetFileName(path));
    }

    /// <summary>Uses a recording that the microphone made. The previous recording goes only now, because this one turned out well.</summary>
    public void UseRecording(string path, string displayName)
    {
        if (_ownedRecording is not null && !string.Equals(_ownedRecording, path, StringComparison.OrdinalIgnoreCase))
            MicrophoneViewModel.DeleteQuietly(_ownedRecording);
        _ownedRecording = path;
        Use(path, displayName);
    }

    private void Use(string path, string displayName)
    {
        // A recording of ours is replaced by the next choice; a file the user picked is never touched.
        if (_ownedRecording is not null && !string.Equals(_ownedRecording, path, StringComparison.OrdinalIgnoreCase))
        {
            MicrophoneViewModel.DeleteQuietly(_ownedRecording);
            _ownedRecording = null;
        }

        FilePath = path;
        FileName = displayName;
        SourceChanged?.Invoke(this, EventArgs.Empty);
    }
}
