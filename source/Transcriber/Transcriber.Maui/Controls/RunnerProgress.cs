using System.Diagnostics;
using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;

namespace Transcriber.Maui.Controls;

/// <summary>
/// A progress bar with a small pixel-art runner on it. The runner runs along the bar as the work advances.
/// When the work ends, a burst of sparks plays at the end of the bar.
/// The pictures are the sprite sheets in Resources/Raw: eight frames of the run and six frames of the burst.
/// </summary>
public sealed class RunnerProgress : ContentView
{
    private const int RunnerCell = 32, RunnerFrames = 8, SparkCell = 48, SparkFrames = 6;
    private const double RunFramesPerSecond = 12, SparkSeconds = 0.7;

    public static readonly BindableProperty ProgressProperty = BindableProperty.Create(
        nameof(Progress), typeof(double), typeof(RunnerProgress), 0.0,
        propertyChanged: (bindable, _, _) => ((RunnerProgress)bindable).OnProgressChanged());

    public static readonly BindableProperty TrackColorProperty = BindableProperty.Create(
        nameof(TrackColor), typeof(Color), typeof(RunnerProgress), Colors.Gray);

    public static readonly BindableProperty FillColorProperty = BindableProperty.Create(
        nameof(FillColor), typeof(Color), typeof(RunnerProgress), Colors.SlateBlue);

    private static SKImage? s_runner, s_spark;
    private static bool s_loading;

    private readonly SKCanvasView _canvas = new() { HeightRequest = 72 };
    private readonly IDispatcherTimer? _timer;
    private readonly Stopwatch _clock = Stopwatch.StartNew();
    private readonly bool _animate = AnimationsAreOn();

    private double _shown;               // where the runner is now, from 0 to 1. It follows Progress smoothly.
    private double _lastTick;
    private double? _sparkStart;         // seconds on the clock when the burst began
    private bool _finished;

    public RunnerProgress()
    {
        Content = _canvas;
        _canvas.PaintSurface += OnPaint;

        _timer = Dispatcher.CreateTimer();
        _timer.Interval = TimeSpan.FromMilliseconds(33);
        _timer.Tick += (_, _) => Tick();

        HandlerChanged += (_, _) => UpdateTimer();
        PropertyChanged += (_, e) => { if (e.PropertyName == nameof(IsVisible)) UpdateTimer(); };
    }

    /// <summary>A number from 0 to 1.</summary>
    public double Progress { get => (double)GetValue(ProgressProperty); set => SetValue(ProgressProperty, value); }

    public Color TrackColor { get => (Color)GetValue(TrackColorProperty); set => SetValue(TrackColorProperty, value); }

    public Color FillColor { get => (Color)GetValue(FillColorProperty); set => SetValue(FillColorProperty, value); }

    private bool Running => Handler is not null && IsVisible && _animate && !(_finished && SparkIsOver);

    private bool SparkIsOver => _sparkStart is { } start && _clock.Elapsed.TotalSeconds - start > SparkSeconds;

    private void OnProgressChanged()
    {
        var progress = Math.Clamp(Progress, 0, 1);
        if (progress < _shown - 0.5 || progress == 0)
        {
            // A new run starts from the beginning.
            _shown = 0;
            _sparkStart = null;
            _finished = false;
        }
        SemanticProperties.SetDescription(this, $"Progress {(int)Math.Round(progress * 100)} percent");

        if (!_animate)
        {
            _shown = progress;
            _finished = progress >= 1;
        }
        UpdateTimer();
        _canvas.InvalidateSurface();
    }

    private void UpdateTimer()
    {
        if (_timer is null) return;
        if (Running)
        {
            _lastTick = _clock.Elapsed.TotalSeconds;
            if (!_timer.IsRunning) _timer.Start();
            EnsureSpritesLoaded();
        }
        else if (_timer.IsRunning) _timer.Stop();
    }

