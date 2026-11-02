using UnityEngine;

// Hafif geri bildirim efektleri: toz/kıvılcım parçacıkları ve kamera sarsıntısı.
// Havuzlu SpriteRenderer kullanır; ek asset gerektirmez (daire dokusu kodla üretilir).
[DefaultExecutionOrder(100)]
public class Juice : MonoBehaviour
{
    const int MaxParticles = 48;
    static Juice instance;

    struct Particle { public bool alive; public Vector2 pos, vel; public float age, life, size; public Color color; }

    Particle[] particles = new Particle[MaxParticles];
    SpriteRenderer[] renderers = new SpriteRenderer[MaxParticles];
    Sprite dot;
    Material material;
    Camera cam;
    Vector3 lastShake;
    float shakeAmount, shakeUntil, shakeDuration;

    // material: oyuncu sprite'ının kullandığı materyal (WebGL'de shader kırpılmasın diye yeniden kullanılır).
    public static void Ensure(Material spriteMaterial)
    {
        if (instance) return;
        var go = new GameObject("Juice");
        instance = go.AddComponent<Juice>();
        instance.material = spriteMaterial;
        instance.Build();
    }

    void Build()
    {
        const int N = 16;
        var tex = new Texture2D(N, N, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
        for (int y = 0; y < N; y++)
            for (int x = 0; x < N; x++)
            {
                float d = Vector2.Distance(new Vector2(x + .5f, y + .5f), Vector2.one * N * .5f) / (N * .5f);
                tex.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01((1 - d) * 3f)));
            }
        tex.Apply();
        dot = Sprite.Create(tex, new Rect(0, 0, N, N), Vector2.one * .5f, N);
        for (int i = 0; i < MaxParticles; i++)
        {
            var go = new GameObject("p", typeof(SpriteRenderer));
            go.transform.SetParent(transform, false);
            var r = go.GetComponent<SpriteRenderer>();
            r.sprite = dot; r.sharedMaterial = material; r.sortingOrder = 25; r.enabled = false;
            renderers[i] = r;
        }
    }

    void OnDestroy() { if (instance == this) instance = null; }

    public static void Burst(Vector2 position, int count, Color color, float speed, float upward, float life, float size)
    {
        if (!instance) return;
        instance.Emit(position, count, color, speed, upward, life, size);
    }

    // Ayak altında toz: yatayda yayılır, hafif yukarı kalkar.
    public static void Dust(Vector2 feet, float strength)
    {
        Burst(feet, Mathf.RoundToInt(Mathf.Lerp(3, 9, strength)), new Color(.82f, .74f, .6f, .75f), Mathf.Lerp(1.2f, 2.6f, strength), .6f, .35f, Mathf.Lerp(.12f, .2f, strength));
    }

    public static void Shake(float amount, float duration)
    {
        if (!instance) return;
        instance.shakeAmount = Mathf.Max(instance.shakeAmount, amount);
        instance.shakeDuration = duration; instance.shakeUntil = Time.time + duration;
    }

    void Emit(Vector2 position, int count, Color color, float speed, float upward, float life, float size)
    {
        for (int n = 0; n < count; n++)
        {
            int slot = -1;
            for (int i = 0; i < MaxParticles; i++) if (!particles[i].alive) { slot = i; break; }
            if (slot < 0) return;
            float angle = Random.Range(0f, Mathf.PI * 2f);
            var dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle) * .5f + upward);
            particles[slot] = new Particle { alive = true, pos = position, vel = dir * speed * Random.Range(.5f, 1f), age = 0, life = life * Random.Range(.7f, 1.1f), size = size * Random.Range(.7f, 1.2f), color = color };
            renderers[slot].enabled = true;
        }
    }

    // Update kamera sarsıntısını geri alır; böylece InfiniteTower kamerayı sarsıntısız konumdan hesaplar.
    void Update()
    {
        if (!cam) cam = Camera.main;
        if (cam) cam.transform.position -= lastShake;
        lastShake = Vector3.zero;
        float dt = Time.deltaTime;
        for (int i = 0; i < MaxParticles; i++)
        {
            if (!particles[i].alive) continue;
            particles[i].age += dt;
            if (particles[i].age >= particles[i].life) { particles[i].alive = false; renderers[i].enabled = false; continue; }
            particles[i].vel.y -= 2.5f * dt;
            particles[i].vel *= 1f - Mathf.Min(1f, 3f * dt);
            particles[i].pos += particles[i].vel * dt;
            float t = particles[i].age / particles[i].life;
            var c = particles[i].color; c.a *= 1f - t;
            renderers[i].transform.position = particles[i].pos;
            renderers[i].transform.localScale = Vector3.one * particles[i].size * (1f - t * .4f);
            renderers[i].color = c;
        }
    }

    // Sınıf sırası 100: InfiniteTower (-80) kamerayı yerleştirdikten sonra çalışır.
    void LateUpdate()
    {
        if (!cam || Time.time >= shakeUntil) { shakeAmount = 0; return; }
        float falloff = (shakeUntil - Time.time) / Mathf.Max(.01f, shakeDuration);
        var offset = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), 0) * shakeAmount * falloff;
        cam.transform.position += offset; lastShake = offset;
    }
}
