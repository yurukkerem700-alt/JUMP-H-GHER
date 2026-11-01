using UnityEngine;

// Başlangıç sahnesine canlılık: mezardan çıkmaya çalışan zombi ve dalda duran kuzgunlar.
// Grafikler Resources/Decor altındaki yatay şeritlerdir (kare = yükseklik x yükseklik); dosya yoksa hiçbir şey eklenmez.
//   Resources/Decor/ZombiMezar.png : kareler sırayla oynar (ileri-geri), taban orta noktası zeminde
//   Resources/Decor/Kuzgun.png     : kare 0 = durur, diğerleri = başını çevirir / gagalar / kanat silker
// Konumlar arka plan resminin kenarlarına göre oranlıdır; ekran oranı değişince birlikte kayar.
// Doğal dursun diye: sahnenin sisli turuncu ışığına boyanır, altında yumuşak gölge vardır, kareler birbirine
// yumuşakça geçer ve hareket düzensiz aralıklarla (bazen duraksayarak) ilerler.
public class GraveDecor : MonoBehaviour
{
    // 0..1: arka plan resminin soldan / yukarıdan oranı (zombi için tabanın, kuzgunlar için ayak yerinin konumu)
    const float ZombieU = .182f, ZombieV = .715f, ZombieHeight = 1.6f, Sink = .05f;
    const float BranchU = .863f, BranchV = .18f, RavenHeight = .75f;
    static readonly Vector2[] RavenOffsets = { new Vector2(0, 0), new Vector2(-.02f, .008f), new Vector2(.015f, -.006f) };
    static readonly float[] RavenScale = { 1f, .88f, .8f };
    // Sahne ışığına uydurma: biraz kısık, sıcak ve sisli
    static readonly Color ZombieTint = new Color(.76f, .66f, .62f), RavenTint = new Color(.85f, .82f, .86f);

    SpriteRenderer bg, zombie, zombieNext, shadow;
    Sprite[] zombieFrames, ravenFrames;
    SpriteRenderer[] ravens;
    float[] nextAct, actUntil;
    int zi, zdir = 1; float zNext, zFadeStart = -1;

    public void Init(InfiniteTower tower)
    {
        var background = GameObject.Find("Mezarlik");
        if (!background) { enabled = false; return; }
        bg = background.GetComponent<SpriteRenderer>();
        var material = tower.coins.coin.GetComponentInChildren<SpriteRenderer>().sharedMaterial;
        zombieFrames = Slice("ZombiMezar", ZombieHeight);
        ravenFrames = Slice("Kuzgun", RavenHeight);
        if (zombieFrames != null)
        {
            shadow = Make("Mezar Gölgesi", SoftShadow(), material, -21);
            zombie = Make("Mezardaki Zombi", zombieFrames[0], material, -20);
            zombieNext = Make("Mezardaki Zombi (geçiş)", zombieFrames[0], material, -19);
            zombie.color = ZombieTint; zombieNext.color = new Color(ZombieTint.r, ZombieTint.g, ZombieTint.b, 0);
            zNext = Time.time + Random.Range(.6f, 1.4f);
        }
        if (ravenFrames != null)
        {
            ravens = new SpriteRenderer[RavenOffsets.Length]; nextAct = new float[ravens.Length]; actUntil = new float[ravens.Length];
            for (int i = 0; i < ravens.Length; i++)
            {
                ravens[i] = Make("Kuzgun " + (i + 1), ravenFrames[0], material, -18);
                ravens[i].flipX = i == 1; ravens[i].color = RavenTint;
                ravens[i].transform.localScale = Vector3.one * RavenScale[i];
                nextAct[i] = Time.time + Random.Range(3f, 9f);
            }
        }
        if (zombie == null && ravens == null) enabled = false;
    }

