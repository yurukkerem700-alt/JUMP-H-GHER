using System;

namespace SesAtolyesi.Dsp
{
    public enum SurfaceType { Stone, Grass, Dirt, Gravel, Wood, Snow, Water, Metal, Sand }

    /// <summary>
    /// Tüm sesler burada, kayıt dosyası olmadan, matematikle üretilir.
    /// Her tarif bir "tohum" alır: farklı tohum = aynı tür sesin biraz farklı versiyonu.
    /// </summary>
    public static class Recipes
    {
        public const int OneShotRate = 44100;
        public const int LoopRate = 32000;

        static float Pow(float a, float b) { return (float)Math.Pow(a, b); }
        static float Exp(float a) { return (float)Math.Exp(a); }

        // ------------------------------------------------------------------ ADIM / ÇARPMA

        /// <summary>Tek bir "vuruş": ayakkabı topuğu, ayak ucu, düşen nesne. size: 1 = ayakkabı, büyük = ağır ve yavaş.</summary>
        static void Strike(float[] b, int start, SurfaceType s, float I, float size, Rng r)
        {
            int fs = OneShotRate;
            float k = Pow(size, -0.45f);            // ağır = daha pes
            float L = Pow(size, 0.35f);             // ağır = daha uzun
            float v = r.Range(0.9f, 1.12f);         // her vuruşta hafif farklı ton
            switch (s)
            {
                case SurfaceType.Stone:
                    Buf.Burst(b, start, fs, 0.9f * I, new Biquad(fs).BandPass(3200f * k * v, 1.2f), 0.0003f, 0.006f * L, r);
                    Buf.Modal(b, start, fs, new[] { 140f * k * v, 300f * k * v, 610f * k * v, 1250f * k }, new[] { .07f * L, .05f * L, .04f * L, .03f * L }, new[] { 1f, .6f, .35f, .15f }, 0.8f * I);
                    Buf.Burst(b, start + (int)(0.004f * fs), fs, 0.12f * I, new Biquad(fs).HighPass(5000f), 0f, 0.03f, r);
                    break;
                case SurfaceType.Grass:
                    Buf.Burst(b, start, fs, 0.5f * I, new Biquad(fs).LowPass(2200f * k * v), 0.012f, 0.06f * L, r);
                    Buf.Burst(b, start, fs, 0.35f * I, new Biquad(fs).BandPass(5200f * v, 0.8f), 0.015f, 0.09f * L, r);
                    Grains(b, start + (int)(0.005f * fs), fs, 0.12f * L, (int)(14 * I) + 3, 2500f, 7500f, 0.2f, 0.001f, 0.6f, 1.3f, r);
                    Buf.Modal(b, start, fs, new[] { 75f * k, 140f * k }, new[] { .05f * L, .03f * L }, new[] { 1f, .6f }, 0.5f * I);
                    break;
                case SurfaceType.Dirt:
                    Grains(b, start, fs, 0.09f * L, (int)(28 * I) + 4, 700f, 3200f, 0.3f, 0.0012f, 0.5f, 1.4f, r);
                    Buf.Burst(b, start, fs, 0.4f * I, new Biquad(fs).LowPass(1400f * k), 0.004f, 0.05f * L, r);
                    Buf.Modal(b, start, fs, new[] { 95f * k * v, 190f * k, 330f * k }, new[] { .07f * L, .05f * L, .03f * L }, new[] { 1f, .5f, .2f }, 0.7f * I);
                    break;
                case SurfaceType.Gravel:
                    Grains(b, start, fs, 0.16f * L, (int)(70 * I) + 8, 1400f, 6500f, 0.32f, 0.0009f, 0.45f, 1.6f, r);
                    Buf.Modal(b, start, fs, new[] { 85f * k, 170f * k }, new[] { .06f * L, .04f * L }, new[] { 1f, .5f }, 0.55f * I);
                    Buf.Burst(b, start, fs, 0.25f * I, new Biquad(fs).BandPass(2800f, 0.9f), 0.002f, 0.04f, r);
                    break;
                case SurfaceType.Wood:
                    Buf.Modal(b, start, fs, new[] { 105f * k * v, 190f * k, 340f * k, 560f * k, 910f * k }, new[] { .14f * L, .11f * L, .08f * L, .06f * L, .04f * L }, new[] { 1f, .8f, .5f, .3f, .18f }, 0.9f * I);
                    Buf.Burst(b, start, fs, 0.5f * I, new Biquad(fs).BandPass(1400f * v, 1.5f), 0.0004f, 0.005f, r);
                    break;
                case SurfaceType.Snow:
                    Grains(b, start, fs, 0.15f * L, (int)(45 * I) + 6, 900f, 3200f, 0.26f, 0.0016f, 0.7f, 1.4f, r);
                    Buf.Burst(b, start, fs, 0.4f * I, new Biquad(fs).LowPass(1300f), 0.01f, 0.07f * L, r);
                    Buf.Modal(b, start, fs, new[] { 55f * k, 100f * k }, new[] { .06f * L, .04f * L }, new[] { 1f, .6f }, 0.45f * I);
                    if (r.Chance(0.25f * I)) Buf.Chirp(b, start + (int)(0.01f * fs), fs, 0.06f * I, 1900f, 2400f, 0.02f);
                    break;
                case SurfaceType.Water:
                    Buf.Burst(b, start, fs, 0.6f * I, new Biquad(fs).BandPass(1800f * v, 0.7f), 0.004f, 0.09f * L, r);
                    Buf.Burst(b, start, fs, 0.25f * I, new Biquad(fs).HighPass(4000f), 0.002f, 0.05f, r);
                    int bubbles = 2 + (int)(4 * I);
                    for (int i = 0; i < bubbles; i++)
                    {
                        float f0 = r.Range(450f, 1700f) * k;
                        Buf.Chirp(b, start + (int)(r.Range(0.01f, 0.12f) * fs), fs, 0.18f * I * r.Range(0.5f, 1f), f0, f0 * r.Range(1.5f, 2.2f), r.Range(0.01f, 0.025f));
                    }
                    Buf.Modal(b, start, fs, new[] { 100f * k, 180f * k }, new[] { .06f * L, .04f * L }, new[] { 1f, .6f }, 0.35f * I);
                    break;
                case SurfaceType.Metal:
                    Buf.Modal(b, start, fs, new[] { 430f * k * v, 1160f * k, 1930f * k, 3050f * k, 4300f * k }, new[] { .4f * L, .3f * L, .22f * L, .14f * L, .09f * L }, new[] { 1f, .7f, .5f, .35f, .2f }, 0.8f * I);
                    Buf.Burst(b, start, fs, 0.5f * I, new Biquad(fs).HighPass(4000f), 0f, 0.003f, r);
                    break;
                default: // Sand
                    Grains(b, start, fs, 0.17f * L, (int)(80 * I) + 8, 1000f, 4000f, 0.2f, 0.0014f, 0.8f, 1.3f, r);
                    Buf.Burst(b, start, fs, 0.35f * I, new Biquad(fs).LowPass(1100f), 0.012f, 0.08f * L, r);
                    Buf.Modal(b, start, fs, new[] { 60f * k, 110f * k }, new[] { .06f * L, .04f * L }, new[] { 1f, .6f }, 0.35f * I);
                    break;
            }
        }