    private void Tick()
    {
        var now = _clock.Elapsed.TotalSeconds;
        var dt = Math.Min(now - _lastTick, 0.1);
        _lastTick = now;

        var target = Math.Clamp(Progress, 0, 1);
        var gap = target - _shown;
        if (gap > 0)
        {
            // Ease towards the target. The work reports in jumps, and the runner should not jump.
            _shown += Math.Min(gap, Math.Max(gap * Math.Min(1, dt * 4), dt * 0.5));
        }
        else if (gap < 0) _shown = target;

        if (!_finished && target >= 1 && _shown >= 0.999)
        {
            _shown = 1;
            _finished = true;
            _sparkStart = now;
        }

        _canvas.InvalidateSurface();
        if (!Running) _timer?.Stop();
    }

    private void OnPaint(object? sender, SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        canvas.Clear();
        if (Width <= 0) return;

        var density = e.Info.Width / (float)Width;
        var pad = 24 * density;
        var trackHeight = 10 * density;
        var trackTop = e.Info.Height - 16 * density - trackHeight / 2;
        var trackWidth = e.Info.Width - 2 * pad;
        var shown = (float)Math.Clamp(_shown, 0, 1);

        using var paint = new SKPaint { IsAntialias = true };
        var track = new SKRect(pad, trackTop, pad + trackWidth, trackTop + trackHeight);
        paint.Color = TrackColor.ToSKColor();
        canvas.DrawRoundRect(track, trackHeight / 2, trackHeight / 2, paint);

        var fillRight = Math.Max(pad + trackHeight, pad + shown * trackWidth);
        paint.Color = FillColor.ToSKColor();
        canvas.DrawRoundRect(new SKRect(pad, trackTop, fillRight, trackTop + trackHeight), trackHeight / 2, trackHeight / 2, paint);

        if (s_runner is null) return;

        // Whole-number scaling keeps the pixels square. The runner stands on the bar.
        var scale = Math.Max(1, (int)Math.Round(density * 1.6));
        var sampling = new SKSamplingOptions(SKFilterMode.Nearest, SKMipmapMode.None);
        var x = pad + shown * trackWidth;

        var frame = _finished ? 0 : (int)(_clock.Elapsed.TotalSeconds * RunFramesPerSecond) % RunnerFrames;
        if (!_animate) frame = 0;
        var runnerBox = SKRect.Create(x - RunnerCell * scale / 2f, trackTop - (RunnerCell - 1) * scale, RunnerCell * scale, RunnerCell * scale);
        canvas.DrawImage(s_runner, SKRect.Create(frame * RunnerCell, 0, RunnerCell, RunnerCell), runnerBox, sampling);

        if (s_spark is not null && _sparkStart is { } start && _animate)
        {
            var sparkFrame = (int)((_clock.Elapsed.TotalSeconds - start) / (SparkSeconds / SparkFrames));
            if (sparkFrame < SparkFrames)
            {
                var size = SparkCell * scale;
                var top = Math.Max(0, trackTop - size * 0.55f);
                canvas.DrawImage(s_spark, SKRect.Create(sparkFrame * SparkCell, 0, SparkCell, SparkCell),
                    SKRect.Create(x - size / 2f + 8 * scale, top, size, size), sampling);
            }
        }
    }

    private void EnsureSpritesLoaded()
    {
        if (s_runner is not null || s_loading) return;
        s_loading = true;
        _ = LoadAsync();

        async Task LoadAsync()
        {
            try
            {
                s_runner = await LoadImageAsync("runner.png");
                s_spark = await LoadImageAsync("spark.png");
            }
            catch (Exception)
            {
                // Without the pictures the control is a plain bar.
            }
            finally
            {
                s_loading = false;
                Dispatcher.Dispatch(_canvas.InvalidateSurface);
            }
        }
    }

    private static async Task<SKImage> LoadImageAsync(string name)
    {
        await using var stream = await FileSystem.OpenAppPackageFileAsync(name);
        using var data = SKData.Create(stream);
        return SKImage.FromEncodedData(data);
    }

    /// <summary>People who turn off animations in the system settings get a bar that does not move on its own.</summary>
    private static bool AnimationsAreOn()
    {
#if WINDOWS
        try { return new Windows.UI.ViewManagement.UISettings().AnimationsEnabled; }
        catch (Exception) { return true; }
#else
        return true;
#endif
    }
}
