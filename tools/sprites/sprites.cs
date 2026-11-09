#:package System.Drawing.Common@10.0.0
using System.Drawing;

// Draws the progress bar sprites as pixel art. Usage: dotnet run sprites.cs <output folder>
// Every sprite sheet is one line in the table below: file name, size of one frame, number of frames, the drawing
// code, and whether the frames get a dark outline. To add a sprite, write a Draw function and add a line.
// The app side is in source/Transcriber/Transcriber.ViewModels/Progress/ProgressLooks.cs.
var outDir = args.Length > 0 ? args[0] : ".";
Directory.CreateDirectory(outDir);

const int SparkCell = 48, SparkFrames = 6;
SpriteSheet[] sheets =
[
    new("runner.png", 32, 8, DrawRunner, Outline: true),
    new("princess.png", 32, 9, DrawPrincess, Outline: true),
    new("couple.png", 48, 6, DrawCouple, Outline: false),
    new("surfer.png", 32, 8, DrawSurfer, Outline: true),
    new("marathoner.png", 32, 8, DrawMarathoner, Outline: true),
    new("cowboy.png", 32, 8, DrawCowboy, Outline: true),
    new("spark.png", SparkCell, SparkFrames, DrawSpark, Outline: false),
];

var built = new List<Canvas>();
foreach (var sheet in sheets)
{
    var strip = new Canvas(sheet.Cell * sheet.Frames, sheet.Cell);
    for (var f = 0; f < sheet.Frames; f++)
    {
        var cell = new Canvas(sheet.Cell, sheet.Cell);
        sheet.Draw(cell, f);
        if (sheet.Outline) cell.Outline(Palette.Outline);
        strip.Paste(cell, f * sheet.Cell, 0);
    }
    strip.Save(Path.Combine(outDir, sheet.File), 1);
    built.Add(strip);
}

// A magnified picture for looking at the frames. Not used by the app.
const int Zoom = 5;
var preview = new Canvas(built.Max(b => b.W) * Zoom + 20, built.Sum(b => b.H * Zoom + 10) + 10);
preview.FillBackground(Color.FromArgb(70, 90, 140));
var top = 10;
foreach (var strip in built)
{
    preview.PasteScaled(strip, 10, top, Zoom);
    top += strip.H * Zoom + 10;
}
preview.Save(Path.Combine(outDir, "preview.png"), 1);
Console.WriteLine("Done: " + outDir);

static void DrawRunner(Canvas c, int f)
{
    var t = 2 * Math.PI * f / 8;
    var bob = Math.Round(-1.0 * Math.Abs(Math.Cos(t)));
    var hip = (x: 13.5, y: 18.2 + bob);
    const double lean = 0.3;                                  // leans forward, like the cowboy
    var shoulder = (x: hip.x + 7.8 * Math.Sin(lean), y: hip.y - 7.8 * Math.Cos(lean));
    var head = (x: shoulder.x + 2.0, y: shoulder.y - 4.2);

    // The scarf streams behind the neck. It is drawn first so that the body is in front of it.
    var neck = (x: shoulder.x - 1.0, y: shoulder.y - 0.8);
    var prev = neck;
    for (var i = 1; i <= 6; i++)
    {
        var p = (x: neck.x - i * 2.1, y: neck.y + 1.2 * Math.Sin(t * 2 - i * 0.9) * (0.4 + i * 0.22) + i * 0.2);
        c.Line(prev.x, prev.y, p.x, p.y, i == 6 ? 0.8 : 1.15, i % 2 == 0 ? Palette.ScarfDark : Palette.Scarf);
        c.Line(prev.x, prev.y - 0.9, p.x, p.y - 0.9, 0.3, RunnerTones.ScarfLight);
        prev = p;
    }

    // The far arm and the far leg are in darker colours, so that they sit behind the body.
    DrawArmAngles(c, shoulder, ArmUpper(t + Math.PI), ArmUpper(t + Math.PI) + 1.8, far: true, Outfits.Hero);
    DrawRunnerLeg(c, hip, t + Math.PI, far: true, Outfits.Hero, 6.2, 5.7);

    DrawHeroTorso(c, hip, shoulder);
    DrawRunnerLeg(c, hip, t, far: false, Outfits.Hero, 6.2, 5.7);
    DrawHeroHead(c, head, t, eyesClosed: false);
    DrawArmAngles(c, shoulder, ArmUpper(t), ArmUpper(t) + 1.8, far: false, Outfits.Hero);
}

static double ArmUpper(double ph) => -1.1 * Math.Sin(ph) - 0.2;

/// <summary>Baggy trousers at the hip, a gold sash, then a teal vest that leaves the chest bare in the middle.</summary>
static void DrawHeroTorso(Canvas c, (double x, double y) hip, (double x, double y) shoulder)
{
    c.Ball(hip.x, hip.y + 0.4, 3.7, RunnerTones.Trousers);
    c.Limb(hip.x - 0.5, hip.y - 1.0, shoulder.x - 0.5, shoulder.y + 0.8, 2.9, RunnerTones.Vest);
    c.Line(hip.x - 3.2, hip.y - 1.3, hip.x + 3.4, hip.y - 2.0, 1.0, RunnerTones.Sash.Base);
}

