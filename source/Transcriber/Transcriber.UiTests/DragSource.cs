using System.Collections.Specialized;
using System.Drawing;
using System.Windows.Forms;
using FlaUI.Core.Input;
using Point = System.Drawing.Point;

/// <summary>
/// A tiny always-on-top window that starts a real OLE file drag and releases it over a screen position,
/// exactly like dragging files out of Explorer.
/// </summary>
public static class DragSource
{
    public static string DropFiles(Point target, TimeSpan timeout, Action? whileOverTarget, params string[] files)
    {
        Exception? failure = null;
        var report = "";
        var finished = new ManualResetEventSlim();

        var thread = new Thread(() =>
        {
            try { report = Run(target, whileOverTarget, files); }
            catch (Exception ex) { failure = ex; }
            finally { finished.Set(); }
        }) { IsBackground = true };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();

        if (!finished.Wait(timeout))
        {
            Mouse.Up(MouseButton.Left); // never leave the button held down
            throw new TimeoutException("The drag did not finish. The mouse may be in use by something else.");
        }
        if (failure is not null) throw failure;
        return report;
    }

    private static string Run(Point target, Action? whileOverTarget, string[] files)
    {
        const int startX = 120, startY = 90;
        var step = 0;
        var effect = (DragDropEffects)(-1);
        var overAt = "";
        const int steps = 15;

        using var form = new Form
        {
            StartPosition = FormStartPosition.Manual,
            Location = new Point(20, 20),
            Size = new Size(220, 120),
            TopMost = true,
            FormBorderStyle = FormBorderStyle.FixedToolWindow,
            Text = "drag source",
        };
        var label = new Label { Text = "DRAG", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter };
        form.Controls.Add(label);

        var timer = new System.Windows.Forms.Timer { Interval = 60 };
        timer.Tick += (_, _) =>
        {
            step++;
            if (step <= steps)
            {
                var x = startX + (target.X - startX) * step / steps;
                var y = startY + (target.Y - startY) * step / steps;
                Mouse.Position = new Point(x, y);
            }
            else if (step == steps + 8) { overAt = $"cursor at {Mouse.Position}"; whileOverTarget?.Invoke(); }
            else if (step == steps + 12) Mouse.Up(MouseButton.Left);
        };

        label.MouseDown += (_, _) =>
        {
            timer.Start();
            var data = new DataObject();
            var list = new StringCollection();
            list.AddRange(files);
            data.SetFileDropList(list);
            effect = label.DoDragDrop(data, DragDropEffects.Copy);
            timer.Stop();
            form.Close();
        };

        form.Shown += (_, _) =>
        {
            var start = new Point(form.Left + 100, form.Top + 70);
            var begin = new System.Windows.Forms.Timer { Interval = 500 };
            begin.Tick += (_, _) =>
            {
                begin.Stop();
                Mouse.Position = new Point(form.Left + 100, form.Top + 70);
                AppDriver.SaveScreen("drag-before-press");
                Mouse.Down(MouseButton.Left);
                Mouse.MoveBy(4, 4);
            };
            begin.Start();
        };

        Application.Run(form);
        return $"effect={effect}, target={target}, {overAt}";
    }
}
