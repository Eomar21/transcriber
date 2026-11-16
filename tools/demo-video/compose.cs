#:package SkiaSharp@3.119.1
#:property PublishAot=false
#:property JsonSerializerIsReflectionEnabledByDefault=true
using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using SkiaSharp;

// Builds the demo video from a screen recording of the real app and a timeline of what happened.
// Usage: dotnet run compose.cs <capture.mkv> <timeline.json> <output.mp4> [music.wav] [--stills 3,10,25]
// Every frame is drawn in code: an animated background, the window with a shadow, a camera that zooms to the active
// control, click ripples, captions, a title card and an end card. The frames go to ffmpeg, which encodes the video.
// The recording and the timeline come from the UI test DemoVideoTests. See README.md in this folder.

const int OutW = 1920, OutH = 1080, Fps = 60;
const double Intro = 4.2, Outro = 5.2, EnterTime = 1.0, LeaveTime = 0.8;

var positional = args.Where(a => !a.StartsWith("--")).ToArray();
if (positional.Length < 3) { Console.Error.WriteLine("Usage: compose.cs <capture.mkv> <timeline.json> <output.mp4> [music.wav] [--stills 3,10]"); return 1; }
var capturePath = positional[0];
var timeline = JsonSerializer.Deserialize<Timeline>(File.ReadAllText(positional[1]), new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
var outputPath = positional[2];
var musicPath = positional.Length > 3 ? positional[3] : null;
var stillsArg = Array.IndexOf(args, "--stills");
double[] stills = stillsArg >= 0 ? args[stillsArg + 1].Split(',').Select(s => double.Parse(s, CultureInfo.InvariantCulture)).ToArray() : [];
var ffmpeg = Environment.GetEnvironmentVariable("FFMPEG_EXE") ?? "ffmpeg";

// ---- Time map: the part of the recording that is used, without the cuts, with the fast parts sped up. ----
var srcStart = Math.Max(0, (timeline.Chapters.FirstOrDefault()?.T ?? 1.0) - 0.5);
var srcEnd = timeline.End;
var breaks = new SortedSet<double> { srcStart, srcEnd };
foreach (var c in timeline.Cuts) { breaks.Add(Math.Clamp(c.From, srcStart, srcEnd)); breaks.Add(Math.Clamp(c.To, srcStart, srcEnd)); }
foreach (var s in timeline.Speeds) { breaks.Add(Math.Clamp(s.From, srcStart, srcEnd)); breaks.Add(Math.Clamp(s.To, srcStart, srcEnd)); }
var pieces = new List<Piece>();
var b = breaks.ToArray();
var outAt = 0.0;
for (var i = 0; i + 1 < b.Length; i++)
{
    var mid = (b[i] + b[i + 1]) / 2;
    if (b[i + 1] - b[i] < 1e-6 || timeline.Cuts.Any(c => mid >= c.From && mid < c.To)) continue;
    var rate = 1.0;
    var sp = timeline.Speeds.FirstOrDefault(s => mid >= s.From && mid < s.To);
    if (sp is not null) rate = Math.Max(1.0, (sp.To - sp.From) / sp.Seconds);
    pieces.Add(new Piece(b[i], b[i + 1], rate, outAt));
    outAt += (b[i + 1] - b[i]) / rate;
}
var mainLength = outAt;

double MapOut(double src)
{
    foreach (var p in pieces)
        if (src < p.To) return p.OutFrom + Math.Max(0, src - p.From) / p.Rate;
    return mainLength;
}
double MapSrc(double outMain)
{
    foreach (var p in pieces)
        if (outMain < p.OutFrom + (p.To - p.From) / p.Rate) return p.From + (outMain - p.OutFrom) * p.Rate;
    return pieces[^1].To;
}
bool InCut(double src) => timeline.Cuts.Any(c => src >= c.From && src < c.To);

var mainStart = Intro - 0.9;                       // the window comes in just after the title has gone
var mainEnd = mainStart + mainLength;
var total = mainEnd + Outro - 0.6;
Console.WriteLine(string.Create(CultureInfo.InvariantCulture, $"Video length: {total:F2} seconds ({mainLength:F1} s of the recording)"));
if (args.Contains("--length")) return 0;
if (args.Contains("--debug"))
{
    foreach (var c in timeline.Clicks) Console.WriteLine(string.Create(CultureInfo.InvariantCulture, $"click at {c.T:F2} s of the recording is at {mainStart + MapOut(c.T):F2} s of the video"));
    foreach (var s in timeline.Speeds) Console.WriteLine(string.Create(CultureInfo.InvariantCulture, $"fast part {s.From:F1} to {s.To:F1} s of the recording becomes {s.Seconds:F1} s"));
    return 0;
}

// ---- The events in output time. ----
var clicks = timeline.Clicks.Where(c => c.T >= srcStart && c.T < srcEnd && !InCut(c.T)).Select(c => (T: mainStart + MapOut(c.T), c.X, c.Y)).ToList();
var focuses = timeline.Focus.Where(f => f.T >= srcStart && f.T < srcEnd && !InCut(f.T))
    .Select(f => (T: mainStart + MapOut(f.T), End: mainStart + MapOut(Math.Min(f.T + f.Hold, srcEnd)), f.X, f.Y, f.W, f.H, f.Zoom)).ToList();
var caps = timeline.Captions.Where(c => c.T0 >= srcStart && c.T0 < srcEnd).OrderBy(c => c.T0)
    .Select(c => (T0: mainStart + MapOut(c.T0), Len: Math.Max(2.4, c.T1 - c.T0), c.Text)).ToList();
var chapters = timeline.Chapters.Select(c => (T: mainStart + MapOut(c.T), c.Title)).ToList();
var speedBadges = pieces.Where(p => p.Rate > 1.3).Select(p => (From: mainStart + p.OutFrom, To: mainStart + p.OutFrom + (p.To - p.From) / p.Rate, p.Rate)).ToList();

// ---- Look. ----
var fontName = OperatingSystem.IsWindows() ? "Segoe UI" : "Helvetica Neue";
var regular = SKTypeface.FromFamilyName(fontName, SKFontStyleWeight.Normal, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright);
var semibold = SKTypeface.FromFamilyName(fontName, SKFontStyleWeight.SemiBold, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright);
var bold = SKTypeface.FromFamilyName(fontName, SKFontStyleWeight.Bold, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright);
var accent = SKColor.Parse("#FFD43B");
var violet = SKColor.Parse("#7C5CFF");
var magenta = SKColor.Parse("#D600AA");
var teal = SKColor.Parse("#2DD4BF");

var W = timeline.Width;
var H = timeline.Height;
var baseScale = Math.Min((OutW - 2 * 150.0) / W, (OutH - 2 * 100.0) / H);
const int Inset = 2;                               // the recording has a thin edge of the desktop. It is cropped.
var sampling = new SKSamplingOptions(SKCubicResampler.Mitchell);

// ---- Decoder: the recording as raw frames at 60 frames each second. ----
var decoder = new Process
{
    StartInfo = new ProcessStartInfo(ffmpeg)
    {
        UseShellExecute = false, RedirectStandardOutput = true, CreateNoWindow = true,
        ArgumentList = { "-loglevel", "error", "-i", capturePath, "-vf", $"setpts=PTS-STARTPTS,fps={Fps}", "-pix_fmt", "bgra", "-f", "rawvideo", "-" },
    },
};
decoder.Start();
var decoded = decoder.StandardOutput.BaseStream;
var frameBytes = new byte[W * H * 4];
var frameIndex = -1;
SKBitmap? sourceFrame = null;
SKImage? sourceImage = null;

bool ReadUntil(int index)
{
    while (frameIndex < index)
    {
        var read = 0;
        while (read < frameBytes.Length)
        {
            var n = decoded.Read(frameBytes, read, frameBytes.Length - read);
            if (n <= 0) return sourceImage is not null;
            read += n;
        }
        frameIndex++;
    }
    sourceImage?.Dispose();
    sourceFrame?.Dispose();
    sourceFrame = new SKBitmap(new SKImageInfo(W, H, SKColorType.Bgra8888, SKAlphaType.Opaque));
    System.Runtime.InteropServices.Marshal.Copy(frameBytes, 0, sourceFrame.GetPixels(), frameBytes.Length);
    sourceImage = SKImage.FromBitmap(sourceFrame);
    return true;
}

// ---- Encoder. ----
Process? encoder = null;
Stream? encoderIn = null;
var lastStill = stills.Length > 0 ? stills.Max() : -1;
if (stills.Length == 0)
{
    var psi = new ProcessStartInfo(ffmpeg) { UseShellExecute = false, RedirectStandardInput = true, RedirectStandardError = true, CreateNoWindow = true };
    foreach (var a in new[] { "-y", "-loglevel", "error", "-f", "rawvideo", "-pix_fmt", "bgra", "-s", $"{OutW}x{OutH}", "-r", Fps.ToString(), "-i", "-" }) psi.ArgumentList.Add(a);
    if (musicPath is not null) { psi.ArgumentList.Add("-i"); psi.ArgumentList.Add(musicPath); }
    foreach (var a in new[] { "-c:v", "libx264", "-preset", "slow", "-crf", Environment.GetEnvironmentVariable("DEMO_CRF") ?? "25", "-tune", "animation", "-pix_fmt", "yuv420p", "-movflags", "+faststart", "-t", total.ToString("F3", CultureInfo.InvariantCulture) }) psi.ArgumentList.Add(a);
    if (musicPath is not null)
        foreach (var a in new[] { "-c:a", "aac", "-b:a", "128k", "-af", $"afade=t=in:st=0:d=2,afade=t=out:st={(total - 3.5).ToString("F2", CultureInfo.InvariantCulture)}:d=3.5" }) psi.ArgumentList.Add(a);
    psi.ArgumentList.Add(outputPath);
    encoder = Process.Start(psi)!;
    encoder.ErrorDataReceived += (_, e) => { if (e.Data is not null) Console.Error.WriteLine(e.Data); };
    encoder.BeginErrorReadLine();
    encoderIn = encoder.StandardInput.BaseStream;
}

using var surface = SKSurface.Create(new SKImageInfo(OutW, OutH, SKColorType.Bgra8888, SKAlphaType.Premul));
var canvas = surface.Canvas;
var cam = new Camera(W / 2.0, H / 2.0, 1.0);
var frames = (int)Math.Ceiling(total * Fps);
var watch = Stopwatch.StartNew();
var stillDir = Path.GetDirectoryName(Path.GetFullPath(outputPath))!;

for (var i = 0; i < frames; i++)
{
    var t = i / (double)Fps;
    if (lastStill >= 0 && t > lastStill + 0.05) break;
    var mainT = t - mainStart;

    // Camera: it eases toward the active zoom target and back to the whole window.
    var target = (X: W / 2.0, Y: H / 2.0, Z: 1.0);
    foreach (var f in focuses)
        if (t >= f.T - 0.15 && t <= f.End)
        {
            var fitW = 0.9 * OutW / baseScale / Math.Max(f.W, 1);
            var fitH = 0.9 * OutH / baseScale / Math.Max(f.H, 1);
            var z = Math.Max(1.0, Math.Min(f.Zoom, Math.Min(fitW, fitH)));
            target = (f.X + f.W / 2.0, f.Y + f.H / 2.0, z);
        }
    cam.Update(target.X, target.Y, target.Z, 1.0 / Fps);

    var drawNeeded = stills.Length == 0 || stills.Any(s => Math.Abs(s - t) < 0.5 / Fps);
    if (!drawNeeded) continue;
    ReadUntil((int)Math.Round(MapSrc(Math.Clamp(mainT, 0, mainLength - 1e-6)) * Fps));

    DrawBackground(canvas, t);

    // The title card.
    var introAlpha = 1 - Smooth((t - (Intro - 1.7)) / 0.8);
    if (introAlpha > 0.001) DrawIntro(canvas, t, introAlpha);

    // The window.
    var enter = Smooth(mainT / EnterTime);
    var leave = Smooth((mainEnd - t) / LeaveTime);
    var visible = Math.Min(enter, leave);
    if (visible > 0.001 && sourceImage is not null)
    {
        var s = baseScale * cam.Z * (0.93 + 0.07 * visible);
        var rise = (1 - enter) * 90 - (1 - leave) * 60;
        var visW = OutW / s;
        var visH = OutH / s;
        var cx = visW < W ? Math.Clamp(cam.X, visW / 2, W - visW / 2) : W / 2.0;
        // The focus sits a little above the middle, so the caption does not cover it.
        var cy = visH < H ? Math.Clamp(cam.Y + 0.05 * visH, visH / 2, H - visH / 2) : H / 2.0;
        canvas.Save();
        canvas.Translate(OutW / 2f, (float)(OutH / 2 + rise));
        canvas.Scale((float)s);
        canvas.Translate((float)-cx, (float)-cy);
        var radius = (float)(12 / baseScale);
        var rect = new SKRect(Inset, 1, W - Inset, H - Inset);
        using (var shadow = new SKPaint { Color = new SKColor(0, 0, 0, (byte)(150 * visible)), MaskFilter = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, (float)(34 / baseScale)), IsAntialias = true })
        {
            canvas.Save();
            canvas.Translate(0, (float)(22 / baseScale));
            canvas.DrawRoundRect(rect, radius, radius, shadow);
            canvas.Restore();
        }
        canvas.Save();
        canvas.ClipRoundRect(new SKRoundRect(rect, radius, radius), antialias: true);
        using (var paint = new SKPaint { Color = SKColors.White.WithAlpha((byte)(255 * visible)), IsAntialias = true })
            canvas.DrawImage(sourceImage, 0, 0, sampling, paint);
        // The click ripples are drawn on the window, so they follow the zoom.
        foreach (var c in clicks)
            DrawRipple(canvas, c.X, c.Y, (t - c.T) / 0.7, 1 / (baseScale * cam.Z));
        // The pointer. It is hidden while the dialog is cut out of the recording, and while the window comes in or goes.
        var srcNow = MapSrc(Math.Clamp(mainT, 0, mainLength - 1e-6));
        if (visible > 0.97 && PointerAt(srcNow) is { } pointer)
        {
            var pressed = clicks.Any(c => t >= c.T - 0.02 && t < c.T + 0.16);
            DrawPointer(canvas, pointer.X, pointer.Y, 1 / (baseScale * cam.Z), pressed);
        }
        canvas.Restore();
        canvas.Restore();
    }

    // The labels would cover the window when the camera is close, so they fade out then.
    var labelAlpha = 1 - Smooth((cam.Z - 1.0) / 0.2);
    DrawChapter(canvas, t, labelAlpha);
    DrawSpeedBadge(canvas, t, labelAlpha);
    DrawCaptions(canvas, t);

    var outro = Smooth((t - (mainEnd - 0.2)) / 1.0);
    if (outro > 0.001) DrawOutro(canvas, t, outro);

    // The fade of the whole picture at the end.
    var fade = Smooth((total - t) / 0.9);
    if (fade < 0.999)
    {
        using var black = new SKPaint { Color = new SKColor(0, 0, 0, (byte)(255 * (1 - fade))) };
        canvas.DrawRect(0, 0, OutW, OutH, black);
    }

    if (stills.Length > 0)
    {
        using var img = surface.Snapshot();
        using var data = img.Encode(SKEncodedImageFormat.Png, 100);
        var path = Path.Combine(stillDir, string.Create(CultureInfo.InvariantCulture, $"still-{t:F1}.png"));
        File.WriteAllBytes(path, data.ToArray());
        Console.WriteLine("Wrote " + path);
    }
    else
    {
        using var pixmap = surface.PeekPixels();
        encoderIn!.Write(pixmap.GetPixelSpan());
        if (i % (Fps * 5) == 0) Console.WriteLine(string.Create(CultureInfo.InvariantCulture, $"{t:F0} of {total:F0} s ({watch.Elapsed.TotalSeconds:F0} s used)"));
    }
}

