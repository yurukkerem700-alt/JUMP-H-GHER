using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

// Sayfa sayfa bölüm haritası: her sayfa referans resimdeki gibi 5 düğümlü boyalı bir sahnedir.
// Sayfa p, 4p+1 ... 4p+5. bölümleri gösterir (üst düğüm, sonraki sayfanın alt düğümüdür). Yukarı kaydır / SONRAKİ: daha zor bölümler.
public partial class LevelMap
{
    const int PerPage = 4, Slots = 5, Pages = LevelDefs.Count / PerPage; // 25 sayfa
    const float ArtW = 1672, ArtH = 941;
    static readonly Vector2[] Spots = { new Vector2(1205, 938), new Vector2(1313, 745), new Vector2(1032, 552), new Vector2(667, 382), new Vector2(435, 230) };
    static readonly Color Gold = new Color(1f, .77f, .3f), Dim = new Color(.25f, .2f, .26f), Cream = new Color(1, .88f, .65f);

    readonly GameObject root;
    readonly RectTransform art;
    readonly Image artImage;
    readonly TextMeshProUGUI total, pageLabel;
    readonly Image[] dim = new Image[Slots], halo = new Image[Slots];
    readonly TextMeshProUGUI[] number = new TextMeshProUGUI[Slots];
    readonly Image[][] stars = new Image[Slots][];
    readonly GameObject[] slot = new GameObject[Slots];
    readonly int[] levelAt = new int[Slots];
    readonly GameObject card;
    readonly TextMeshProUGUI[] cardLines = new TextMeshProUGUI[9];
    readonly Action<int> start;
    int cardLevel, page;
    public bool IsOpen => root.activeSelf;

    public LevelMap(Transform parent, TMP_FontAsset font, Action<int> pick, Action endless, Action back)
    {
        start = pick;
        root = new GameObject("Bölüm Haritası", typeof(Image)); root.transform.SetParent(parent, false);
        Stretch(root.GetComponent<RectTransform>());
        root.GetComponent<Image>().color = new Color(.03f, .02f, .05f, 1);
        root.AddComponent<MapSwipe>().onPage = delta => ShowPage(page + delta);

        // Boyalı sahne: 1672x941 tuval birimi, MapFit ekrana göre ölçekler; düğümler bu tuvalin çocuğudur.
        var artGo = new GameObject("Sahne", typeof(Image), typeof(MapFit)); artGo.transform.SetParent(root.transform, false);
        art = artGo.GetComponent<RectTransform>(); art.anchorMin = art.anchorMax = art.pivot = new Vector2(.5f, .5f); art.sizeDelta = new Vector2(ArtW, ArtH);
        artImage = artGo.GetComponent<Image>(); artImage.raycastTarget = false;
        var bg = Painted("MapBackground");
        if (bg) artImage.sprite = bg;
        Fade(art, true); Fade(art, false);

        for (int i = 0; i < Slots; i++) BuildSlot(i, font);

        var bar = new GameObject("Üst Şerit", typeof(RectTransform)); bar.transform.SetParent(root.transform, false);
        var br = bar.GetComponent<RectTransform>(); br.anchorMin = new Vector2(0, 1); br.anchorMax = Vector2.one; br.pivot = new Vector2(.5f, 1); br.sizeDelta = new Vector2(0, 170);
        var plate = new GameObject("Başlık Levhası", typeof(Image)); plate.transform.SetParent(bar.transform, false);
        var plr = plate.GetComponent<RectTransform>(); plr.anchorMin = plr.anchorMax = new Vector2(.5f, .66f); plr.sizeDelta = new Vector2(500, 82);
        Plate(plate.GetComponent<Image>(), "MapPlateTitle"); plate.GetComponent<Image>().raycastTarget = false;
        UiArt.Label(bar.transform, font, "BÖLÜMLER", .5f, .66f, 460, 60, 38, Cream);
        total = UiArt.Label(bar.transform, font, "", .5f, .36f, 500, 40, 22, Cream);
        UiArt.Pic(bar.transform, UiArt.Star, Gold, .5f, .36f, 34, 34).rectTransform.anchoredPosition = new Vector2(-125, 0);
        pageLabel = UiArt.Label(bar.transform, font, "", .5f, .12f, 700, 36, 22, Cream);
        Plate(UiArt.Btn(bar.transform, font, "GERİ", .13f, .66f, 190, 66, () => back()).GetComponent<Image>());
        Plate(UiArt.Btn(bar.transform, font, "SONSUZ MOD", .85f, .66f, 250, 66, () => endless()).GetComponent<Image>());

        var nav = new GameObject("Sayfa Düğmeleri", typeof(RectTransform)); nav.transform.SetParent(root.transform, false);
        Stretch(nav.GetComponent<RectTransform>());
        Plate(UiArt.Btn(nav.transform, font, "ÖNCEKİ", .15f, .07f, 220, 66, () => ShowPage(page - 1)).GetComponent<Image>());
        Plate(UiArt.Btn(nav.transform, font, "SONRAKİ", .15f, .17f, 220, 66, () => ShowPage(page + 1)).GetComponent<Image>());

        card = BuildCard(root.transform, font);
        root.SetActive(false);
    }

