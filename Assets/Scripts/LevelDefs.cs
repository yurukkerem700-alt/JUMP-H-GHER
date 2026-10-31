using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

public enum GoalKind { Floor, Coins, Survive }

public struct LevelDef
{
    public int number, target, tier, hearts, zombies, goblins;
    public GoalKind kind;
    public float twoStarTime, threeStarTime, limit, survive; // limit > 0: süre dolunca bölüm kaybedilir; survive: hayatta kalma süresi
    public float enemySpeed;
    public bool HardLimit => limit > 0;
    public bool HasEnemies => zombies + goblins > 0;
    public string EnemyText => zombies > 0 && goblins > 0 ? "ZOMBİ x" + zombies + " + GOBLİN x" + goblins : zombies > 0 ? "ZOMBİ x" + zombies : goblins > 0 ? "GOBLİN x" + goblins : "";
    public string Goal => kind == GoalKind.Floor ? target + ". kata çık, kapıdan geç" : kind == GoalKind.Coins ? target + " coin topla" : LevelDefs.Clock(survive) + " boyunca hayatta kal";
    public string TierName => LevelDefs.TierNames[tier];
    public Color TierColor => LevelDefs.TierColors[tier];
    public bool Timed => kind != GoalKind.Survive && twoStarTime > 0;

    // 1 yıldız: bölümü bitir. 2 yıldız: süre (hayatta kalmada en çok 1 can kaybı). 3 yıldız: süre + hiç can kaybetmemek.
    public string[] StarRules => kind == GoalKind.Survive
        ? new[] { "Süreyi tamamla", "En çok 1 can kaybet", "Hiç can kaybetme" }
        : new[] { "Bölümü tamamla", LevelDefs.Clock(twoStarTime) + " içinde bitir", LevelDefs.Clock(threeStarTime) + " içinde, hiç can kaybetmeden bitir" };

    public int StarsFor(float time, int livesLost)
    {
        if (kind == GoalKind.Survive) return 1 + (livesLost <= 1 ? 1 : 0) + (livesLost == 0 ? 1 : 0);
        return 1 + (time <= twoStarTime ? 1 : 0) + (time <= threeStarTime && livesLost == 0 ? 1 : 0);
    }
}

public static class LevelDefs
{
    public const int Count = 100, PerTier = 20;
    public static readonly string[] TierNames = { "ÇIRAK", "GEZGİN", "USTA", "UZMAN", "EFSANE" };
    public static readonly Color[] TierColors =
    {
        new Color(.5f, .88f, .5f), new Color(.42f, .76f, 1f), new Color(.78f, .58f, 1f), new Color(1f, .62f, .3f), new Color(1f, .36f, .36f)
    };

    // 100 bölüm, 5 zorluk kademesi (20'şer bölüm): Çırak, Gezgin, Usta, Uzman, Efsane.
    // Kademe içinde 4 bölümlük döngü: kat hedefi, coin hedefi, hayatta kal (Çırak'ta kat), az canlı kat hedefi.
    // Her 5. bölüm (100 ve hayatta kalma hariç) süreli: 2 yıldız süresi aşılırsa bölüm kaybedilir.
    // Uzman ve Efsane kademelerinde yalnız 2 can vardır; "az canlı" bölümlerde can bir daha azalır.
    public static LevelDef Get(int n)
    {
        n = Mathf.Clamp(n, 1, Count);
        var d = new LevelDef { number = n };
        d.tier = (n - 1) / PerTier;
        int p = (n - 1) % PerTier + 1, phase = p % 4;
        d.kind = n == Count ? GoalKind.Floor : phase == 2 ? GoalKind.Coins : phase == 3 && d.tier > 0 ? GoalKind.Survive : GoalKind.Floor;
        d.target = Mathf.RoundToInt(5 + (n - 1) * 95f / 99f);
        int baseHearts = d.tier >= 3 ? 2 : 3;
        d.hearts = n == Count ? 2 : phase == 0 ? Mathf.Max(d.tier == 0 ? 2 : 1, baseHearts - 1) : baseHearts;
        if (d.kind == GoalKind.Survive) d.hearts = 3; // hayatta kalmada 2. yıldız "en çok 1 can" şartıdır
        AssignEnemies(ref d, p);
        if (d.kind == GoalKind.Survive)
        {
            d.survive = Round5(40 + d.tier * 15 + p);
            return d;
        }
        float shrink = 1 - d.tier * .04f;
        float two = Round5((15 + d.target * 5f) * shrink), three = Round5((10 + d.target * 3.2f) * shrink);
        if (n % 5 == 0 && n < Count) { d.limit = two; two = Round5(two * .8f); }
        d.twoStarTime = two; d.threeStarTime = Mathf.Min(three, two - 5);
        return d;
    }