encoderIn?.Close();
encoder?.WaitForExit();
try { decoder.Kill(); } catch { }
if (encoder is not null) Console.WriteLine(encoder.ExitCode == 0 ? "Done: " + outputPath : "ffmpeg failed.");
return encoder?.ExitCode ?? 0;

// =============================== Drawing ===============================

static double Smooth(double k) { k = Math.Clamp(k, 0, 1); return k * k * (3 - 2 * k); }
static double EaseOutCubic(double k) { k = Math.Clamp(k, 0, 1); return 1 - Math.Pow(1 - k, 3); }

void DrawBackground(SKCanvas c, double t)
{
    using (var bg = new SKPaint { Shader = SKShader.CreateLinearGradient(new SKPoint(0, 0), new SKPoint(OutW, OutH), [SKColor.Parse("#0B0820"), SKColor.Parse("#1A1052"), SKColor.Parse("#12082E")], [0f, 0.55f, 1f], SKShaderTileMode.Clamp) })
        c.DrawRect(0, 0, OutW, OutH, bg);
    // Slow soft lights.
    void Glow(double x, double y, double r, SKColor color, byte alpha)
    {
        using var p = new SKPaint { Shader = SKShader.CreateRadialGradient(new SKPoint((float)x, (float)y), (float)r, [color.WithAlpha(alpha), color.WithAlpha(0)], [0f, 1f], SKShaderTileMode.Clamp) };
        c.DrawCircle((float)x, (float)y, (float)r, p);
    }
    Glow(380 + 140 * Math.Sin(t * 0.25), 300 + 90 * Math.Cos(t * 0.21), 760, violet, 95);
    Glow(1550 + 160 * Math.Cos(t * 0.19), 820 + 110 * Math.Sin(t * 0.27), 820, magenta, 70);
    Glow(1000 + 220 * Math.Sin(t * 0.14 + 1), 1100 + 40 * Math.Cos(t * 0.3), 700, teal, 40);
}