        /// <summary>Çakıl, kum, kar gibi malzemelerin "çıtırtısı": çok sayıda küçük tık.</summary>
        static void Grains(float[] b, int start, int fs, float dur, int count, float fMin, float fMax,
                           float amp, float tau, float noiseMix, float curve, Rng r)
        {
            for (int i = 0; i < count; i++)
            {
                int at = start + (int)(dur * Pow(r.Value(), curve) * fs);
                float a = amp * (0.15f + Pow(r.Value(), 1.5f));
                Buf.Tick(b, at, fs, a, r.Range(fMin, fMax), tau * r.Range(0.6f, 1.5f), noiseMix, r);
            }
        }

        /// <summary>Yumuşak zeminlerde (çim, kar, kum) tiz kısımları kırpar; sert zeminler olduğu gibi kalır.</summary>
        static void SoftenSurface(float[] b, SurfaceType s, int fs)
        {
            float cut;
            switch (s)
            {
                case SurfaceType.Grass: cut = 2800f; break;
                case SurfaceType.Sand: cut = 2600f; break;
                case SurfaceType.Snow: cut = 2800f; break;
                case SurfaceType.Dirt: cut = 4500f; break;
                case SurfaceType.Water: cut = 6000f; break;
                default: return;
            }
            var f1 = new Biquad(fs).LowPass(cut);
            var f2 = new Biquad(fs).LowPass(cut);
            for (int i = 0; i < b.Length; i++) b[i] = f2.Process(f1.Process(b[i]));
        }

