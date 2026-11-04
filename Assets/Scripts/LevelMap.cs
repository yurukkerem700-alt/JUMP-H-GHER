using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Biyomlu, kesintisiz dikey bölüm haritası (v3): 6 biyom görseli üst üste dizilir, her 6 bölüm bir biyom sayfasıdır.
// Alt tarafta bölüm 1, yukarı kaydırdıkça sonraki biyomlar gelir. Biyom sınırlarında iki biyomun rengini taşıyan sis geçişi vardır.
// Biyom görselleri Resources/UI/Biomes/<ad>.png (forest, ruins, crystal, ember, ice, sky); yoksa biyom rengiyle düz zemin çizilir.
public partial class LevelMap
{
    const int PerBiome = 6, Total = LevelDefs.Count, Tiles = (Total + PerBiome - 1) / PerBiome;
    static readonly string[] BiomeNames = { "BÜYÜLÜ ORMAN", "KADİM HARABELER", "KRİSTAL MAĞARALAR", "KOR VADİSİ", "BUZ SIRADAĞLARI", "GÖKYÜZÜ ADALARI" };
    static readonly string[] BiomeFiles = { "forest", "ruins", "crystal", "ember", "ice", "sky" };
    static readonly Color[] Mist =
    {
        new Color32(178, 139, 224, 255), new Color32(255, 180, 113, 255), new Color32(105, 218, 255, 255),
        new Color32(255, 131, 92, 255), new Color32(151, 209, 255, 255), new Color32(221, 174, 255, 255)
    };
    // Madalyonların yerleri: görsellerdeki yolu izler (x: soldan oran, y: biyom sayfasının üstünden oran).
    static readonly Vector2[] Road =
    {
        new Vector2(.865f, .855f), new Vector2(.735f, .700f), new Vector2(.575f, .545f),
        new Vector2(.410f, .390f), new Vector2(.255f, .235f), new Vector2(.135f, .105f)
    };
    static readonly Color Gold = new Color(1f, .77f, .3f), Dim = new Color(.25f, .2f, .26f), Cream = new Color(1, .88f, .65f);

    class Node { public RectTransform holder; public Image dim, halo, medal, lockBadge; public TextMeshProUGUI number, nextBadge; public Image[] stars; }

    readonly GameObject root;
    readonly ScrollRect scroll;
    readonly RectTransform content, view;
    readonly Node[] nodes = new Node[Total];
    readonly TextMeshProUGUI totalText, biomeLabel;
    readonly GameObject card;
    readonly TextMeshProUGUI[] cardLines = new TextMeshProUGUI[9];
    readonly Action<int> start;
    int cardLevel, pendingScroll;
    float tileH = 900, viewH = 900;
    public bool IsOpen => root.activeSelf;