void DrawText(SKCanvas c, string text, double x, double y, float size, SKTypeface face, SKColor color, SKTextAlign align = SKTextAlign.Center)
{
    using var font = new SKFont(face, size) { Subpixel = true, Edging = SKFontEdging.SubpixelAntialias };
    using var p = new SKPaint { Color = color, IsAntialias = true };
    c.DrawText(text, (float)x, (float)y, align, font, p);
}

float Measure(string text, float size, SKTypeface face)
{
    using var font = new SKFont(face, size);
    return font.MeasureText(text);
}

void DrawWave(SKCanvas c, double cx, double cy, double t, double alpha, double width = 520)
{
    const int bars = 29;
    var step = width / bars;
    for (var i = 0; i < bars; i++)
    {
        var k = (i - bars / 2.0) / (bars / 2.0);
        var envelope = Math.Exp(-k * k * 2.4);
        var h = 14 + 120 * envelope * (0.55 + 0.45 * Math.Sin(t * 3.1 + i * 0.7)) * (0.7 + 0.3 * Math.Sin(t * 1.3 + i * 1.9));
        var x = cx - width / 2 + i * step + step / 2;
        using var p = new SKPaint
        {
            IsAntialias = true,
            Shader = SKShader.CreateLinearGradient(new SKPoint(0, (float)(cy - h)), new SKPoint(0, (float)(cy + h)), [violet.WithAlpha((byte)(255 * alpha)), teal.WithAlpha((byte)(255 * alpha))], null, SKShaderTileMode.Clamp),
        };
        c.DrawRoundRect(new SKRect((float)(x - step * 0.3), (float)(cy - h / 2), (float)(x + step * 0.3), (float)(cy + h / 2)), 6, 6, p);
    }
}