/// <summary>Dark hair at the back and the top, a face, a gold headband with a tail, an eye, an eyebrow and a smile.</summary>
static void DrawHeroHead(Canvas c, (double x, double y) head, double t, bool eyesClosed)
{
    c.Ball(head.x - 1.0, head.y - 0.7, 3.8, RunnerTones.Hair);
    c.Ball(head.x + 1.1, head.y + 1.0, 3.0, RunnerTones.Skin);
    c.Line(head.x - 2.6, head.y - 0.4, head.x + 3.0, head.y - 1.2, 0.45, RunnerTones.Sash.Base);
    var flutter = Math.Sin(t * 2) * 0.8;
    c.Line(head.x - 2.6, head.y - 0.4, head.x - 5.4, head.y + 0.3 + flutter, 0.4, RunnerTones.Sash.Base);
    DrawEye(c, head, eyesClosed);
    c.Set((int)Math.Round(head.x + 2.4), (int)Math.Round(head.y - 0.5), RunnerTones.Hair.Shade);
    c.Set((int)Math.Round(head.x + 3.0), (int)Math.Round(head.y + 2.5), RunnerTones.Skin.Shade);
}

static void DrawEye(Canvas c, (double x, double y) head, bool closed)
{
    if (closed)
    {
        c.Set((int)Math.Round(head.x + 2.0), (int)Math.Round(head.y + 0.7), Palette.Outline);
        c.Set((int)Math.Round(head.x + 2.8), (int)Math.Round(head.y + 0.7), Palette.Outline);
        return;
    }
    c.Set((int)Math.Round(head.x + 2.0), (int)Math.Round(head.y + 0.5), Palette.Bib);
    c.Set((int)Math.Round(head.x + 2.8), (int)Math.Round(head.y + 0.5), Palette.Outline);
}

static void DrawRunnerLeg(Canvas c, (double x, double y) hip, double ph, bool far, Outfit o, double thigh, double shin)
{
    var a1 = 1.05 * Math.Sin(ph) + 0.08;
    // The knee bends most while the leg swings forward, and the foot kicks up behind.
    var a2 = a1 - (0.8 + 0.8 * Math.Max(0, Math.Cos(ph)));
    DrawLegAngles(c, hip, a1, a2, far, o, thigh, shin);
}

static void DrawLegAngles(Canvas c, (double x, double y) hip, double a1, double a2, bool far, Outfit o, double thigh, double shin)
{
    var trousers = far ? o.TrousersFar : o.Trousers;
    var shoe = far ? o.ShoeFar : o.Shoe;
    var cuff = far ? o.CuffFar : o.Cuff;
    var knee = (x: hip.x + thigh * Math.Sin(a1), y: hip.y + thigh * Math.Cos(a1));
    var ankle = (x: knee.x + shin * Math.Sin(a2), y: knee.y + shin * Math.Cos(a2));
    c.Limb(hip.x, hip.y, knee.x, knee.y, 2.3, trousers);
    c.Ball(knee.x, knee.y, 2.4, trousers);                      // the trousers puff out at the knee
    c.Limb(knee.x, knee.y, ankle.x, ankle.y, 1.8, trousers);
    c.Ball(ankle.x, ankle.y, 1.5, cuff);                        // gold cuff
    c.Limb(ankle.x + 0.2, ankle.y + 1.0, ankle.x + 2.7, ankle.y + 1.5, 1.35, shoe);
    c.Set((int)Math.Round(ankle.x + 4.0), (int)Math.Round(ankle.y + 0.5), shoe.Base); // the curled toe
}

static void DrawArmAngles(Canvas c, (double x, double y) shoulder, double b1, double b2, bool far, Outfit o)
{
    var skin = far ? o.SkinFar : o.Skin;
    var elbow = (x: shoulder.x + 4.6 * Math.Sin(b1), y: shoulder.y + 4.6 * Math.Cos(b1));
    var hand = (x: elbow.x + 4.0 * Math.Sin(b2), y: elbow.y + 4.0 * Math.Cos(b2));
    c.Limb(shoulder.x, shoulder.y, elbow.x, elbow.y, 1.35, skin);
    c.Limb(elbow.x, elbow.y, hand.x, hand.y, 1.15, skin);
    c.Ball(hand.x, hand.y, 1.3, skin);
}

/// <summary>An arm that reaches a point: the elbow bends down and out. Used when the two hold each other.</summary>
static void DrawArmTo(Canvas c, (double x, double y) shoulder, (double x, double y) target, Outfit o)
{
    var mid = (x: (shoulder.x + target.x) / 2, y: (shoulder.y + target.y) / 2 + 2.4);
    c.Limb(shoulder.x, shoulder.y, mid.x, mid.y, 1.35, o.Skin);
    c.Limb(mid.x, mid.y, target.x, target.y, 1.15, o.Skin);
    c.Ball(target.x, target.y, 1.3, o.Skin);
}

// ---- The princess: she runs in front of the hero ------------------------------------------------

static void DrawPrincess(Canvas c, int f)
{
    if (f == 8)
    {
        // The last frame of the strip is not a run frame. She stands and waits at the finish line.
        DrawStandingFigure(c, princess: true, hipX: 13.5, ankleY: 30.0, lean: 0.12, reach: null, eyesClosed: false);
        return;
    }
    var t = 2 * Math.PI * f / 8;
    var bob = Math.Round(-1.0 * Math.Abs(Math.Cos(t)));
    var hip = (x: 13.5, y: 18.6 + bob);
    const double lean = 0.26;
    var shoulder = (x: hip.x + 7.4 * Math.Sin(lean), y: hip.y - 7.4 * Math.Cos(lean));
    var head = (x: shoulder.x + 1.9, y: shoulder.y - 4.0);

    DrawPonytail(c, head, t);
    DrawArmAngles(c, shoulder, ArmUpper(t + Math.PI), ArmUpper(t + Math.PI) + 1.8, far: true, Outfits.Princess);
    DrawRunnerLeg(c, hip, t + Math.PI, far: true, Outfits.Princess, 6.0, 5.5);
    DrawPrincessTorso(c, hip, shoulder);
    DrawRunnerLeg(c, hip, t, far: false, Outfits.Princess, 6.0, 5.5);
    DrawPrincessHead(c, head, eyesClosed: false);
    DrawArmAngles(c, shoulder, ArmUpper(t), ArmUpper(t) + 1.8, far: false, Outfits.Princess);
}