    public LevelMap(Transform parent, TMP_FontAsset font, Action<int> pick, Action endless, Action back)
    {
        start = pick;
        root = new GameObject("Bölüm Haritası", typeof(Image)); root.transform.SetParent(parent, false);
        Stretch(root.GetComponent<RectTransform>());
        root.GetComponent<Image>().color = new Color(.03f, .02f, .05f, 1);

        var viewGo = new GameObject("Görünüm", typeof(Image), typeof(RectMask2D), typeof(ScrollRect), typeof(MapLayout));
        viewGo.transform.SetParent(root.transform, false);
        view = viewGo.GetComponent<RectTransform>(); Stretch(view);
        viewGo.GetComponent<Image>().color = new Color(0, 0, 0, 0);
        var contentGo = new GameObject("İçerik", typeof(RectTransform)); contentGo.transform.SetParent(view, false);
        content = contentGo.GetComponent<RectTransform>();
        content.anchorMin = new Vector2(0, 1); content.anchorMax = Vector2.one; content.pivot = new Vector2(.5f, 1);
        content.sizeDelta = new Vector2(0, 900f * Tiles); content.anchoredPosition = Vector2.zero;
        scroll = viewGo.GetComponent<ScrollRect>();
        scroll.content = content; scroll.viewport = view; scroll.horizontal = false; scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped; scroll.inertia = true; scroll.decelerationRate = .1f; scroll.scrollSensitivity = 70;
        scroll.onValueChanged.AddListener(_ => UpdateBiome());
        viewGo.GetComponent<MapLayout>().changed = Relayout;

        for (int k = 0; k < Tiles; k++) BuildTile(k);
        for (int k = 0; k < Tiles - 1; k++) BuildTransition(k);
        for (int i = 0; i < Total; i++) BuildNode(i, font);

        var bar = new GameObject("Üst Şerit", typeof(RectTransform)); bar.transform.SetParent(root.transform, false);
        var br = bar.GetComponent<RectTransform>(); br.anchorMin = new Vector2(0, 1); br.anchorMax = Vector2.one; br.pivot = new Vector2(.5f, 1); br.sizeDelta = new Vector2(0, 150);
        var plate = new GameObject("Başlık Levhası", typeof(Image)); plate.transform.SetParent(bar.transform, false);
        var plr = plate.GetComponent<RectTransform>(); plr.anchorMin = plr.anchorMax = new Vector2(.5f, .64f); plr.sizeDelta = new Vector2(500, 82);
        Plate(plate.GetComponent<Image>(), "MapPlateTitle"); plate.GetComponent<Image>().raycastTarget = false;
        UiArt.Label(bar.transform, font, "BÖLÜMLER", .5f, .64f, 460, 60, 38, Cream);
        totalText = UiArt.Label(bar.transform, font, "", .5f, .22f, 500, 40, 22, Cream);
        UiArt.Pic(bar.transform, UiArt.Star, Gold, .5f, .22f, 34, 34).rectTransform.anchoredPosition = new Vector2(-125, 0);
        Plate(UiArt.Btn(bar.transform, font, "GERİ", .13f, .64f, 190, 66, () => back()).GetComponent<Image>());
        Plate(UiArt.Btn(bar.transform, font, "SONSUZ MOD", .85f, .64f, 250, 66, () => endless()).GetComponent<Image>());

        var foot = new GameObject("Alt Şerit", typeof(RectTransform)); foot.transform.SetParent(root.transform, false);
        var fr = foot.GetComponent<RectTransform>(); fr.anchorMin = Vector2.zero; fr.anchorMax = new Vector2(1, 0); fr.pivot = new Vector2(.5f, 0); fr.sizeDelta = new Vector2(0, 120);
        var fp = new GameObject("Biyom Levhası", typeof(Image)); fp.transform.SetParent(foot.transform, false);
        var fpr = fp.GetComponent<RectTransform>(); fpr.anchorMin = fpr.anchorMax = new Vector2(.5f, .6f); fpr.sizeDelta = new Vector2(620, 74);
        Plate(fp.GetComponent<Image>(), "MapPlateTitle"); fp.GetComponent<Image>().raycastTarget = false;
        biomeLabel = UiArt.Label(foot.transform, font, BiomeNames[0], .5f, .6f, 560, 56, 30, Cream);
        UiArt.Label(foot.transform, font, "YUKARI KAYDIR  •  KEŞFET", .5f, .12f, 700, 32, 20, new Color(.8f, .72f, .66f));

        card = BuildCard(root.transform, font);
        root.SetActive(false);
    }

    static int BiomeOf(int tile) => tile % BiomeNames.Length;

    // Biyom görseli: tek sayıdaki sayfalar yatay aynalanır, böylece yol sağdan sola zikzak çizer (madalyon yerleri de aynalanır).
    void BuildTile(int k)
    {
        int b = BiomeOf(k);
        var art = Painted("Biomes/" + BiomeFiles[b]);
        var img = UiArt.Pic(content, art, art ? Color.white : Color.Lerp(Color.black, Mist[b], .32f), .5f, .5f, 0, 0);
        img.name = "Biyom " + (k + 1); img.raycastTarget = false; img.preserveAspect = false;
        var r = img.rectTransform;
        r.anchorMin = new Vector2(0, k / (float)Tiles); r.anchorMax = new Vector2(1, (k + 1) / (float)Tiles);
        r.offsetMin = r.offsetMax = Vector2.zero;
        if (k % 2 == 1) r.localScale = new Vector3(-1, 1, 1);
    }

