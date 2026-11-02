using UnityEngine;

// Başlangıç sahnesine canlılık: mezardan çıkmaya çalışan zombi ve dalda duran kuzgunlar.
// Grafikler Resources/Decor altındaki yatay şeritlerdir (kare = yükseklik x yükseklik); dosya yoksa hiçbir şey eklenmez.
//   Resources/Decor/ZombiMezar.png : kareler sırayla oynar (ileri-geri), taban orta noktası zeminde
//   Resources/Decor/Kuzgun.png     : kare 0 = durur, diğerleri = başını çevirir / gagalar / kanat silker
//   Resources/Decor/Baykus.png     : sol ağacın dalında baykuş (3 kare: durur / yana bakar)
//   Resources/Decor/KaraKedi.png   : sağdaki mezar taşında kara kedi (3 kare: oturur / döner)
//   Resources/Decor/Yarasa.png     : tek kare; kanat çırpma kodla (yarasalar ayın çevresinde uçar)
//   Ruh ışıkları (ateşböcekleri) tamamen kodla çizilir, dosya gerekmez.
// Konumlar arka plan resminin kenarlarına göre oranlıdır; ekran oranı değişince birlikte kayar.
// Doğal dursun diye: sahnenin sisli turuncu ışığına boyanır, altında yumuşak gölge vardır, kareler birbirine
// yumuşakça geçer ve hareket düzensiz aralıklarla (bazen duraksayarak) ilerler.
public class GraveDecor : MonoBehaviour
{
    // 0..1: arka plan resminin soldan / yukarıdan oranı (zombi için tabanın, kuzgunlar için ayak yerinin konumu)
    const float ZombieU = .182f, ZombieV = .715f, ZombieHeight = 2.3f, Sink = .05f;
    const float RavenHeight = 1.15f, FootMargin = .031f; // FootMargin: karenin altı ile ayakların arası (kare yüksekliğine oranla)
    const float OwlU = .165f, OwlV = .189f, OwlHeight = 1.05f; // sol ağacın dalının üstü
    const float CatU = .863f, CatV = .5455f, CatHeight = 1.15f; // mezar taşının tepe noktası
    const float BatHeight = 1.9f; const int BatCount = 2, WispCount = 16;
    static readonly Color OwlTint = new Color(.85f, .8f, .82f), CatTint = new Color(.8f, .78f, .86f), BatTint = new Color(.7f, .62f, .66f);
    // Kuzgunlar sağ ağacın kalın dalının düz üst yüzeyine konar: (u, v) = ayakların bastığı nokta (arka plan görselinden ölçüldü).
    static readonly Vector2[] RavenOffsets = { new Vector2(.890f, .1315f), new Vector2(.926f, .1325f) };
    static readonly float[] RavenScale = { 1f, .88f };
    // Sahne ışığına uydurma: biraz kısık, sıcak ve sisli
    static readonly Color ZombieTint = new Color(.76f, .66f, .62f), RavenTint = new Color(.85f, .82f, .86f);

    class Perch { public SpriteRenderer shadow; public float drop, width; public SpriteRenderer sr; public Sprite[] frames; public float u, v, next, until, minWait, maxWait; }
    readonly System.Collections.Generic.List<Perch> perches = new System.Collections.Generic.List<Perch>();
    SpriteRenderer[] bats, wisps, ravenShadows; Sprite[] batFrames; float[] wispSeed;
    Sprite[][] batModels;
    SpriteRenderer bg, zombie, zombieNext, shadow;
    Sprite[] zombieFrames, ravenFrames;
    SpriteRenderer[] ravens;
    float[] nextAct, actUntil;
    int zi, zdir = 1; float zNext, zFadeStart = -1;

    public void Init(InfiniteTower tower) => Init(tower.coins.coin.GetComponentInChildren<SpriteRenderer>().sharedMaterial);