void DrawIntro(SKCanvas c, double t, double alpha)
{
    var a = (byte)(255 * alpha);
    var inWave = Smooth(t / 0.9);
    DrawWave(c, OutW / 2.0, 330, t, alpha * inWave);
    var rise = (1 - EaseOutCubic((t - 0.35) / 0.9)) * 40;
    var titleAlpha = (byte)(a * Smooth((t - 0.35) / 0.7));
    DrawText(c, "Transcriber", OutW / 2.0, 575 + rise, 150, bold, SKColors.White.WithAlpha(titleAlpha));
    var subAlpha = (byte)(a * Smooth((t - 0.9) / 0.7));
    DrawText(c, "Speech tools that stay on your computer.", OutW / 2.0, 655 + rise * 0.6, 46, regular, SKColor.Parse("#D8D2FF").WithAlpha(subAlpha));

    string[] chips = ["Transcribe", "Speaker labels", "Subtitles", "Practise speaking", "Works offline"];
    var widths = chips.Select(s => Measure(s, 30, semibold) + 56).ToArray();
    var gap = 22f;
    var x = (OutW - (widths.Sum() + gap * (chips.Length - 1))) / 2;
    for (var i = 0; i < chips.Length; i++)
    {
        var k = Smooth((t - 1.5 - i * 0.18) / 0.5);
        var rect = new SKRect(x, 760 + (float)((1 - k) * 24), x + widths[i], 760 + 64 + (float)((1 - k) * 24));
        using (var fill = new SKPaint { Color = new SKColor(255, 255, 255, (byte)(28 * k * alpha)), IsAntialias = true }) c.DrawRoundRect(rect, 32, 32, fill);
        using (var line = new SKPaint { Color = new SKColor(255, 255, 255, (byte)(70 * k * alpha)), IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 1.5f }) c.DrawRoundRect(rect, 32, 32, line);
        DrawText(c, chips[i], rect.MidX, rect.MidY + 11, 30, semibold, SKColors.White.WithAlpha((byte)(255 * k * alpha)));
        x += widths[i] + gap;
    }
}

