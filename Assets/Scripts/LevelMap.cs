using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Kıvrımlı, kaydırılan bölüm haritası. Bölgeler mevcut oyun temalarıyla aynı eşikte değişir.
public class LevelMap
{
    const float Step = 150, PadBottom = 170, PadTop = 330, NodeSize = 100;
    static readonly Color Done = new Color(1f, .77f, .3f), Available = new Color(1f, .5f, .42f), Locked = new Color(.27f, .24f, .3f), Gold = new Color(1f, .77f, .3f), Dim = new Color(.25f, .2f, .26f);

    sealed class Region
    {
        public int theme, first, last;
        public float tintFloor;
    }

    readonly GameObject root;
    readonly ScrollRect scroll;
    readonly RectTransform content;
    readonly TextMeshProUGUI total;
    readonly Image[] node = new Image[LevelDefs.Count + 1];
    readonly Image[] halo = new Image[LevelDefs.Count + 1];
    readonly Button[] nodeButton = new Button[LevelDefs.Count + 1];
    readonly TextMeshProUGUI[] number = new TextMeshProUGUI[LevelDefs.Count + 1];
    readonly Image[][] stars = new Image[LevelDefs.Count + 1][];
    readonly List<Region> regions = new List<Region>();
    readonly List<Sprite> generatedSprites = new List<Sprite>();
    readonly float height = PadBottom + PadTop + LevelDefs.Count * Step;
    readonly TMP_FontAsset font;
    readonly Sprite skySprite;
    readonly Texture2D themeAtlas;
    public bool IsOpen => root.activeSelf;

    public LevelMap(Transform parent, TMP_FontAsset font, Sprite skySprite, Texture2D themeAtlas, Action<int> pick, Action endless, Action back)
    {
        this.font = font;
        this.skySprite = skySprite;
        this.themeAtlas = themeAtlas;
        BuildRegions();

        root = new GameObject("Bölüm Haritası", typeof(Image)); root.transform.SetParent(parent, false);
        Stretch(root.GetComponent<RectTransform>());
        root.GetComponent<Image>().color = new Color(.035f, .045f, .075f, 1);

        var viewport = new GameObject("Görünüm", typeof(RectMask2D)); viewport.transform.SetParent(root.transform, false);
        Stretch(viewport.GetComponent<RectTransform>());
        content = new GameObject("İçerik", typeof(RectTransform)).GetComponent<RectTransform>(); content.SetParent(viewport.transform, false);
        content.anchorMin = new Vector2(0, 1); content.anchorMax = new Vector2(1, 1); content.pivot = new Vector2(.5f, 1); content.sizeDelta = new Vector2(0, height);
        scroll = root.AddComponent<ScrollRect>();
        scroll.viewport = viewport.GetComponent<RectTransform>(); scroll.content = content;
        scroll.horizontal = false; scroll.vertical = true; scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 60;
        Canvas.ForceUpdateCanvases();

        CreateRegionArt();
        for (int n = 1; n < LevelDefs.Count; n++)
            for (int k = 1; k <= 4; k++)
            {
                float t = k / 5f;
                var dot = UiArt.Pic(content, UiArt.Disc, new Color(1, .8f, .55f, .35f), Mathf.Lerp(X(n), X(n + 1), t), 0, 16, 16);
                dot.rectTransform.anchoredPosition = new Vector2(0, Mathf.Lerp(Y(n), Y(n + 1), t));
            }

        for (int n = 1; n <= LevelDefs.Count; n++)
        {
            int level = n;
            halo[n] = UiArt.Pic(content, UiArt.Disc, new Color(1f, .78f, .42f, .28f), X(n), 0, NodeSize * 1.75f, NodeSize * 1.75f);
            halo[n].rectTransform.anchoredPosition = new Vector2(0, Y(n));
            halo[n].gameObject.SetActive(false);
            node[n] = UiArt.Pic(content, UiArt.Disc, Locked, X(n), 0, NodeSize, NodeSize);
            node[n].rectTransform.anchoredPosition = new Vector2(0, Y(n));
            node[n].raycastTarget = true;
            nodeButton[n] = node[n].gameObject.AddComponent<Button>();
            nodeButton[n].transition = Selectable.Transition.None;
            nodeButton[n].onClick.AddListener(() => { if (LevelDefs.Unlocked(level)) pick(level); });
            number[n] = UiArt.Label(node[n].transform, font, n.ToString(), .5f, .5f, NodeSize, NodeSize, 40, new Color(.12f, .06f, .08f));
            number[n].fontStyle = FontStyles.Bold;
            stars[n] = new Image[3];
            for (int s = 0; s < 3; s++)
            {
                stars[n][s] = UiArt.Pic(content, UiArt.Star, Dim, X(n), 0, 34, 34);
                stars[n][s].rectTransform.anchoredPosition = new Vector2((s - 1) * 36, Y(n) - NodeSize / 2 - 22 + (s == 1 ? -4 : 0));
            }
        }

        var bar = new GameObject("Üst Şerit", typeof(Image)); bar.transform.SetParent(root.transform, false);
        bar.GetComponent<Image>().color = new Color(.055f, .07f, .11f, .97f);
        var br = bar.GetComponent<RectTransform>(); br.anchorMin = new Vector2(0, 1); br.anchorMax = Vector2.one; br.pivot = new Vector2(.5f, 1); br.sizeDelta = new Vector2(0, 130);
        UiArt.Label(bar.transform, font, "YOL HARİTASI", .5f, .68f, 500, 58, 36, new Color(1, .82f, .56f));
        total = UiArt.Label(bar.transform, font, "", .5f, .27f, 600, 38, 19, new Color(.75f, .8f, .87f));
        UiArt.Btn(bar.transform, font, "GERİ", .13f, .5f, 190, 66, () => back());
        UiArt.Btn(bar.transform, font, "SONSUZ MOD", .85f, .5f, 250, 66, () => endless());
        root.SetActive(false);
    }

