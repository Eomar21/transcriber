namespace Transcriber.ViewModels.Progress;

/// <summary>
/// All the looks the progress bar can have. To add a look:
/// 1. Add a draw function and a line in tools/sprites/sprites.cs. Run the tool and copy the PNG to Resources/Raw.
/// 2. Add one <see cref="ProgressLook"/> to <see cref="All"/> below.
/// Nothing else changes. The picker and the control use the list.
/// </summary>
public static class ProgressLooks
{
    /// <summary>The runner on a plain bar. The control uses it when it has no other look.</summary>
    public static ProgressLook Runner { get; } = new(
        Id: "runner", Sprite: "runner.png", Cell: 32, Frames: 8, FramesPerSecond: 12,
        Fill: "#512BD4", FillDark: "#AC99EA");

    /// <summary>The surfer on a wave.</summary>
    public static ProgressLook Surfer { get; } = new(
        Id: "surfer", Sprite: "surfer.png", Cell: 32, Frames: 8, FramesPerSecond: 8,
        Fill: "#1B9AAA", FillDark: "#4FD1D9", Decoration: TrackDecoration.Wave);

    /// <summary>The marathon runner between a start line and a finish line.</summary>
    public static ProgressLook Marathoner { get; } = new(
        Id: "marathoner", Sprite: "marathoner.png", Cell: 32, Frames: 8, FramesPerSecond: 10,
        Fill: "#E8590C", FillDark: "#FF8A4C", Decoration: TrackDecoration.StartAndFinish);

    public static IReadOnlyList<ProgressLook> All { get; } = [Runner, Surfer, Marathoner];
}
