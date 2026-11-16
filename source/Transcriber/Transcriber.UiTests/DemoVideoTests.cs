using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.Core.Input;
using FlaUI.Core.WindowsAPI;

/// <summary>Runs only when asked: it moves the real mouse and records the screen for the demo video.</summary>
public sealed class DemoFactAttribute : FactAttribute
{
    public DemoFactAttribute()
    {
        if (Environment.GetEnvironmentVariable("TRANSCRIBER_UI_TESTS") != "1" ||
            Environment.GetEnvironmentVariable("TRANSCRIBER_DEMO_VIDEO") != "1")
            Skip = "Set TRANSCRIBER_UI_TESTS=1 and TRANSCRIBER_DEMO_VIDEO=1 to record the demo video (it moves the mouse).";
    }
}

/// <summary>
/// Drives the real app with the real mouse while ffmpeg records the window. It writes two files into
/// DEMO_VIDEO_OUT: capture.mkv, and timeline.json with the clicks, zoom targets, captions, cuts and speed changes.
/// The composer in tools/demo-video turns both files into the finished video.
/// </summary>
public class DemoVideoTests
{
    [DllImport("user32.dll")] private static extern bool SetProcessDPIAware();
    [DllImport("dwmapi.dll")] private static extern int DwmGetWindowAttribute(IntPtr hwnd, int attribute, out Rect rect, int size);
    [StructLayout(LayoutKind.Sequential)] private struct Rect { public int Left, Top, Right, Bottom; }
    static DemoVideoTests() => SetProcessDPIAware();

    private record Click(double T, int X, int Y);
    private record Focus(double T, double Hold, int X, int Y, int W, int H, double Zoom);
    private record Caption(double T0, double T1, string Text);
    private record Chapter(double T, string Title);
    private record Cut(double From, double To);
    private record Speed(double From, double To, double Seconds);

    private readonly List<Click> _clicks = [];
    private readonly List<Click> _mouse = [];                  // the path of the pointer, drawn again in the composer
    private readonly List<Focus> _focus = [];
    private readonly List<Caption> _captions = [];
    private readonly List<Chapter> _chapters = [];
    private readonly List<Cut> _cuts = [];
    private readonly List<Speed> _speeds = [];
    private Process _ffmpeg = null!;
    private Rectangle _box;
    private AppDriver _app = null!;
    private string _out = "";

    private static string Ffmpeg => Environment.GetEnvironmentVariable("FFMPEG_EXE") ?? "ffmpeg";
    private static double Now => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000.0;
    private static void Pause(int ms) => Thread.Sleep(ms);

    private void Start(AppDriver app)
    {
        _app = app;
        _out = Environment.GetEnvironmentVariable("DEMO_VIDEO_OUT") ?? Path.Combine(AppContext.BaseDirectory, "demo-video");
        Directory.CreateDirectory(_out);
        Pause(800);
        DwmGetWindowAttribute(app.App.MainWindowHandle, 9, out var r, Marshal.SizeOf<Rect>());
        _box = new Rectangle(r.Left, r.Top, (r.Right - r.Left) & ~1, (r.Bottom - r.Top) & ~1);
        Mouse.Position = new Point(_box.X + _box.Width * 3 / 4, _box.Y + 250);
        _mouse.Add(new Click(Now, Mouse.Position.X - _box.X, Mouse.Position.Y - _box.Y));

        var psi = new ProcessStartInfo(Ffmpeg) { UseShellExecute = false, RedirectStandardInput = true, RedirectStandardError = true, CreateNoWindow = true };
        foreach (var a in new[]
        {
            "-y", "-loglevel", "error", "-f", "gdigrab", "-framerate", "60",
            "-offset_x", _box.X.ToString(), "-offset_y", _box.Y.ToString(), "-video_size", $"{_box.Width}x{_box.Height}",
            "-draw_mouse", "0", "-i", "desktop", "-copyts",
            "-c:v", "libx264", "-preset", "ultrafast", "-crf", "10", "-pix_fmt", "yuv444p",
            Path.Combine(_out, "capture.mkv"),
        }) psi.ArgumentList.Add(a);
        _ffmpeg = Process.Start(psi)!;
        _ffmpeg.ErrorDataReceived += (_, _) => { };
        _ffmpeg.BeginErrorReadLine();
        Pause(1200);                                        // the first frames are not lost
    }

    private void MoveTo(Point target)
    {
        var from = Mouse.Position;
        const int steps = 48;
        for (var i = 1; i <= steps; i++)
        {
            var k = (double)i / steps;
            var e = k * k * (3 - 2 * k);                                   // slow at both ends
            var p = new Point((int)(from.X + (target.X - from.X) * e), (int)(from.Y + (target.Y - from.Y) * e));
            Mouse.Position = p;
            _mouse.Add(new Click(Now, p.X - _box.X, p.Y - _box.Y));
            Thread.Sleep(14);
        }
    }