/// <summary>A long black ponytail with a gold tie. It streams behind her and waves along its length.</summary>
static void DrawPonytail(Canvas c, (double x, double y) head, double t)
{
    var root = (x: head.x - 3.0, y: head.y - 0.2);
    var prev = root;
    for (var i = 1; i <= 7; i++)
    {
        var p = (x: root.x - i * 1.9, y: root.y + 1.3 * Math.Sin(t * 2 - i * 0.8) * (0.4 + i * 0.2) + i * 0.3);
        c.Line(prev.x, prev.y, p.x, p.y, i == 7 ? 0.7 : Math.Max(0.9, 1.5 - i * 0.07), i % 3 == 0 ? RunnerTones.PrincessHair.Shade : RunnerTones.PrincessHair.Base);
        c.Line(prev.x, prev.y - 0.8, p.x, p.y - 0.8, 0.25, RunnerTones.PrincessHair.Light);
        prev = p;
    }
    c.Ball(root.x - 1.2, root.y + 0.3, 1.0, RunnerTones.Sash);   // the gold tie
}

/// <summary>A short rose top, a bare waist, a gold belt and baggy lilac trousers.</summary>
static void DrawPrincessTorso(Canvas c, (double x, double y) hip, (double x, double y) shoulder)
{
    c.Ball(hip.x, hip.y + 0.4, 3.5, RunnerTones.PrincessTrousers);
    c.Limb(hip.x - 0.3, hip.y - 1.0, shoulder.x - 0.3, shoulder.y + 0.8, 2.3, RunnerTones.Skin);
    // The top covers the upper half of the body.
    var split = (x: shoulder.x + (hip.x - shoulder.x) * 0.5, y: shoulder.y + (hip.y - shoulder.y) * 0.5);
    c.Limb(split.x - 0.3, split.y, shoulder.x - 0.5, shoulder.y + 0.8, 2.8, RunnerTones.PrincessTop);
    c.Line(hip.x - 3.0, hip.y - 1.3, hip.x + 3.2, hip.y - 2.0, 0.9, RunnerTones.Sash.Base);
}


static void DrawPrincessHead(Canvas c, (double x, double y) head, bool eyesClosed)
{
    c.Ball(head.x - 1.0, head.y - 0.6, 3.8, RunnerTones.PrincessHair);
    c.Ball(head.x + 1.1, head.y + 1.0, 3.0, RunnerTones.Skin);
    c.Line(head.x - 2.4, head.y - 0.3, head.x + 3.0, head.y - 1.2, 0.45, RunnerTones.Sash.Base);
    c.Set((int)Math.Round(head.x + 1.2), (int)Math.Round(head.y - 0.5), RunnerTones.Jewel);
    DrawEye(c, head, eyesClosed);
    if (!eyesClosed) c.Set((int)Math.Round(head.x + 2.8), (int)Math.Round(head.y - 0.4), Palette.Outline);   // the lash
    c.Set((int)Math.Round(head.x + 3.0), (int)Math.Round(head.y + 2.6), RunnerTones.Lips);
}

// ---- A figure that stands: used for the princess at the finish line and for the kiss ----------------

/// <summary>
/// One of the two characters standing, leaning forward by <paramref name="lean"/>. The feet are at <paramref name="ankleY"/>.
/// <paramref name="reach"/> is a point the near arm reaches for, or null to let the arm hang.
/// </summary>
static void DrawStandingFigure(Canvas c, bool princess, double hipX, double ankleY, double lean, (double x, double y)? reach, bool eyesClosed)
{
    var o = princess ? Outfits.Princess : Outfits.Hero;
    var thigh = princess ? 6.0 : 6.2;
    var shin = princess ? 5.5 : 5.7;
    var torso = princess ? 7.4 : 7.8;
    var hip = (x: hipX, y: ankleY - (thigh * Math.Cos(0.14) + shin * Math.Cos(0.04)));
    var shoulder = (x: hip.x + torso * Math.Sin(lean), y: hip.y - torso * Math.Cos(lean));
    var head = (x: shoulder.x + (princess ? 1.9 : 2.0), y: shoulder.y - (princess ? 4.0 : 4.2));

    DrawArmAngles(c, shoulder, 0.12, 0.3, far: true, o);
    DrawLegAngles(c, hip, -0.12, -0.05, far: true, o, thigh, shin);
    if (princess) DrawPrincessTorso(c, hip, shoulder); else DrawHeroTorso(c, hip, shoulder);
    DrawLegAngles(c, hip, 0.14, 0.04, far: false, o, thigh, shin);
    if (princess) DrawPrincessHead(c, head, eyesClosed); else DrawHeroHead(c, head, 0, eyesClosed);
    if (reach is { } target) DrawArmTo(c, shoulder, target, o);
    else DrawArmAngles(c, shoulder, -0.1, 0.5, far: false, o);
}

