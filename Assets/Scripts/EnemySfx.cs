using UnityEngine;

public enum EnemySound { ZombieSpawn, GoblinSpawn, ZombieAttack, GoblinAttack, ZombieHurt, GoblinHurt }

// Zombi ve goblin için kod ile üretilen korkutucu sesler: doğuş, saldırı ve tekme yiyince acı sesi.
// Ses seviyesi SesAtolyesi'nin Master/Sfx ayarlarına bağlıdır.
public static class EnemySfx
{
    const int Rate = 22050;
    static readonly AudioClip[] clips = new AudioClip[6];
    static AudioSource source;

    public static void Play(EnemySound sound, float volume = 1f)
    {
        var clip = clips[(int)sound]; if (!clip) clips[(int)sound] = clip = Build(sound);
        if (!source)
        {
            var go = new GameObject("DusmanSesleri"); Object.DontDestroyOnLoad(go);
            source = go.AddComponent<AudioSource>(); source.playOnAwake = false; source.spatialBlend = 0;
        }
        source.pitch = Random.Range(.94f, 1.06f);
        source.PlayOneShot(clip, Mathf.Clamp01(volume * SesAtolyesi.Ses.Master * SesAtolyesi.Ses.Sfx));
    }

    public static void Release() { for (int i = 0; i < clips.Length; i++) { if (clips[i]) Object.Destroy(clips[i]); clips[i] = null; } if (source) Object.Destroy(source.gameObject); }

    public static float[] Samples(EnemySound sound)
    {
        switch (sound)
        {
            // Zombi: "uuuh→aaah" diye açılan, titreyen, nefesli ve sürünen derin inleme.
            case EnemySound.ZombieSpawn: return Growl(1.9f, 92, 46, 2.3f, .13f, 9f, .32f, .40f, 2.4f, 330, 720, 1050, .62f, 3);
            // Zombi saldırı: boğazdan gelen, bozuk (distorsiyonlu) ve hızlı titreyen kükreme.
            case EnemySound.ZombieAttack: return Growl(.75f, 160, 52, 5f, .08f, 34f, .70f, .60f, 5.5f, 650, 420, 1150, .92f, 5);
            // Zombi acı: kısa, çöken bir hırıltı.
            case EnemySound.ZombieHurt: return Growl(.55f, 135, 55, 3f, .10f, 18f, .45f, .45f, 3.5f, 520, 820, 1200, .75f, 9);
            // Goblin doğuş: hızlanan, çatlak kıkırtı ve sonunda cıyaklama.
            case EnemySound.GoblinSpawn: return GoblinCackle();
            // Goblin saldırı: hırlama ve yırtıcı bağırış.
            case EnemySound.GoblinAttack: return GoblinSnarl();
            // Goblin acı: art arda iki cıyaklama.
            default: return GoblinYelp();
        }
    }

    static AudioClip Build(EnemySound sound)
    {
        var d = Samples(sound);
        var clip = AudioClip.Create("dusman_" + sound, d.Length, 1, Rate, false); clip.SetData(d, 0); return clip;
    }

    static float Tanh(float x) => (float)System.Math.Tanh(x);
    static float Noise(System.Random r) => (float)(r.NextDouble() * 2 - 1);
    static float Env(float t, float dur, float attack) => Mathf.Clamp01(t / attack) * Mathf.Pow(Mathf.Clamp01(1 - t / dur), 1.3f);

    static float[] Norm(float[] d, float peak)
    {
        float max = 1e-6f; foreach (var v in d) max = Mathf.Max(max, Mathf.Abs(v));
        float g = peak / max; for (int i = 0; i < d.Length; i++) d[i] *= g;
        return d;
    }

    // İki kutuplu rezonatör: sesi bir ağız/boğaz boşluğu gibi renklendirir (formant).
    class Res
    {
        float a1, a2, g, y1, y2;
        public void Set(float f, float bw)
        {
            float r = Mathf.Exp(-Mathf.PI * bw / Rate);
            a1 = -2 * r * Mathf.Cos(2 * Mathf.PI * f / Rate); a2 = r * r; g = 1 - r;
        }
        public float Run(float x) { float y = g * x - a1 * y1 - a2 * y2; y2 = y1; y1 = y; return y; }
    }