    // İki biyom arasında: iki rengin sisi, yumuşak bulut lekeleri.
    void BuildTransition(int k)
    {
        Color lower = Mist[BiomeOf(k)], upper = Mist[BiomeOf(k + 1)];
        float fy = (k + 1) / (float)Tiles;
        const float Fade = 520;
        for (int half = 0; half < 2; half++)
        {
            // Sınırda iki yarı aynı rengi taşır; sert renk çizgisi yerine sis iki biyomu birbirine karıştırır.
            var c = Color.Lerp(lower, upper, .5f); c.a = .82f;
            var img = UiArt.Pic(content, FadeSprite, c, .5f, fy, 0, Fade); img.raycastTarget = false; img.name = "Sis " + (k + 1) + (half == 0 ? "a" : "b");
            var r = img.rectTransform; r.anchorMin = new Vector2(0, fy); r.anchorMax = new Vector2(1, fy); r.pivot = new Vector2(.5f, 1);
            r.sizeDelta = new Vector2(0, Fade); r.anchoredPosition = Vector2.zero;
            if (half == 1) r.localScale = new Vector3(1, -1, 1);
        }
        for (int i = 0; i < 9; i++)
        {
            float seed = k * 37 + i * 19, blend = Seeded(seed + 23);
            var c = Color.Lerp(upper, lower, blend); c.a = .22f + Seeded(seed + 7) * .1f;
            float size = 380 + Seeded(seed + 11) * 420;
            var img = UiArt.Pic(content, BlobSprite, c, Seeded(seed), fy, size, size * .6f); img.raycastTarget = false; img.name = "Bulut " + (k + 1) + "_" + i;
            img.rectTransform.anchoredPosition = new Vector2(0, (Seeded(seed + 5) - .5f) * Fade * .8f);
        }
    }

    void BuildNode(int i, TMP_FontAsset font)
    {
        int tile = i / PerBiome, slotIndex = i % PerBiome;
        var p = Road[slotIndex];
        float x = tile % 2 == 1 ? 1 - p.x : p.x, y = (tile + 1 - p.y) / Tiles;
        var holder = new GameObject("Bölüm " + (i + 1), typeof(RectTransform)); holder.transform.SetParent(content, false);
        var hr = holder.GetComponent<RectTransform>(); hr.anchorMin = hr.anchorMax = new Vector2(x, y); hr.pivot = new Vector2(.5f, .5f);
        hr.anchoredPosition = Vector2.zero; hr.sizeDelta = Vector2.zero;
        var n = new Node { holder = hr };
        n.halo = Put(holder.transform, HaloSprite, Color.white, 0, 0, 168);
        n.medal = Put(holder.transform, UiArt.Disc, new Color(.15f, .09f, .11f, .96f), 0, 0, 124);
        n.dim = Put(holder.transform, UiArt.Disc, new Color(0, 0, 0, .55f), 0, 0, 124);
        n.number = UiArt.Label(holder.transform, font, (i + 1).ToString(), .5f, .5f, 128, 128, 56, Cream);
        n.number.fontStyle = FontStyles.Bold; n.number.rectTransform.anchoredPosition = Vector2.zero; n.number.raycastTarget = false;
        n.lockBadge = Put(holder.transform, LockSprite, new Color(.78f, .72f, .81f), 48, 42, 38);
        n.nextBadge = UiArt.Label(holder.transform, font, "SIRADAKİ", .5f, .5f, 145, 34, 22, Color.white);
        n.nextBadge.rectTransform.anchoredPosition = new Vector2(0, -77);
        n.nextBadge.outlineColor = new Color32(21, 10, 25, 255);
        n.nextBadge.outlineWidth = .28f;
        n.stars = new Image[3];
        for (int s = 0; s < 3; s++) n.stars[s] = Put(holder.transform, UiArt.Star, Dim, (s - 1) * 46, -70 + (s == 1 ? -9 : 0), 48);
        foreach (var img in holder.GetComponentsInChildren<Image>()) img.raycastTarget = false;
        var hit = Put(holder.transform, UiArt.Disc, new Color(0, 0, 0, 0), 0, 0, 140);
        hit.raycastTarget = true;
        int level = i + 1;
        hit.gameObject.AddComponent<Button>().onClick.AddListener(() =>
        {
            if (LevelDefs.Unlocked(level)) { SesAtolyesi.Ses.UiClick(); ShowCard(level); }
        });
        nodes[i] = n;
    }

    static Image Put(Transform parent, Sprite sprite, Color color, float x, float y, float size)
    {
        var img = UiArt.Pic(parent, sprite, color, .5f, .5f, size, size);
        img.rectTransform.anchoredPosition = new Vector2(x, y);
        return img;
    }

    static float Seeded(float seed) { float v = Mathf.Sin(seed * 127.1f + 311.7f) * 43758.5453f; return v - Mathf.Floor(v); }