    static SpriteRenderer Make(string name, Sprite sprite, Material material, int order)
    {
        var sr = new GameObject(name, typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
        sr.sprite = sprite; sr.sharedMaterial = material; sr.sortingOrder = order; return sr;
    }

    static Sprite[] Slice(string name, float targetHeight)
    {
        var texture = Resources.Load<Texture2D>("Decor/" + name);
        if (!texture) return null;
        int h = texture.height, n = Mathf.Max(1, texture.width / h);
        // Küçük (piksel sanatı) kareler keskin kalsın
        if (h <= 128) texture.filterMode = FilterMode.Point;
        var frames = new Sprite[n];
        for (int i = 0; i < n; i++) frames[i] = Sprite.Create(texture, new Rect(i * h, 0, h, h), new Vector2(.5f, 0), h / targetHeight);
        return frames;
    }

    // Yumuşak kenarlı koyu elips: zombinin yığınını zemine oturtur.
    static Sprite SoftShadow()
    {
        const int w = 128, h = 48;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        var px = new Color32[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float dx = (x + .5f) / w * 2 - 1, dy = (y + .5f) / h * 2 - 1, r = Mathf.Sqrt(dx * dx + dy * dy);
                float a = Mathf.Clamp01(1 - r); a = a * a * .62f;
                px[y * w + x] = new Color32(8, 5, 4, (byte)(a * 255));
            }
        tex.SetPixels32(px); tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(.5f, .5f), 64);
    }

    Vector3 At(float u, float v)
    {
        var b = bg.bounds;
        return new Vector3(b.min.x + u * b.size.x, b.max.y - v * b.size.y, 0);
    }

    void LateUpdate()
    {
        if (!bg) return;
        float t = Time.time;
        if (zombie) StepZombie(t);
        if (ravens != null)
            for (int i = 0; i < ravens.Length; i++)
            {
                int n = ravenFrames.Length;
                if (t >= nextAct[i] && n > 1) { actUntil[i] = t + Random.Range(.6f, 1.5f); nextAct[i] = actUntil[i] + Random.Range(5f, 14f); ravens[i].sprite = ravenFrames[Random.Range(1, n)]; }
                if (t >= actUntil[i]) ravens[i].sprite = ravenFrames[0];
                ravens[i].transform.position = At(BranchU + RavenOffsets[i].x, BranchV + RavenOffsets[i].y);
            }
    }

    void StepZombie(float t)
    {
        // Kareler arası düzensiz bekleme ve yumuşak geçiş: durağan, ağır, güçlükle ilerleyen bir hareket.
        if (t >= zNext)
        {
            int n = zombieFrames.Length, ni = zi + zdir;
            if (ni >= n || ni < 0) { zdir = -zdir; ni = zi + zdir; }
            zombieNext.sprite = zombieFrames[ni]; zi = ni; zFadeStart = t;
            zNext = t + Random.Range(.7f, 1.5f) + (Random.value < .2f ? Random.Range(.8f, 1.8f) : 0);
        }
        if (zFadeStart >= 0)
        {
            float k = Mathf.SmoothStep(0, 1, Mathf.Clamp01((t - zFadeStart) / .45f));
            zombieNext.color = new Color(ZombieTint.r, ZombieTint.g, ZombieTint.b, k);
            if (k >= 1) { zombie.sprite = zombieNext.sprite; zombieNext.color = new Color(ZombieTint.r, ZombieTint.g, ZombieTint.b, 0); zFadeStart = -1; }
        }
        var p = At(ZombieU, ZombieV) + new Vector3(0, -Sink, 0);
        float breathe = 1 + Mathf.Sin(t * 1.1f) * .012f;
        zombie.transform.position = zombieNext.transform.position = p;
        zombie.transform.localScale = zombieNext.transform.localScale = new Vector3(1, breathe, 1);
        shadow.transform.position = p + new Vector3(0, .05f, 0);
        shadow.transform.localScale = new Vector3(ZombieHeight * 1.05f, ZombieHeight * .3f, 1);
    }
}