    // Boğaz kaynağı + nefes + iki formant + titreme (tremolo) + bozulma. Zombi inlemesi, kükremesi ve acı sesi buradan çıkar.
    static float[] Growl(float dur, float f0, float f1, float wobHz, float wobDepth, float tremHz, float tremDepth,
        float breath, float drive, float formantA, float formantB, float formant2, float peak, int seed)
    {
        var d = new float[(int)(dur * Rate)]; var rnd = new System.Random(seed);
        float phase = 0, jit = 0; var r1 = new Res(); var r2 = new Res();
        for (int i = 0; i < d.Length; i++)
        {
            float t = i / (float)Rate, k = t / dur;
            jit += (Noise(rnd) - jit) * .05f; // ses telindeki düzensizlik
            float f = Mathf.Lerp(f0, f1, Mathf.Pow(k, .7f)) * (1 + wobDepth * Mathf.Sin(2 * Mathf.PI * wobHz * t) + .05f * jit);
            phase += f / Rate; phase -= Mathf.Floor(phase);
            float src = (1 - 2 * phase) + breath * Noise(rnd);
            r1.Set(Mathf.Lerp(formantA, formantB, k), 140); r2.Set(formant2, 220);
            float y = r1.Run(src) + .7f * r2.Run(src);
            y *= 1 - tremDepth * (.5f + .5f * Mathf.Sin(2 * Mathf.PI * tremHz * t));
            d[i] = Tanh(y * drive) * Env(t, dur, .05f);
        }
        return Norm(d, peak);
    }

    // Goblin sesi hece hece kurulur: her hecenin perdesi, ağız şekli (formantlar), nefesi ve kısıklığı ayrı verilir.
    struct Syl
    {
        public float t0, len, fa, fb, f1a, f1b, f2a, f2b, breath, rough, amp;
        public Syl(float t0, float len, float fa, float fb, float f1a, float f1b, float f2a, float f2b, float breath, float rough, float amp)
        { this.t0 = t0; this.len = len; this.fa = fa; this.fb = fb; this.f1a = f1a; this.f1b = f1b; this.f2a = f2a; this.f2b = f2b; this.breath = breath; this.rough = rough; this.amp = amp; }
    }

    // Tek katman: kısık gırtlak sesi (her ikinci titreşim zayıf = diplofoni), düzensiz perde, nefesli hece başı,
    // growlHz ile aslan kükremesi gibi hızlı genlik titremesi ve wet ile ıslak, çıtırtılı salya sesi.
    static float[] Goblin(float dur, Syl[] syl, float drive, int seed, float growlHz, float growlDepth, float wet)
    {
        var d = new float[(int)(dur * Rate)]; var rnd = new System.Random(seed);
        float phase = 0, jit = 0, rgh = 0, gate = 0; bool odd = false;
        var nasal = new Res(); var r1 = new Res(); var r2 = new Res(); var r3 = new Res(); var rw = new Res();
        nasal.Set(220, 120); r3.Set(2600, 450); rw.Set(3500, 1500);
        for (int i = 0; i < d.Length; i++)
        {
            float t = i / (float)Rate, src = 0, env = 0; int j = -1;
            if (i % (Rate / 90) == 0) { gate = Mathf.Pow((float)rnd.NextDouble(), 3f); } // ~90 Hz'de rastgele çıtırtı kapısı
            for (int k = 0; k < syl.Length; k++) if (t >= syl[k].t0 && t < syl[k].t0 + syl[k].len) { j = k; break; }
            float wetSrc = 0;
            if (j >= 0)
            {
                var s = syl[j]; float local = t - s.t0, u = local / s.len;
                jit += (Noise(rnd) - jit) * .15f; rgh += (Noise(rnd) - rgh) * .3f;
                float f = Mathf.Lerp(s.fa, s.fb, Mathf.Pow(u, .8f)) * (1 + .05f * jit + .03f * Mathf.Sin(2 * Mathf.PI * 47 * t));
                phase += f / Rate; if (phase >= 1) { phase -= 1; odd = !odd; }
                float g = (1 - 2 * phase) * (odd ? .5f : 1f) * (1 + s.rough * rgh);
                float burst = Mathf.Exp(-local * 55f); // hecenin başındaki nefesli patlama
                src = g + s.breath * (.35f + burst * 2f) * Noise(rnd);
                wetSrc = Noise(rnd) * gate * wet;
                env = Mathf.Clamp01(local / .012f) * Mathf.Pow(1 - u, .6f) * s.amp;
                if (growlHz > 0) env *= 1 - growlDepth * (.5f + .5f * Mathf.Sin(2 * Mathf.PI * growlHz * t));
                r1.Set(Mathf.Lerp(s.f1a, s.f1b, u), 110); r2.Set(Mathf.Lerp(s.f2a, s.f2b, u), 180);
            }
            float y = r1.Run(src) + .8f * r2.Run(src) + .4f * r3.Run(src) + .6f * nasal.Run(src) + 3f * rw.Run(wetSrc);
            d[i] = Tanh(y * drive) * env;
        }
        return d;
    }