/// <summary>The two of them at the finish line: they step close, lean in, kiss, and a heart floats up.</summary>
static void DrawCouple(Canvas c, int f)
{
    const double HipX = 15.0, AnkleY = 44.0;
    double[] lean = [0.05, 0.18, 0.30, 0.30, 0.30, 0.30];
    var l = lean[f];
    var closed = f >= 2;

    // His head and shoulder in the cell, so that her arm and his arm can reach each other.
    var heroShoulder = (x: HipX + 7.8 * Math.Sin(l), y: AnkleY - 11.8 - 7.8 * Math.Cos(l));
    var mirror = 47.0;

    var hero = new Canvas(c.W, c.H);
    DrawStandingFigure(hero, princess: false, hipX: HipX, ankleY: AnkleY, lean: l, reach: (x: HipX + 9.5, y: AnkleY - 15.0), eyesClosed: closed);
    hero.Outline(Palette.Outline);

    var princess = new Canvas(c.W, c.H);
    // Her hand reaches his neck. The picture is drawn facing right and flipped, so the point is measured from the mirror.
    DrawStandingFigure(princess, princess: true, hipX: HipX, ankleY: AnkleY, lean: l,
        reach: (x: mirror - heroShoulder.x - 1.0, y: heroShoulder.y - 2.0), eyesClosed: closed);
    princess.Outline(Palette.Outline);

    c.Paste(hero, 0, 0);
    c.Paste(princess.Flipped(), 0, 0);

    if (f >= 2)
    {
        var kiss = (x: mirror / 2.0, y: AnkleY - 11.8 - 7.8 * Math.Cos(l) - 4.2 + 2.6);
        c.Set((int)Math.Round(kiss.x - 0.5), (int)Math.Round(kiss.y), RunnerTones.Lips);
        c.Set((int)Math.Round(kiss.x + 0.5), (int)Math.Round(kiss.y), RunnerTones.Lips);
    }
    // A heart rises, grows and fades.
    if (f >= 3) DrawHeart(c, 24, f switch { 3 => 12, 4 => 9, _ => 5 }, large: f >= 4, fade: f == 5);
}

static void DrawHeart(Canvas c, int cx, int top, bool large, bool fade)
{
    string[] small = [".XX.XX.", "XXXXXXX", "XXXXXXX", ".XXXXX.", "..XXX..", "...X..."];
    string[] big = [".XXX.XXX.", "XXXXXXXXX", "XXXXXXXXX", "XXXXXXXXX", ".XXXXXXX.", "..XXXXX..", "...XXX...", "....X...."];
    var shape = large ? big : small;
    var heart = new Canvas(c.W, c.H);
    var fill = fade ? Color.FromArgb(255, 255, 140, 160) : Color.FromArgb(255, 236, 52, 84);
    var left = cx - shape[0].Length / 2;
    for (var y = 0; y < shape.Length; y++)
        for (var x = 0; x < shape[y].Length; x++)
            if (shape[y][x] == 'X') heart.Set(left + x, top + y, fill);
    heart.Set(left + 1, top + 1, Color.FromArgb(255, 255, 214, 222));   // the shine
    heart.Outline(Color.FromArgb(255, 150, 28, 56));
    c.Paste(heart, 0, 0);
}

static void DrawSurfer(Canvas c, int f)
{
    var t = 2 * Math.PI * f / 8;
    var crouch = 1.1 * Math.Sin(t);                 // the knees bend and straighten as the surfer rides
    var tilt = 0.9 * Math.Sin(t + 0.8);             // the nose of the board rises and falls a little
    double BoardY(double x) => 29.5 - tilt * (x - 15.5) / 12.5;

    // Board: a dark rail under a light deck with a stripe, and a round nose.
    c.Line(4, BoardY(4), 27, BoardY(27), 1.35, Palette.BoardRail);
    c.Line(4, BoardY(4) - 0.2, 27, BoardY(27) - 0.2, 0.8, Palette.Board);
    c.Line(8, BoardY(8) - 0.2, 24, BoardY(24) - 0.2, 0.3, Palette.BoardStripe);
    c.Circle(28, BoardY(28), 0.9, Palette.BoardRail);

    var hip = (x: 15.5, y: 19.0 + crouch);
    var shoulder = (x: 17.5, y: 11.0 + crouch * 0.6);
    var head = (x: 19.0, y: 6.4 + crouch * 0.5);

    // The back arm (darker) reaches behind for balance. The back leg follows.
    var backHand = (x: 8.5, y: 12.0 + 2.2 * Math.Sin(t + 1.2));
    var backElbow = (x: (shoulder.x + backHand.x) / 2, y: (shoulder.y + backHand.y) / 2 + 0.8);
    c.Line(shoulder.x, shoulder.y, backElbow.x, backElbow.y, 0.95, Palette.SkinShade);
    c.Line(backElbow.x, backElbow.y, backHand.x, backHand.y, 0.85, Palette.SkinShade);
    DrawSurfLeg(c, hip, (x: 12.3, y: 24.2 + crouch * 0.3), (x: 10.5, y: BoardY(10.5) - 1.6), Palette.SkinShade);

    // Torso, and swim shorts with a stripe.
    c.Line(hip.x, hip.y, shoulder.x, shoulder.y, 2.0, Palette.Skin);
    c.Circle(hip.x, hip.y + 0.4, 3.1, Palette.Shorts);
    c.Line(hip.x - 2.6, hip.y - 1.4, hip.x + 2.6, hip.y - 1.4, 0.45, Palette.ShortsStripe);

    DrawSurfLeg(c, hip, (x: 20.3, y: 24.0 + crouch * 0.3), (x: 21.5, y: BoardY(21.5) - 1.6), Palette.Skin);

    // Head: sun-bleached hair that streams back in the wind, a face and an eye.
    for (var i = 0; i < 4; i++)
    {
        var tx = head.x - 3.0 - i * 1.7;
        var ty = head.y - 1.0 + 0.5 * i + 1.0 * Math.Sin(t * 2 - i * 0.9);
        c.Line(head.x - 2.0, head.y - 1.2, tx, ty, 0.9 - i * 0.12, Palette.SurfHair);
    }
    c.Circle(head.x - 0.8, head.y - 0.9, 3.3, Palette.SurfHair);
    c.Circle(head.x + 1.0, head.y + 0.8, 2.9, Palette.Skin);
    c.Set((int)Math.Round(head.x + 2.0), (int)Math.Round(head.y + 0.4), Palette.Outline);

    // The front arm reaches ahead.
    var frontHand = (x: 26.0, y: 12.5 + 2.2 * Math.Sin(t));
    var elbow = (x: (shoulder.x + frontHand.x) / 2, y: (shoulder.y + frontHand.y) / 2 + 0.8);
    c.Line(shoulder.x, shoulder.y, elbow.x, elbow.y, 1.0, Palette.Skin);
    c.Line(elbow.x, elbow.y, frontHand.x, frontHand.y, 0.9, Palette.Skin);
}

