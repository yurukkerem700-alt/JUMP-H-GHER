using UnityEngine;

public enum EnemySound { ZombieSpawn, GoblinSpawn, ZombieAttack, GoblinAttack }

// Zombi ve goblin için kod ile üretilen sesler: doğuş uyarısı ve saldırı çığlıkları.
// Ses seviyesi SesAtolyesi'nin Master/Sfx ayarlarına bağlıdır.
public static class EnemySfx
{
    const int Rate = 22050;
    static readonly AudioClip[] clips = new AudioClip[4];
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

    static AudioClip Build(EnemySound sound)
    {
        float[] d;
        switch (sound)
        {
            case EnemySound.ZombieSpawn: d = Voice(1.1f, 95, 55, 5f, .35f, .55f, 3); break;   // derin, uzun inleme
            case EnemySound.GoblinSpawn: d = Cackle(.7f); break;                                // tiz kıkırtı
            case EnemySound.ZombieAttack: d = Voice(.55f, 140, 70, 14f, .6f, .85f, 2); break;  // hırıltılı kükreme
            default: d = Screech(.4f); break;                                                   // keskin çığlık
        }
        var clip = AudioClip.Create("dusman_" + sound, d.Length, 1, Rate, false); clip.SetData(d, 0); return clip;
    }

    static float Env(float t, float dur, float attack) => Mathf.Clamp01(t / attack) * Mathf.Pow(Mathf.Clamp01(1 - t / dur), 1.4f);

    // Testere dişi + titreşim (vibrato) + gürültü; f0'dan f1'e iner. Formant yerine alçak geçiren basit süzgeç.
    static float[] Voice(float dur, float f0, float f1, float vibHz, float noise, float peak, int harmonics)
    {
        var d = new float[(int)(dur * Rate)]; float phase = 0, lp = 0; var rnd = new System.Random(7);
        for (int i = 0; i < d.Length; i++)
        {
            float t = i / (float)Rate, k = t / dur;
            float f = Mathf.Lerp(f0, f1, k) * (1 + .06f * Mathf.Sin(2 * Mathf.PI * vibHz * t));
            phase += f / Rate; phase -= Mathf.Floor(phase);
            float s = 0; for (int h = 1; h <= harmonics + 3; h++) s += Mathf.Sin(2 * Mathf.PI * phase * h) / h;
            float n = (float)(rnd.NextDouble() * 2 - 1) * noise;
            lp += (s + n - lp) * .22f;
            d[i] = lp * Env(t, dur, .04f) * peak;
        }
        return d;
    }

    static float[] Cackle(float dur)
    {
        var d = new float[(int)(dur * Rate)]; float phase = 0;
        for (int i = 0; i < d.Length; i++)
        {
            float t = i / (float)Rate, burst = t * 11f % 1f; // ~11 kısa "hi" sesi
            float f = 620 + 520 * (1 - burst) + 220 * Mathf.Sin(t * 9f);
            phase += f / Rate; phase -= Mathf.Floor(phase);
            float s = Mathf.Sin(2 * Mathf.PI * phase) + .5f * Mathf.Sin(4 * Mathf.PI * phase) + .25f * (phase < .5f ? 1 : -1);
            d[i] = s * Mathf.Pow(1 - burst, 1.6f) * Mathf.Clamp01(1 - t / dur) * .4f;
        }
        return d;
    }

    static float[] Screech(float dur)
    {
        var d = new float[(int)(dur * Rate)]; float phase = 0; var rnd = new System.Random(11);
        for (int i = 0; i < d.Length; i++)
        {
            float t = i / (float)Rate, k = t / dur;
            float f = Mathf.Lerp(1300, 2400, Mathf.Sin(k * Mathf.PI * .5f)) * (1 + .04f * Mathf.Sin(2 * Mathf.PI * 38 * t));
            phase += f / Rate; phase -= Mathf.Floor(phase);
            float s = (phase < .5f ? 1 : -1) * .5f + Mathf.Sin(2 * Mathf.PI * phase) * .5f + (float)(rnd.NextDouble() * 2 - 1) * .15f;
            d[i] = s * Env(t, dur, .015f) * .45f;
        }
        return d;
    }
}