void DrawOutro(SKCanvas c, double t, double alpha)
{
    var local = t - (mainEnd - 0.2);
    var a = (byte)(255 * alpha);
    using (var veil = new SKPaint { Color = new SKColor(11, 8, 32, (byte)(215 * alpha)) }) c.DrawRect(0, 0, OutW, OutH, veil);
    DrawWave(c, OutW / 2.0, 300, t, alpha);
    var rise = (1 - EaseOutCubic((local - 0.2) / 0.9)) * 36;
    DrawText(c, "Transcriber", OutW / 2.0, 520 + rise, 130, bold, SKColors.White.WithAlpha((byte)(a * Smooth((local - 0.2) / 0.6))));
    DrawText(c, "Free and open source. Windows and macOS.", OutW / 2.0, 600 + rise * 0.7, 44, regular, SKColor.Parse("#D8D2FF").WithAlpha((byte)(a * Smooth((local - 0.6) / 0.6))));
    var url = "github.com/Eomar21/transcriber";
    var w = Measure(url, 42, semibold) + 90;
    var k = Smooth((local - 1.2) / 0.6);
    var rect = new SKRect((float)(OutW / 2 - w / 2), 680, (float)(OutW / 2 + w / 2), 680 + 84);
    using (var fill = new SKPaint { Color = accent.WithAlpha((byte)(255 * k * alpha)), IsAntialias = true }) c.DrawRoundRect(rect, 42, 42, fill);
    DrawText(c, url, rect.MidX, rect.MidY + 14, 42, semibold, SKColor.Parse("#1A1052").WithAlpha((byte)(255 * k * alpha)));
}

