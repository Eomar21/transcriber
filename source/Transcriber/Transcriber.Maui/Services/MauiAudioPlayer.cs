#if WINDOWS
using Windows.Media.Core;
using Windows.Media.Playback;
#elif MACCATALYST
using AVFoundation;
using Foundation;
#endif
using Transcriber.ViewModels;

namespace Transcriber.Maui.Services;

/// <summary>Plays the converted audio with the operating system's own media player (WinRT on Windows, AVFoundation on macOS).</summary>
public sealed class MauiAudioPlayer : IAudioPlayer
{
    public event EventHandler? StateChanged;

    private IDispatcherTimer? _timer;
    private bool _playing;

    private void Raise() => StateChanged?.Invoke(this, EventArgs.Empty);

    // A few updates per second while playing so the player bar and the highlighted line follow along.
    private void StartTicking()
    {
        _timer ??= CreateTimer();
        _timer?.Start();
    }

    private IDispatcherTimer? CreateTimer()
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is null) return null;
        var timer = dispatcher.CreateTimer();
        timer.Interval = TimeSpan.FromMilliseconds(250);
        timer.Tick += (_, _) => Raise();
        return timer;
    }

    private void StopTicking() => _timer?.Stop();

    private void Ended()
    {
        _playing = false;
        StopTicking();
        MainThread.BeginInvokeOnMainThread(Raise);
    }

    public bool IsPlaying => _playing;

#if WINDOWS
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
#elif MACCATALYST
    private AVAudioPlayer? _player;

    public TimeSpan Position => _player is null ? TimeSpan.Zero : TimeSpan.FromSeconds(_player.CurrentTime);

    public void Play(string path, TimeSpan from)
    {
        Stop();
        var player = AVAudioPlayer.FromUrl(NSUrl.FromFilename(path), out var error);
        if (player is null || error is not null) { Raise(); return; }

        player.FinishedPlaying += (_, _) => Ended();
        player.CurrentTime = from.TotalSeconds;
        player.Play();
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
            _player.Stop();
            _player.Dispose();
            _player = null;
        }
        Raise();
    }
#else
    public TimeSpan Position => TimeSpan.Zero;
    public void Play(string path, TimeSpan from) { }
    public void Pause() { }
    public void Resume() { }
    public void Stop() { }
#endif
}
