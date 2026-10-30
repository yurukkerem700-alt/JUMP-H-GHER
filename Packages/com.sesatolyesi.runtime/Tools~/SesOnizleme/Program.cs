// Ses Atölyesi — önizleme ve doğrulama aracı.
// Tüm sesleri üretir, WAV olarak yazar (kulakla dinlemek için) ve sayısal kontroller yapar.
// Kullanım: dotnet run -c Release -- [çıktı-klasörü]
using System.Diagnostics;
using System.Numerics;
using SesAtolyesi.Dsp;

string outDir = args.Length > 0 ? args[0] : "out";
Directory.CreateDirectory(outDir);

var cases = new List<(string name, int rate, bool loop, Func<float[]> gen)>();
void Add(string n, int rate, bool loop, Func<float[]> g) => cases.Add((n, rate, loop, g));

foreach (SurfaceType s in Enum.GetValues(typeof(SurfaceType)))
{
    var st = s;
    Add($"footstep_{st}", 44100, false, () => Recipes.Footstep(st, 1f, 1));
    Add($"jump_{st}", 44100, false, () => Recipes.Jump(st, 1));
    Add($"land_{st}", 44100, false, () => Recipes.Land(st, 0.7f, 1));
}
Add("footstep_Stone_light", 44100, false, () => Recipes.Footstep(SurfaceType.Stone, 0.6f, 1));
Add("footstep_Stone_heavy", 44100, false, () => Recipes.Footstep(SurfaceType.Stone, 1.8f, 1));
Add("footstep_Grass_heavy", 44100, false, () => Recipes.Footstep(SurfaceType.Grass, 1.8f, 1));
Add("impact_Wood_light", 44100, false, () => Recipes.Impact(SurfaceType.Wood, 0.3f, 0.6f, 1));
Add("impact_Wood_heavy", 44100, false, () => Recipes.Impact(SurfaceType.Wood, 20f, 0.9f, 1));
Add("impact_Metal", 44100, false, () => Recipes.Impact(SurfaceType.Metal, 2f, 0.8f, 1));
Add("impact_Stone", 44100, false, () => Recipes.Impact(SurfaceType.Stone, 5f, 0.8f, 1));
Add("coin", 44100, false, () => Recipes.Coin(1));
Add("pickup", 44100, false, () => Recipes.Pickup(1));
Add("hit", 44100, false, () => Recipes.Hit(1));
Add("swoosh", 44100, false, () => Recipes.Swoosh(0.35f, 1));
Add("ui_click", 44100, false, () => Recipes.UiClick(1));
Add("ui_confirm", 44100, false, () => Recipes.UiConfirm(1));
Add("thunder_near", 32000, false, () => Recipes.Thunder(0.15f, 1));
Add("thunder_far", 32000, false, () => Recipes.Thunder(0.85f, 1));
Add("wind_light", 32000, true, () => Recipes.Wind(0.25f, 1));
Add("wind_strong", 32000, true, () => Recipes.Wind(0.9f, 1));
Add("leaves", 32000, true, () => Recipes.Leaves(0.6f, 1));
Add("rain_light", 32000, true, () => Recipes.Rain(0.25f, 1));
Add("rain_heavy", 32000, true, () => Recipes.Rain(0.9f, 1));
Add("fire", 32000, true, () => Recipes.Fire(1));
Add("stream", 32000, true, () => Recipes.Stream(1));
Add("crickets", 32000, true, () => Recipes.Crickets(1));

int fail = 0;
void Fail(string m) { fail++; Console.WriteLine("  !! HATA: " + m); }

var centroids = new Dictionary<string, double>();
var lengths = new Dictionary<string, float[]>();
double totalMs = 0;

Console.WriteLine($"{"ses",-24}{"süre(s)",8}{"tepe dB",9}{"rms dB",8}{"merkez Hz",11}{"üretim ms",11}");
foreach (var c in cases)
{
    var sw = Stopwatch.StartNew();
    var d = c.gen();
    sw.Stop();
    totalMs += sw.Elapsed.TotalMilliseconds;
    lengths[c.name] = d;

    double peak = 0, sum = 0;
    bool bad = false;
    foreach (var v in d)
    {
        if (float.IsNaN(v) || float.IsInfinity(v)) bad = true;
        peak = Math.Max(peak, Math.Abs(v)); sum += (double)v * v;
    }
    double rms = Math.Sqrt(sum / d.Length);
    double cen = Centroid(d, c.rate, c.loop);
    centroids[c.name] = cen;
    double dur = d.Length / (double)c.rate;
    Console.WriteLine($"{c.name,-24}{dur,8:F2}{20 * Math.Log10(peak + 1e-12),9:F1}{20 * Math.Log10(rms + 1e-12),8:F1}{cen,11:F0}{sw.Elapsed.TotalMilliseconds,11:F1}");

    if (bad) Fail($"{c.name}: NaN/sonsuz değer var");
    if (peak > 1.0) Fail($"{c.name}: tepe 1.0'ı aşıyor (patlama/cızırtı)");
    if (peak < 0.05) Fail($"{c.name}: ses neredeyse sessiz");
    if (!c.loop && Math.Abs(d[0]) > 0.02) Fail($"{c.name}: başlangıç tıkırtısı (ilk örnek {d[0]:F3})");
    if (!c.loop && Math.Abs(d[^1]) > 0.02) Fail($"{c.name}: bitiş tıkırtısı (son örnek {d[^1]:F3})");
    if (sw.Elapsed.TotalMilliseconds > 1000) Fail($"{c.name}: üretim çok yavaş");
    if (c.loop && !SeamOk(d)) Fail($"{c.name}: döngü birleşim yerinde süreksizlik var");
    WriteWav(Path.Combine(outDir, c.name + ".wav"), d, c.rate);
}
Console.WriteLine($"\nToplam üretim süresi: {totalMs:F0} ms ({cases.Count} ses)");