        public static float[] Footstep(SurfaceType s, float weight, int seed)
        {
            weight = Math.Max(0.4f, Math.Min(3f, weight));
            var r = new Rng(seed * 31 + (int)s * 977 + 5);
            int fs = OneShotRate;
            var b = Buf.New(0.55f, fs);
            Strike(b, 0, s, 1f * r.Range(0.9f, 1.05f), weight, r);
            Strike(b, (int)(r.Range(0.07f, 0.11f) * fs), s, 0.5f * r.Range(0.8f, 1.1f), weight * 0.6f, r);
            SoftenSurface(b, s, fs);
            return Buf.FinishOneShot(b, fs, Math.Min(0.8f, 0.3f + 0.15f * weight));
        }

        public static float[] Jump(SurfaceType s, int seed)
        {
            var r = new Rng(seed * 17 + 3);
            int fs = OneShotRate;
            var b = Buf.New(0.4f, fs);
            Buf.Swept(b, 0, fs, 0.18f, 500f, 1800f, 0.8f, 0.03f, 0.09f, 0.35f, r);
            Strike(b, 0, s, 0.55f, 0.8f, r);
            SoftenSurface(b, s, fs);
            return Buf.FinishOneShot(b, fs, 0.45f);
        }

        public static float[] Land(SurfaceType s, float intensity, int seed)
        {
            intensity = Math.Max(0f, Math.Min(1f, intensity));
            var r = new Rng(seed * 13 + (int)s * 101 + 7);
            int fs = OneShotRate;
            var b = Buf.New(0.7f, fs);
            float size = 1.1f + 1.3f * intensity;
            Strike(b, 0, s, 0.6f + 0.5f * intensity, size, r);
            Strike(b, (int)(r.Range(0.025f, 0.05f) * fs), s, 0.5f + 0.4f * intensity, size * 0.9f, r);
            Buf.Burst(b, 0, fs, 0.25f * intensity, new Biquad(fs).LowPass(900f), 0.01f, 0.12f, r);
            SoftenSurface(b, s, fs);
            return Buf.FinishOneShot(b, fs, 0.45f + 0.35f * intensity);
        }

        /// <summary>Cisim çarpması: kütle ne kadar büyükse o kadar pes ve uzun; hız ne kadar büyükse o kadar sert.</summary>
        public static float[] Impact(SurfaceType s, float mass, float speed01, int seed)
        {
            mass = Math.Max(0.05f, mass);
            speed01 = Math.Max(0.05f, Math.Min(1f, speed01));
            var r = new Rng(seed * 19 + (int)s * 131 + 9);
            int fs = OneShotRate;
            var b = Buf.New(0.9f, fs);
            float size = Math.Max(0.4f, Math.Min(6f, Pow(mass, 0.5f) * 1.2f));
            Strike(b, 0, s, 0.4f + 0.7f * speed01, size, r);
            SoftenSurface(b, s, fs);
            return Buf.FinishOneShot(b, fs, 0.35f + 0.45f * speed01);
        }

        // ------------------------------------------------------------------ ARAYÜZ / ÖDÜL

        static void Bell(float[] b, int start, int fs, float f0, float amp, float len)
        {
            var ratio = new[] { 1f, 2.01f, 2.99f, 4.17f };
            var gain = new[] { 1f, .45f, .25f, .12f };
            var t60 = new[] { .35f * len, .25f * len, .18f * len, .12f * len };
            var f = new float[4];
            for (int i = 0; i < 4; i++) f[i] = f0 * ratio[i];
            Buf.Modal(b, start, fs, f, t60, gain, amp);
        }

