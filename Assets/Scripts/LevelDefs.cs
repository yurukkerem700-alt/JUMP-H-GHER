using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

public enum GoalKind { Floor, Coins }

public struct LevelDef
{
    public int number, target;
    public GoalKind kind;
    public float twoStarTime, threeStarTime, limit; // limit > 0: süre dolunca bölüm kaybedilir
    public bool HardLimit => limit > 0;
    public string Goal => kind == GoalKind.Floor ? target + ". kata çık, kapıdan geç" : target + " coin topla";
}

public static class LevelDefs
{
    public const int Count = 100;

    // 1. bölüm 5 kat/coin, 100. bölüm 100 kat. Tek numaralı bölümler kat, çift numaralılar coin hedefidir; 100. bölüm kat hedefidir.
    // Her 5. bölüm (100 hariç) süreli: 2 yıldız süresi aşılırsa bölüm kaybedilir.
    public static LevelDef Get(int n)
    {
        n = Mathf.Clamp(n, 1, Count);
        var d = new LevelDef { number = n };
        d.kind = n == Count || n % 2 == 1 ? GoalKind.Floor : GoalKind.Coins;
        d.target = Mathf.RoundToInt(5 + (n - 1) * 95f / 99f);
        float two = Round5(15 + d.target * 5f), three = Round5(10 + d.target * 3.2f);
        if (n % 5 == 0 && n < Count) { d.limit = two; two = Round5(two * .8f); }
        d.twoStarTime = two; d.threeStarTime = Mathf.Min(three, two - 5);
        return d;
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
    static Sprite star, disc;
    public static Sprite Star => star ? star : star = Build(64, 64, 64, (u, v) => StarAlpha(u * 2, v * 2));
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
        var b = go.GetComponent<Button>(); b.onClick.AddListener(action);
        var t = Label(go.transform, font, title, .5f, .5f, w, h, 25, new Color(1, .81f, .59f));
        t.rectTransform.anchorMin = Vector2.zero; t.rectTransform.anchorMax = Vector2.one; t.rectTransform.offsetMin = t.rectTransform.offsetMax = Vector2.zero;
        return b;
    }
}
