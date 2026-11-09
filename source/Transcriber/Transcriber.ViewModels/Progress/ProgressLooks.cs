namespace Transcriber.ViewModels.Progress;

/// <summary>
/// All the looks the progress bar can have. To add a look:
/// 1. Add a draw function and a line in tools/sprites/sprites.cs. Run the tool and copy the PNG to Resources/Raw.
/// 2. Add one <see cref="ProgressLook"/> to <see cref="All"/> below.
/// Nothing else changes. The picker and the control use the list.
/// </summary>
public static class ProgressLooks
{
    /// <summary>The runner on a plain bar, with the princess who runs in front of him. They kiss at the finish line. The control uses it when it has no other look.</summary>
    public static ProgressLook Runner { get; } = new(
        Id: "runner", Sprite: "runner.png", Cell: 32, Frames: 8, FramesPerSecond: 12,
        Fill: "#512BD4", FillDark: "#AC99EA",
        Companion: new("princess.png", Frames: 8, Lead: 42),
        Finale: new("couple.png", Cell: 48, Frames: 6, Seconds: 1.5));

    /// <summary>The surfer on a wave.</summary>
    public static ProgressLook Surfer { get; } = new(
        Id: "surfer", Sprite: "surfer.png", Cell: 32, Frames: 8, FramesPerSecond: 8,
        Fill: "#1B9AAA", FillDark: "#4FD1D9", Decoration: TrackDecoration.Wave);

    /// <summary>The marathon runner between a start line and a finish line.</summary>
    public static ProgressLook Marathoner { get; } = new(
        Id: "marathoner", Sprite: "marathoner.png", Cell: 32, Frames: 8, FramesPerSecond: 10,
        Fill: "#E8590C", FillDark: "#FF8A4C", Decoration: TrackDecoration.StartAndFinish);

    /// <summary>A cowboy who runs from a volcano. The bar is the lava that follows him.</summary>
    public static ProgressLook Volcano { get; } = new(
        Id: "volcano", Sprite: "cowboy.png", Cell: 32, Frames: 8, FramesPerSecond: 12,
        Fill: "#FF6A1A", FillDark: "#FF8A4C", Decoration: TrackDecoration.Volcano);

    /// <summary>The hero and the princess on a magic carpet that floats on a wave.</summary>
    public static ProgressLook Carpet { get; } = new(
        Id: "carpet", Sprite: "carpet.png", Cell: 48, Frames: 8, FramesPerSecond: 8,
        Fill: "#7A3FD1", FillDark: "#B794F4", Decoration: TrackDecoration.Wave);

    public static IReadOnlyList<ProgressLook> All { get; } = [Runner, Surfer, Marathoner, Volcano, Carpet];
}