        public static float[] Coin(int seed)
        {
            var r = new Rng(seed * 23 + 1);
            int fs = OneShotRate;
            var b = Buf.New(0.7f, fs);
            float d = r.Range(0.98f, 1.03f);
            Bell(b, 0, fs, 987.8f * d, 0.8f, 0.7f);
            Bell(b, (int)(0.075f * fs), fs, 1318.5f * d, 1f, 1f);
            Buf.Burst(b, 0, fs, 0.15f, new Biquad(fs).HighPass(5000f), 0f, 0.004f, r);
            return Buf.FinishOneShot(b, fs, 0.6f);
        }

        public static float[] Pickup(int seed)
        {
            var r = new Rng(seed * 29 + 2);
            int fs = OneShotRate;
            var b = Buf.New(0.8f, fs);
            var notes = new[] { 523.3f, 659.3f, 784f, 1046.5f };
            for (int i = 0; i < notes.Length; i++)
                Bell(b, (int)(i * 0.07f * fs), fs, notes[i] * r.Range(0.99f, 1.01f), 0.7f + 0.1f * i, 0.8f);
            return Buf.FinishOneShot(b, fs, 0.55f);
        }

        public static float[] Hit(int seed)
        {
            var r = new Rng(seed * 37 + 4);
            int fs = OneShotRate;
            var b = Buf.New(0.45f, fs);
            Buf.Burst(b, 0, fs, 1f, new Biquad(fs).BandPass(900f * r.Range(0.9f, 1.15f), 0.8f), 0.001f, 0.05f, r);
            Buf.Modal(b, 0, fs, new[] { 70f, 140f, 260f }, new[] { .12f, .08f, .05f }, new[] { 1f, .7f, .4f }, 0.9f);
            for (int i = 0; i < b.Length; i++) b[i] = (float)Math.Tanh(b[i] * 1.8f);
            return Buf.FinishOneShot(b, fs, 0.7f);
        }

        public static float[] Swoosh(float seconds, int seed)
        {
            var r = new Rng(seed * 41 + 6);
            int fs = OneShotRate;
            seconds = Math.Max(0.1f, Math.Min(1.5f, seconds));
            var b = Buf.New(seconds + 0.2f, fs);
            Buf.Swept(b, 0, fs, seconds, 700f, 2800f, 0.9f, seconds * 0.35f, seconds * 0.35f, 1f, r);
            return Buf.FinishOneShot(b, fs, 0.4f);
        }

        public static float[] UiClick(int seed)
        {
            var r = new Rng(seed * 43 + 8);
            int fs = OneShotRate;
            var b = Buf.New(0.12f, fs);
            Buf.Burst(b, 0, fs, 0.5f, new Biquad(fs).HighPass(2500f), 0f, 0.002f, r);
            Buf.Modal(b, 0, fs, new[] { 2200f * r.Range(0.97f, 1.03f), 3300f }, new[] { .03f, .02f }, new[] { 1f, .3f }, 0.8f);
            return Buf.FinishOneShot(b, fs, 0.35f);
        }

        public static float[] UiConfirm(int seed)
        {
            var r = new Rng(seed * 47 + 10);
            int fs = OneShotRate;
            var b = Buf.New(0.5f, fs);
            Bell(b, 0, fs, 660f, 0.6f, 0.5f);
            Bell(b, (int)(0.08f * fs), fs, 990f * r.Range(0.99f, 1.01f), 0.8f, 0.6f);
            return Buf.FinishOneShot(b, fs, 0.4f);
        }

        // ------------------------------------------------------------------ ORTAM DÖNGÜLERİ (dikişsiz)