    void BuildRegions()
    {
        int first = 1, theme = ThemeForLevel(1);
        for (int level = 2; level <= LevelDefs.Count; level++)
        {
            int nextTheme = ThemeForLevel(level);
            if (nextTheme == theme) continue;
            regions.Add(new Region { first = first, last = level - 1, theme = theme, tintFloor = LevelDefs.Get(first).target - 1 });
            first = level; theme = nextTheme;
        }
        regions.Add(new Region { first = first, last = LevelDefs.Count, theme = theme, tintFloor = LevelDefs.Get(first).target - 1 });
    }

    void CreateRegionArt()
    {
        foreach (var region in regions)
        {
            Color tint = TowerThemes.TintAtFloor(Mathf.RoundToInt(region.tintFloor));
            float bottom = Y(region.first) - Step * .48f;
            float bandHeight = Y(region.last) - Y(region.first) + Step * .96f;
            var band = new GameObject(TowerThemes.Names[region.theme] + " Bölgesi", typeof(Image)); band.transform.SetParent(content, false);
            var bandRect = band.GetComponent<RectTransform>(); bandRect.anchorMin = new Vector2(0, 0); bandRect.anchorMax = new Vector2(1, 0); bandRect.pivot = new Vector2(.5f, 0);
            bandRect.sizeDelta = new Vector2(0, bandHeight); bandRect.anchoredPosition = new Vector2(0, bottom);
            band.GetComponent<Image>().color = new Color(tint.r * .24f + .025f, tint.g * .22f + .035f, tint.b * .28f + .065f, .44f);
            band.GetComponent<Image>().raycastTarget = false;

            var edge = new GameObject("Bölge Sınırı", typeof(Image)); edge.transform.SetParent(content, false);
            var edgeRect = edge.GetComponent<RectTransform>(); edgeRect.anchorMin = new Vector2(0, 0); edgeRect.anchorMax = new Vector2(1, 0); edgeRect.pivot = new Vector2(.5f, .5f);
            edgeRect.sizeDelta = new Vector2(0, 3); edgeRect.anchoredPosition = new Vector2(0, bottom);
            edge.GetComponent<Image>().color = new Color(tint.r, tint.g, tint.b, .62f); edge.GetComponent<Image>().raycastTarget = false;

            CreateRegionBadge(region, tint);
        }
    }

