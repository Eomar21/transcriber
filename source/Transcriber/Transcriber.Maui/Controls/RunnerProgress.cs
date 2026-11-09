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
    private const double ApproachSeconds = 0.4;       // the second character walks the last steps to the first
    private const int KissDistance = 17;                      // pixels of the finale picture between the two characters
    private const float KissCellOffset = 17.5f;               // pixels from the left edge of the finale picture to the main character

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
    private double? _approachStart;      // seconds on the clock when the characters reached the end of the bar
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

    private bool SparkIsOver => _sparkStart is { } start && _clock.Elapsed.TotalSeconds - start > Math.Max(SparkSeconds, (Look ?? ProgressLooks.Runner).Finale?.Seconds ?? 0) + 0.2;

    private void OnProgressChanged()
    {
        var progress = Math.Clamp(Progress, 0, 1);
        if (progress < _shown - 0.5 || progress == 0)
        {
            // A new run starts from the beginning.
            _shown = 0;
            _sparkStart = null;
            _approachStart = null;
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
            // With a finale, the second character waits and the first one runs the last steps. Then the finale starts.
            if ((Look ?? ProgressLooks.Runner).Finale is null) _sparkStart = now;
            else _approachStart = now;
        }
        else if (_approachStart is { } approach && _sparkStart is null && now - approach >= ApproachSeconds)
        {
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
        var left = look.Decoration == TrackDecoration.Volcano ? 60 * density : pad;   // the volcano needs room at the start
        var trackHeight = 10 * density;
        var trackTop = e.Info.Height - 16 * density - trackHeight / 2;
        var trackWidth = e.Info.Width - left - pad;
        var shown = (float)Math.Clamp(_shown, 0, 1);
        var seconds = (float)_clock.Elapsed.TotalSeconds;
        var fill = Look is null ? FillColor.ToSKColor() : ParseColor(IsDark() ? look.FillDark : look.Fill);

        using var paint = new SKPaint { IsAntialias = true };
        var track = new SKRect(left, trackTop, left + trackWidth, trackTop + trackHeight);
        paint.Color = TrackColor.ToSKColor();
        canvas.DrawRoundRect(track, trackHeight / 2, trackHeight / 2, paint);

        var fillRight = Math.Max(left + trackHeight, left + shown * trackWidth);
        if (look.Decoration == TrackDecoration.Volcano)
        {
            DrawLava(canvas, paint, density, seconds, left, fillRight, trackTop, trackHeight);
        }
        else
        {
            paint.Color = fill;
            canvas.DrawRoundRect(new SKRect(left, trackTop, fillRight, trackTop + trackHeight), trackHeight / 2, trackHeight / 2, paint);
        }

        // Whole-number scaling keeps the pixels square. The character stands on the bar, or on the wave.
        var scale = Math.Max(1, (int)Math.Round(density * 1.6));
        // With a companion, the companion runs in front. Both reach the end of the bar, the companion first.
        var companion = look.Companion;
        var lead = companion is null ? 0f : (float)companion.Lead * density;
        var x = left + shown * (trackWidth - lead);
        var companionX = left + lead + shown * (trackWidth - lead);
        if (companion is not null && _approachStart is { } approach)
            x += (float)Math.Clamp((seconds - approach) / ApproachSeconds, 0, 1) * (lead - KissDistance * scale);
        var lift = 0f;
        if (look.Decoration == TrackDecoration.Wave)
        {
            var wave = new Wave(density, seconds, RiderX: x);
            DrawWave(canvas, paint, wave, left + trackHeight / 2, fillRight, trackTop, trackHeight, fill);
            lift = wave.Height(x - Wave.RiderOffset * density, left + trackHeight / 2, fillRight);
        }
        else if (look.Decoration == TrackDecoration.StartAndFinish)
        {
            DrawStartAndFinish(canvas, paint, density, seconds, left, left + trackWidth, trackTop, trackHeight);
        }
        else if (look.Decoration == TrackDecoration.Volcano)
        {
            DrawVolcano(canvas, paint, density, seconds, left, trackTop, trackHeight);
            DrawSafePost(canvas, paint, density, seconds, left + trackWidth, trackTop, trackHeight);
        }

        if (!s_sprites.TryGetValue(look.Sprite, out var sprite)) return;

        var sampling = new SKSamplingOptions(SKFilterMode.Nearest, SKMipmapMode.None);
        // At the finale, one picture of the two characters takes the place of both.
        var finale = look.Finale;
        var finaleSheet = finale is not null && s_sprites.TryGetValue(finale.Sprite, out var finaleImage) ? finaleImage : null;
        var atFinale = finale is not null && finaleSheet is not null && _sparkStart is not null && _animate;
        // The main character runs until the finish, and also while he walks the last steps to the companion.
        var runs = _animate && (!_finished || (_approachStart is not null && _sparkStart is null));

        if (!atFinale && companion is not null && s_sprites.TryGetValue(companion.Sprite, out var companionSheet))
        {
            var companionFrame = runs || (_animate && !_finished) ? (int)(seconds * look.FramesPerSecond) % companion.Frames : companion.Frames;
            var companionBox = SKRect.Create(companionX - look.Cell * scale / 2f, trackTop - lift - (look.Cell - 1) * scale, look.Cell * scale, look.Cell * scale);
            canvas.DrawImage(companionSheet, SKRect.Create(companionFrame * look.Cell, 0, look.Cell, look.Cell), companionBox, sampling);
        }

        if (atFinale)
        {
            var index = Math.Min(finale!.Frames - 1, (int)((seconds - _sparkStart!.Value) / (finale.Seconds / finale.Frames)));
            var finaleBox = SKRect.Create(x - KissCellOffset * scale, trackTop - lift - (finale.Cell - 1) * scale, finale.Cell * scale, finale.Cell * scale);
            canvas.DrawImage(finaleSheet!, SKRect.Create(index * finale.Cell, 0, finale.Cell, finale.Cell), finaleBox, sampling);
        }
        else
        {
            var frame = runs ? (int)(seconds * look.FramesPerSecond) % look.Frames : 0;
            var box = SKRect.Create(x - look.Cell * scale / 2f, trackTop - lift - (look.Cell - 1) * scale, look.Cell * scale, look.Cell * scale);
            canvas.DrawImage(sprite, SKRect.Create(frame * look.Cell, 0, look.Cell, look.Cell), box, sampling);
        }

        // The sparks go off at the end of the bar. With a companion, that is where the companion stands.
        var sparkX = companion is null ? x : companionX;
        if (s_sprites.TryGetValue(SparkSprite, out var spark) && _sparkStart is { } start && _animate)
        {
            var sparkFrame = (int)((_clock.Elapsed.TotalSeconds - start) / (SparkSeconds / SparkFrames));
            if (sparkFrame < SparkFrames)
            {
                var size = SparkCell * scale;
                var top = Math.Max(0, trackTop - lift - size * 0.55f);
                canvas.DrawImage(spark, SKRect.Create(sparkFrame * SparkCell, 0, SparkCell, SparkCell),
                    SKRect.Create(sparkX - size / 2f + 8 * scale, top, size, size), sampling);
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

    /// <summary>
    /// The filled part of the bar as lava: hot and yellow at the volcano, darker and redder at the front.
    /// Bubbles pulse on the top edge, and bright streaks flow along the bar.
    /// </summary>
    private static void DrawLava(SKCanvas canvas, SKPaint paint, float d, float seconds, float left, float right, float trackTop, float trackHeight)
    {
        var length = right - left;
        if (length < 2) return;
        var radius = trackHeight / 2;
        var rect = new SKRect(left, trackTop, right, trackTop + trackHeight);

        using var shader = SKShader.CreateLinearGradient(
            new SKPoint(left, 0), new SKPoint(left + Math.Max(length, 60 * d), 0),
            [SKColor.Parse("#FFD23F"), SKColor.Parse("#FF6A1A"), SKColor.Parse("#C8300F")],
            [0f, 0.45f, 1f], SKShaderTileMode.Clamp);
        paint.Style = SKPaintStyle.Fill;
        paint.Shader = shader;
        canvas.DrawRoundRect(rect, radius, radius, paint);

        // Bubbles pulse on the top edge.
        const float spacing = 17;
        for (var k = 0; (k + 0.5f) * spacing * d < length - 6 * d; k++)
        {
            var r = d * (1.2f + 1.0f * MathF.Abs(MathF.Sin(seconds * 2.2f + k * 1.7f)));
            canvas.DrawCircle(left + (k + 0.5f) * spacing * d + 4 * d, trackTop + 0.5f * d, r, paint);
        }
        paint.Shader = null;

        // Bright streaks flow away from the volcano.
        canvas.Save();
        canvas.ClipRoundRect(new SKRoundRect(rect, radius, radius), antialias: true);
        paint.Color = SKColor.Parse("#FFF0A8").WithAlpha(210);
        for (var k = 0; k < 40; k++)
        {
            var offset = (k * 26 * d + seconds * 34 * d) % (length + 26 * d);
            if (offset > length) continue;
            var y = trackTop + (2.2f + (k % 3) * 2.4f) * d;
            canvas.DrawRoundRect(new SKRect(left + offset, y, left + offset + 7 * d, y + 1.6f * d), 0.8f * d, 0.8f * d, paint);
        }
        canvas.Restore();
    }

    /// <summary>A volcano at the start of the bar. It spits out lava and a stream of lava runs from the crater into the bar.</summary>
    private static void DrawVolcano(SKCanvas canvas, SKPaint paint, float d, float seconds, float left, float trackTop, float trackHeight)
    {
        var baseY = trackTop + trackHeight;
        var topY = baseY - 36 * d;
        var cx = left - 30 * d;
        var halfBase = 27 * d;
        var halfTop = 6 * d;
        var pulse = 0.5f + 0.5f * MathF.Sin(seconds * 5);

        // A glow around the crater.
        using (var glow = SKShader.CreateRadialGradient(new SKPoint(cx, topY), 26 * d,
            [SKColor.Parse("#FF9A3C").WithAlpha((byte)(70 + 40 * pulse)), SKColor.Parse("#FF9A3C").WithAlpha(0)], SKShaderTileMode.Clamp))
        {
            paint.Shader = glow;
            canvas.DrawCircle(cx, topY, 26 * d, paint);
            paint.Shader = null;
        }

        // The cone, with a lighter left side and some cracks.
        paint.Style = SKPaintStyle.Fill;
        using var cone = new SKPath();
        cone.MoveTo(cx - halfBase, baseY);
        cone.LineTo(cx - halfTop, topY);
        cone.LineTo(cx + halfTop, topY);
        cone.LineTo(cx + halfBase, baseY);
        cone.Close();
        paint.Color = SKColor.Parse("#4A3A42");
        canvas.DrawPath(cone, paint);
        using var lit = new SKPath();
        lit.MoveTo(cx - halfBase, baseY);
        lit.LineTo(cx - halfTop, topY);
        lit.LineTo(cx - halfTop + 7 * d, topY);
        lit.LineTo(cx - halfBase + 16 * d, baseY);
        lit.Close();
        paint.Color = SKColor.Parse("#5E4A52");
        canvas.DrawPath(lit, paint);

        // The stream: it runs down the right side of the cone and into the bar.
        paint.Style = SKPaintStyle.Stroke;
        paint.StrokeCap = SKStrokeCap.Round;
        paint.StrokeWidth = 4.5f * d;
        paint.Color = SKColor.Parse("#FF6A1A");
        using var stream = new SKPath();
        stream.MoveTo(cx + halfTop - 1 * d, topY + 1 * d);
        stream.LineTo(cx + halfBase - 3 * d, baseY - 2.5f * d);
        canvas.DrawPath(stream, paint);
        paint.StrokeWidth = 2f * d;
        paint.Color = SKColor.Parse("#FFD23F");
        canvas.DrawPath(stream, paint);
        paint.Style = SKPaintStyle.Fill;
        paint.Color = SKColor.Parse("#FF6A1A");
        canvas.DrawRect(cx + halfBase - 5 * d, trackTop, left - (cx + halfBase - 5 * d) + 2 * d, trackHeight, paint);

        // The crater.
        paint.Color = SKColor.Parse("#FF8A2A");
        canvas.DrawOval(new SKRect(cx - halfTop - 1 * d, topY - 2.4f * d, cx + halfTop + 1 * d, topY + 2.4f * d), paint);
        paint.Color = SKColor.Parse("#FFE066").WithAlpha((byte)(170 + 85 * pulse));
        canvas.DrawOval(new SKRect(cx - halfTop + 2 * d, topY - 1.2f * d, cx + halfTop - 2 * d, topY + 1.2f * d), paint);

        // Blobs of lava are thrown out of the crater and fall back.
        const int blobs = 10;
        for (var i = 0; i < blobs; i++)
        {
            var life = (seconds * 0.75f + i / (float)blobs) % 1f;
            var spread = ((i * 7919 % 100) / 100f - 0.5f) * 2;               // from -1 to 1, and it is the same for each blob
            var height = (14 + (i * 37 % 10) * 1.3f) * d;
            var bx = cx + spread * 22 * d * life;
            var by = topY - height * 4 * life * (1 - life);
            paint.Color = (life < 0.5f ? SKColor.Parse("#FFE066") : SKColor.Parse("#FF7A2A")).WithAlpha((byte)(255 * (1 - life * life * life)));
            canvas.DrawCircle(bx, by, d * (2.3f - 1.2f * life), paint);
        }
    }

    /// <summary>A small post with a green flag at the end of the bar: the safe place.</summary>
    private static void DrawSafePost(SKCanvas canvas, SKPaint paint, float d, float seconds, float x, float trackTop, float trackHeight)
    {
        var bottom = trackTop + trackHeight + 3 * d;
        var top = trackTop - 26 * d;
        paint.Style = SKPaintStyle.Fill;
        paint.Color = SKColor.Parse("#8A5A34");
        canvas.DrawRect(x - 0.9f * d, top, 1.8f * d, bottom - top, paint);
        paint.Color = SKColor.Parse("#2FB35A");
        canvas.DrawRect(x + 0.9f * d, top, 12 * d, 7 * d + 1.2f * d * MathF.Sin(seconds * 6), paint);
    }

    private static SKColor ParseColor(string hex) => Color.FromArgb(hex).ToSKColor();

    private static bool IsDark() => Application.Current?.RequestedTheme == AppTheme.Dark;

    /// <summary>The sprite sheets that a look needs, and the burst.</summary>
    private static IEnumerable<string> SpriteNames(ProgressLook look)
    {
        yield return look.Sprite;
        if (look.Companion is { } companion) yield return companion.Sprite;
        if (look.Finale is { } finale) yield return finale.Sprite;
        yield return SparkSprite;
    }

    private void EnsureSpritesLoaded()
    {
        foreach (var name in SpriteNames(Look ?? ProgressLooks.Runner))
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