// The place of the pointer at a time of the recording: between the samples it moves in a straight line.
(double X, double Y)? PointerAt(double src)
{
    var path = timeline.Mouse;
    if (path is null || path.Count == 0 || src < path[0].T || InCut(src)) return null;
    var last = path.FindLastIndex(m => m.T <= src);
    var a = path[last];
    if (last + 1 >= path.Count) return (a.X, a.Y);
    var b = path[last + 1];
    var span = b.T - a.T;
    var k = span > 0 && span < 0.25 ? Math.Clamp((src - a.T) / span, 0, 1) : 0;   // a long gap means it stood still
    return (a.X + (b.X - a.X) * k, a.Y + (b.Y - a.Y) * k);
}

void DrawPointer(SKCanvas c, double x, double y, double unit, bool pressed)
{
    var s = (float)(unit * 1.35 * (pressed ? 0.86 : 1.0));
    using var path = new SKPath();
    // An arrow with its tip at the point.
    path.MoveTo(0, 0); path.LineTo(0, 17); path.LineTo(4.2f, 13.1f); path.LineTo(7.2f, 19.6f); path.LineTo(10f, 18.4f); path.LineTo(7.1f, 12f); path.LineTo(12.4f, 12f); path.Close();
    c.Save();
    c.Translate((float)x, (float)y);
    c.Scale(s);
    using (var shadow = new SKPaint { Color = new SKColor(0, 0, 0, 120), MaskFilter = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, 1.6f), IsAntialias = true })
    {
        c.Save();
        c.Translate(1.2f, 2f);
        c.DrawPath(path, shadow);
        c.Restore();
    }
    using (var fill = new SKPaint { Color = SKColors.White, IsAntialias = true }) c.DrawPath(path, fill);
    using (var line = new SKPaint { Color = new SKColor(20, 16, 40), IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 1.3f, StrokeJoin = SKStrokeJoin.Round }) c.DrawPath(path, line);
    c.Restore();
}

