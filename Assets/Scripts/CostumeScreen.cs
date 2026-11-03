using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Kostümler ekranı: kartlara dokununca karakter anında değişir, seçim kaydedilir.
public class CostumeScreen
{
    readonly int columns;
    const float CardHeight = 420, RowGap = 450, Top = 40;

    class Card { public string Id; public Image Frame, Pic; public TextMeshProUGUI State; }

    readonly GameObject root;
    readonly Sprite plate;
    readonly List<Card> cards = new List<Card>();
    readonly List<Sprite> previews = new List<Sprite>();
    readonly ScrollRect scroll;
    TextMeshProUGUI walletLabel, notice; float noticeUntil;
    public bool IsOpen => root.activeSelf;

    public CostumeScreen(Transform parent, TMP_FontAsset font, Action back)
    {
        columns = Screen.height > Screen.width ? 2 : 3;
        root = new GameObject("Kostümler", typeof(Image)); root.transform.SetParent(parent, false);
        var r = root.GetComponent<RectTransform>(); r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero;
        root.GetComponent<Image>().color = new Color(.035f, .025f, .045f, 1);
        var t = root.transform;
        plate = GameFlow.Sliced("MapPlateSide", new Vector4(36, 24, 36, 24));
        var tex = Resources.Load<Texture2D>("UI/MapBackground");
        if (tex)
        {
            // Ana menüyle aynı boyalı arka plan (haritadaki düğüm halkaları dışarıda kalacak şekilde üst bölüm), karartılmış.
            var bg = new GameObject("Arka Plan", typeof(Image), typeof(AspectRatioFitter)); bg.transform.SetParent(t, false);
            float cw = tex.width * .68f, ch = tex.height * .43f;
            var bi = bg.GetComponent<Image>(); bi.sprite = Sprite.Create(tex, new Rect(tex.width - cw, tex.height - ch, cw, ch), Vector2.one * .5f, 100); bi.raycastTarget = false; bi.color = new Color(.55f, .5f, .55f);
            var fit = bg.GetComponent<AspectRatioFitter>(); fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent; fit.aspectRatio = cw / ch;
            var dim = UiArt.Pic(t, null, new Color(.03f, .01f, .04f, .55f), .5f, .5f, 0, 0); dim.rectTransform.anchorMin = Vector2.zero; dim.rectTransform.anchorMax = Vector2.one; dim.rectTransform.offsetMin = dim.rectTransform.offsetMax = Vector2.zero;
        }
        if (plate) { var bar = UiArt.Pic(t, plate, Color.white, .5f, .95f, 520, 84); bar.type = Image.Type.Sliced; }
        var title = UiArt.Label(t, font, "KOSTÜMLER", .5f, .95f, 500, 70, 40, new Color(1, .82f, .56f)); title.outlineColor = new Color32(30, 5, 5, 255); title.outlineWidth = .2f;
        walletLabel = UiArt.Label(t, font, "", .86f, .95f, 300, 60, 24, new Color(1f, .77f, .3f));
        notice = UiArt.Label(t, font, "", .5f, .89f, 800, 50, 26, new Color(1, .5f, .4f));
        var backBtn = UiArt.Btn(t, font, "GERİ", .14f, .95f, 190, 66, () => back());
        var backImg = backBtn.GetComponent<Image>(); if (plate) { backImg.sprite = plate; backImg.type = Image.Type.Sliced; backImg.color = new Color(.9f, .85f, .9f); }

        var view = new GameObject("Kaydırma Alanı", typeof(RectTransform), typeof(Image), typeof(RectMask2D), typeof(ScrollRect)); view.transform.SetParent(t, false);
        var vr = view.GetComponent<RectTransform>(); vr.anchorMin = Vector2.zero; vr.anchorMax = new Vector2(1, .9f); vr.offsetMin = vr.offsetMax = Vector2.zero;
        view.GetComponent<Image>().color = new Color(0, 0, 0, 0);
        var contentGo = new GameObject("İçerik", typeof(RectTransform)); contentGo.transform.SetParent(view.transform, false);
        var content = contentGo.GetComponent<RectTransform>(); content.anchorMin = new Vector2(0, 1); content.anchorMax = new Vector2(1, 1); content.pivot = new Vector2(.5f, 1);
        int rows = (Costumes.All.Length + columns - 1) / columns;
        content.sizeDelta = new Vector2(0, Top + rows * RowGap + 20); content.anchoredPosition = Vector2.zero;
        scroll = view.GetComponent<ScrollRect>(); scroll.viewport = vr; scroll.content = content; scroll.horizontal = false; scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 40;

        for (int i = 0; i < Costumes.All.Length; i++) cards.Add(MakeCard(content, font, Costumes.All[i], i));
        root.SetActive(false);
    }

