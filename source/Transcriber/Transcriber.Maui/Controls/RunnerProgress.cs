using System.Diagnostics;
using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;
using Transcriber.ViewModels.Progress;

namespace Transcriber.Maui.Controls;

/// <summary>
/// A progress bar with a small pixel-art character on it. The character moves along the bar as the work advances.
/// When the work ends, a burst of sparks plays at the end of the bar.
/// The <see cref="Look"/> says which sprite sheet, which colours and which decoration (for example a wave) the bar has.
/// The pictures are the sprite sheets in Resources/Raw. All looks share the burst: six frames.
/// </summary>
public sealed class RunnerProgress : ContentView
{
    private const int SparkCell = 48, SparkFrames = 6;
    private const double SparkSeconds = 0.7;
    private const string SparkSprite = "spark.png";

    public static readonly BindableProperty ProgressProperty = BindableProperty.Create(
        nameof(Progress), typeof(double), typeof(RunnerProgress), 0.0,
        propertyChanged: (bindable, _, _) => ((RunnerProgress)bindable).OnProgressChanged());

    public static readonly BindableProperty TrackColorProperty = BindableProperty.Create(
        nameof(TrackColor), typeof(Color), typeof(RunnerProgress), Colors.Gray);

    public static readonly BindableProperty FillColorProperty = BindableProperty.Create(
        nameof(FillColor), typeof(Color), typeof(RunnerProgress), Colors.SlateBlue);

    /// <summary>The look of the bar. When it is not set, the bar has the runner and uses <see cref="FillColor"/>.</summary>
    public static readonly BindableProperty LookProperty = BindableProperty.Create(
        nameof(Look), typeof(ProgressLook), typeof(RunnerProgress), null,
        propertyChanged: (bindable, _, _) => ((RunnerProgress)bindable).OnLookChanged());

    // Sprite sheets by file name. They are shared by all bars and are used only on the UI thread.
    private static readonly Dictionary<string, SKImage> s_sprites = new();
    private static readonly HashSet<string> s_loading = new();

    private readonly SKCanvasView _canvas = new() { HeightRequest = 84 };
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

    public ProgressLook? Look { get => (ProgressLook?)GetValue(LookProperty); set => SetValue(LookProperty, value); }

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

    private void OnLookChanged()
    {
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

        var look = Look ?? ProgressLooks.Runner;
        var density = e.Info.Width / (float)Width;
        var pad = 24 * density;
        var trackHeight = 10 * density;
        var trackTop = e.Info.Height - 16 * density - trackHeight / 2;
        var trackWidth = e.Info.Width - 2 * pad;
        var shown = (float)Math.Clamp(_shown, 0, 1);
        var seconds = (float)_clock.Elapsed.TotalSeconds;
        var fill = Look is null ? FillColor.ToSKColor() : ParseColor(IsDark() ? look.FillDark : look.Fill);

        using var paint = new SKPaint { IsAntialias = true };
        var track = new SKRect(pad, trackTop, pad + trackWidth, trackTop + trackHeight);
        paint.Color = TrackColor.ToSKColor();
        canvas.DrawRoundRect(track, trackHeight / 2, trackHeight / 2, paint);

        var fillRight = Math.Max(pad + trackHeight, pad + shown * trackWidth);
        paint.Color = fill;
        canvas.DrawRoundRect(new SKRect(pad, trackTop, fillRight, trackTop + trackHeight), trackHeight / 2, trackHeight / 2, paint);

        // Whole-number scaling keeps the pixels square. The character stands on the bar, or on the wave.
        var scale = Math.Max(1, (int)Math.Round(density * 1.6));
        var x = pad + shown * trackWidth;
        var lift = 0f;
        if (look.Decoration == TrackDecoration.Wave)
        {
            var wave = new Wave(density, seconds, RiderX: x);
            DrawWave(canvas, paint, wave, pad + trackHeight / 2, fillRight, trackTop, trackHeight, fill);
            lift = wave.Height(x - Wave.RiderOffset * density, pad + trackHeight / 2, fillRight);
        }
        else if (look.Decoration == TrackDecoration.StartAndFinish)
        {
            DrawStartAndFinish(canvas, paint, density, seconds, pad, pad + trackWidth, trackTop, trackHeight);
        }

        if (!s_sprites.TryGetValue(look.Sprite, out var sprite)) return;

        var sampling = new SKSamplingOptions(SKFilterMode.Nearest, SKMipmapMode.None);
        var frame = _finished || !_animate ? 0 : (int)(_clock.Elapsed.TotalSeconds * look.FramesPerSecond) % look.Frames;
        var box = SKRect.Create(x - look.Cell * scale / 2f, trackTop - lift - (look.Cell - 1) * scale, look.Cell * scale, look.Cell * scale);
        canvas.DrawImage(sprite, SKRect.Create(frame * look.Cell, 0, look.Cell, look.Cell), box, sampling);

        if (s_sprites.TryGetValue(SparkSprite, out var spark) && _sparkStart is { } start && _animate)
        {
            var sparkFrame = (int)((_clock.Elapsed.TotalSeconds - start) / (SparkSeconds / SparkFrames));
            if (sparkFrame < SparkFrames)
            {
                var size = SparkCell * scale;
                var top = Math.Max(0, trackTop - lift - size * 0.55f);
                canvas.DrawImage(spark, SKRect.Create(sparkFrame * SparkCell, 0, SparkCell, SparkCell),
                    SKRect.Create(x - size / 2f + 8 * scale, top, size, size), sampling);
            }
        }
    }