    void BuildSlot(int i, TMP_FontAsset font)
    {
        var holder = new GameObject("Düğüm " + i, typeof(RectTransform)); holder.transform.SetParent(art, false);
        var hr = holder.GetComponent<RectTransform>(); hr.anchorMin = hr.anchorMax = hr.pivot = new Vector2(.5f, .5f);
        hr.anchoredPosition = new Vector2(Spots[i].x - ArtW / 2, ArtH / 2 - Spots[i].y); hr.sizeDelta = Vector2.zero;
        slot[i] = holder;
        halo[i] = Put(holder.transform, HaloSprite, Color.white, 0, 0, 168);
        dim[i] = Put(holder.transform, UiArt.Disc, new Color(0, 0, 0, .55f), 0, 0, 128);
        number[i] = UiArt.Label(holder.transform, font, "", .5f, .5f, 128, 128, 56, Cream);
        number[i].fontStyle = FontStyles.Bold; number[i].rectTransform.anchoredPosition = Vector2.zero;
        stars[i] = new Image[3];
        for (int s = 0; s < 3; s++) stars[i][s] = Put(holder.transform, UiArt.Star, Dim, (s - 1) * 46, -66 + (s == 1 ? -9 : 0), 48);
        var hit = Put(holder.transform, UiArt.Disc, new Color(0, 0, 0, 0), 0, 0, 132);
        hit.raycastTarget = true;
        int index = i;
        hit.gameObject.AddComponent<Button>().onClick.AddListener(() =>
        {
            int level = levelAt[index];
            if (level >= 1 && level <= LevelDefs.Count && LevelDefs.Unlocked(level)) { SesAtolyesi.Ses.UiClick(); ShowCard(level); }
        });
    }

    static Image Put(Transform parent, Sprite sprite, Color color, float x, float y, float size)
    {
        var img = UiArt.Pic(parent, sprite, color, .5f, .5f, size, size);
        img.rectTransform.anchoredPosition = new Vector2(x, y);
        return img;
    }

    // Sahnenin üst ve alt kenarını koyu zemine eritir (ekran sahneden uzunsa görünen boşluk için)
    static void Fade(RectTransform parent, bool top)
    {
        var img = UiArt.Pic(parent, FadeSprite, new Color(.03f, .02f, .05f, 1), .5f, top ? 1 : 0, ArtW + 4, 150);
        img.rectTransform.pivot = new Vector2(.5f, top ? 1 : 0);
        img.rectTransform.anchoredPosition = Vector2.zero;
        if (!top) img.rectTransform.localScale = new Vector3(1, -1, 1);
    }

    static Sprite fadeSprite, haloSprite;
    static Sprite FadeSprite => fadeSprite ? fadeSprite : fadeSprite = UiArt.Build(4, 64, 64, (u, v) => Mathf.SmoothStep(0, 1, Mathf.Clamp01(v + .5f)));
    static Sprite HaloSprite => haloSprite ? haloSprite : haloSprite = UiArt.Build(128, 128, 128, (u, v) =>
    {
        float r = Mathf.Sqrt(u * u + v * v) * 2;
        return Mathf.Clamp01(1 - Mathf.Abs(r - .88f) * 7);
    });

    static void Stretch(RectTransform r) { r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero; }

    public void Open()
    {
        page = Mathf.Clamp((LevelDefs.Current() - 2) / PerPage, 0, Pages - 1); // güncel bölüm alttaki yarım düğüm olmasın
        root.SetActive(true); ShowPage(page);
    }
    public void Close() { card.SetActive(false); root.SetActive(false); }