    Card MakeCard(RectTransform content, TMP_FontAsset font, Costumes.Entry entry, int index)
    {
        int col = index % columns, row = index / columns;
        float cell = .96f / columns;
        float x0 = .02f + col * cell;
        var go = new GameObject(entry.Name, typeof(Image), typeof(Button)); go.transform.SetParent(content, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(x0, 1); rect.anchorMax = new Vector2(x0 + cell - .02f, 1); rect.pivot = new Vector2(.5f, 1);
        rect.anchoredPosition = new Vector2(0, -(Top + row * RowGap)); rect.sizeDelta = new Vector2(0, CardHeight);
        // Anchor aralığı genişlik verdiği için pivotu ortada tutmak üzere x ofseti sıfır kalır.
        var frame = go.GetComponent<Image>();
        if (plate) { frame.sprite = plate; frame.type = Image.Type.Sliced; }
        var button = go.GetComponent<Button>(); string id = entry.Id;
        button.onClick.AddListener(() => { SesAtolyesi.Ses.UiClick(); Choose(id); });

        // Önizleme sprite'ı Open() sırasında kurulur: varsayılan kostümün sayfası sahne başlarken hazır olmayabilir.
        var pic = UiArt.Pic(go.transform, null, Color.white, .5f, .6f, 0, 0);
        pic.preserveAspect = true; pic.enabled = false;
        pic.rectTransform.anchorMin = new Vector2(.14f, .3f); pic.rectTransform.anchorMax = new Vector2(.86f, .94f);
        pic.rectTransform.offsetMin = pic.rectTransform.offsetMax = Vector2.zero;

        var name = UiArt.Label(go.transform, font, entry.Name, .5f, .2f, 0, 60, 24, new Color(1, .86f, .66f));
        name.rectTransform.anchorMin = new Vector2(.02f, .2f); name.rectTransform.anchorMax = new Vector2(.98f, .2f); name.rectTransform.sizeDelta = new Vector2(0, 60);
        name.enableAutoSizing = true; name.fontSizeMin = 14; name.fontSizeMax = 24; name.outlineColor = new Color32(20, 4, 4, 255); name.outlineWidth = .18f;
        var state = UiArt.Label(go.transform, font, "", .5f, .07f, 0, 50, 22, new Color(.75f, .66f, .62f));
        state.rectTransform.anchorMin = new Vector2(.02f, .07f); state.rectTransform.anchorMax = new Vector2(.98f, .07f); state.rectTransform.sizeDelta = new Vector2(0, 50);
        return new Card { Id = entry.Id, Frame = frame, Pic = pic, State = state };
    }

    void Choose(string id)
    {
        if (!Costumes.Owned(id))
        {
            if (!Costumes.Buy(id)) { Say("COIN YETERSİZ  •  " + Costumes.Price(id) + " COIN GEREK"); return; }
            SesAtolyesi.Ses.Pickup();
        }
        notice.text = ""; Costumes.SelectedId = id; Refresh();
    }

    void Say(string text) { notice.text = text; noticeUntil = Time.unscaledTime + 2.5f; }

    void Refresh()
    {
        walletLabel.text = "CÜZDAN  " + PlayerProgress.Wallet;
        string selected = Costumes.SelectedId;
        foreach (var c in cards)
        {
            bool on = c.Id == selected;
            if (!c.Pic.sprite) { var sprite = Costumes.Preview(c.Id); if (sprite) { previews.Add(sprite); c.Pic.sprite = sprite; c.Pic.enabled = true; } }
            c.Frame.color = plate ? (on ? new Color(1.45f, .85f, .55f, 1) : new Color(.8f, .78f, .85f, 1)) : (on ? new Color(.45f, .24f, .14f, .98f) : new Color(.16f, .1f, .13f, .95f));
            bool owned = Costumes.Owned(c.Id);
            c.State.text = on ? "SEÇİLİ" : owned ? "SEÇ" : "KİLİTLİ  •  " + Costumes.Price(c.Id) + " COIN";
            c.State.color = on ? new Color(1, .77f, .5f) : owned ? new Color(.75f, .66f, .62f) : new Color(1f, .68f, .3f);
            c.Pic.color = owned ? Color.white : new Color(.38f, .36f, .42f, 1);
        }
    }

    public void Open() { notice.text = ""; Refresh(); root.SetActive(true); scroll.verticalNormalizedPosition = 1; }
    public void Close() { root.SetActive(false); }
}
