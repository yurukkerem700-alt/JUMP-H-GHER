using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Kıvrımlı, kaydırılan bölüm haritası. 1. bölüm en altta, 100. bölüm en üsttedir.
public class LevelMap
{
    const float Step = 150, PadBottom = 170, PadTop = 330, NodeSize = 100;
    static readonly Color Done = new Color(1f, .77f, .3f), Available = new Color(1f, .5f, .42f), Locked = new Color(.27f, .24f, .3f), Gold = new Color(1f, .77f, .3f), Dim = new Color(.25f, .2f, .26f);
    readonly GameObject root;
    readonly ScrollRect scroll;
    readonly RectTransform content;
    readonly TextMeshProUGUI total;
    readonly Image[] node = new Image[LevelDefs.Count + 1];
    readonly TextMeshProUGUI[] number = new TextMeshProUGUI[LevelDefs.Count + 1];
    readonly Image[][] stars = new Image[LevelDefs.Count + 1][];
    readonly float height = PadBottom + PadTop + LevelDefs.Count * Step;
    public bool IsOpen => root.activeSelf;

    public LevelMap(Transform parent, TMP_FontAsset font, Action<int> pick, Action endless, Action back)
    {
        root = new GameObject("Bölüm Haritası", typeof(Image)); root.transform.SetParent(parent, false);
        Stretch(root.GetComponent<RectTransform>());
        root.GetComponent<Image>().color = new Color(.07f, .05f, .09f, 1);

        var viewport = new GameObject("Görünüm", typeof(RectMask2D)); viewport.transform.SetParent(root.transform, false);
        Stretch(viewport.GetComponent<RectTransform>());
        content = new GameObject("İçerik", typeof(RectTransform)).GetComponent<RectTransform>(); content.SetParent(viewport.transform, false);
        content.anchorMin = new Vector2(0, 1); content.anchorMax = new Vector2(1, 1); content.pivot = new Vector2(.5f, 1); content.sizeDelta = new Vector2(0, height);
        scroll = root.AddComponent<ScrollRect>();
        scroll.viewport = viewport.GetComponent<RectTransform>(); scroll.content = content;
        scroll.horizontal = false; scroll.vertical = true; scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 60;

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
            node[n] = UiArt.Pic(content, UiArt.Disc, Locked, X(n), 0, NodeSize, NodeSize);
            node[n].rectTransform.anchoredPosition = new Vector2(0, Y(n));
            node[n].raycastTarget = true;
            node[n].gameObject.AddComponent<Button>().onClick.AddListener(() => { if (LevelDefs.Unlocked(level)) pick(level); });
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
        bar.GetComponent<Image>().color = new Color(.1f, .06f, .09f, .96f);
        var br = bar.GetComponent<RectTransform>(); br.anchorMin = new Vector2(0, 1); br.anchorMax = Vector2.one; br.pivot = new Vector2(.5f, 1); br.sizeDelta = new Vector2(0, 130);
        UiArt.Label(bar.transform, font, "BÖLÜMLER", .5f, .66f, 500, 60, 36, new Color(1, .77f, .5f));
        total = UiArt.Label(bar.transform, font, "", .5f, .27f, 500, 40, 20, new Color(.75f, .66f, .62f));
        UiArt.Btn(bar.transform, font, "GERİ", .13f, .5f, 190, 66, () => back());
        UiArt.Btn(bar.transform, font, "SONSUZ MOD", .85f, .5f, 250, 66, () => endless());
        root.SetActive(false);
    }

    static void Stretch(RectTransform r) { r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero; }
    static float X(int n) => .5f + .27f * Mathf.Sin(n * .9f);
    static float Y(int n) => PadBottom + (n - 1) * Step; // içeriğin altından

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
        total.text = "YILDIZ  " + LevelDefs.TotalStars() + " / " + LevelDefs.Count * 3;
        for (int n = 1; n <= LevelDefs.Count; n++)
        {
            int s = LevelDefs.Stars(n);
            bool unlocked = LevelDefs.Unlocked(n);
            node[n].color = s > 0 ? Done : unlocked ? Available : Locked;
            number[n].color = unlocked ? new Color(.12f, .06f, .08f) : new Color(.55f, .5f, .58f);
            for (int i = 0; i < 3; i++) { stars[n][i].gameObject.SetActive(s > 0); stars[n][i].color = i < s ? Gold : Dim; }
        }
        node[current].transform.localScale = Vector3.one * 1.15f;
        for (int n = 1; n <= LevelDefs.Count; n++) if (n != current) node[n].transform.localScale = Vector3.one;
    }
}
