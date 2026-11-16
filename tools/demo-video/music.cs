// Makes the music for the demo video. The track is made by this program from sine waves and noise, so it has no
// third-party licence and is covered by the licence of this repository.
// Usage: dotnet run music.cs <seconds> <output.wav>
// Tempo, key and chords are the constants below. Change them and run the program again to get a different track.
using System.Text;

if (args.Length < 2) { Console.Error.WriteLine("Usage: music.cs <seconds> <output.wav>"); return 1; }
var seconds = double.Parse(args[0], System.Globalization.CultureInfo.InvariantCulture);
const int Rate = 48000;
const double Bpm = 100;
const double Beat = 60.0 / Bpm;
const double Bar = Beat * 4;
var samples = (int)(seconds * Rate);
var left = new float[samples];
var right = new float[samples];
var random = new Random(7);

// One chord for each bar: the bass note and the notes of the pad (MIDI numbers). A minor, then F, C and G.
(int Bass, int[] Pad)[] chords =
[
    (45, [57, 60, 64, 67, 71]),   // A minor 9
    (41, [53, 57, 60, 64, 69]),   // F major 7
    (48, [52, 55, 59, 64, 67]),   // C major 7
    (43, [50, 55, 59, 62, 64]),   // G 6/9
];
int[] arpPattern = [0, 2, 4, 2, 3, 1, 2, 4];

static double Hz(int midi) => 440.0 * Math.Pow(2, (midi - 69) / 12.0);
static double Tau(double t) => 2 * Math.PI * t;
var bars = (int)Math.Ceiling(seconds / Bar);
var drumsFrom = 2;                       // bars with no drums at the start
var drumsTo = bars - 2;                  // and at the end

// Separate buses, so the pad and the arpeggio can have reverb and the drums stay dry.
var wetL = new float[samples]; var wetR = new float[samples];
var drumL = new float[samples]; var drumR = new float[samples];
var kickEnvelope = new float[samples];   // used to duck the pad on each kick

void Add(float[] l, float[] r, int start, int index, double value, double pan)
{
    var i = start + index;
    if (i < 0 || i >= samples) return;
    l[i] += (float)(value * (pan > 0 ? 1 - pan : 1));
    r[i] += (float)(value * (pan < 0 ? 1 + pan : 1));
}

// ---- Kick and clap and hat. ----
for (var bar = drumsFrom; bar < drumsTo; bar++)
{
    var t0 = bar * Bar;
    foreach (var beat in new[] { 0.0, 2.0 })
        Kick(t0 + beat * Beat, 1.0);
    Kick(t0 + 2.5 * Beat, 0.55);
    foreach (var beat in new[] { 1.0, 3.0 }) Clap(t0 + beat * Beat, 0.5);
    for (var e = 0; e < 8; e++) Hat(t0 + (e + 0.5) * Beat / 2 * 1.0 + (e % 2 == 1 ? 0 : 0), e % 2 == 1 ? 0.30 : 0.14);
}

void Kick(double time, double gain)
{
    var start = (int)(time * Rate);
    var length = (int)(0.32 * Rate);
    var phase = 0.0;
    for (var n = 0; n < length; n++)
    {
        var t = n / (double)Rate;
        var frequency = 46 + 90 * Math.Exp(-t * 28);
        phase += Tau(frequency) / Rate;
        var env = Math.Exp(-t * 11) * Math.Min(1, t / 0.002);
        var v = Math.Sin(phase) * env * 0.85 * gain;
        Add(drumL, drumR, start, n, v, 0);
        if (start + n < samples) kickEnvelope[start + n] = Math.Max(kickEnvelope[start + n], (float)(env * gain));
    }
}

void Clap(double time, double gain)
{
    var start = (int)(time * Rate);
    var length = (int)(0.28 * Rate);
    double lp = 0, hp = 0;
    for (var n = 0; n < length; n++)
    {
        var t = n / (double)Rate;
        var noise = random.NextDouble() * 2 - 1;
        lp += 0.35 * (noise - lp);
        hp += 0.05 * (lp - hp);
        var band = lp - hp;
        var burst = Math.Exp(-t * 55) + 0.6 * Math.Exp(-Math.Max(0, t - 0.012) * 40) * (t > 0.012 ? 1 : 0);
        var tone = Math.Sin(Tau(190 * t)) * Math.Exp(-t * 28) * 0.25;
        Add(drumL, drumR, start, n, (band * 2.2 * burst * Math.Exp(-t * 12) + tone) * 0.30 * gain, 0.08);
    }
}