static void DrawSurfLeg(Canvas c, (double x, double y) hip, (double x, double y) knee, (double x, double y) foot, Color skin)
{
    c.Line(hip.x, hip.y, knee.x, knee.y, 1.6, skin);
    c.Line(knee.x, knee.y, foot.x, foot.y, 1.35, skin);
    c.Circle(foot.x, foot.y + 0.9, 1.1, skin);
}

static void DrawMarathoner(Canvas c, int f)
{
    var t = 2 * Math.PI * f / 8;
    var bob = Math.Round(-1.0 * Math.Abs(Math.Cos(t)));
    var hip = (x: 14.0, y: 17.6 + bob);
    const double lean = 0.18;
    var shoulder = (x: hip.x + 8.5 * Math.Sin(lean), y: hip.y - 8.5 * Math.Cos(lean));
    var head = (x: shoulder.x + 1.6, y: shoulder.y - 4.2);

    // The back arm and the back leg are drawn first, in darker tones.
    DrawMarathonArm(c, shoulder, t + Math.PI, back: true);
    DrawMarathonLeg(c, hip, t + Math.PI, back: true);

    // Torso: an orange singlet with a white race bib, and navy shorts.
    c.Line(hip.x, hip.y, shoulder.x, shoulder.y, 2.4, Palette.Singlet);
    var mx = (int)Math.Round((hip.x + shoulder.x) / 2 + 0.6);
    var my = (int)Math.Round((hip.y + shoulder.y) / 2 - 0.5);
    for (var dy = -1; dy <= 2; dy++)
        for (var dx = -1; dx <= 1; dx++)
            c.Set(mx + dx, my + dy, (dx + dy) % 2 == 0 && dy is > -1 and < 2 ? Palette.BibInk : Palette.Bib);
    c.Circle(hip.x, hip.y + 0.2, 3.1, Palette.RunShorts);
    c.Line(hip.x - 2.8, hip.y - 1.6, hip.x + 2.8, hip.y - 1.6, 0.45, Palette.Singlet);

    DrawMarathonLeg(c, hip, t, back: false);

    // Head: a face under a red cap with a short brim, and an eye.
    c.Circle(head.x + 0.8, head.y + 0.6, 3.0, Palette.Skin);
    c.Circle(head.x - 0.3, head.y - 1.4, 3.0, Palette.Cap);
    c.Line(head.x + 1.2, head.y - 1.2, head.x + 4.4, head.y - 0.6, 0.5, Palette.CapDark);
    c.Set((int)Math.Round(head.x + 2.2), (int)Math.Round(head.y + 0.6), Palette.Outline);

    DrawMarathonArm(c, shoulder, t, back: false);
}

static void DrawMarathonLeg(Canvas c, (double x, double y) hip, double ph, bool back)
{
    var skin = back ? Palette.SkinShade : Palette.Skin;
    var shoe = back ? Palette.ShoeBack : Palette.ShoeBright;
    var a1 = 1.0 * Math.Sin(ph);
    var a2 = a1 - (0.8 + 0.8 * Math.Max(0, Math.Cos(ph)));
    var knee = (x: hip.x + 6.3 * Math.Sin(a1), y: hip.y + 6.3 * Math.Cos(a1));
    var foot = (x: knee.x + 6.0 * Math.Sin(a2), y: knee.y + 6.0 * Math.Cos(a2));
    c.Line(hip.x, hip.y, knee.x, knee.y, 1.7, skin);
    c.Line(knee.x, knee.y, foot.x, foot.y, 1.35, skin);
    c.Circle(foot.x + 0.9, foot.y + 0.2, 1.6, shoe);
    if (!back) c.Line(foot.x - 0.2, foot.y + 1.0, foot.x + 2.0, foot.y + 1.0, 0.35, Palette.ShoeAccent);
}

static void DrawMarathonArm(Canvas c, (double x, double y) shoulder, double ph, bool back)
{
    var skin = back ? Palette.SkinShade : Palette.Skin;
    var b1 = -1.0 * Math.Sin(ph);
    var elbow = (x: shoulder.x + 4.8 * Math.Sin(b1), y: shoulder.y + 4.8 * Math.Cos(b1));
    var b2 = b1 + 1.7;                                  // the elbow stays bent, like a runner's
    var hand = (x: elbow.x + 4.2 * Math.Sin(b2), y: elbow.y + 4.2 * Math.Cos(b2));
    c.Line(shoulder.x, shoulder.y, elbow.x, elbow.y, 1.0, skin);
    c.Line(elbow.x, elbow.y, hand.x, hand.y, 0.9, skin);
}