    // Çırak: 1-15 düşmansız, 16-20 tek zombi. Gezgin: zombi (1, sonra 2), her 4. bölümde ek goblin.
    // Usta: goblin (1, sonra 2), çift numaralı bölümlerde ek zombi. Uzman: 2 zombi + 2 goblin (her 4. bölümde 3 goblin).
    // Efsane: 3 zombi + 2 goblin (her 4. bölümde 3 goblin). 100. bölüm: 3 zombi + 3 goblin.
    static void AssignEnemies(ref LevelDef d, int p)
    {
        int n = d.number;
        switch (d.tier)
        {
            case 0: if (n >= 16) d.zombies = 1; break;
            case 1: d.zombies = n < 31 ? 1 : 2; d.goblins = p % 4 == 0 ? 1 : 0; break;
            case 2: d.goblins = n < 51 ? 1 : 2; d.zombies = p % 2 == 0 ? 1 : 0; break;
            case 3: d.zombies = 2; d.goblins = p % 4 == 0 ? 3 : 2; break;
            default: d.zombies = 3; d.goblins = p % 4 == 0 ? 3 : 2; break;
        }
        if (n == Count) { d.zombies = 3; d.goblins = 3; }
        if (d.HasEnemies) d.enemySpeed = Mathf.Min(1.6f, 1 + d.tier * .08f + (p - 1) * .006f);
    }

    static float Round5(float v) => Mathf.Max(5, Mathf.Round(v / 5) * 5);

    public static int Stars(int n) => PlayerPrefs.GetInt("LvStars" + n, 0);
    public static void SaveStars(int n, int stars) { if (stars > Stars(n)) { PlayerPrefs.SetInt("LvStars" + n, stars); PlayerPrefs.Save(); } }
    public static bool Unlocked(int n) => n <= 1 || Stars(n - 1) > 0;
    public static bool GameComplete => Stars(Count) > 0;
    public static int Current() { for (int n = 1; n <= Count; n++) if (Stars(n) == 0) return n; return Count; }
    public static int TotalStars() { int s = 0; for (int n = 1; n <= Count; n++) s += Stars(n); return s; }
    public static string Clock(float seconds) { int s = Mathf.CeilToInt(Mathf.Max(0, seconds)); return (s / 60).ToString("00") + ":" + (s % 60).ToString("00"); }
}

// Menü, harita ve sonuç ekranları için ortak, prosedürel arayüz parçaları.
public static class UiArt
{
    static Sprite star, disc, heart;
    public static Sprite Star => star ? star : star = Build(64, 64, 64, (u, v) => StarAlpha(u * 2, v * 2));
    // Kalp: (x²+y²-1)³ - x²y³ <= 0 eğrisi
    public static Sprite Heart => heart ? heart : heart = Build(64, 64, 64, (u, v) =>
    {
        float x = u * 2.4f, y = v * 2.4f + .1f, a = x * x + y * y - 1;
        return .5f - (a * a * a - x * x * y * y * y) * 8;
    });
    public static Sprite Disc => disc ? disc : disc = Build(64, 64, 64, (u, v) => Mathf.Clamp01((.97f - Mathf.Sqrt(u * u + v * v) * 2) * 32 + .5f));