    void CreateRegionBadge(Region region, Color tint)
    {
        float avgX = 0;
        for (int n = region.first; n <= region.last; n++) avgX += X(n);
        avgX /= Mathf.Max(1, region.last - region.first + 1);
        bool placeLeft = avgX >= .5f;
        float x = placeLeft ? .13f : .87f;
        float width = Mathf.Clamp(content.rect.width * .32f, 150, 232);
        float midY = (Y(region.first) + Y(region.last)) * .5f;
        var badge = new GameObject("Bölge: " + TowerThemes.Names[region.theme], typeof(Image)); badge.transform.SetParent(content, false);
        var image = badge.GetComponent<Image>(); image.color = new Color(.035f, .045f, .07f, .9f); image.raycastTarget = false;
        var rect = badge.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = new Vector2(x, 0); rect.sizeDelta = new Vector2(width, 108); rect.anchoredPosition = new Vector2(0, midY);

        var art = RegionSprite(region.theme);
        var landscape = UiArt.Pic(badge.transform, art, new Color(1, 1, 1, .22f), .5f, .5f, width, 108);
        landscape.preserveAspect = false;
        var inset = new GameObject("Bölge Rengi", typeof(Image)); inset.transform.SetParent(badge.transform, false);
        inset.GetComponent<Image>().color = new Color(.025f, .035f, .055f, .55f); inset.GetComponent<Image>().raycastTarget = false;
        var insetRect = inset.GetComponent<RectTransform>(); insetRect.anchorMin = Vector2.zero; insetRect.anchorMax = Vector2.one; insetRect.offsetMin = insetRect.offsetMax = Vector2.zero;
        inset.transform.SetAsLastSibling();

        var border = new GameObject("Vurgu", typeof(Image)); border.transform.SetParent(badge.transform, false);
        border.GetComponent<Image>().color = tint; border.GetComponent<Image>().raycastTarget = false;
        var borderRect = border.GetComponent<RectTransform>(); borderRect.anchorMin = new Vector2(placeLeft ? 1 : 0, 0); borderRect.anchorMax = new Vector2(placeLeft ? 1 : 0, 1); borderRect.pivot = new Vector2(.5f, .5f); borderRect.sizeDelta = new Vector2(5, 0); borderRect.anchoredPosition = Vector2.zero;
        border.transform.SetAsLastSibling();

        var title = UiArt.Label(badge.transform, font, TowerThemes.Names[region.theme], .5f, .7f, width - 28, 40, 21, new Color(1, .87f, .68f));
        title.alignment = placeLeft ? TextAlignmentOptions.Right : TextAlignmentOptions.Left;
        var progress = UiArt.Label(badge.transform, font, "BÖLÜM " + region.first + "–" + region.last, .5f, .34f, width - 28, 28, 16, new Color(.83f, .84f, .87f));
        progress.alignment = placeLeft ? TextAlignmentOptions.Right : TextAlignmentOptions.Left;
        var enemy = EnemyArt.Get((EnemyKind)region.theme);
        var creature = UiArt.Pic(badge.transform, enemy.frames[0], new Color(1, 1, 1, .96f), placeLeft ? .1f : .9f, .5f, 62, 90);
        creature.preserveAspect = true;
        creature.transform.SetAsLastSibling();
    }

    Sprite RegionSprite(int theme)
    {
        if (theme == 0 || !themeAtlas) return skySprite;
        int panel = theme - 1;
        int width = themeAtlas.width / 2, height = themeAtlas.height / 2;
        var sprite = Sprite.Create(themeAtlas, new Rect((panel % 2) * width, (1 - panel / 2) * height, width, height), Vector2.one * .5f, 100);
        generatedSprites.Add(sprite);
        return sprite;
    }

    static int ThemeForLevel(int level) => TowerThemes.ThemeAtFloor(LevelDefs.Get(level).target - 1);
    static void Stretch(RectTransform r) { r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero; }
    static float X(int n) => .5f + .27f * Mathf.Sin(n * .9f);
    static float Y(int n) => PadBottom + (n - 1) * Step;

    public void Open() { Refresh(); root.SetActive(true); Canvas.ForceUpdateCanvases(); ScrollTo(LevelDefs.Current()); }
    public void Close() { root.SetActive(false); }

    void ScrollTo(int n)
    {
        float view = scroll.viewport.rect.height, range = height - view;
        scroll.verticalNormalizedPosition = range <= 0 ? 0 : Mathf.Clamp01((Y(n) - view / 2) / range);
    }

    void Refresh()
    {
        int current = LevelDefs.Current();
        total.text = "BÖLÜM " + current + " / " + LevelDefs.Count + "     •     YILDIZ " + LevelDefs.TotalStars() + " / " + LevelDefs.Count * 3;
        for (int n = 1; n <= LevelDefs.Count; n++)
        {
            int score = LevelDefs.Stars(n);
            bool unlocked = LevelDefs.Unlocked(n);
            Color biome = TowerThemes.TintAtFloor(LevelDefs.Get(n).target - 1);
            node[n].color = score > 0 ? Done : unlocked ? Color.Lerp(Available, biome, .38f) : Locked;
            nodeButton[n].interactable = unlocked;
            number[n].color = unlocked ? new Color(.12f, .06f, .08f) : new Color(.55f, .5f, .58f);
            halo[n].gameObject.SetActive(n == current);
            halo[n].color = new Color(biome.r, biome.g, biome.b, .32f);
            for (int i = 0; i < 3; i++)
            {
                stars[n][i].gameObject.SetActive(true);
                stars[n][i].color = i < score ? Gold : unlocked ? Dim : new Color(Dim.r, Dim.g, Dim.b, .48f);
            }
        }
    }

    public void Dispose()
    {
        foreach (var sprite in generatedSprites) if (sprite) UnityEngine.Object.Destroy(sprite);
        generatedSprites.Clear();
    }
}