    private Rectangle Local(AutomationElement el)
    {
        var r = el.BoundingRectangle;
        return new Rectangle(r.X - _box.X, r.Y - _box.Y, r.Width, r.Height);
    }

    /// <summary>Asks the composer to zoom to a control for a while.</summary>
    private void ZoomTo(AutomationElement el, double hold, double zoom = 1.6)
    {
        var l = Local(el);
        if (l.Width <= 0 || l.Height <= 0) return;                         // not on screen: no zoom
        // A small control gets a wider area around it, so the zoom does not jump to a tiny spot.
        var w = Math.Max(l.Width, 360);
        var h = Math.Max(l.Height, 200);
        var x = Math.Clamp(l.X + l.Width / 2 - w / 2, 0, Math.Max(0, _box.Width - w));
        var y = Math.Clamp(l.Y + l.Height / 2 - h / 2, 0, Math.Max(0, _box.Height - h));
        _focus.Add(new Focus(Now, hold, x, y, w, h, zoom));
    }

    /// <summary>Zooms to a control for as long as the wait takes.</summary>
    private void ZoomWhile(AutomationElement el, double zoom, Action wait)
    {
        var start = Now;
        var before = _focus.Count;
        ZoomTo(el, 0, zoom);
        wait();
        if (_focus.Count > before) _focus[^1] = _focus[^1] with { T = start, Hold = Now - start + 0.4 };
    }

    /// <summary>The control with this id that is on screen. The same id can exist on the other tab, where it has no size.</summary>
    private AutomationElement? Shown(string id) =>
        _app.Window.FindAllDescendants(cf => cf.ByAutomationId(id)).FirstOrDefault(e => e.BoundingRectangle.Width > 0);

    private void Say(string text, double seconds) => _captions.Add(new Caption(Now, Now + seconds, text));

    private void ClickEl(AutomationElement el, bool nearLeft = false, double zoom = 0)
    {
        var r = el.BoundingRectangle;
        var p = new Point(nearLeft ? r.X + Math.Min(r.Width / 2, 40) : r.X + r.Width / 2, r.Y + r.Height / 2);
        if (zoom > 0) ZoomTo(el, 2.4, zoom);
        MoveTo(p);
        Pause(260);
        _clicks.Add(new Click(Now, p.X - _box.X, p.Y - _box.Y));
        Mouse.Click(MouseButton.Left);
        Pause(650);
    }

    private void ClickId(string id, bool nearLeft = false, double zoom = 0) => ClickEl(_app.Get(id), nearLeft, zoom);

    private static void Until(Func<bool> done, int timeoutSeconds)
    {
        var t = Stopwatch.StartNew();
        while (t.Elapsed.TotalSeconds < timeoutSeconds)
        {
            try { if (done()) return; } catch (COMException) { }
            Thread.Sleep(120);
        }
        throw new TimeoutException();
    }

    private void SmoothScroll(double to)
    {
        var scroller = _app.Get("PageScroller");
        var from = scroller.Patterns.Scroll.Pattern.VerticalScrollPercent.Value;
        const int steps = 90;
        for (var i = 0; i <= steps; i++)
        {
            var k = (double)i / steps;
            scroller.Patterns.Scroll.Pattern.SetScrollPercent(-1, from + (to - from) * k * k * (3 - 2 * k));
            Thread.Sleep(22);
        }
    }

    private void Finish()
    {
        Pause(900);
        var endEpoch = Now;
        _ffmpeg.StandardInput.Write("q");
        _ffmpeg.StandardInput.Flush();
        _ffmpeg.WaitForExit(30000);

        // The video time stamps are the wall clock. The first one says when the video starts.
        var info = new ProcessStartInfo(Ffmpeg) { UseShellExecute = false, RedirectStandardError = true, CreateNoWindow = true };
        info.ArgumentList.Add("-i");
        info.ArgumentList.Add(Path.Combine(_out, "capture.mkv"));
        using var probe = Process.Start(info)!;
        var text = probe.StandardError.ReadToEnd();
        probe.WaitForExit();
        var m = Regex.Match(text, @"start: (\d+\.\d+)");
        Assert.True(m.Success, "The start time of the video was not found.");
        var start = double.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
        double T(double epoch) => Math.Round(epoch - start, 3);

        var timeline = new
        {
            width = _box.Width,
            height = _box.Height,
            end = T(endEpoch),
            clicks = _clicks.Select(c => new { t = T(c.T), x = c.X, y = c.Y }),
            mouse = _mouse.Select(c => new { t = T(c.T), x = c.X, y = c.Y }),
            focus = _focus.Select(f => new { t = T(f.T), hold = f.Hold, x = f.X, y = f.Y, w = f.W, h = f.H, zoom = f.Zoom }),
            captions = _captions.Select(c => new { t0 = T(c.T0), t1 = T(c.T1), text = c.Text }),
            chapters = _chapters.Select(c => new { t = T(c.T), title = c.Title }),
            cuts = _cuts.Select(c => new { from = T(c.From), to = T(c.To) }),
            speeds = _speeds.Select(s => new { from = T(s.From), to = T(s.To), seconds = s.Seconds }),
        };
        File.WriteAllText(Path.Combine(_out, "timeline.json"), JsonSerializer.Serialize(timeline, new JsonSerializerOptions { WriteIndented = true }));
    }

