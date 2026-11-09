using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Transcriber.ViewModels.Transcribe;

/// <summary>Plays the audio of a transcript line from its start, and keeps the line that is spoken right now marked.</summary>
public sealed partial class PlaybackViewModel : ObservableObject
{
    private readonly IAudioPlayer _player;

    // Original file -> the converted audio that is played for it.
    private Dictionary<string, string> _files = new(StringComparer.OrdinalIgnoreCase);
    private IReadOnlyList<SegmentItem> _segments = [];
    private string? _playingFile;
    private SegmentItem? _activeSegment;

    public PlaybackViewModel(IAudioPlayer player)
    {
        _player = player;
        _player.StateChanged += (_, _) => UpdateState();
    }

    /// <summary>A line cannot be played, for example because its file was moved. The text is for the person.</summary>
    public event EventHandler<string>? Problem;

    [ObservableProperty] private bool _isPlaying;

    /// <summary>True once something has been played, so the small player bar is shown.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowPlayHint))]
    private bool _hasPlayback;

    /// <summary>Shows the "click a line" tip until something has been played.</summary>
    public bool ShowPlayHint => !HasPlayback;

    /// <summary>Player bar text, for example "talk.mp3  00:12".</summary>
    [ObservableProperty] private string _playbackText = string.Empty;

    /// <summary>Takes the lines of a finished run and the converted audio that belongs to each file.</summary>
    public void SetSource(IReadOnlyList<SegmentItem> segments, Dictionary<string, string> convertedFiles)
    {
        _files = convertedFiles;
        _segments = segments;
    }

    [RelayCommand]
    public void PlaySegment(SegmentItem? segment)
    {
        if (segment is null) return;

        // Prefer the converted audio, fall back to the original file for formats the platform can play.
        if (!_files.TryGetValue(segment.FilePath, out var path) || !File.Exists(path))
            path = segment.FilePath;
        if (!File.Exists(path))
        {
            Problem?.Invoke(this, $"Couldn't find {Path.GetFileName(segment.FilePath)} to play it.");
            return;
        }

        _playingFile = segment.FilePath;
        HasPlayback = true;
        _player.Play(path, segment.Start);
        UpdateState();
    }

    [RelayCommand]
    private void TogglePlayback()
    {
        if (_player.IsPlaying) _player.Pause(); else _player.Resume();
        UpdateState();
    }

    [RelayCommand]
    private void StopPlayback()
    {
        _player.Stop();
        HasPlayback = false;
        UpdateState();
    }

    /// <summary>Stops the sound and removes the converted copies of the last run. A new run calls this first.</summary>
    public void Release()
    {
        _player.Stop();
        HasPlayback = false;
        UpdateState();
        // Only converted copies are temp files; the user's own wav is used in place and must stay.
        DeleteTempAudio(ConvertedCopies(_files));
        _files.Clear();
    }

    /// <summary>The converted audio files in a map from original file to audio. A file that is its own audio is not a copy.</summary>
    public static IEnumerable<string> ConvertedCopies(Dictionary<string, string> files) =>
        files.Where(kv => !string.Equals(kv.Key, kv.Value, StringComparison.OrdinalIgnoreCase)).Select(kv => kv.Value).ToList();

    public static void DeleteTempAudio(IEnumerable<string> paths)
    {
        foreach (var p in paths)
            try { File.Delete(p); } catch { /* best effort */ }
    }

    private void UpdateState()
    {
        IsPlaying = _player.IsPlaying;
        var position = _player.Position;
        PlaybackText = HasPlayback && _playingFile is not null
            ? $"{Path.GetFileName(_playingFile)}  {position:hh\\:mm\\:ss}"
            : string.Empty;

        // Highlight the line being spoken right now.
        var current = HasPlayback && _playingFile is not null
            ? _segments.FirstOrDefault(s =>
                string.Equals(s.FilePath, _playingFile, StringComparison.OrdinalIgnoreCase) &&
                position >= s.Start && position < s.End)
            : null;
        if (ReferenceEquals(current, _activeSegment)) return;
        if (_activeSegment is not null) _activeSegment.IsActive = false;
        _activeSegment = current;
        if (current is not null) current.IsActive = true;
    }
}