    void ShowPage(int p)
    {
        page = Mathf.Clamp(p, 0, Pages - 1);
        int first = page * PerPage + 1;
        int tier = (Mathf.Min(first + 2, LevelDefs.Count) - 1) / LevelDefs.PerTier;
        total.text = "YILDIZ  " + LevelDefs.TotalStars() + " / " + LevelDefs.Count * 3;
        pageLabel.text = LevelDefs.TierNames[tier] + "   •   BÖLÜM " + first + " - " + Mathf.Min(first + Slots - 1, LevelDefs.Count) + "   •   SAYFA " + (page + 1) + " / " + Pages;
        pageLabel.color = LevelDefs.TierColors[tier];
        artImage.color = Color.Lerp(Color.white, LevelDefs.TierColors[tier], .16f);
        int current = LevelDefs.Current();
        for (int i = 0; i < Slots; i++)
        {
            int level = first + i;
            levelAt[i] = level;
            bool exists = level <= LevelDefs.Count;
            slot[i].SetActive(exists);
            if (!exists) continue;
            int s = LevelDefs.Stars(level);
            bool unlocked = LevelDefs.Unlocked(level);
            int levelTier = (level - 1) / LevelDefs.PerTier;
            number[i].text = level.ToString();
            number[i].color = unlocked ? Cream : new Color(.5f, .46f, .52f);
            dim[i].gameObject.SetActive(!unlocked);
            halo[i].gameObject.SetActive(unlocked);
            halo[i].color = s > 0 ? Gold : level == current ? Color.white : LevelDefs.TierColors[levelTier];
            slot[i].transform.localScale = Vector3.one * (level == current ? 1.12f : 1f);
            for (int k = 0; k < 3; k++) { stars[i][k].gameObject.SetActive(s > 0); stars[i][k].color = k < s ? Gold : Dim; }
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
        for (int i = 0; i < cardLines.Length; i++) cardLines[i] = UiArt.Label(panel.transform, font, "", .5f, ys[i], 800, 56, sizes[i], new Color(.9f, .82f, .76f));
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
        cardLines[3].text = d.kind == GoalKind.Survive ? "SÜRE: " + LevelDefs.Clock(d.survive) + " dolunca bölüm biter"
            : d.HardLimit ? "SÜRE SINIRI: " + LevelDefs.Clock(d.limit) + " (aşarsan kaybedersin)" : "SÜRE SINIRI: yok";
        cardLines[4].text = d.HasEnemies ? "DÜŞMAN: " + d.EnemyText + (d.enemySpeed > 1.01f ? "  (hız x" + d.enemySpeed.ToString("0.0") + ")" : "") : "DÜŞMAN: yok";
        cardLines[5].text = "CAN: " + d.hearts;
        var rules = d.StarRules;
        for (int i = 0; i < 3; i++) { cardLines[6 + i].text = (i + 1) + " YILDIZ  •  " + rules[i]; cardLines[6 + i].color = LevelDefs.Stars(n) > i ? Gold : new Color(.9f, .82f, .76f); }
        card.SetActive(true);
    }
}

// Sahneyi ekrana oturtur: genişlik her zaman sığar; ekran çok darsa düğümler kesilmesin diye en az 1000 birim görünür.
public class MapFit : MonoBehaviour
{
    void LateUpdate()
    {
        var parent = (RectTransform)transform.parent;
        float w = parent.rect.width, h = parent.rect.height;
        if (w <= 0 || h <= 0) return;
        float cover = Mathf.Max(w / 1672f, h / 941f);
        float s = Mathf.Max(Mathf.Min(cover, w / 1000f), w / 1672f);
        transform.localScale = Vector3.one * s;
    }
}

// Yukarı / aşağı sürükleme ve fare tekerleği ile sayfa değiştirir.
public class MapSwipe : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IScrollHandler
{
    public Action<int> onPage;
    Vector2 from; bool dragging;
    public void OnBeginDrag(PointerEventData e) { from = e.position; dragging = true; }
    public void OnDrag(PointerEventData e) { }
    public void OnEndDrag(PointerEventData e)
    {
        if (!dragging) return; dragging = false;
        float dy = e.position.y - from.y, dx = e.position.x - from.x;
        if (Mathf.Abs(dy) > 70 && Mathf.Abs(dy) > Mathf.Abs(dx)) onPage?.Invoke(dy > 0 ? 1 : -1);
    }
    public void OnScroll(PointerEventData e) { if (Mathf.Abs(e.scrollDelta.y) > .1f) onPage?.Invoke(e.scrollDelta.y > 0 ? 1 : -1); }
}