void Hat(double time, double gain)
{
    var start = (int)(time * Rate);
    var length = (int)(0.07 * Rate);
    double last = 0;
    for (var n = 0; n < length; n++)
    {
        var t = n / (double)Rate;
        var noise = random.NextDouble() * 2 - 1;
        var high = noise - last * 0.92;                   // a simple high pass
        last = noise;
        Add(drumL, drumR, start, n, high * Math.Exp(-t * 85) * 0.13 * gain, -0.15);
    }
}

// ---- Pad, bass and arpeggio. ----
for (var bar = 0; bar < bars; bar++)
{
    var chord = chords[bar % chords.Length];
    var t0 = bar * Bar;
    var barStart = (int)(t0 * Rate);
    var barLength = (int)(Bar * Rate) + (int)(0.9 * Rate);         // the pad rings into the next bar
    var cutoffBase = 1100 + 900 * (0.5 + 0.5 * Math.Sin(Tau(t0 / 38.0)));

    // Pad: a few detuned, soft saw voices for each note.
    foreach (var midi in chord.Pad)
    {
        var f = Hz(midi);
        foreach (var detune in new[] { -0.0035, 0.0, 0.0035 })
        {
            var fd = f * (1 + detune);
            var pan = detune * 90;
            for (var n = 0; n < barLength; n++)
            {
                var t = n / (double)Rate;
                var attack = Math.Min(1, t / 0.5);
                var release = t < Bar ? 1 : Math.Exp(-(t - Bar) * 4.0);
                var env = attack * release * 0.020;
                double v = 0;
                for (var h = 1; h <= 7; h++)
                {
                    var fh = fd * h;
                    if (fh > 9000) break;
                    v += Math.Sin(Tau(fh * (t0 + t))) / h / (1 + Math.Pow(fh / cutoffBase, 2));
                }
                Add(wetL, wetR, barStart, n, v * env, pan);
            }
        }
    }

    // Bass: a round sine note on the first beat and a short one before the third beat.
    foreach (var (beat, len, gain) in new[] { (0.0, 1.7, 1.0), (2.5, 0.8, 0.7) })
    {
        var f = Hz(chord.Bass);
        var start = (int)((t0 + beat * Beat) * Rate);
        var length = (int)(len * Beat * Rate * 1.4);
        for (var n = 0; n < length; n++)
        {
            var t = n / (double)Rate;
            var env = Math.Min(1, t / 0.01) * Math.Exp(-t * 2.4) * gain;
            var v = (Math.Sin(Tau(f * t)) + 0.28 * Math.Sin(Tau(f * 2 * t)) + 0.08 * Math.Sin(Tau(f * 3 * t))) * env * 0.42;
            Add(drumL, drumR, start, n, v, 0);
        }
    }

    // Arpeggio: a soft pluck on each eighth note. The first two bars are quiet and the notes build up.
    var arpLevel = bar < drumsFrom ? 0.55 : 1.0;
    for (var step = 0; step < 8; step++)
    {
        if (bar >= drumsTo && step % 2 == 1) continue;               // thinner at the end
        var note = chord.Pad[arpPattern[step] % chord.Pad.Length] + 12;
        var f = Hz(note);
        var start = (int)((t0 + step * Beat / 2) * Rate);
        var length = (int)(0.9 * Rate);
        var gain = (step % 4 == 0 ? 1.0 : 0.75) * arpLevel;
        for (var n = 0; n < length; n++)
        {
            var t = n / (double)Rate;
            var env = Math.Min(1, t / 0.003) * Math.Exp(-t * 6.5) * gain;
            var v = (Math.Sin(Tau(f * t)) + 0.38 * Math.Sin(Tau(2 * f * t)) * Math.Exp(-t * 9) + 0.12 * Math.Sin(Tau(3 * f * t)) * Math.Exp(-t * 14)) * env * 0.16;
            Add(wetL, wetR, start, n, v, (step % 2 == 0 ? -0.35 : 0.35));
        }
    }
}