static void DrawCowboy(Canvas c, int f)
{
    var t = 2 * Math.PI * f / 8;
    var bob = Math.Round(-1.0 * Math.Abs(Math.Cos(t)));
    var hip = (x: 14.0, y: 18.2 + bob);
    const double lean = 0.3;                                  // leans forward: the lava is close behind
    var shoulder = (x: hip.x + 7.8 * Math.Sin(lean), y: hip.y - 7.8 * Math.Cos(lean));
    var head = (x: shoulder.x + 1.8, y: shoulder.y - 3.8);

    // The bandana streams behind the neck.
    var neck = (x: shoulder.x - 0.8, y: shoulder.y - 0.6);
    var prev = neck;
    for (var i = 1; i <= 5; i++)
    {
        var p = (x: neck.x - i * 1.9, y: neck.y + 1.0 * Math.Sin(t * 2 - i * 0.9) * (0.4 + i * 0.2) + i * 0.15);
        c.Line(prev.x, prev.y, p.x, p.y, 1.0, i % 2 == 0 ? Palette.BandanaDark : Palette.Bandana);
        prev = p;
    }

    DrawCowboyArm(c, shoulder, t + Math.PI, back: true);
    DrawCowboyLeg(c, hip, t + Math.PI, back: true);

    // Torso: a cream shirt under a tan vest, and a belt.
    c.Line(hip.x, hip.y, shoulder.x, shoulder.y, 2.3, Palette.Shirt);
    c.Line(hip.x - 0.4, hip.y - 1.0, shoulder.x - 0.4, shoulder.y + 0.8, 1.2, Palette.Vest2);
    c.Circle(hip.x, hip.y + 0.3, 3.1, Palette.Jeans);
    c.Line(hip.x - 2.8, hip.y - 1.8, hip.x + 2.8, hip.y - 1.8, 0.5, Palette.Belt);

    DrawCowboyLeg(c, hip, t, back: false);

    // Head: a wide-eyed face, a bandana at the chin, and a brown hat with a band and a wide brim.
    c.Circle(head.x + 0.8, head.y + 0.8, 2.9, Palette.Skin);
    c.Line(head.x - 5.4, head.y - 1.4, head.x + 5.6, head.y - 2.0, 0.9, Palette.Hat);
    c.Circle(head.x - 0.2, head.y - 3.0, 2.5, Palette.Hat);
    c.Line(head.x - 2.2, head.y - 1.9, head.x + 1.8, head.y - 2.2, 0.4, Palette.HatBand);
    c.Set((int)Math.Round(head.x + 2.2), (int)Math.Round(head.y + 0.3), Palette.Bib);
    c.Set((int)Math.Round(head.x + 2.8), (int)Math.Round(head.y + 0.4), Palette.Outline);
    c.Set((int)Math.Round(head.x + 2.8), (int)Math.Round(head.y + 2.4), Palette.Outline);

    DrawCowboyArm(c, shoulder, t, back: false);
}

static void DrawCowboyLeg(Canvas c, (double x, double y) hip, double ph, bool back)
{
    var jeans = back ? Palette.JeansShade : Palette.Jeans;
    var boot = back ? Palette.BootShade : Palette.Boot;
    var a1 = 1.05 * Math.Sin(ph);
    var a2 = a1 - (0.8 + 0.8 * Math.Max(0, Math.Cos(ph)));
    var knee = (x: hip.x + 6.2 * Math.Sin(a1), y: hip.y + 6.2 * Math.Cos(a1));
    var foot = (x: knee.x + 5.9 * Math.Sin(a2), y: knee.y + 5.9 * Math.Cos(a2));
    c.Line(hip.x, hip.y, knee.x, knee.y, 1.8, jeans);
    c.Line(knee.x, knee.y, foot.x, foot.y, 1.5, jeans);
    c.Circle(foot.x + 0.9, foot.y + 0.1, 1.6, boot);
    c.Set((int)Math.Round(foot.x - 0.6), (int)Math.Round(foot.y + 1.6), boot);   // the boot heel
}

static void DrawCowboyArm(Canvas c, (double x, double y) shoulder, double ph, bool back)
{
    var sleeve = back ? Palette.ShirtShade : Palette.Shirt;
    var skin = back ? Palette.SkinShade : Palette.Skin;
    var b1 = -1.1 * Math.Sin(ph) - 0.2;
    var elbow = (x: shoulder.x + 4.6 * Math.Sin(b1), y: shoulder.y + 4.6 * Math.Cos(b1));
    var b2 = b1 + 1.8;
    var hand = (x: elbow.x + 4.0 * Math.Sin(b2), y: elbow.y + 4.0 * Math.Cos(b2));
    c.Line(shoulder.x, shoulder.y, elbow.x, elbow.y, 1.1, sleeve);
    c.Line(elbow.x, elbow.y, hand.x, hand.y, 0.9, skin);
}

static void DrawSpark(Canvas c, int f)
{
    var prog = f / (double)(SparkFrames - 1);
    var mid = SparkCell / 2.0;
    var radius = 5 + prog * 17;
    Color[] ramp = [Palette.White, Palette.Yellow, Palette.Gold, Palette.Orange, Palette.Orange, Palette.Ember];
    var colour = ramp[f];

    if (f < 4) c.Circle(mid, mid, 7 * (1 - prog) + 1.5, f == 0 ? Palette.White : Palette.Yellow);
    for (var i = 0; i < 8; i++)
    {
        var a = i * Math.PI / 4 + 0.2;
        var r0 = radius * 0.45;
        c.Line(mid + r0 * Math.Cos(a), mid + r0 * Math.Sin(a), mid + radius * Math.Cos(a), mid + radius * Math.Sin(a),
            Math.Max(0.4, 1.6 * (1 - prog)), colour);
    }
    var rnd = new Random(7);
    for (var i = 0; i < 14 - f * 2; i++)
    {
        var a = rnd.NextDouble() * Math.PI * 2;
        var r = radius * (0.5 + rnd.NextDouble() * 0.9);
        var size = f < 3 ? 1.0 : 0.5;
        c.Circle(mid + r * Math.Cos(a), mid + r * Math.Sin(a), size, rnd.Next(2) == 0 ? colour : Palette.Yellow);
    }
}

record SpriteSheet(string File, int Cell, int Frames, Action<Canvas, int> Draw, bool Outline);

