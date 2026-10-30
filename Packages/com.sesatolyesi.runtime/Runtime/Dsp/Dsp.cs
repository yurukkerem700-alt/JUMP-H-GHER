using System;

namespace SesAtolyesi.Dsp
{
    /// <summary>Küçük, deterministik rastgele sayı üreteci. Aynı tohum = aynı ses.</summary>
    public sealed class Rng
    {
        ulong _s;

        public Rng(int seed)
        {
            unchecked { _s = (ulong)seed * 0x9E3779B97F4A7C15UL + 0x632BE59BD9B4E019UL; }
            if (_s == 0) _s = 88172645463325252UL;
            for (int i = 0; i < 8; i++) NextU();
        }

        public ulong NextU() { _s ^= _s << 13; _s ^= _s >> 7; _s ^= _s << 17; return _s; }
        public float Value() { return (NextU() >> 40) * (1f / 16777216f); }
        public float Range(float a, float b) { return a + (b - a) * Value(); }
        public float Signed() { return Value() * 2f - 1f; }
        public bool Chance(float p) { return Value() < p; }
    }

    /// <summary>İkinci derece filtre (RBJ). Katsayılar çalışırken değiştirilebilir.</summary>
    public sealed class Biquad
    {
        readonly float _fs;
        float _b0 = 1, _b1, _b2, _a1, _a2, _z1, _z2;

        /// <summary>Beyaz gürültüyü bu filtreden geçirince kaybolan gücü telafi eden çarpan.</summary>
        public float Comp = 1f;

        public Biquad(float sampleRate) { _fs = sampleRate; }

        void Set(double b0, double b1, double b2, double a0, double a1, double a2)
        {
            double inv = 1.0 / a0;
            _b0 = (float)(b0 * inv); _b1 = (float)(b1 * inv); _b2 = (float)(b2 * inv);
            _a1 = (float)(a1 * inv); _a2 = (float)(a2 * inv);
        }

        float Clamp(float f) { return Math.Max(10f, Math.Min(f, _fs * 0.45f)); }
        static float MakeUp(double power) { return (float)Math.Max(1.0, Math.Min(30.0, 1.0 / Math.Sqrt(Math.Max(0.001, Math.Min(1.0, power))))); }

        public Biquad LowPass(float f, float q = 0.707f)
        {
            f = Clamp(f); q = Math.Max(0.1f, q);
            double w = 2 * Math.PI * f / _fs, c = Math.Cos(w), al = Math.Sin(w) / (2 * q);
            Set((1 - c) / 2, 1 - c, (1 - c) / 2, 1 + al, -2 * c, 1 - al);
            Comp = MakeUp(f / (_fs * 0.5));
            return this;
        }

        public Biquad HighPass(float f, float q = 0.707f)
        {
            f = Clamp(f); q = Math.Max(0.1f, q);
            double w = 2 * Math.PI * f / _fs, c = Math.Cos(w), al = Math.Sin(w) / (2 * q);
            Set((1 + c) / 2, -(1 + c), (1 + c) / 2, 1 + al, -2 * c, 1 - al);
            Comp = MakeUp(1.0 - f / (_fs * 0.5));
            return this;
        }

        public Biquad BandPass(float f, float q)
        {
            f = Clamp(f); q = Math.Max(0.1f, q);
            double w = 2 * Math.PI * f / _fs, c = Math.Cos(w), al = Math.Sin(w) / (2 * q);
            Set(al, 0, -al, 1 + al, -2 * c, 1 - al);
            Comp = MakeUp(1.57 * f / q / (_fs * 0.5));
            return this;
        }

        public float Process(float x)
        {
            float y = _b0 * x + _z1;
            _z1 = _b1 * x - _a1 * y + _z2;
            _z2 = _b2 * x - _a2 * y;
            return y;
        }
    }

    public sealed class PinkNoise
    {
        readonly Rng _r;
        float _b0, _b1, _b2, _b3, _b4, _b5, _b6;
        public PinkNoise(Rng r) { _r = r; }