    public void Init(Material material)
    {
        var background = GameObject.Find("Mezarlik");
        if (!background) { enabled = false; return; }
        bg = background.GetComponent<SpriteRenderer>();
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
                if (ravenShadows == null) ravenShadows = new SpriteRenderer[ravens.Length];
                ravenShadows[i] = Make("Kuzgun Gölgesi " + (i + 1), SoftShadow(), material, -19); ravenShadows[i].color = new Color(1, 1, 1, .3f);
            }
        }
        AddPerch("Baykuş", "Baykus", OwlHeight, OwlU, OwlV, OwlTint, false, material, 4f, 10f);
        AddPerch("Kara Kedi", "KaraKedi", CatHeight, CatU, CatV, CatTint, true, material, 5f, 12f);
        batFrames = Slice("Yarasa", BatHeight);
        if (batFrames != null)
        {
            bats = new SpriteRenderer[BatCount];
            batModels=new Sprite[BatCount][];
            for (int i = 0; i < BatCount; i++) { batModels[i]=SkyCreatureArt.Get(2+i%2); bats[i] = Make("Yarasa " + (i + 1), batModels[i]!=null?batModels[i][0]:batFrames[0], material, -25); bats[i].color = BatTint; }
        }
        BuildWisps(material);
        if (zombie == null && ravens == null && perches.Count == 0 && bats == null && wisps == null) enabled = false;
    }

    void AddPerch(string name, string file, float height, float u, float v, Color tint, bool flip, Material material, float minWait, float maxWait)
    {
        var frames = Slice(file, height); if (frames == null) return;
        var sr = Make(name, frames[0], material, -17); sr.color = tint; sr.flipX = flip;
        var shadow = Make(name + " Gölgesi", SoftShadow(), material, -18); shadow.color = new Color(1, 1, 1, flip ? .8f : .3f); // taş üstünde belirgin, ince dalda hafif
        perches.Add(new Perch { sr = sr, shadow = shadow, drop = height * FootMargin - .01f, width = height * .5f, frames = frames, u = u, v = v, minWait = minWait, maxWait = maxWait, next = Time.time + Random.Range(minWait, maxWait) });
    }

    // Ruh ışıkları: sisin içinde süzülen, nabız gibi yanıp sönen küçük soluk ışıklar (kodla çizilir).
    void BuildWisps(Material material)
    {
        var glow = GlowSprite();
        wisps = new SpriteRenderer[WispCount]; wispSeed = new float[WispCount * 4];
        for (int i = 0; i < WispCount; i++)
        {
            wisps[i] = Make("Ruh Işığı " + (i + 1), glow, material, -30);
            bool warm = i % 5 == 0;
            wisps[i].color = warm ? new Color(1f, .78f, .4f, 0) : new Color(.55f, 1f, .8f, 0);
            for (int k = 0; k < 4; k++) wispSeed[i * 4 + k] = Random.value;
        }
    }

    static Sprite GlowSprite()
    {
        const int n = 48;
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        var px = new Color32[n * n];
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float dx = (x + .5f) / n * 2 - 1, dy = (y + .5f) / n * 2 - 1, r = Mathf.Sqrt(dx * dx + dy * dy);
                float a = Mathf.Clamp01(1 - r); a = a * a * a;
                float core = Mathf.Clamp01(1 - r * 3.5f);
                px[y * n + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(a * .8f + core) * 255));
            }
        tex.SetPixels32(px); tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(.5f, .5f), 100);
    }

    void StepAmbient(float t)
    {
        foreach (var p in perches)
        {
            int n = p.frames.Length;
            if (t >= p.next && n > 1) { p.until = t + Random.Range(.8f, 1.8f); p.next = p.until + Random.Range(p.minWait, p.maxWait); p.sr.sprite = p.frames[Random.Range(1, n)]; }
            if (t >= p.until) p.sr.sprite = p.frames[0];
            float sway = Mathf.Sin(t * .6f + p.u * 9f);
            p.sr.transform.position = At(p.u, p.v) + Vector3.down * p.drop;
            p.sr.transform.rotation = Quaternion.Euler(0, 0, sway * .7f);
            p.sr.transform.localScale = new Vector3(1, 1 + Mathf.Sin(t * 1.1f + p.u * 7f) * .007f, 1);
            p.shadow.transform.position = At(p.u, p.v) + new Vector3(.04f, 0, 0);
            p.shadow.transform.localScale = new Vector3(p.width, p.width * .2f, 1);
        }
        if (bats != null)
            for (int i = 0; i < bats.Length; i++)
            {
                float period = 26f + i * 11f, k = Mathf.Repeat(t / period + i * .47f, 1f);
                bool right = i % 2 == 0; float u = right ? Mathf.Lerp(-.1f, 1.1f, k) : Mathf.Lerp(1.1f, -.1f, k);
                float v = .16f + i * .07f + Mathf.Sin(t * 1.3f + i * 2f) * .035f;
                float flap = .55f + .45f * Mathf.Abs(Mathf.Cos(t * (11f + i * 2f)));
                bats[i].transform.position = At(u, v);
                if(batModels[i]!=null)
                {
                    bats[i].sprite=batModels[i][(Mathf.FloorToInt(t*(9+i))+i)%4];
                    bats[i].transform.localScale=Vector3.one;
                }
                else bats[i].transform.localScale = new Vector3(flap, 1f, 1f);
                bats[i].transform.rotation = Quaternion.Euler(0, 0, Mathf.Sin(t * 2.2f + i) * 6f);
                bats[i].flipX = !right;
            }
        if (wisps != null)
            for (int i = 0; i < wisps.Length; i++)
            {
                float a = wispSeed[i * 4], b = wispSeed[i * 4 + 1], c = wispSeed[i * 4 + 2], d = wispSeed[i * 4 + 3];
                float u = Mathf.Lerp(.12f, .88f, a) + Mathf.Sin(t * (.15f + c * .2f) + d * 6.28f) * .03f;
                float v = Mathf.Lerp(.42f, .72f, b) + Mathf.Sin(t * (.22f + a * .2f) + c * 6.28f) * .025f;
                float pulse = .5f + .5f * Mathf.Sin(t * (.9f + d * 1.1f) + c * 6.28f);
                var col = wisps[i].color; col.a = .12f + .6f * pulse * pulse; wisps[i].color = col;
                wisps[i].transform.position = At(u, v);
                wisps[i].transform.localScale = Vector3.one * (.5f + .35f * pulse + b * .15f);
            }
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
        StepAmbient(t);
        if (zombie) StepZombie(t);
        if (ravens != null)
            for (int i = 0; i < ravens.Length; i++)
            {
                int n = ravenFrames.Length;
                if (t >= nextAct[i] && n > 1) { actUntil[i] = t + Random.Range(.6f, 1.5f); nextAct[i] = actUntil[i] + Random.Range(5f, 14f); ravens[i].sprite = ravenFrames[Random.Range(1, n)]; }
                if (t >= actUntil[i]) ravens[i].sprite = ravenFrames[0];
                float scale = RavenScale[i], sway = Mathf.Sin(t * .7f + i * 2.1f);
                ravens[i].transform.position = At(RavenOffsets[i].x, RavenOffsets[i].y) + Vector3.down * (RavenHeight * scale * FootMargin - .01f);
                ravens[i].transform.rotation = Quaternion.Euler(0, 0, sway * .9f);
                ravens[i].transform.localScale = new Vector3(scale, scale * (1 + Mathf.Sin(t * 1.3f + i) * .006f), 1);
                if (ravenShadows != null) { ravenShadows[i].transform.position = At(RavenOffsets[i].x, RavenOffsets[i].y) + new Vector3(.05f, 0, 0); ravenShadows[i].transform.localScale = new Vector3(RavenHeight * scale * .5f, RavenHeight * scale * .1f, 1); }
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
