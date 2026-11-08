using System.Diagnostics;
using FlaUI.Core;
using Application = FlaUI.Core.Application;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Capturing;
using FlaUI.UIA3;

/// <summary>Runs the UI tests only when asked, so a plain "dotnet test" never opens windows.</summary>
public sealed class UiFactAttribute : FactAttribute
{
    public UiFactAttribute()
    {
        if (Environment.GetEnvironmentVariable("TRANSCRIBER_UI_TESTS") != "1")
            Skip = "Set TRANSCRIBER_UI_TESTS=1 to run the UI tests (they launch the real app).";
    }
}

/// <summary>Opt-in on top of UiFact: moves the real mouse, so it is meant for CI or an idle machine.</summary>
public sealed class UiDragFactAttribute : FactAttribute
{
    public UiDragFactAttribute()
    {
        if (Environment.GetEnvironmentVariable("TRANSCRIBER_UI_TESTS") != "1" ||
            Environment.GetEnvironmentVariable("TRANSCRIBER_UI_DRAG") != "1")
            Skip = "Set TRANSCRIBER_UI_TESTS=1 and TRANSCRIBER_UI_DRAG=1 to run the drag and drop test (it moves the mouse).";
    }
}

/// <summary>Starts the real app and finds controls by the AutomationIds set in the XAML.</summary>
public sealed class AppDriver : IDisposable
{
    private readonly UIA3Automation _automation = new();
    public Application App { get; }
    public Window Window { get; }

    public AppDriver(params string[] files)
    {
        var exe = Environment.GetEnvironmentVariable("TRANSCRIBER_APP_EXE") ?? DefaultExe();
        if (!File.Exists(exe))
            throw new FileNotFoundException("Build the app first or set TRANSCRIBER_APP_EXE.", exe);

        var psi = new ProcessStartInfo(exe) { UseShellExecute = false, WorkingDirectory = Path.GetDirectoryName(exe)! };
        foreach (var f in files) psi.ArgumentList.Add(f);
        App = Application.Launch(psi);
        Window = App.GetMainWindow(_automation, TimeSpan.FromSeconds(60))
                 ?? throw new InvalidOperationException("The app window did not appear.");
        Wait(() => Find("TranscribeButton") is not null, TimeSpan.FromSeconds(30), "the page to load");
    }

    private static string DefaultExe()
    {
        var root = AppContext.BaseDirectory;
        while (root is not null && !Directory.Exists(Path.Combine(root, "Transcriber.Maui"))) root = Path.GetDirectoryName(root);
        return Path.Combine(root ?? "", "Transcriber.Maui", "bin", "Debug", "net10.0-windows10.0.19041.0", "win-x64", "Transcriber.Maui.exe");
    }

    public AutomationElement? Find(string automationId) =>
        Window.FindFirstDescendant(cf => cf.ByAutomationId(automationId));

    public AutomationElement Get(string automationId) =>
        Find(automationId) ?? throw new InvalidOperationException($"Control '{automationId}' was not found.");

    public string TextOf(string automationId) => Find(automationId)?.Name ?? string.Empty;

    public void Click(string automationId) => Get(automationId).AsButton().Invoke();

    public static void Wait(Func<bool> condition, TimeSpan timeout, string what)
    {
        var until = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < until)
        {
            if (condition()) return;
            Thread.Sleep(250);
        }
        throw new TimeoutException($"Timed out waiting for {what}.");
    }

    public void SaveScreenshot(string name)
    {
        try
        {
            var dir = Path.Combine(AppContext.BaseDirectory, "screenshots");
            Directory.CreateDirectory(dir);
            Capture.Element(Window).ToFile(Path.Combine(dir, name + ".png"));
        }
        catch { /* a missing screenshot must never hide the real failure */ }
    }

    public void Dispose()
    {
        try { if (!App.HasExited) App.Close(); } catch { }
        try { if (!App.HasExited) App.Kill(); } catch { }
        _automation.Dispose();
        App.Dispose();
    }
}
