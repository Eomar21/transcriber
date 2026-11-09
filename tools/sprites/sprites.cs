#:package System.Drawing.Common@10.0.0
using System.Drawing;

// Draws the runner and the spark as pixel art. Usage: dotnet run sprites.cs <output folder>
var outDir = args.Length > 0 ? args[0] : ".";
Directory.CreateDirectory(outDir);

const int RunnerCell = 32, RunnerFrames = 8;
const int SparkCell = 48, SparkFrames = 6;

var runner = new Canvas(RunnerCell * RunnerFrames, RunnerCell);
for (var f = 0; f < RunnerFrames; f++)
{
    var cell = new Canvas(RunnerCell, RunnerCell);
    DrawRunner(cell, f);
    cell.Outline(Palette.Outline);
    runner.Paste(cell, f * RunnerCell, 0);
}

var spark = new Canvas(SparkCell * SparkFrames, SparkCell);
for (var f = 0; f < SparkFrames; f++)
{
    var cell = new Canvas(SparkCell, SparkCell);
    DrawSpark(cell, f);
    spark.Paste(cell, f * SparkCell, 0);
}

runner.Save(Path.Combine(outDir, "runner.png"), 1);
spark.Save(Path.Combine(outDir, "spark.png"), 1);

// A magnified picture for looking at the frames. Not used by the app.
var preview = new Canvas(spark.W * 5 + 20, 32 * 5 + SparkCell * 5 + 30, new Color[0, 0]);
preview.FillBackground(Color.FromArgb(70, 90, 140));
preview.PasteScaled(runner, 10, 10, 5);
preview.PasteScaled(spark, 10, 32 * 5 + 20, 5);
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

static class Palette
{
    public static readonly Color Skin = Color.FromArgb(234, 172, 120), SkinShade = Color.FromArgb(190, 130, 90);
    public static readonly Color Hair = Color.FromArgb(52, 36, 44);
    public static readonly Color Vest = Color.FromArgb(32, 160, 172), VestDark = Color.FromArgb(20, 110, 124);
    public static readonly Color Trousers = Color.FromArgb(246, 238, 214), TrousersShade = Color.FromArgb(196, 184, 156);
    public static readonly Color Sash = Color.FromArgb(232, 182, 52);
    public static readonly Color Scarf = Color.FromArgb(226, 92, 42), ScarfDark = Color.FromArgb(170, 60, 32);
    public static readonly Color Shoe = Color.FromArgb(128, 74, 42), ShoeShade = Color.FromArgb(90, 52, 32);
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
