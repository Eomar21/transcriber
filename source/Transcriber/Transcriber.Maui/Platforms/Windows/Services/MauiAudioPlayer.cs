using Windows.Media.Core;
using Windows.Media.Playback;

namespace Transcriber.Maui.Services;

// The Windows player, built on the WinRT MediaPlayer. The shared state is in Services/MauiAudioPlayer.cs.
public sealed partial class MauiAudioPlayer
{
    private MediaPlayer? _player;

    public TimeSpan Position => _player?.PlaybackSession.Position ?? TimeSpan.Zero;

    public void Play(string path, TimeSpan from)
    {
        Stop();
        var player = new MediaPlayer { AutoPlay = false, Source = MediaSource.CreateFromUri(new Uri(path)) };
        // The position can only be set once the file has opened.
        player.MediaOpened += (_, _) =>
        {
            player.PlaybackSession.Position = from;
            player.Play();
        };
        player.MediaEnded += (_, _) => Ended();
        player.MediaFailed += (_, _) => Ended();
        _player = player;
        _playing = true;
        StartTicking();
        Raise();
    }

    public void Pause()
    {
        _player?.Pause();
        _playing = false;
        Raise();
    }

    public void Resume()
    {
        if (_player is null) return;
        _player.Play();
        _playing = true;
        StartTicking();
        Raise();
    }

    public void Stop()
    {
        StopTicking();
        _playing = false;
        if (_player is not null)
        {
            _player.Pause();
            _player.Dispose();
            _player = null;
        }
        Raise();
    }
}