void DrawRipple(SKCanvas c, double x, double y, double k, double unit)
{
    if (k < 0 || k > 1) return;
    var e = EaseOutCubic(k);
    var fade = 1 - k;
    var r = (18 + 60 * e) * unit;
    using var fill = new SKPaint { Color = accent.WithAlpha((byte)(95 * fade)), IsAntialias = true };
    using var ring = new SKPaint { Color = accent.WithAlpha((byte)(235 * fade)), IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = (float)((6 - 4 * k) * unit) };
    c.DrawCircle((float)x, (float)y, (float)(r * 0.55), fill);
    c.DrawCircle((float)x, (float)y, (float)r, ring);
}

void DrawCaptions(SKCanvas c, double t)
{
    for (var i = 0; i < caps.Count; i++)
    {
        var cap = caps[i];
        var end = cap.T0 + cap.Len;
        if (i + 1 < caps.Count) end = Math.Min(end, caps[i + 1].T0 - 0.1);
        var local = t - cap.T0;
        if (local < 0 || t > end + 0.01) continue;
        var inK = EaseOutCubic(local / 0.45);
        var outK = Smooth((end - t) / 0.35);
        var alpha = Math.Min(inK, outK);
        const float size = 40;
        var text = cap.Text;
        var w = Measure(text, size, semibold) + 104;
        var y = OutH - 50 + (1 - inK) * 24;
        var rect = new SKRect((float)(OutW / 2 - w / 2), (float)(y - 38), (float)(OutW / 2 + w / 2), (float)(y + 38));
        using (var blur = new SKPaint { Color = new SKColor(0, 0, 0, (byte)(110 * alpha)), MaskFilter = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, 18), IsAntialias = true })
            c.DrawRoundRect(new SKRect(rect.Left, rect.Top + 10, rect.Right, rect.Bottom + 10), 38, 38, blur);
        using (var fill = new SKPaint { Color = new SKColor(14, 10, 40, (byte)(232 * alpha)), IsAntialias = true }) c.DrawRoundRect(rect, 38, 38, fill);
        using (var line = new SKPaint { Color = new SKColor(255, 255, 255, (byte)(60 * alpha)), IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 1.5f }) c.DrawRoundRect(rect, 38, 38, line);
        using (var dot = new SKPaint { Color = accent.WithAlpha((byte)(255 * alpha)), IsAntialias = true }) c.DrawCircle(rect.Left + 38, rect.MidY, 8, dot);
        DrawText(c, text, rect.Left + 62, rect.MidY + 13.5, size, semibold, SKColors.White.WithAlpha((byte)(255 * alpha)), SKTextAlign.Left);
    }
}