        /// <summary>Rüzgar: esinti dalgalanır, kuvvetlendikçe tonu yükselir ve ıslık eklenir. intensity 0..1.</summary>
        public static float[] Wind(float intensity, int seed, float seconds = 8f)
        {
            intensity = Math.Max(0f, Math.Min(1f, intensity));
            int fs = LoopRate;
            var r = new Rng(seed * 7 + 11);
            int xf = (int)(1.2f * fs);
            var b = new float[(int)(seconds * fs) + xf];
            var pink = new PinkNoise(r);
            var pink2 = new PinkNoise(r);
            var bp = new Biquad(fs);
            var lp = new Biquad(fs).LowPass(Buf.Lerp(500f, 1100f, intensity), 0.7f);
            var whistle = new Biquad(fs);
            const int G = 5;
            var gf = new float[G]; var gp = new float[G]; var ga = new float[G];
            float asum = 0f;
            for (int g = 0; g < G; g++)
            {
                gf[g] = r.Range(0.05f, 0.5f) * (0.6f + intensity);
                gp[g] = r.Range(0f, 6.2832f);
                ga[g] = 1f / (g + 1);
                asum += ga[g];
            }
            float baseF = Buf.Lerp(260f, 620f, intensity);
            float depth = Buf.Lerp(0.3f, 0.65f, intensity);
            float gust = 0.5f, bpOut = 0f, wsOut = 0f;
            for (int i = 0; i < b.Length; i++)
            {
                float t = i / (float)fs;
                if ((i & 31) == 0)
                {
                    float s = 0f;
                    for (int g = 0; g < G; g++) s += ga[g] * (float)Math.Sin(6.2832 * gf[g] * t + gp[g]);
                    gust = 0.5f + 0.5f * Math.Max(-1f, Math.Min(1f, s / asum * 1.4f));
                    float center = baseF * (0.75f + 0.5f * gust);
                    bp.BandPass(center, 1.4f);
                    whistle.BandPass(center * 2.6f, 9f);
                }
                float x = pink.Next();
                bpOut = bp.Process(x) * bp.Comp;
                float lpOut = lp.Process(pink2.Next());
                wsOut = whistle.Process(x) * whistle.Comp;
                float amp = 1f - depth * (1f - gust);
                b[i] = amp * (bpOut * 0.8f + lpOut * 1.4f) + wsOut * 0.35f * gust * gust * intensity * intensity;
            }
            Buf.DcBlock(b);
            Buf.Normalize(b, 0.9f);
            return Buf.Loopify(b, xf, 0.5f);
        }

        /// <summary>Yaprak hışırtısı: rüzgarla birlikte dalgalanan çok sayıda küçük hışırtı.</summary>
        public static float[] Leaves(float intensity, int seed, float seconds = 8f)
        {
            intensity = Math.Max(0f, Math.Min(1f, intensity));
            int fs = LoopRate;
            var r = new Rng(seed * 53 + 12);
            int xf = (int)(1.2f * fs);
            var b = new float[(int)(seconds * fs) + xf];
            float rate = Buf.Lerp(120f, 650f, intensity);
            float f1 = r.Range(0.08f, 0.2f), f2 = r.Range(0.25f, 0.5f), p1 = r.Range(0f, 6.28f), p2 = r.Range(0f, 6.28f);
            Buf.Poisson(r, rate, b.Length, fs, at =>
            {
                float t = at / (float)fs;
                float gust = 0.5f + 0.25f * (float)Math.Sin(6.2832 * f1 * t + p1) + 0.25f * (float)Math.Sin(6.2832 * f2 * t + p2);
                float a = (0.05f + Pow(r.Value(), 2f)) * (0.25f + 0.75f * gust);
                Buf.Tick(b, at, fs, a, r.Range(3000f, 8500f), r.Range(0.0015f, 0.005f), 0.85f, r);
            });
            Buf.DcBlock(b);
            return Buf.Loopify(b, xf, 0.45f);
        }

