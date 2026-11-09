using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Transcriber.Services.Interfaces;
using Transcriber.Services.Models;

namespace Transcriber.ViewModels.Transcribe;

/// <summary>What the person can say about how to transcribe: model, language, output format, hints and speakers.</summary>
public sealed partial class OptionsViewModel : SectionViewModel, IModelSuggestion
{
    private readonly IModelCatalog _modelCatalog;
    private readonly ILanguageCatalog _languageCatalog;
    private readonly IWhisperService _whisper;

    public OptionsViewModel(
        IModelManager modelManager,
        IModelCatalog modelCatalog,
        ILanguageCatalog languageCatalog,
        IWhisperService whisper,
        RunState run) : base(run)
    {
        _modelCatalog = modelCatalog;
        _languageCatalog = languageCatalog;
        _whisper = whisper;

        Models = modelCatalog.All.Select(m => new ModelOption(m, modelManager)).ToList();
        Formats =
        [
            new(TranscriptFormat.Txt, "Text (.txt)"),
            new(TranscriptFormat.Srt, "Subtitles (.srt)"),
            new(TranscriptFormat.Vtt, "Web subtitles (.vtt)"),
            new(TranscriptFormat.Json, "Data (.json)"),
        ];

        _selectedModelOption = Models.First(m => m.Model == modelCatalog.Default);
        RebuildLanguages();
        _selectedFormat = Formats[0];
        _selectedSpeakerCount = SpeakerCounts[0];
    }

    // ---- Language ------------------------------------------------------------------------

    /// <summary>The languages offered for the selected model. The list changes when another model is selected.</summary>
    public ObservableCollection<LanguageOption> Languages { get; } = [];

    private LanguageOption _selectedLanguage = null!;

    public LanguageOption SelectedLanguage
    {
        get => _selectedLanguage;
        // A picker briefly reports "nothing selected" while its list is rebuilt; that must not clear the choice.
        set
        {
            if (value is null || !SetProperty(ref _selectedLanguage, value)) return;
            NotifyLanguageAdvice();
        }
    }

    /// <summary>Under the language picker: says when a bigger model would be clearly more accurate for the language.</summary>
    public string LanguageAdvice => SelectedLanguage?.Advice ?? string.Empty;

    public bool HasLanguageAdvice => LanguageAdvice.Length > 0;

    private void NotifyLanguageAdvice()
    {
        OnPropertyChanged(nameof(LanguageAdvice));
        OnPropertyChanged(nameof(HasLanguageAdvice));
    }

    // Keeps the language when the new model still offers it; otherwise falls back to auto-detect.
    private void RebuildLanguages()
    {
        var model = SelectedModel;
        var previous = _selectedLanguage?.Code ?? _languageCatalog.AutoCode;
        var items = _languageCatalog.For(model)
            .Select(l =>
            {
                var advice = _languageCatalog.Advice(l.Code, model);
                return new LanguageOption(l.Code, l.Name, advice, advice is null ? null : $"better with {_modelCatalog.Suggested.Tier}");
            })
            .ToList();

        Languages.Clear();
        foreach (var item in items) Languages.Add(item);

        _selectedLanguage = items.FirstOrDefault(i => i.Code == previous) ?? items[0];
        OnPropertyChanged(nameof(SelectedLanguage));
        NotifyLanguageAdvice();
    }

    // ---- Model ---------------------------------------------------------------------------

    public IReadOnlyList<ModelOption> Models { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedModel), nameof(ModelHint), nameof(HasModelHint))]
    private ModelOption _selectedModelOption;

    partial void OnSelectedModelOptionChanged(ModelOption value) => RebuildLanguages();

    public WhisperModel SelectedModel => SelectedModelOption.Model;

    /// <summary>Under the picker: tells the user a download is needed before the first run with this model.</summary>
    public string ModelHint => SelectedModelOption.IsInstalled
        ? string.Empty
        : $"Needs a one-time download of {SelectedModelOption.SizeText}. You will be asked before it starts.";

    public bool HasModelHint => ModelHint.Length > 0;

    /// <summary>The label of the button that switches to the suggested model, for example "Use the Medium model".</summary>
    public string UseSuggestedModelText => $"Use the {_modelCatalog.Suggested.Tier} model";

    private bool CanUseSuggestedModel() => IsIdle;

    /// <summary>Switches to the model that is more accurate for the selected language.</summary>
    [RelayCommand(CanExecute = nameof(CanUseSuggestedModel))]
    private void UseSuggestedModel() =>
        SelectedModelOption = Models.First(m => m.Model == _modelCatalog.Suggested);

    /// <summary>After a run a model can have been downloaded, so the install state and the hint are read again.</summary>
    public void RefreshInstalledModels()
    {
        foreach (var m in Models) m.Refresh();
        OnPropertyChanged(nameof(ModelHint));
        OnPropertyChanged(nameof(HasModelHint));
    }

    protected override void OnRunStateChanged() => UseSuggestedModelCommand.NotifyCanExecuteChanged();

    // ---- Output and recognition hints ----------------------------------------------------

    public IReadOnlyList<FormatOption> Formats { get; }

    [ObservableProperty] private FormatOption _selectedFormat;
    [ObservableProperty] private string? _prompt;
    [ObservableProperty] private bool _useGpu = true;

    // ---- Speakers ------------------------------------------------------------------------

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

    // ---- For a run -----------------------------------------------------------------------

    /// <summary>The settings of the next run, as the transcription service reads them.</summary>
    public TranscriptionOptions ToTranscriptionOptions() => new()
    {
        Language = SelectedLanguage.Code,
        Model = SelectedModel,
        Prompt = Prompt,
        UseGpu = UseGpu,
        IdentifySpeakers = IdentifySpeakers && CanIdentifySpeakers,
        SpeakerCount = SelectedSpeakerCount.Count,
    };
}