        public float Next()
        {
            float w = _r.Signed();
            _b0 = 0.99886f * _b0 + w * 0.0555179f;
            _b1 = 0.99332f * _b1 + w * 0.0750759f;
            _b2 = 0.96900f * _b2 + w * 0.1538520f;
            _b3 = 0.86650f * _b3 + w * 0.3104856f;
            _b4 = 0.55000f * _b4 + w * 0.5329522f;
            _b5 = -0.7616f * _b5 - w * 0.0168980f;
            float p = _b0 + _b1 + _b2 + _b3 + _b4 + _b5 + _b6 + w * 0.5362f;
            _b6 = w * 0.115926f;
            return p * 0.11f;
        }
    }

    public sealed class BrownNoise
    {
        readonly Rng _r;
        float _b;
        public BrownNoise(Rng r) { _r = r; }
        public float Next() { _b = (_b + 0.02f * _r.Signed()) / 1.02f; return _b * 3.5f; }
    }

    /// <summary>Ses tamponu yardımcıları: tık, rezonans, gürültü patlaması, döngü, son işlem.</summary>
    public static class Buf
    {
        public static float[] New(float seconds, int fs) { return new float[Math.Max(1, (int)(seconds * fs))]; }

        /// <summary>Sönümlenen kısa titreşim ("tık"). noiseMix=0 saf ton, 1 saf gürültü.</summary>
        public static void Tick(float[] b, int start, int fs, float amp, float freq, float tau, float noiseMix, Rng r)
        {
            if (start >= b.Length) return;
            int n = (int)(tau * fs * 5f) + 2;
            double w = 2 * Math.PI * freq / fs;
            float c = (float)(2 * Math.Cos(w));
            float sp = (float)Math.Sin(-w), sc = 0f;
            float k = (float)Math.Exp(-1.0 / (tau * fs));
            float e = amp;
            for (int i = 0; i < n; i++)
            {
                int j = start + i;
                if (j >= b.Length) break;
                if (j >= 0) b[j] += e * ((1f - noiseMix) * sc + noiseMix * r.Signed());
                float next = c * sc - sp; sp = sc; sc = next;
                e *= k;
            }
        }

        /// <summary>Birden çok sönümlü sinüs toplamı: tahta, taş, metal gibi cisimlerin "gövde" sesi.</summary>
        public static void Modal(float[] b, int start, int fs, float[] freq, float[] t60, float[] gain, float amp)
        {
            for (int m = 0; m < freq.Length; m++)
            {
                float f = freq[m];
                if (f >= fs * 0.45f || f < 5f) continue;
                double w = 2 * Math.PI * f / fs;
                float c = (float)(2 * Math.Cos(w));
                float sp = (float)Math.Sin(-w), sc = 0f;
                float dec = (float)Math.Pow(0.001, 1.0 / (t60[m] * fs));
                float e = amp * gain[m];
                int n = (int)(t60[m] * fs) + 1;
                for (int i = 0; i < n; i++)
                {
                    int j = start + i;
                    if (j >= b.Length) break;
                    if (j >= 0) b[j] += e * sc;
                    float next = c * sc - sp; sp = sc; sc = next;
                    e *= dec;
                }
            }
        }

        /// <summary>Filtrelenmiş gürültü patlaması. Filtre null olabilir.</summary>
        public static void Burst(float[] b, int start, int fs, float amp, Biquad f, float attack, float decay, Rng r)
        {
            int n = (int)((attack + decay * 6f) * fs);
            float k = (float)Math.Exp(-1.0 / (decay * fs));
            float e = 1f;
            float comp = f != null ? f.Comp : 1f;
            for (int i = 0; i < n; i++)
            {
                int j = start + i;
                if (j >= b.Length) break;
                float x = r.Signed();
                if (f != null) x = f.Process(x) * comp;
                float a = (attack > 0f) ? Math.Min(1f, i / (attack * fs)) : 1f;
                if (j >= 0) b[j] += x * e * a * amp;
                e *= k;
            }
        }

        /// <summary>Süpürülen (frekansı değişen) bant geçiren gürültü: swoosh, nefes, rüzgar sesleri.</summary>
        public static void Swept(float[] b, int start, int fs, float dur, float f0, float f1, float q,
                                 float attack, float decay, float amp, Rng r)
        {
            var f = new Biquad(fs);
            int n = (int)(dur * fs);
            for (int i = 0; i < n; i++)
            {
                int j = start + i;
                if (j >= b.Length) break;
                if ((i & 31) == 0) f.BandPass(f0 * (float)Math.Pow(f1 / f0, i / (float)n), q);
                float t = i / (float)fs;
                float a = (attack > 0f) ? Math.Min(1f, t / attack) : 1f;
                float e = a * (float)Math.Exp(-t / decay);
                if (j >= 0) b[j] += f.Process(r.Signed()) * f.Comp * e * amp;
            }
        }