void DrawChapter(SKCanvas c, double t, double labelAlpha)
{
    for (var i = 0; i < chapters.Count; i++)
    {
        var start = chapters[i].T;
        var end = i + 1 < chapters.Count ? chapters[i + 1].T : mainEnd - 0.3;
        if (t < start || t > end) continue;
        var alpha = Math.Min(Smooth((t - start) / 0.5), Smooth((end - t) / 0.4)) * labelAlpha;
        var label = chapters[i].Title;
        var w = Measure(label, 30, semibold) + 76;
        var rect = new SKRect(70, 52, (float)(70 + w), 52 + 62);
        using (var fill = new SKPaint { Color = new SKColor(255, 255, 255, (byte)(30 * alpha)), IsAntialias = true }) c.DrawRoundRect(rect, 31, 31, fill);
        using (var line = new SKPaint { Color = new SKColor(255, 255, 255, (byte)(75 * alpha)), IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 1.5f }) c.DrawRoundRect(rect, 31, 31, line);
        using (var dot = new SKPaint { Color = teal.WithAlpha((byte)(255 * alpha)), IsAntialias = true }) c.DrawCircle(rect.Left + 32, rect.MidY, 8, dot);
        DrawText(c, label, rect.Left + 54, rect.MidY + 10.5, 30, semibold, SKColors.White.WithAlpha((byte)(255 * alpha)), SKTextAlign.Left);
    }
}

void DrawSpeedBadge(SKCanvas c, double t, double labelAlpha)
{
    foreach (var s in speedBadges)
    {
        if (t < s.From || t > s.To) continue;
        var alpha = Math.Min(Smooth((t - s.From) / 0.3), Smooth((s.To - t) / 0.3)) * labelAlpha;
        var label = string.Create(CultureInfo.InvariantCulture, $"Sped up {s.Rate:0.#}x");
        var w = Measure(label, 30, semibold) + 76;
        var rect = new SKRect((float)(OutW - 70 - w), 52, OutW - 70, 52 + 62);
        using (var fill = new SKPaint { Color = accent.WithAlpha((byte)(235 * alpha)), IsAntialias = true }) c.DrawRoundRect(rect, 31, 31, fill);
        DrawText(c, label, rect.MidX, rect.MidY + 10.5, 30, semibold, SKColor.Parse("#1A1052").WithAlpha((byte)(255 * alpha)));
    }
}

// =============================== Types ===============================

internal sealed class Camera(double x, double y, double z)
{
    private double _vx, _vy, _vz;
    public double X { get; private set; } = x;
    public double Y { get; private set; } = y;
    public double Z { get; private set; } = z;

    /// <summary>Critically damped motion, so the camera starts and stops softly like a real camera operator.</summary>
    public void Update(double tx, double ty, double tz, double dt)
    {
        X = Damp(X, tx, ref _vx, 0.55, dt);
        Y = Damp(Y, ty, ref _vy, 0.55, dt);
        Z = Damp(Z, tz, ref _vz, 0.65, dt);
    }

    private static double Damp(double current, double target, ref double velocity, double smoothTime, double dt)
    {
        var omega = 2.0 / smoothTime;
        var x = omega * dt;
        var exp = 1.0 / (1.0 + x + 0.48 * x * x + 0.235 * x * x * x);
        var change = current - target;
        var temp = (velocity + omega * change) * dt;
        velocity = (velocity - omega * temp) * exp;
        return target + (change + temp) * exp;
    }
}

internal sealed record Piece(double From, double To, double Rate, double OutFrom);
internal sealed record ClickEvent(double T, int X, int Y);
internal sealed record FocusEvent(double T, double Hold, int X, int Y, int W, int H, double Zoom);
internal sealed record CaptionEvent(double T0, double T1, string Text);
internal sealed record ChapterEvent(double T, string Title);
internal sealed record CutEvent(double From, double To);
internal sealed record SpeedEvent(double From, double To, double Seconds);
internal sealed record Timeline(int Width, int Height, double End, List<ClickEvent> Clicks, List<ClickEvent> Mouse, List<FocusEvent> Focus, List<CaptionEvent> Captions, List<ChapterEvent> Chapters, List<CutEvent> Cuts, List<SpeedEvent> Speeds);