        /// <summary>Yağmur: ince tabaka + tek tek damlalar + ara sıra "plink". intensity 0..1.</summary>
        public static float[] Rain(float intensity, int seed, float seconds = 8f)
        {
            intensity = Math.Max(0f, Math.Min(1f, intensity));
            int fs = LoopRate;
            var r = new Rng(seed * 59 + 13);
            int xf = (int)(1.2f * fs);
            var b = new float[(int)(seconds * fs) + xf];
            var pink = new PinkNoise(r);
            var brown = new BrownNoise(r);
            var hp = new Biquad(fs).HighPass(500f);
            var lp = new Biquad(fs).LowPass(7500f);
            var lp2 = new Biquad(fs).LowPass(180f);
            float bed = Buf.Lerp(0.05f, 0.22f, intensity);
            float rum = 0.06f * intensity * intensity;
            for (int i = 0; i < b.Length; i++)
                b[i] = lp.Process(hp.Process(pink.Next())) * bed * 3f + lp2.Process(brown.Next()) * rum * 4f;
            float rate = Buf.Lerp(70f, 1100f, intensity);
            Buf.Poisson(r, rate, b.Length, fs, at =>
                Buf.Tick(b, at, fs, 0.35f * Pow(r.Value(), 2.2f) + 0.01f, r.Range(2500f, 9500f), r.Range(0.0006f, 0.0022f), 0.55f, r));
            Buf.Poisson(r, Buf.Lerp(4f, 40f, intensity), b.Length, fs, at =>
                Buf.Tick(b, at, fs, 0.25f * Pow(r.Value(), 1.5f), r.Range(1400f, 4200f), r.Range(0.004f, 0.012f), 0.05f, r));
            Buf.DcBlock(b);
            return Buf.Loopify(b, xf, 0.5f);
        }

        /// <summary>Şimşek sonrası gök gürültüsü. distance01: 0 = hemen başının üstünde, 1 = çok uzakta.</summary>
        public static float[] Thunder(float distance01, int seed)
        {
            distance01 = Math.Max(0f, Math.Min(1f, distance01));
            int fs = LoopRate;
            var r = new Rng(seed * 61 + 14);
            var b = Buf.New(8f, fs);
            var brown = new BrownNoise(r);
            var lp = new Biquad(fs);
            float c0 = Buf.Lerp(900f, 260f, distance01);
            float T = Buf.Lerp(1.8f, 3.4f, distance01);
            const int N = 4;
            var bt = new float[N]; var bw = new float[N]; var ba = new float[N];
            for (int i = 0; i < N; i++) { bt[i] = r.Range(0.0f, 3.5f); bw[i] = r.Range(0.3f, 0.9f); ba[i] = r.Range(0.4f, 1f); }
            for (int i = 0; i < b.Length; i++)
            {
                float t = i / (float)fs;
                if ((i & 31) == 0) lp.LowPass(Buf.Lerp(c0, 55f, 1f - Exp(-t / 1.8f)), 0.7f);
                float bump = 0.4f;
                for (int k = 0; k < N; k++) { float d = (t - bt[k]) / bw[k]; bump += ba[k] * Exp(-d * d); }
                b[i] = lp.Process(brown.Next()) * bump * Exp(-t / T);
            }
            float crack = Pow(1f - distance01, 1.3f);
            if (crack > 0.02f)
            {
                Buf.Burst(b, 0, fs, 6f * crack, new Biquad(fs).HighPass(700f), 0.0008f, Buf.Lerp(0.08f, 0.3f, distance01), r);
                Buf.Swept(b, 0, fs, 0.3f, 3000f, 500f, 0.8f, 0.002f, 0.12f, 4f * crack, r);
            }
            return Buf.FinishOneShot(b, fs, 0.9f);
        }