    static Sprite lockSprite;
    static Sprite LockSprite => lockSprite ? lockSprite : lockSprite = UiArt.Build(40, 40, 40, (x, y) =>
    {
        float body = Mathf.Max(Mathf.Abs(x) - .32f, Mathf.Abs(y + .16f) - .19f);
        float shackle = Mathf.Max(Mathf.Abs(Mathf.Sqrt(x * x + (y - .035f) * (y - .035f)) - .19f) - .052f, .035f - y);
        return Mathf.Clamp01(.5f - Mathf.Min(body, shackle) * 40);
    });
    static Sprite fadeSprite, haloSprite, blobSprite;
    // Alt kenar saydam, üst kenar opak
    static Sprite FadeSprite => fadeSprite ? fadeSprite : fadeSprite = UiArt.Build(4, 64, 64, (u, v) => Mathf.SmoothStep(0, 1, Mathf.Clamp01(v + .5f)));
    static Sprite BlobSprite => blobSprite ? blobSprite : blobSprite = UiArt.Build(64, 64, 64, (u, v) =>
    {
        float r = Mathf.Clamp01(1 - Mathf.Sqrt(u * u + v * v) * 2);
        return r * r * (3 - 2 * r);
    });
    static Sprite HaloSprite => haloSprite ? haloSprite : haloSprite = UiArt.Build(128, 128, 128, (u, v) =>
    {
        float r = Mathf.Sqrt(u * u + v * v) * 2;
        return Mathf.Clamp01(1 - Mathf.Abs(r - .88f) * 7);
    });

    static void Stretch(RectTransform r) { r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero; }

    // Ekran boyutu değişince (ve açılışta) sayfa yüksekliğini ve madalyon ölçeğini yeniden hesaplar.
    void Relayout(Vector2 size)
    {
        viewH = size.y;
        tileH = Mathf.Max(size.x * .5625f, size.y * 1.02f);
        content.sizeDelta = new Vector2(0, tileH * Tiles);
        float s = Mathf.Clamp(tileH / 900f, .8f, 1.5f);
        foreach (var n in nodes) n.holder.localScale = Vector3.one * s;
        if (pendingScroll > 0) { ScrollTo(pendingScroll); pendingScroll = 0; }
        UpdateBiome();
    }

    // Bölüm ekranın biraz altında kalacak şekilde kaydırır (üstte yeni yol görünsün).
    void ScrollTo(int level)
    {
        int i = Mathf.Clamp(level - 1, 0, Total - 1), tile = i / PerBiome;
        var p = Road[i % PerBiome];
        float nodeFromBottom = (tile + 1 - p.y) * tileH, contentH = tileH * Tiles, maxUp = Mathf.Max(0, contentH - viewH);
        float viewBottom = nodeFromBottom - viewH * .42f;
        content.anchoredPosition = new Vector2(0, Mathf.Clamp(maxUp - viewBottom, 0, maxUp));
    }

    void UpdateBiome()
    {
        float contentH = tileH * Tiles, centerFromBottom = Mathf.Max(0, contentH - viewH) - content.anchoredPosition.y + viewH * .5f;
        int tile = Mathf.Clamp(Mathf.FloorToInt(centerFromBottom / tileH), 0, Tiles - 1);
        if (biomeLabel) biomeLabel.text = BiomeNames[BiomeOf(tile)];
    }

    public void Open()
    {
        root.SetActive(true); Refresh();
        pendingScroll = Mathf.Clamp(LevelDefs.Current(), 1, Total);
    }
    public void OpenCurrent()
    {
        Open();
        ShowCard(pendingScroll);
    }

    public void Close() { card.SetActive(false); root.SetActive(false); }

    void Refresh()
    {
        totalText.text = "YILDIZ  " + LevelDefs.TotalStars() + " / " + Total * 3;
        int current = LevelDefs.Current();
        for (int i = 0; i < Total; i++)
        {
            int level = i + 1, s = LevelDefs.Stars(level), tier = (level - 1) / LevelDefs.PerTier;
            bool unlocked = LevelDefs.Unlocked(level);
            var n = nodes[i];
            n.number.color = unlocked ? Cream : new Color(.5f, .46f, .52f);
            n.dim.gameObject.SetActive(!unlocked);
            n.lockBadge.gameObject.SetActive(!unlocked);
            n.nextBadge.gameObject.SetActive(unlocked && level == current && s == 0);
            n.halo.gameObject.SetActive(unlocked);
            n.halo.color = s > 0 ? Gold : level == current ? Color.white : LevelDefs.TierColors[tier];
            n.medal.color = s > 0 ? new Color(.3f, .19f, .08f, .97f) : new Color(.15f, .09f, .11f, .96f);
            n.holder.GetChild(0).localScale = Vector3.one * (level == current ? 1.12f : 1f);
            for (int k = 0; k < 3; k++) { n.stars[k].gameObject.SetActive(s > 0); n.stars[k].color = k < s ? Gold : Dim; }
        }
    }