// ---- Duck the wet bus on the kick, then add delay and reverb to it. ----
for (var i = 0; i < samples; i++)
{
    var duck = 1 - 0.45 * Math.Min(1, kickEnvelope[i]);
    wetL[i] *= (float)duck;
    wetR[i] *= (float)duck;
}

// Ping pong delay: a dotted eighth note.
var delay = (int)(0.75 * Beat * Rate);
var delayL = new float[samples]; var delayR = new float[samples];
for (var i = 0; i < samples; i++)
{
    var fromL = i >= delay ? delayR[i - delay] * 0.42f : 0;
    var fromR = i >= delay ? delayL[i - delay] * 0.42f : 0;
    delayL[i] = wetL[i] * 0.6f + fromL;
    delayR[i] = wetR[i] * 0.6f + fromR;
}
for (var i = 0; i < samples; i++) { wetL[i] += delayL[i] * 0.45f; wetR[i] += delayR[i] * 0.45f; }

// A small reverb: parallel comb filters and two all pass filters, a little different for each side.
float[] Reverb(float[] input, int shift)
{
    int[] combs = [1557 + shift, 1617 + shift, 1491 + shift, 1422 + shift, 1277 + shift, 1356 + shift];
    var outputBuffer = new float[samples];
    foreach (var d in combs)
    {
        var buffer = new float[d];
        var lp = 0f;
        var pos = 0;
        for (var i = 0; i < samples; i++)
        {
            var y = buffer[pos];
            lp += 0.55f * (y - lp);
            buffer[pos] = input[i] + lp * 0.86f;
            pos = (pos + 1) % d;
            outputBuffer[i] += y * 0.16f;
        }
    }
    foreach (var d in new[] { 225 + shift / 3, 556 + shift / 3 })
    {
        var buffer = new float[d];
        var pos = 0;
        for (var i = 0; i < samples; i++)
        {
            var bufOut = buffer[pos];
            var x = outputBuffer[i];
            var y = -x + bufOut;
            buffer[pos] = x + bufOut * 0.5f;
            outputBuffer[i] = y;
            pos = (pos + 1) % d;
        }
    }
    return outputBuffer;
}
var reverbL = Reverb(wetL, 0);
var reverbR = Reverb(wetR, 23);

// ---- Mix, soft limit, fade, write. ----
var peak = 0f;
for (var i = 0; i < samples; i++)
{
    var t = i / (double)Rate;
    var fade = Math.Min(1, t / 2.0) * Math.Min(1, (seconds - t) / 3.5);
    var l = (wetL[i] * 0.80 + reverbL[i] * 0.55 + drumL[i]) * fade;
    var r = (wetR[i] * 0.80 + reverbR[i] * 0.55 + drumR[i]) * fade;
    left[i] = (float)Math.Tanh(l * 1.3);
    right[i] = (float)Math.Tanh(r * 1.3);
    peak = Math.Max(peak, Math.Max(Math.Abs(left[i]), Math.Abs(right[i])));
}
var scale = 0.5f / peak;                                            // peaks at about minus six decibels full scale: a quiet bed under the video

using (var file = File.Create(args[1]))
using (var writer = new BinaryWriter(file))
{
    var dataLength = samples * 4;
    writer.Write(Encoding.ASCII.GetBytes("RIFF")); writer.Write(36 + dataLength); writer.Write(Encoding.ASCII.GetBytes("WAVEfmt "));
    writer.Write(16); writer.Write((short)1); writer.Write((short)2); writer.Write(Rate); writer.Write(Rate * 4); writer.Write((short)4); writer.Write((short)16);
    writer.Write(Encoding.ASCII.GetBytes("data")); writer.Write(dataLength);
    for (var i = 0; i < samples; i++)
    {
        writer.Write((short)Math.Round(left[i] * scale * 32767));
        writer.Write((short)Math.Round(right[i] * scale * 32767));
    }
}
Console.WriteLine($"Wrote {args[1]}: {seconds:F1} seconds, {bars} bars at {Bpm} beats a minute.");
return 0;
