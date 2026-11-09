using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Transcriber.Coach.Interfaces;
using Transcriber.Coach.Models;

namespace Transcriber.ViewModels.Practice;

/// <summary>The practice runs that were saved before, newest first.</summary>
public sealed partial class HistoryViewModel : ObservableObject
{
    private readonly IPracticeHistory _history;

    public HistoryViewModel(IPracticeHistory history)
    {
        _history = history;
        Reload();
    }

    [ObservableProperty] private IReadOnlyList<SessionItem> _sessions = [];
    [ObservableProperty] private bool _hasHistory;

    /// <summary>The report of the latest run in a language, to compare a new run with it. Null when there is none.</summary>
    public SpeechReport? LastReport(string language) => _history.Load().LastOrDefault(s => s.Language == language)?.Report;

    public void Add(PracticeSession session)
    {
        _history.Add(session);
        Reload();
    }

    private void Reload()
    {
        Sessions = _history.Load()
            .OrderByDescending(s => s.When)
            .Select(s => new SessionItem(s.Id, s.Title, s.When.LocalDateTime.ToString("d MMM yyyy HH:mm", CultureInfo.InvariantCulture), Summarise(s.Report)))
            .ToList();
        HasHistory = Sessions.Count > 0;
    }

    private static string Summarise(SpeechReport r) =>
        $"{r.WordsPerMinute.ToString("0", CultureInfo.InvariantCulture)} wpm · {r.FillersPer100Words.ToString("0.#", CultureInfo.InvariantCulture)} fillers per 100 · {r.Repeats.Count} stumbles";

    [RelayCommand]
    private void DeleteSession(SessionItem? item)
    {
        if (item is null) return;
        _history.Delete(item.Id);
        Reload();
    }

    [RelayCommand]
    private void ClearHistory()
    {
        _history.Clear();
        Reload();
    }
}