    private static string DemoFile(string name) => Path.Combine(Environment.GetEnvironmentVariable("DEMO_VIDEO_FILES") ?? @"C:\demo", name);

    [DemoFact]
    public void Record_demo_video()
    {
        // The practice history is moved aside, so the Practice tab starts empty. It comes back at the end.
        var history = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Transcriber", "practice-history.json");
        var aside = history + ".demo-backup";
        var hadHistory = File.Exists(history);
        if (hadHistory) File.Move(history, aside, overwrite: true);

        using var app = new AppDriver(DemoFile("two-speakers.wav"));
        try
        {
            Start(app);

            // Scene 1: transcribe a recording and label the speakers.
            _chapters.Add(new Chapter(Now, "Transcribe"));
            Say("Add a recording. Audio or video.", 2.8);
            ZoomTo(app.Get("FilesSummary"), 2.6, 1.5);
            Pause(2800);
            Say("Find out who is speaking.", 2.6);
            ClickId("IdentifySpeakersSwitch", nearLeft: true, zoom: 1.7);
            Pause(1000);
            Until(() => app.Get("TranscribeButton").IsEnabled, 30);
            Say("It runs on your computer. Nothing goes online.", 3.6);
            ClickId("TranscribeButton", zoom: 1.5);
            var runStart = Now;
            Until(() => Shown("RunProgress") is not null, 15);
            Pause(800);                                                    // the page has scrolled to the bar
            ZoomWhile(Shown("RunProgress")!, 1.6, () => Until(() => app.TextOf("ResultMessage") == "Transcript saved", 300));
            _speeds.Add(new Speed(runStart + 0.8, Now - 0.4, 8.0));
            Pause(1200);
            Say("The transcript is saved next to your file.", 3.2);
            ZoomTo(app.Get("ResultMessage"), 2.8, 1.6);
            Pause(3200);
            SmoothScroll(100);
            Pause(700);
            Say("Click a line to play it.", 3.0);
            var row = app.Find("Segments")!.FindFirstDescendant(cf => cf.ByControlType(ControlType.ListItem))!;
            ClickEl(row, zoom: 1.5);
            Pause(2800);
            ClickId("StopButton");
            Pause(700);

            // Scene 2: practise speaking.
            _chapters.Add(new Chapter(Now, "Practice"));
            Say("Practise speaking. Get feedback.", 3.0);
            ClickEl(app.Window.FindFirstDescendant(cf => cf.ByControlType(ControlType.TabItem).And(cf.ByName("Practice")))!);
            Until(() => app.Find("PracticeFileName") is not null, 10);
            Pause(1200);

            var choose = app.Get("PracticeChooseButton");
            var cr = choose.BoundingRectangle;
            MoveTo(new Point(cr.X + cr.Width / 2, cr.Y + cr.Height / 2));
            Pause(260);
            _clicks.Add(new Click(Now, cr.X + cr.Width / 2 - _box.X, cr.Y + cr.Height / 2 - _box.Y));
            Task.Run(() => Mouse.Click(MouseButton.Left));
            var cutFrom = Now + 0.45;                                       // the dialog is cut out of the video
            Window[] modals = [];
            Until(() => (modals = app.Window.ModalWindows).Length > 0, 20);
            var dialog = modals[0];
            dialog.Focus();
            Pause(600);
            Keyboard.Type(DemoFile("practice-sample.wav"));
            Pause(300);
            Keyboard.Press(VirtualKeyShort.ENTER);
            Keyboard.Release(VirtualKeyShort.ENTER);
            Until(() => app.TextOf("PracticeFileName").Contains("practice-sample"), 20);
            _cuts.Add(new Cut(cutFrom, Now + 0.2));
            Pause(1500);

            Say("See your pace, filler words and pauses.", 3.6);
            ClickId("AnalyzeButton", zoom: 1.5);
            var analyzeStart = Now;
            Pause(900);
            if (Shown("RunProgress") is { } bar) ZoomWhile(bar, 1.5, () => Until(() => app.Find("PracticeTips") is not null, 300));
            else Until(() => app.Find("PracticeTips") is not null, 300);
            if (Now - analyzeStart > 3) _speeds.Add(new Speed(analyzeStart + 0.8, Now - 0.4, 4.0));
            Pause(1500);
            ZoomTo(app.Get("PracticeMetrics"), 3.2, 1.5);
            Pause(3400);
            SmoothScroll(100);
            Pause(2200);
            Finish();
        }
        finally
        {
            try { _ffmpeg?.Kill(); } catch { }
            app.Dispose();
            if (hadHistory) File.Move(aside, history, overwrite: true);
            else File.Delete(history);                                     // the demo sessions do not stay
        }
    }
}
