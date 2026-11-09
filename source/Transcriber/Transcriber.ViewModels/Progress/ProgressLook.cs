namespace Transcriber.ViewModels.Progress;

/// <summary>What is drawn along the bar, besides the sprite.</summary>
public enum TrackDecoration
{
    /// <summary>A plain bar.</summary>
    None,

    /// <summary>The filled part of the bar has a moving wave on top. The sprite rides the wave.</summary>
    Wave,

    /// <summary>A start line at the beginning of the bar and a finish line with a flag at the end.</summary>
    StartAndFinish,

    /// <summary>A volcano at the start of the bar. The filled part of the bar is lava that follows the runner.</summary>
    Volcano,
}

/// <summary>
/// One look for the progress bar: a sprite and the theme that goes with it.
/// The sprite is a strip of equal square frames in Resources/Raw. The strip is made by tools/sprites/sprites.cs.
/// Colours are text like "#512BD4", so this project does not depend on the UI toolkit.
/// </summary>
/// <param name="Id">A short name that does not change. Tests and logs use it.</param>
/// <param name="Sprite">The file name of the strip, for example "runner.png".</param>
/// <param name="Cell">The width and the height of one frame, in pixels.</param>
/// <param name="Frames">The number of frames in the strip.</param>
/// <param name="FramesPerSecond">How fast the frames play.</param>
/// <param name="Fill">The colour of the filled part of the bar in the light theme.</param>
/// <param name="FillDark">The colour of the filled part of the bar in the dark theme.</param>
/// <param name="Decoration">What is drawn on the bar. The default is nothing.</param>
/// <param name="Companion">A second character who runs in front. The default is none.</param>
/// <param name="Finale">A picture for the end of the bar. The default is none.</param>
public sealed record ProgressLook(
    string Id,
    string Sprite,
    int Cell,
    int Frames,
    double FramesPerSecond,
    string Fill,
    string FillDark,
    TrackDecoration Decoration = TrackDecoration.None,
    Companion? Companion = null,
    Finale? Finale = null);

/// <summary>
/// A second character who runs in front of the main one. Both reach the end of the bar, the companion first.
/// The strip has one more frame than <paramref name="Frames"/>: the last frame is the companion standing and waiting.
/// </summary>
/// <param name="Sprite">The file name of the strip, for example "princess.png". The frames have the same size as the frames of the look.</param>
/// <param name="Frames">The number of frames of the run.</param>
/// <param name="Lead">How far in front of the main character the companion runs, in density-independent units.</param>
public sealed record Companion(string Sprite, int Frames, double Lead);

/// <summary>
/// A picture that plays at the end of the bar, instead of the characters, next to the burst of sparks.
/// The strip has square frames. The picture is drawn with the same scale as the characters.
/// </summary>
/// <param name="Sprite">The file name of the strip, for example "couple.png".</param>
/// <param name="Cell">The width and the height of one frame, in pixels.</param>
/// <param name="Frames">The number of frames. The last frame stays on the screen.</param>
/// <param name="Seconds">How long the frames take to play.</param>
public sealed record Finale(string Sprite, int Cell, int Frames, double Seconds);