    /// <summary>The wave on the filled part of the bar: a moving surface that swells under the rider and is flat at both ends.</summary>
    private readonly record struct Wave(float Density, float Seconds, float RiderX)
    {
        /// <summary>How far behind the front of the bar the rider stands, in density-independent units.</summary>
        public const float RiderOffset = 8;

        public float Height(float x, float left, float right)
        {
            var d = Density;
            var surface = 0.55f + 0.45f * MathF.Sin(2 * MathF.PI * x / (64 * d) - Seconds * 3.2f);
            var swell = 1 + 0.7f * MathF.Exp(-MathF.Pow((x - (RiderX - RiderOffset * d)) / (34 * d), 2));
            var start = Math.Clamp((x - left) / (28 * d), 0, 1);       // flat where the bar starts
            var end = Math.Clamp((right - x) / (RiderOffset * d), 0, 1);   // flat at the front of the bar
            return 3.4f * d * surface * swell * start * end;
        }
    }

    private static void DrawWave(SKCanvas canvas, SKPaint paint, Wave wave, float left, float right, float trackTop, float trackHeight, SKColor fill)
    {
        if (right - left < 4) return;
        var bottom = trackTop + trackHeight / 2;
        var step = Math.Max(2f, wave.Density * 2);

        using var path = new SKPath();
        path.MoveTo(left, bottom);
        var xs = new List<float>();
        for (var x = left; x < right; x += step) xs.Add(x);
        xs.Add(right);
        foreach (var x in xs) path.LineTo(x, trackTop - wave.Height(x, left, right));
        path.LineTo(right, bottom);
        path.Close();

        paint.Style = SKPaintStyle.Fill;
        paint.Color = fill;
        canvas.DrawPath(path, paint);

        // A light line on the crest looks like foam.
        using var crest = new SKPath();
        crest.MoveTo(xs[0], trackTop - wave.Height(xs[0], left, right));
        foreach (var x in xs.Skip(1)) crest.LineTo(x, trackTop - wave.Height(x, left, right));
        paint.Style = SKPaintStyle.Stroke;
        paint.StrokeWidth = Math.Max(1f, 1.4f * wave.Density);
        paint.StrokeCap = SKStrokeCap.Round;
        paint.Color = SKColors.White.WithAlpha(150);
        canvas.DrawPath(crest, paint);
        paint.Style = SKPaintStyle.Fill;
    }

    /// <summary>
    /// A white start line with a green flag at the beginning of the bar, and a checkered finish line with a checkered flag at the end.
    /// The lines cross the bar. They are drawn under the runner, so the runner passes in front of them.
    /// </summary>
    private static void DrawStartAndFinish(SKCanvas canvas, SKPaint paint, float d, float seconds, float startX, float finishX, float trackTop, float trackHeight)
    {
        var overhang = 3 * d;
        var top = trackTop - overhang;
        var bottom = trackTop + trackHeight + overhang;
        var pole = SKColor.Parse("#7A7F8E");
        var dark = SKColor.Parse("#22222E");
        var poleHeight = 30 * d;
        var flagW = 13 * d;
        var flagH = 8 * d;

        void Pole(float x)
        {
            paint.Style = SKPaintStyle.Fill;
            paint.Color = pole;
            canvas.DrawRect(x - 0.75f * d, top - poleHeight, 1.5f * d, poleHeight + (bottom - top), paint);
        }

        // A flag that waves: its free edge moves up and down a little.
        void Flag(float x, Action<float, float, float, float> paintFlag)
        {
            var flagTop = top - poleHeight;
            paintFlag(x + 0.75f * d, flagTop, flagW, flagH + 1.2f * d * MathF.Sin(seconds * 6));
        }

        // Start line: a white stripe with a dark edge, and a green flag.
        Pole(startX);
        paint.Color = dark;
        canvas.DrawRect(startX - 2.2f * d, top, 4.4f * d, bottom - top, paint);
        paint.Color = SKColors.White;
        canvas.DrawRect(startX - 1.5f * d, top + 0.7f * d, 3f * d, bottom - top - 1.4f * d, paint);
        Flag(startX, (x, y, w, h) =>
        {
            paint.Color = SKColor.Parse("#2FB35A");
            canvas.DrawRect(x, y, w, h, paint);
        });

        // Finish line: two columns of black and white squares across the bar, and a checkered flag.
        Pole(finishX);
        var squares = 4;
        var cell = (bottom - top) / squares;
        for (var column = 0; column < 2; column++)
            for (var row = 0; row < squares; row++)
            {
                paint.Color = (row + column) % 2 == 0 ? dark : SKColors.White;
                canvas.DrawRect(finishX - cell + column * cell, top + row * cell, cell, cell, paint);
            }
        Flag(finishX, (x, y, w, h) =>
        {
            var cols = 4;
            var rows = 3;
            for (var column = 0; column < cols; column++)
                for (var row = 0; row < rows; row++)
                {
                    paint.Color = (row + column) % 2 == 0 ? dark : SKColors.White;
                    canvas.DrawRect(x + column * w / cols, y + row * h / rows, w / cols + 0.5f, h / rows + 0.5f, paint);
                }
        });
    }

    private static SKColor ParseColor(string hex) => Color.FromArgb(hex).ToSKColor();

    private static bool IsDark() => Application.Current?.RequestedTheme == AppTheme.Dark;

    private void EnsureSpritesLoaded()
    {
        foreach (var name in new[] { (Look ?? ProgressLooks.Runner).Sprite, SparkSprite })
        {
            if (s_sprites.ContainsKey(name) || !s_loading.Add(name)) continue;
            _ = LoadAsync(name);
        }

        async Task LoadAsync(string name)
        {
            try
            {
                s_sprites[name] = await LoadImageAsync(name);
            }
            catch (Exception)
            {
                // Without the picture the control draws the bar without that sprite.
            }
            finally
            {
                s_loading.Remove(name);
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