static class Palette
{
    public static readonly Color Skin = Color.FromArgb(234, 172, 120), SkinShade = Color.FromArgb(190, 130, 90);
    public static readonly Color Hair = Color.FromArgb(52, 36, 44);
    public static readonly Color Vest = Color.FromArgb(32, 160, 172), VestDark = Color.FromArgb(20, 110, 124);
    public static readonly Color Trousers = Color.FromArgb(246, 238, 214), TrousersShade = Color.FromArgb(196, 184, 156);
    public static readonly Color Sash = Color.FromArgb(232, 182, 52);
    public static readonly Color Scarf = Color.FromArgb(226, 92, 42), ScarfDark = Color.FromArgb(170, 60, 32);
    public static readonly Color Shoe = Color.FromArgb(128, 74, 42), ShoeShade = Color.FromArgb(90, 52, 32);
    public static readonly Color SurfHair = Color.FromArgb(236, 200, 96);
    public static readonly Color Board = Color.FromArgb(250, 244, 226), BoardRail = Color.FromArgb(24, 110, 140), BoardStripe = Color.FromArgb(240, 112, 52);
    public static readonly Color Shorts = Color.FromArgb(232, 70, 96), ShortsStripe = Color.FromArgb(255, 226, 120);
    public static readonly Color Singlet = Color.FromArgb(242, 120, 40), Bib = Color.FromArgb(250, 250, 246), BibInk = Color.FromArgb(40, 44, 120);
    public static readonly Color RunShorts = Color.FromArgb(36, 52, 110), Cap = Color.FromArgb(214, 52, 52), CapDark = Color.FromArgb(150, 34, 38);
    public static readonly Color ShoeBright = Color.FromArgb(248, 248, 250), ShoeBack = Color.FromArgb(188, 190, 202), ShoeAccent = Color.FromArgb(110, 214, 84);
    public static readonly Color Hat = Color.FromArgb(150, 94, 52), HatBand = Color.FromArgb(232, 190, 70);
    public static readonly Color Bandana = Color.FromArgb(210, 52, 60), BandanaDark = Color.FromArgb(152, 34, 46);
    public static readonly Color Shirt = Color.FromArgb(242, 232, 206), ShirtShade = Color.FromArgb(196, 184, 156), Vest2 = Color.FromArgb(176, 120, 70);
    public static readonly Color Jeans = Color.FromArgb(52, 92, 168), JeansShade = Color.FromArgb(34, 62, 120), Belt = Color.FromArgb(120, 70, 36);
    public static readonly Color Boot = Color.FromArgb(120, 70, 40), BootShade = Color.FromArgb(84, 48, 30);
    public static readonly Color Outline = Color.FromArgb(34, 22, 34);
    public static readonly Color White = Color.FromArgb(255, 255, 240), Yellow = Color.FromArgb(255, 232, 90);
    public static readonly Color Gold = Color.FromArgb(255, 196, 40), Orange = Color.FromArgb(255, 130, 30), Ember = Color.FromArgb(200, 70, 30);
}

class Canvas
{
    private readonly Color[,] px;
    public int W { get; }
    public int H { get; }

    public Canvas(int w, int h) { W = w; H = h; px = new Color[w, h]; }
    public Canvas(int w, int h, Color[,] _) : this(w, h) { }

    public void Set(int x, int y, Color c) { if (x >= 0 && y >= 0 && x < W && y < H) px[x, y] = c; }

    /// <summary>The same picture turned left to right.</summary>
    public Canvas Flipped()
    {
        var flipped = new Canvas(W, H);
        for (var y = 0; y < H; y++)
            for (var x = 0; x < W; x++)
                flipped.px[W - 1 - x, y] = px[x, y];
        return flipped;
    }

    public void Circle(double cx, double cy, double r, Color c)
    {
        for (var y = (int)Math.Floor(cy - r); y <= Math.Ceiling(cy + r); y++)
            for (var x = (int)Math.Floor(cx - r); x <= Math.Ceiling(cx + r); x++)
                if ((x - cx) * (x - cx) + (y - cy) * (y - cy) <= r * r) Set(x, y, c);
    }

    public void Line(double x0, double y0, double x1, double y1, double r, Color c)
    {
        var steps = (int)Math.Ceiling(Math.Max(Math.Abs(x1 - x0), Math.Abs(y1 - y0)) * 2) + 1;
        for (var i = 0; i <= steps; i++)
        {
            var k = i / (double)steps;
            Circle(x0 + (x1 - x0) * k, y0 + (y1 - y0) * k, r, c);
        }
    }

    public void Outline(Color c)
    {
        var edge = new List<(int, int)>();
        for (var y = 0; y < H; y++)
            for (var x = 0; x < W; x++)
            {
                if (px[x, y].A != 0) continue;
                foreach (var (dx, dy) in new[] { (1, 0), (-1, 0), (0, 1), (0, -1) })
                {
                    int nx = x + dx, ny = y + dy;
                    if (nx >= 0 && ny >= 0 && nx < W && ny < H && px[nx, ny].A != 0) { edge.Add((x, y)); break; }
                }
            }
        foreach (var (x, y) in edge) px[x, y] = c;
    }

    public void Paste(Canvas other, int ox, int oy)
    {
        for (var y = 0; y < other.H; y++)
            for (var x = 0; x < other.W; x++)
                if (other.px[x, y].A != 0) Set(ox + x, oy + y, other.px[x, y]);
    }

    public void PasteScaled(Canvas other, int ox, int oy, int scale)
    {
        for (var y = 0; y < other.H * scale; y++)
            for (var x = 0; x < other.W * scale; x++)
            {
                var p = other.px[x / scale, y / scale];
                if (p.A != 0) Set(ox + x, oy + y, p);
            }
    }

    public void FillBackground(Color c)
    {
        for (var y = 0; y < H; y++) for (var x = 0; x < W; x++) px[x, y] = c;
    }