    // alpha(u,v): sprite merkezine göre birim cinsinden koordinat
    public static Sprite Build(int w, int h, float ppu, Func<float, float, float> alpha)
    {
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
        var px = new Color32[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                px[y * w + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(Mathf.Clamp01(alpha((x + .5f - w / 2f) / ppu, (y + .5f - h / 2f) / ppu)) * 255));
        tex.SetPixels32(px); tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), Vector2.one * .5f, ppu);
    }

    // Beş köşeli yıldız (ucu yukarıda); x,y: -1..1
    static float StarAlpha(float x, float y)
    {
        const float outer = .95f, inner = .4f, sector = Mathf.PI * 2 / 5;
        float r = Mathf.Sqrt(x * x + y * y);
        float phi = Mathf.Abs(Mathf.Repeat(Mathf.Atan2(y, x) - Mathf.PI / 2 + sector / 2, sector) - sector / 2);
        var p1 = new Vector2(outer, 0);
        var d = new Vector2(inner * Mathf.Cos(sector / 2) - outer, inner * Mathf.Sin(sector / 2));
        var dir = new Vector2(Mathf.Cos(phi), Mathf.Sin(phi));
        float t = -(p1.x * dir.y - p1.y * dir.x) / (d.x * dir.y - d.y * dir.x);
        float edge = (p1 + d * t).magnitude;
        return Mathf.Clamp01((edge - r) * 32 + .5f);
    }

    public static Canvas NewCanvas(string name, int order)
    {
        var canvas = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)).GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = order;
        var scaler = canvas.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1080, 1080); scaler.matchWidthOrHeight = .5f;
        return canvas;
    }

    // cx,cy: üst öğeye göre 0..1 sabitleme noktası; w,h: tuval birimi
    public static TextMeshProUGUI Label(Transform parent, TMP_FontAsset font, string text, float cx, float cy, float w, float h, float size, Color color)
    {
        var go = new GameObject(text, typeof(TextMeshProUGUI)); go.transform.SetParent(parent, false);
        var t = go.GetComponent<TextMeshProUGUI>(); t.font = font; t.text = text; t.fontSize = size; t.color = color; t.alignment = TextAlignmentOptions.Center; t.raycastTarget = false;
        var r = t.rectTransform; r.anchorMin = r.anchorMax = new Vector2(cx, cy); r.sizeDelta = new Vector2(w, h); return t;
    }

    public static Image Pic(Transform parent, Sprite sprite, Color color, float cx, float cy, float w, float h)
    {
        var go = new GameObject("Resim", typeof(Image)); go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>(); img.sprite = sprite; img.color = color; img.raycastTarget = false;
        var r = img.rectTransform; r.anchorMin = r.anchorMax = new Vector2(cx, cy); r.sizeDelta = new Vector2(w, h); return img;
    }

    public static Button Btn(Transform parent, TMP_FontAsset font, string title, float cx, float cy, float w, float h, UnityAction action)
    {
        var go = new GameObject(title, typeof(Image), typeof(Button)); go.transform.SetParent(parent, false);
        var r = go.GetComponent<RectTransform>(); r.anchorMin = r.anchorMax = new Vector2(cx, cy); r.sizeDelta = new Vector2(w, h);
        go.GetComponent<Image>().color = new Color(.22f, .12f, .14f, .95f);
        var b = go.GetComponent<Button>(); b.onClick.AddListener(() => { SesAtolyesi.Ses.UiClick(); action(); });
        var t = Label(go.transform, font, title, .5f, .5f, w, h, 25, new Color(1, .81f, .59f));
        t.rectTransform.anchorMin = Vector2.zero; t.rectTransform.anchorMax = Vector2.one; t.rectTransform.offsetMin = t.rectTransform.offsetMax = Vector2.zero;
        return b;
    }
}