        /// <summary>Frekansı yükselen kısa ton: su kabarcığı, damla.</summary>
        public static void Chirp(float[] b, int start, int fs, float amp, float f0, float f1, float tau)
        {
            if (start >= b.Length) return;
            int n = (int)(tau * fs * 5f) + 2;
            double phase = 0;
            float k = (float)Math.Exp(-1.0 / (tau * fs));
            float e = amp;
            for (int i = 0; i < n; i++)
            {
                int j = start + i;
                if (j >= b.Length) break;
                float t = i / (float)fs;
                float f = f0 + (f1 - f0) * (1f - (float)Math.Exp(-t / (tau * 0.7f)));
                phase += 2 * Math.PI * f / fs;
                if (j >= 0) b[j] += e * (float)Math.Sin(phase);
                e *= k;
            }
        }

        /// <summary>Ortalama oranı "rate" olan rastgele zamanlarda geri çağırma yapar (yağmur damlası gibi).</summary>
        public static void Poisson(Rng r, float rate, int length, int fs, Action<int> onEvent)
        {
            double t = 0;
            double total = length / (double)fs;
            while (true)
            {
                double u = Math.Max(1e-6, 1.0 - r.Value());
                t += -Math.Log(u) / rate;
                if (t >= total) break;
                onEvent((int)(t * fs));
            }
        }

        public static float Smooth(float a, float b, float x)
        {
            float t = Math.Max(0f, Math.Min(1f, (x - a) / (b - a)));
            return t * t * (3f - 2f * t);
        }

        public static float Lerp(float a, float b, float t) { return a + (b - a) * t; }

        public static void DcBlock(float[] b)
        {
            float x1 = 0f, y1 = 0f;
            for (int i = 0; i < b.Length; i++)
            {
                float x = b[i];
                float y = x - x1 + 0.999f * y1;
                x1 = x; y1 = y; b[i] = y;
            }
        }

        public static void Normalize(float[] b, float peak)
        {
            float m = 0f;
            for (int i = 0; i < b.Length; i++)
            {
                float v = b[i];
                if (float.IsNaN(v) || float.IsInfinity(v)) { b[i] = 0f; continue; }
                float a = Math.Abs(v);
                if (a > m) m = a;
            }
            if (m < 1e-9f) return;
            float g = peak / m;
            for (int i = 0; i < b.Length; i++) b[i] *= g;
        }

        /// <summary>Tek seferlik sesin son işlemi: DC temizle, tıkırtısız giriş/çıkış, tepe seviyesine getir.</summary>
        public static float[] FinishOneShot(float[] b, int fs, float peak)
        {
            DcBlock(b);
            int fi = Math.Min(b.Length, (int)(0.0004f * fs));
            for (int i = 0; i < fi; i++) b[i] *= i / (float)fi;
            int fo = Math.Min(b.Length, (int)(0.012f * fs));
            for (int i = 0; i < fo; i++) b[b.Length - 1 - i] *= i / (float)fo;
            Normalize(b, peak);
            return b;
        }

        /// <summary>Sonu başına dikişsiz bağlanan döngü üretir (eşit güçlü çapraz geçiş). Uzunluk = src - xfade.</summary>
        public static float[] Loopify(float[] src, int xfade, float peak)
        {
            int len = src.Length - xfade;
            var o = new float[len];
            for (int i = 0; i < len; i++) o[i] = src[i];
            for (int i = 0; i < xfade; i++)
            {
                float w = i / (float)xfade;
                float a = (float)Math.Sin(w * Math.PI * 0.5);   // yükselen: dönüşte başa gelen kısım
                float bb = (float)Math.Cos(w * Math.PI * 0.5);  // alçalan: ek kuyruk
                o[i] = src[i] * a + src[len + i] * bb;
            }
            Normalize(o, peak);
            return o;
        }
    }
}
