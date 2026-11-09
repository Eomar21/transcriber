using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Transcriber.ViewModels.Transcribe;

/// <summary>The lines of a finished transcript, with search and copy.</summary>
public sealed partial class ResultsViewModel : ObservableObject
{
    private readonly IClipboardService _clipboard;
    private IReadOnlyList<SegmentItem> _allSegments = [];

    public ResultsViewModel(IClipboardService clipboard) => _clipboard = clipboard;

    /// <summary>The person picked a line. The line is played by the playback section, which this one does not know.</summary>
    public event EventHandler<SegmentItem>? SegmentPicked;

    [ObservableProperty] private IReadOnlyList<SegmentItem> _visibleSegments = [];
    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private bool _hasResults;

    /// <summary>
    /// The line the user picked in the list (with the mouse or the keyboard). Picking a line plays it, then the pick is
    /// cleared so the same line can be picked again.
    /// </summary>
    [ObservableProperty] private SegmentItem? _selectedSegment;

    partial void OnSelectedSegmentChanged(SegmentItem? value)
    {
        if (value is null) return;
        SegmentPicked?.Invoke(this, value);
        SelectedSegment = null;
    }

    partial void OnSearchTextChanged(string value) => Refresh();

    /// <summary>Shows the lines of a new run and clears the search.</summary>
    public void Show(IReadOnlyList<SegmentItem> segments)
    {
        _allSegments = segments;
        SearchText = string.Empty;
        Refresh();
    }

    private void Refresh()
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
