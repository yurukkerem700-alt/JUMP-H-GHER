using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Kostümler ekranı: kartlara dokununca karakter anında değişir, seçim kaydedilir.
public class CostumeScreen
{
    const int Columns = 3;
    const float CardHeight = 420, RowGap = 450, Top = 40;

    class Card { public string Id; public Image Frame, Pic; public TextMeshProUGUI State; }

    readonly GameObject root;
    readonly List<Card> cards = new List<Card>();
    readonly List<Sprite> previews = new List<Sprite>();
    readonly ScrollRect scroll;
    public bool IsOpen => root.activeSelf;

    public CostumeScreen(Transform parent, TMP_FontAsset font, Action back)
    {
        root = new GameObject("Kostümler", typeof(Image)); root.transform.SetParent(parent, false);
        var r = root.GetComponent<RectTransform>(); r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero;
        root.GetComponent<Image>().color = new Color(.05f, .03f, .07f, .94f);
        var t = root.transform;
        UiArt.Label(t, font, "KOSTÜMLER", .5f, .95f, 500, 70, 40, new Color(1, .77f, .5f));
        UiArt.Btn(t, font, "GERİ", .14f, .95f, 190, 66, () => back());

        var view = new GameObject("Kaydırma Alanı", typeof(RectTransform), typeof(Image), typeof(RectMask2D), typeof(ScrollRect)); view.transform.SetParent(t, false);
        var vr = view.GetComponent<RectTransform>(); vr.anchorMin = Vector2.zero; vr.anchorMax = new Vector2(1, .9f); vr.offsetMin = vr.offsetMax = Vector2.zero;
        view.GetComponent<Image>().color = new Color(0, 0, 0, 0);
        var contentGo = new GameObject("İçerik", typeof(RectTransform)); contentGo.transform.SetParent(view.transform, false);
        var content = contentGo.GetComponent<RectTransform>(); content.anchorMin = new Vector2(0, 1); content.anchorMax = new Vector2(1, 1); content.pivot = new Vector2(.5f, 1);
        int rows = (Costumes.All.Length + Columns - 1) / Columns;
        content.sizeDelta = new Vector2(0, Top + rows * RowGap + 20); content.anchoredPosition = Vector2.zero;
        scroll = view.GetComponent<ScrollRect>(); scroll.viewport = vr; scroll.content = content; scroll.horizontal = false; scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 40;

        for (int i = 0; i < Costumes.All.Length; i++) cards.Add(MakeCard(content, font, Costumes.All[i], i));
        root.SetActive(false);
    }

    Card MakeCard(RectTransform content, TMP_FontAsset font, Costumes.Entry entry, int index)
    {
        int col = index % Columns, row = index / Columns;
        float x0 = .03f + col * .32f;
        var go = new GameObject(entry.Name, typeof(Image), typeof(Button)); go.transform.SetParent(content, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(x0, 1); rect.anchorMax = new Vector2(x0 + .30f, 1); rect.pivot = new Vector2(.5f, 1);
        rect.anchoredPosition = new Vector2(0, -(Top + row * RowGap)); rect.sizeDelta = new Vector2(0, CardHeight);
        // Anchor aralığı genişlik verdiği için pivotu ortada tutmak üzere x ofseti sıfır kalır.
        var frame = go.GetComponent<Image>();
        var button = go.GetComponent<Button>(); string id = entry.Id;
        button.onClick.AddListener(() => { SesAtolyesi.Ses.UiClick(); Costumes.SelectedId = id; Refresh(); });

        // Önizleme sprite'ı Open() sırasında kurulur: varsayılan kostümün sayfası sahne başlarken hazır olmayabilir.
        var pic = UiArt.Pic(go.transform, null, Color.white, .5f, .6f, 0, 0);
        pic.preserveAspect = true; pic.enabled = false;
        pic.rectTransform.anchorMin = new Vector2(.05f, .27f); pic.rectTransform.anchorMax = new Vector2(.95f, .97f);
        pic.rectTransform.offsetMin = pic.rectTransform.offsetMax = Vector2.zero;

        var name = UiArt.Label(go.transform, font, entry.Name, .5f, .2f, 0, 60, 24, new Color(1, .86f, .66f));
        name.rectTransform.anchorMin = new Vector2(.02f, .2f); name.rectTransform.anchorMax = new Vector2(.98f, .2f); name.rectTransform.sizeDelta = new Vector2(0, 60);
        name.enableAutoSizing = true; name.fontSizeMin = 14; name.fontSizeMax = 24;
        var state = UiArt.Label(go.transform, font, "", .5f, .07f, 0, 50, 22, new Color(.75f, .66f, .62f));
        state.rectTransform.anchorMin = new Vector2(.02f, .07f); state.rectTransform.anchorMax = new Vector2(.98f, .07f); state.rectTransform.sizeDelta = new Vector2(0, 50);
        return new Card { Id = entry.Id, Frame = frame, Pic = pic, State = state };
    }

    void Refresh()
    {
        string selected = Costumes.SelectedId;
        foreach (var c in cards)
        {
            bool on = c.Id == selected;
            if (!c.Pic.sprite) { var sprite = Costumes.Preview(c.Id); if (sprite) { previews.Add(sprite); c.Pic.sprite = sprite; c.Pic.enabled = true; } }
            c.Frame.color = on ? new Color(.45f, .24f, .14f, .98f) : new Color(.16f, .1f, .13f, .95f);
            c.State.text = on ? "SEÇİLİ" : "SEÇ";
            c.State.color = on ? new Color(1, .77f, .5f) : new Color(.75f, .66f, .62f);
        }
    }

    public void Open() { Refresh(); root.SetActive(true); scroll.verticalNormalizedPosition = 1; }
    public void Close() { root.SetActive(false); }
}