// Değişim (varyasyon) kontrolü: farklı tohum = gerçekten farklı ses mi?
Console.WriteLine("\nVaryasyon (korelasyon; 1 = birebir aynı, 0 = tamamen farklı):");
foreach (SurfaceType s in Enum.GetValues(typeof(SurfaceType)))
{
    var a = Recipes.Footstep(s, 1f, 1); var b = Recipes.Footstep(s, 1f, 2);
    double corr = Math.Abs(Correlation(a, b));
    Console.WriteLine($"  footstep {s,-7} {corr:F3}");
    if (corr > 0.9) Fail($"footstep {s}: farklı tohumlar neredeyse aynı ses veriyor");
}

// Fiziksel mantık kontrolleri
Console.WriteLine("\nMantık kontrolleri:");
void Expect(string label, bool ok) { Console.WriteLine($"  {(ok ? "ok " : "!! ")}{label}"); if (!ok) fail++; }
Expect("ağır adım hafif adımdan daha pes", centroids["footstep_Stone_heavy"] < centroids["footstep_Stone_light"]);
Expect("güçlü rüzgar daha tiz", centroids["wind_strong"] > centroids["wind_light"]);
Expect("uzak gök gürültüsü yakından daha pes", centroids["thunder_far"] < centroids["thunder_near"]);
Expect("ağır cisim çarpması hafiften daha pes", centroids["impact_Wood_heavy"] < centroids["impact_Wood_light"]);
Expect("taş zemin çimenden daha parlak", centroids["footstep_Stone"] > centroids["footstep_Grass"]);
Expect("metal ahşaptan daha parlak", centroids["footstep_Metal"] > centroids["footstep_Wood"]);

Console.WriteLine(fail == 0 ? "\nTÜM KONTROLLER GEÇTİ" : $"\n{fail} KONTROL BAŞARISIZ");
return fail == 0 ? 0 : 1;

// ---------------------------------------------------------------- yardımcılar

static double Correlation(float[] a, float[] b)
{
    int n = Math.Min(a.Length, b.Length);
    double sab = 0, saa = 0, sbb = 0;
    for (int i = 0; i < n; i++) { sab += a[i] * b[i]; saa += a[i] * a[i]; sbb += b[i] * b[i]; }
    return sab / Math.Sqrt(saa * sbb + 1e-30);
}

static bool SeamOk(float[] d)
{
    // Birleşim noktasındaki (son->ilk örnek) sıçrama, dosyanın başka yerlerindeki en büyük sıçramayı çok aşmamalı.
    int n = d.Length;
    double seam = Math.Abs(d[0] - d[n - 1]);
    double max = 0;
    for (int i = 1; i < n; i++) max = Math.Max(max, Math.Abs(d[i] - d[i - 1]));
    return seam <= max * 1.2 + 1e-6;
}

static double Centroid(float[] d, int fs, bool loop)
{
    int n = 1;
    while (n < d.Length && n < 32768) n <<= 1;
    var buf = new Complex[n];
    int start = loop ? Math.Max(0, d.Length / 2 - n / 2) : 0;
    for (int i = 0; i < n; i++)
    {
        int j = start + i;
        double w = 0.5 - 0.5 * Math.Cos(2 * Math.PI * i / (n - 1));
        buf[i] = j < d.Length && (loop || true) ? d[j] * w : 0;
    }
    Fft(buf);
    double num = 0, den = 0;
    for (int k = 1; k < n / 2; k++)
    {
        double mag = buf[k].Magnitude; double p = mag * mag;
        num += p * k * fs / (double)n; den += p;
    }
    return den > 0 ? num / den : 0;
}

static void Fft(Complex[] a)
{
    int n = a.Length;
    for (int i = 1, j = 0; i < n; i++)
    {
        int bit = n >> 1;
        for (; (j & bit) != 0; bit >>= 1) j ^= bit;
        j ^= bit;
        if (i < j) (a[i], a[j]) = (a[j], a[i]);
    }
    for (int len = 2; len <= n; len <<= 1)
    {
        double ang = -2 * Math.PI / len;
        var wl = new Complex(Math.Cos(ang), Math.Sin(ang));
        for (int i = 0; i < n; i += len)
        {
            Complex w = 1;
            for (int j = 0; j < len / 2; j++)
            {
                var u = a[i + j]; var v = a[i + j + len / 2] * w;
                a[i + j] = u + v; a[i + j + len / 2] = u - v; w *= wl;
            }
        }
    }
}

static void WriteWav(string path, float[] d, int rate)
{
    using var fs = new FileStream(path, FileMode.Create);
    using var w = new BinaryWriter(fs);
    int dataLen = d.Length * 2;
    w.Write("RIFF"u8); w.Write(36 + dataLen); w.Write("WAVE"u8);
    w.Write("fmt "u8); w.Write(16); w.Write((short)1); w.Write((short)1);
    w.Write(rate); w.Write(rate * 2); w.Write((short)2); w.Write((short)16);
    w.Write("data"u8); w.Write(dataLen);
    foreach (var v in d) w.Write((short)Math.Round(Math.Max(-1f, Math.Min(1f, v)) * 32767f));
}
