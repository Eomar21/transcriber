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
    var bob = Math.Round(-1.5 * Math.Abs(Math.Cos(t)));
    var hip = (x: 14.0, y: 17.0 + bob);
    const double lean = 0.25;
    var shoulder = (x: hip.x + 8 * Math.Sin(lean), y: hip.y - 8 * Math.Cos(lean));
    var head = (x: shoulder.x + 2.0, y: shoulder.y - 5.0);

    // Back arm and back leg are drawn first and use darker tones.
    DrawArm(c, shoulder, t + Math.PI, back: true);
    DrawLeg(c, hip, t + Math.PI, back: true);

    // Torso: vest over a sash and baggy trousers.
    c.Line(hip.x, hip.y, shoulder.x, shoulder.y, 1.9, Palette.Vest);
    c.Line(shoulder.x + 1, shoulder.y + 1, hip.x + 1, hip.y - 2, 0.7, Palette.Skin); // open chest
    c.Circle(hip.x, hip.y, 3.4, Palette.Trousers);
    c.Line(hip.x - 3, hip.y - 2, hip.x + 3, hip.y - 2, 0.9, Palette.Sash);

    DrawLeg(c, hip, t, back: false);

    // Scarf streaming behind the neck.
    var neck = (x: shoulder.x - 1.0, y: shoulder.y - 1.0);
    var prev = neck;
    for (var i = 1; i <= 7; i++)
    {
        var p = (x: neck.x - i * 2.1, y: neck.y + 1.4 * Math.Sin(t * 2 - i * 0.9) * (0.4 + i * 0.22) + i * 0.2);
        c.Line(prev.x, prev.y, p.x, p.y, 1.1, i % 2 == 0 ? Palette.ScarfDark : Palette.Scarf);
        prev = p;
    }

    // Head: dark hair, face, gold headband, one eye.
    c.Circle(head.x - 1, head.y - 1.2, 4.4, Palette.Hair);
    c.Circle(head.x + 1, head.y + 0.8, 3.2, Palette.Skin);
    c.Line(head.x - 2, head.y - 1.6, head.x + 3.5, head.y - 1.6, 0.45, Palette.Sash);
    c.Set((int)Math.Round(head.x + 2.2), (int)Math.Round(head.y + 0.4), Palette.Outline);

    DrawArm(c, shoulder, t, back: false);
}

static void DrawLeg(Canvas c, (double x, double y) hip, double ph, bool back)
{
    var trousers = back ? Palette.TrousersShade : Palette.Trousers;
    var shoe = back ? Palette.ShoeShade : Palette.Shoe;
    var a1 = 0.95 * Math.Sin(ph);
    var a2 = a1 - (0.7 + 0.7 * Math.Max(0, Math.Cos(ph)));
    var knee = (x: hip.x + 6.5 * Math.Sin(a1), y: hip.y + 6.5 * Math.Cos(a1));
    var foot = (x: knee.x + 6.0 * Math.Sin(a2), y: knee.y + 6.0 * Math.Cos(a2));
    c.Line(hip.x, hip.y, knee.x, knee.y, 1.7, trousers);
    c.Line(knee.x, knee.y, foot.x, foot.y, 1.4, trousers);
    c.Circle(foot.x + 0.8, foot.y, 1.5, shoe);
}

static void DrawArm(Canvas c, (double x, double y) shoulder, double ph, bool back)
{
    var skin = back ? Palette.SkinShade : Palette.Skin;
    var b1 = -0.9 * Math.Sin(ph);
    var elbow = (x: shoulder.x + 5 * Math.Sin(b1), y: shoulder.y + 5 * Math.Cos(b1));
    var b2 = b1 + 1.3;
    var hand = (x: elbow.x + 4.5 * Math.Sin(b2), y: elbow.y + 4.5 * Math.Cos(b2));
    c.Line(shoulder.x, shoulder.y, elbow.x, elbow.y, 1.0, back ? Palette.VestDark : Palette.Vest);
    c.Line(elbow.x, elbow.y, hand.x, hand.y, 0.9, skin);
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