    public void Save(string path, int scale)
    {
        using var bmp = new Bitmap(W * scale, H * scale, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        for (var y = 0; y < H * scale; y++)
            for (var x = 0; x < W * scale; x++)
                bmp.SetPixel(x, y, px[x / scale, y / scale]);
        bmp.Save(path, System.Drawing.Imaging.ImageFormat.Png);
    }
}

/// <summary>A colour in three tones: the base, a darker shade and a lighter highlight. The light comes from the top left.</summary>
record Tone(Color Base, Color Shade, Color Light);

/// <summary>The colours of the runner.</summary>
static class RunnerTones
{
    public static readonly Tone Skin = new(Color.FromArgb(236, 176, 128), Color.FromArgb(196, 128, 92), Color.FromArgb(252, 210, 164));
    public static readonly Tone SkinFar = new(Color.FromArgb(196, 138, 100), Color.FromArgb(160, 106, 78), Color.FromArgb(214, 160, 120));
    public static readonly Tone Hair = new(Color.FromArgb(58, 40, 50), Color.FromArgb(34, 22, 32), Color.FromArgb(104, 76, 86));
    public static readonly Tone Vest = new(Color.FromArgb(30, 168, 176), Color.FromArgb(16, 106, 126), Color.FromArgb(104, 220, 218));
    public static readonly Tone Trousers = new(Color.FromArgb(248, 240, 220), Color.FromArgb(198, 184, 160), Color.FromArgb(255, 255, 248));
    public static readonly Tone TrousersFar = new(Color.FromArgb(206, 194, 170), Color.FromArgb(160, 148, 128), Color.FromArgb(226, 216, 194));
    public static readonly Tone Sash = new(Color.FromArgb(240, 190, 56), Color.FromArgb(182, 128, 30), Color.FromArgb(255, 226, 120));
    public static readonly Tone SashFar = new(Color.FromArgb(190, 146, 44), Color.FromArgb(146, 102, 26), Color.FromArgb(212, 176, 84));
    public static readonly Tone Shoe = new(Color.FromArgb(144, 86, 48), Color.FromArgb(94, 54, 34), Color.FromArgb(190, 126, 76));
    public static readonly Tone ShoeFar = new(Color.FromArgb(104, 62, 38), Color.FromArgb(70, 40, 28), Color.FromArgb(140, 90, 56));
    public static readonly Color ScarfLight = Color.FromArgb(255, 150, 80);
    public static readonly Tone PrincessHair = new(Color.FromArgb(34, 28, 52), Color.FromArgb(18, 14, 30), Color.FromArgb(92, 84, 128));
    public static readonly Tone PrincessTop = new(Color.FromArgb(232, 92, 140), Color.FromArgb(160, 52, 98), Color.FromArgb(255, 160, 192));
    public static readonly Tone PrincessTrousers = new(Color.FromArgb(192, 156, 232), Color.FromArgb(132, 98, 178), Color.FromArgb(228, 206, 252));
    public static readonly Tone PrincessTrousersFar = new(Color.FromArgb(150, 122, 190), Color.FromArgb(110, 84, 150), Color.FromArgb(180, 156, 214));
    public static readonly Tone Slipper = new(Color.FromArgb(240, 190, 56), Color.FromArgb(182, 128, 30), Color.FromArgb(255, 226, 120));
    public static readonly Tone SlipperFar = new(Color.FromArgb(190, 146, 44), Color.FromArgb(146, 102, 26), Color.FromArgb(212, 176, 84));
    public static readonly Color Jewel = Color.FromArgb(60, 190, 200), Lips = Color.FromArgb(214, 52, 80);
}

/// <summary>Shapes that are shaded with a <see cref="Tone"/>: the shade fills the shape, the base moves towards the light, and a small highlight sits on the largest shapes.</summary>
static class ToneShading
{
    public static void Ball(this Canvas c, double cx, double cy, double r, Tone tone)
    {
        c.Circle(cx, cy, r, tone.Shade);
        c.Circle(cx - r * 0.18, cy - r * 0.22, r * 0.82, tone.Base);
        if (r >= 2.6) c.Circle(cx - r * 0.38, cy - r * 0.44, r * 0.32, tone.Light);
    }

    public static void Limb(this Canvas c, double x0, double y0, double x1, double y1, double r, Tone tone)
    {
        c.Line(x0, y0, x1, y1, r, tone.Shade);
        c.Line(x0 - r * 0.2, y0 - r * 0.25, x1 - r * 0.2, y1 - r * 0.25, r * 0.78, tone.Base);
        if (r >= 2.2) c.Line(x0 - r * 0.4, y0 - r * 0.5, x1 - r * 0.4, y1 - r * 0.5, r * 0.28, tone.Light);
    }
}

/// <summary>The colours of one character's clothes and skin. The far arm and the far leg use the darker versions.</summary>
record Outfit(Tone Trousers, Tone TrousersFar, Tone Cuff, Tone CuffFar, Tone Shoe, Tone ShoeFar, Tone Skin, Tone SkinFar);

static class Outfits
{
    public static readonly Outfit Hero = new(RunnerTones.Trousers, RunnerTones.TrousersFar, RunnerTones.Sash, RunnerTones.SashFar,
        RunnerTones.Shoe, RunnerTones.ShoeFar, RunnerTones.Skin, RunnerTones.SkinFar);

    // The princess has lilac trousers, gold cuffs and gold slippers.
    public static readonly Outfit Princess = new(RunnerTones.PrincessTrousers, RunnerTones.PrincessTrousersFar, RunnerTones.Sash, RunnerTones.SashFar,
        RunnerTones.Slipper, RunnerTones.SlipperFar, RunnerTones.Skin, RunnerTones.SkinFar);
}
