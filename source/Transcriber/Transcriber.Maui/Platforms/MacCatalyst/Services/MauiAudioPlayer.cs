using AVFoundation;
using Foundation;

namespace Transcriber.Maui.Services;

// The macOS player, built on AVFoundation. The shared state is in Services/MauiAudioPlayer.cs.
public sealed partial class MauiAudioPlayer
{
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
}
