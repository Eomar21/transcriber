using Transcriber.ViewModels;

namespace Transcriber.Maui.Services;

/// <summary>
/// Plays the converted audio with the operating system's own media player. This file holds the state and the timer that
/// both platforms share. The player itself is in <c>Platforms/Windows/Services</c> (WinRT) and
/// <c>Platforms/MacCatalyst/Services</c> (AVFoundation).
/// </summary>
public sealed partial class MauiAudioPlayer : IAudioPlayer
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
}