        /// <summary>Ateş: alçak "hırıltı", cızırtı ve rastgele çıtırtılar.</summary>
        public static float[] Fire(int seed, float seconds = 6f)
        {
            int fs = LoopRate;
            var r = new Rng(seed * 67 + 15);
            int xf = (int)(1.2f * fs);
            var b = new float[(int)(seconds * fs) + xf];
            var brown = new BrownNoise(r);
            var pink = new PinkNoise(r);
            var lp = new Biquad(fs).LowPass(500f);
            var hp = new Biquad(fs).HighPass(3000f);
            float ph1 = r.Range(0f, 6.28f), ph2 = r.Range(0f, 6.28f);
            for (int i = 0; i < b.Length; i++)
            {
                float t = i / (float)fs;
                float flick = 0.7f + 0.2f * (float)Math.Sin(6.2832 * 0.6 * t + ph1) + 0.1f * (float)Math.Sin(6.2832 * 1.7 * t + ph2);
                b[i] = lp.Process(brown.Next()) * 0.9f * flick + hp.Process(pink.Next()) * 0.35f * flick;
            }
            Buf.Poisson(r, 12f, b.Length, fs, at =>
            {
                int pops = 1 + (r.Chance(0.35f) ? r.Chance(0.5f) ? 3 : 2 : 0);
                for (int p = 0; p < pops; p++)
                    Buf.Tick(b, at + (int)(r.Range(0f, 0.015f) * fs), fs, 0.9f * Pow(r.Value(), 1.8f) + 0.1f, r.Range(1800f, 5000f), r.Range(0.0008f, 0.003f), 0.8f, r);
                if (r.Chance(0.12f))
                    Buf.Modal(b, at, fs, new[] { 2400f * r.Range(0.9f, 1.2f), 3600f }, new[] { .02f, .015f }, new[] { 1f, .6f }, 0.5f);
            });
            Buf.DcBlock(b);
            return Buf.Loopify(b, xf, 0.5f);
        }

        /// <summary>Dere / çay: sürekli akış tabakası ve sayısız küçük kabarcık.</summary>
        public static float[] Stream(int seed, float seconds = 8f)
        {
            int fs = LoopRate;
            var r = new Rng(seed * 71 + 16);
            int xf = (int)(1.2f * fs);
            var b = new float[(int)(seconds * fs) + xf];
            var pink = new PinkNoise(r);
            var bp = new Biquad(fs).BandPass(1400f, 0.5f);
            float ph = r.Range(0f, 6.28f);
            for (int i = 0; i < b.Length; i++)
            {
                float t = i / (float)fs;
                b[i] = bp.Process(pink.Next()) * bp.Comp * 0.35f * (0.85f + 0.15f * (float)Math.Sin(6.2832 * 0.35 * t + ph));
            }
            Buf.Poisson(r, 110f, b.Length, fs, at =>
            {
                float f0 = r.Range(500f, 2800f);
                Buf.Chirp(b, at, fs, 0.35f * Pow(r.Value(), 2f) + 0.02f, f0, f0 * r.Range(1.3f, 1.9f), r.Range(0.006f, 0.014f));
            });
            Buf.DcBlock(b);
            return Buf.Loopify(b, xf, 0.5f);
        }

        /// <summary>Gece cırcır böcekleri: birkaç böcek, her biri kendi ritminde kısa darbe grupları atar.</summary>
        public static float[] Crickets(int seed, float seconds = 8f)
        {
            int fs = LoopRate;
            var r = new Rng(seed * 73 + 17);
            int xf = (int)(1.2f * fs);
            var b = new float[(int)(seconds * fs) + xf];
            for (int c = 0; c < 3; c++)
            {
                float fc = r.Range(4300f, 5100f);
                float period = r.Range(0.38f, 0.62f);
                int pulses = 3 + (r.Chance(0.5f) ? 1 : 0);
                float t0 = r.Range(0f, period);
                for (float gs = t0; gs < b.Length / (float)fs; gs += period * r.Range(0.95f, 1.05f))
                {
                    float f = fc * r.Range(0.99f, 1.01f);
                    for (int p = 0; p < pulses; p++)
                    {
                        int s0 = (int)((gs + p * 0.035f) * fs);
                        int n = (int)(0.018f * fs);
                        for (int i = 0; i < n; i++)
                        {
                            int j = s0 + i; if (j >= b.Length) break;
                            float env = (float)Math.Sin(Math.PI * i / n); env *= env;
                            b[j] += 0.25f * env * (float)Math.Sin(2 * Math.PI * f * i / fs);
                        }
                    }
                }
            }
            Buf.DcBlock(b);
            return Buf.Loopify(b, xf, 0.4f);
        }
    }
}