    static Syl[] Shift(Syl[] s, float pitch, float formant)
    {
        var o = (Syl[])s.Clone();
        for (int i = 0; i < o.Length; i++) { o[i].fa *= pitch; o[i].fb *= pitch; o[i].f1a *= formant; o[i].f1b *= formant; o[i].f2a *= formant; o[i].f2b *= formant; }
        return o;
    }

    // Canavar: ana ses + oktav aşağıda göğüs katmanı + iki oktav aşağıda gümbürtü + tiz çığlık katmanı.
    // Sonunda sert kırpma ve crush (örnek tekrarı) ile dijital, tırmalayıcı bir bozulma eklenir.
    static float[] Monster(float dur, Syl[] syl, float drive, float peak, int seed, float growlHz, float growlDepth, float wet, int crush)
    {
        var a = Goblin(dur, syl, drive, seed, growlHz, growlDepth, wet);
        var b = Goblin(dur, Shift(syl, .5f, .75f), drive, seed + 1, growlHz, growlDepth, 0);
        var c = Goblin(dur, Shift(syl, .25f, .6f), drive, seed + 2, growlHz * .5f, growlDepth, 0);
        var h = Goblin(dur, Shift(syl, 2f, 1.2f), drive, seed + 3, growlHz * 1.5f, growlDepth, wet);
        for (int i = 0; i < a.Length; i++) a[i] = Tanh(1.8f * (a[i] + .9f * b[i] + .7f * c[i] + .35f * h[i]));
        if (crush > 1) for (int i = 0; i < a.Length; i++) if (i % crush != 0) a[i] = a[i - 1];
        return Norm(a, peak);
    }

    // Alçak, deli bir kıkırtı ("heh-heh-HEH-heh") ve sonunda uzayan, yükselen vahşi bağırış.
    static float[] GoblinCackle()
    {
        var rnd = new System.Random(4); var list = new System.Collections.Generic.List<Syl>(); float t = .03f, gap = .25f;
        for (int i = 0; i < 9; i++)
        {
            bool a = i % 2 == 0; float fa = 380 - i * 10 + ((float)rnd.NextDouble() - .5f) * 120, len = Mathf.Min(.15f, gap * .8f);
            list.Add(new Syl(t, len, fa, fa * .55f, a ? 600 : 780, a ? 780 : 560, a ? 1300 : 1000, a ? 950 : 1400, .7f, 1.05f, 1 - .02f * i));
            t += gap + ((float)rnd.NextDouble() - .5f) * .04f; gap *= .91f;
        }
        list.Add(new Syl(t + .04f, .75f, 240, 950, 700, 950, 1200, 1700, .6f, 1f, 1f));
        return Monster(t + .95f, list.ToArray(), 5f, .7f, 4, 38, .35f, .55f, 2);
    }

    // "grrr-GRAAH-KRAAAGH": üç aşamalı, giderek yükselen ve kükreme titremeli saldırı.
    static float[] GoblinSnarl() => Monster(1.2f, new[]
    {
        new Syl(0, .42f, 110, 240, 430, 620, 850, 1200, .85f, 1.1f, .85f),
        new Syl(.36f, .42f, 260, 520, 650, 850, 1100, 1500, .7f, 1f, .95f),
        new Syl(.72f, .5f, 420, 1100, 820, 1000, 1350, 1800, .6f, .9f, 1f),
    }, 6f, .85f, 8, 55, .65f, .6f, 3);

    // Art arda üç gırtlaktan gelen, bozuk, acı dolu çığlık.
    static float[] GoblinYelp() => Monster(.7f, new[]
    {
        new Syl(0, .15f, 1000, 380, 760, 650, 1500, 1200, .5f, .9f, 1f),
        new Syl(.19f, .18f, 820, 300, 720, 560, 1400, 1000, .5f, 1f, .95f),
        new Syl(.4f, .3f, 600, 200, 650, 480, 1200, 900, .5f, 1f, .9f),
    }, 4.5f, .75f, 13, 30, .4f, .5f, 2);
}