    // Bölüm özet kartı: zorluk, hedef, süre, düşman, can ve yıldız şartları.
    GameObject BuildCard(Transform parent, TMP_FontAsset font)
    {
        var dimmer = new GameObject("Bölüm Kartı", typeof(Image)); dimmer.transform.SetParent(parent, false);
        Stretch(dimmer.GetComponent<RectTransform>());
        dimmer.GetComponent<Image>().color = new Color(0, 0, 0, .72f);
        var panel = new GameObject("Panel", typeof(Image)); panel.transform.SetParent(dimmer.transform, false);
        var pr = panel.GetComponent<RectTransform>(); pr.anchorMin = new Vector2(.06f, .2f); pr.anchorMax = new Vector2(.94f, .82f); pr.offsetMin = pr.offsetMax = Vector2.zero;
        panel.GetComponent<Image>().color = new Color(.11f, .07f, .12f, .98f);
        float[] ys = { .93f, .85f, .76f, .68f, .6f, .52f, .42f, .34f, .26f };
        int[] sizes = { 44, 30, 26, 24, 24, 24, 22, 22, 22 };
        for (int i = 0; i < cardLines.Length; i++)
        {
            var line = cardLines[i] = UiArt.Label(panel.transform, font, "", .5f, ys[i], 800, 56, sizes[i], new Color(.9f, .82f, .76f));
            var rect = line.rectTransform;
            rect.anchorMin = new Vector2(0, ys[i]);
            rect.anchorMax = new Vector2(1, ys[i]);
            rect.sizeDelta = new Vector2(-32, 56);
            line.enableAutoSizing = true;
            line.fontSizeMin = 15;
            line.fontSizeMax = sizes[i];
        }
        cardLines[0].fontStyle = FontStyles.Bold; cardLines[1].fontStyle = FontStyles.Bold;
        UiArt.Btn(panel.transform, font, "BAŞLA", .3f, .09f, 250, 70, () => { dimmer.SetActive(false); start(cardLevel); });
        UiArt.Btn(panel.transform, font, "GERİ", .7f, .09f, 250, 70, () => dimmer.SetActive(false));
        dimmer.SetActive(false);
        return dimmer;
    }

    void ShowCard(int n)
    {
        cardLevel = n;
        var d = LevelDefs.Get(n);
        cardLines[0].text = "BÖLÜM " + n; cardLines[0].color = Cream;
        cardLines[1].text = "ZORLUK: " + d.TierName; cardLines[1].color = d.TierColor;
        cardLines[2].text = "HEDEF: " + d.Goal;
        cardLines[3].text = d.kind == GoalKind.Survive ? "SÜRE: " + LevelDefs.Clock(d.survive) + " dolunca kapı açılır"
            : d.HardLimit ? "SÜRE SINIRI: " + LevelDefs.Clock(d.limit) + " (aşarsan kaybedersin)" : "SÜRE SINIRI: yok";
        cardLines[4].text = d.HasEnemies ? "DÜŞMAN: " + d.EnemyText + (d.enemySpeed > 1.01f ? "  (hız x" + d.enemySpeed.ToString("0.0") + ")" : "") : "DÜŞMAN: yok";
        int record = LevelDefs.BestTime(n);
        cardLines[5].text = "CAN: " + d.hearts;
        if (record > 0) cardLines[5].text += "  •  EN İYİ SÜRE " + LevelDefs.RecordClock(record);
        var rules = d.StarRules;
        for (int i = 0; i < 3; i++) { cardLines[6 + i].text = (i + 1) + " YILDIZ  •  " + rules[i]; cardLines[6 + i].color = LevelDefs.Stars(n) > i ? Gold : new Color(.9f, .82f, .76f); }
        card.SetActive(true);
    }
}

// Görünüm boyutu değişince haber verir (ilk açılışta da bir kez).
public class MapLayout : MonoBehaviour
{
    public Action<Vector2> changed;
    Vector2 last;
    void OnEnable() { last = Vector2.zero; }
    void LateUpdate()
    {
        var size = ((RectTransform)transform).rect.size;
        if (size.x <= 0 || size.y <= 0 || size == last) return;
        last = size; changed?.Invoke(size);
    }
}
