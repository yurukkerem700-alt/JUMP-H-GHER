using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Başarımlar sayfası: üstte genel istatistikler, altında kaydırılabilir başarım listesi (ilerleme + ödül al).
public class AchievementsScreen
{
    class Row { public TextMeshProUGUI Progress, ButtonText; public RectTransform Fill; public Button Button; public Image ButtonFace, Card; }

    static readonly Color Gold = new Color(1f, .77f, .3f), Soft = new Color(1, .86f, .66f), Muted = new Color(.62f, .55f, .58f);
    const float RowHeight = 150, RowGap = 168;
    readonly GameObject root;
    readonly TMP_FontAsset font;
    readonly Sprite plate;
    readonly List<Row> rows = new List<Row>();
    readonly ScrollRect scroll;
    TextMeshProUGUI stats;
    public bool IsOpen => root.activeSelf;

    public AchievementsScreen(Transform parent, TMP_FontAsset font, Action back)
    {
        this.font = font;
        root = new GameObject("Başarımlar", typeof(Image)); root.transform.SetParent(parent, false);
        var r = root.GetComponent<RectTransform>(); r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero;
        root.GetComponent<Image>().color = new Color(.035f, .025f, .045f, 1);
        var t = root.transform;
        plate = GameFlow.Sliced("MapPlateSide", new Vector4(36, 24, 36, 24));
        var tex = Resources.Load<Texture2D>("UI/MapBackground");
        if (tex)
        {
            var bg = new GameObject("Arka Plan", typeof(Image), typeof(AspectRatioFitter)); bg.transform.SetParent(t, false);
            float cw = tex.width * .68f, ch = tex.height * .43f;
            var bi = bg.GetComponent<Image>(); bi.sprite = Sprite.Create(tex, new Rect(tex.width - cw, tex.height - ch, cw, ch), Vector2.one * .5f, 100); bi.raycastTarget = false; bi.color = new Color(.55f, .5f, .55f);
            var fit = bg.GetComponent<AspectRatioFitter>(); fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent; fit.aspectRatio = cw / ch;
            var dim = UiArt.Pic(t, null, new Color(.03f, .01f, .04f, .6f), .5f, .5f, 0, 0); dim.rectTransform.anchorMin = Vector2.zero; dim.rectTransform.anchorMax = Vector2.one; dim.rectTransform.offsetMin = dim.rectTransform.offsetMax = Vector2.zero;
        }
        if (plate) { var bar = UiArt.Pic(t, plate, Color.white, .5f, .95f, 520, 84); bar.type = Image.Type.Sliced; }
        var title = UiArt.Label(t, font, "BAŞARIMLAR", .5f, .95f, 500, 70, 40, new Color(1, .82f, .56f)); title.outlineColor = new Color32(30, 5, 5, 255); title.outlineWidth = .2f;
        var backBtn = UiArt.Btn(t, font, "GERİ", .14f, .95f, 190, 66, () => back());
        var backImg = backBtn.GetComponent<Image>(); if (plate) { backImg.sprite = plate; backImg.type = Image.Type.Sliced; backImg.color = new Color(.9f, .85f, .9f); }

        stats = UiArt.Label(t, font, "", .5f, .855f, 980, 120, 24, Soft);
        stats.enableAutoSizing = true; stats.fontSizeMin = 14; stats.fontSizeMax = 24;

        var view = new GameObject("Kaydırma Alanı", typeof(RectTransform), typeof(Image), typeof(RectMask2D), typeof(ScrollRect)); view.transform.SetParent(t, false);
        var vr = view.GetComponent<RectTransform>(); vr.anchorMin = Vector2.zero; vr.anchorMax = new Vector2(1, .78f); vr.offsetMin = vr.offsetMax = Vector2.zero;
        view.GetComponent<Image>().color = new Color(0, 0, 0, 0);
        var contentGo = new GameObject("İçerik", typeof(RectTransform)); contentGo.transform.SetParent(view.transform, false);
        var content = contentGo.GetComponent<RectTransform>(); content.anchorMin = new Vector2(0, 1); content.anchorMax = new Vector2(1, 1); content.pivot = new Vector2(.5f, 1);
        var list = PlayerProgress.Achievements;
        content.sizeDelta = new Vector2(0, 20 + list.Length * RowGap + 30); content.anchoredPosition = Vector2.zero;
        scroll = view.GetComponent<ScrollRect>(); scroll.viewport = vr; scroll.content = content; scroll.horizontal = false; scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 40;
        for (int i = 0; i < list.Length; i++) rows.Add(MakeRow(content, i));
        root.SetActive(false);
    }

    static RectTransform Fit(GameObject go, float x0, float y0, float x1, float y1)
    {
        var r = go.GetComponent<RectTransform>(); r.anchorMin = new Vector2(x0, y0); r.anchorMax = new Vector2(x1, y1); r.offsetMin = r.offsetMax = Vector2.zero; return r;
    }

    TextMeshProUGUI Text(Transform parent, string value, float x0, float y0, float x1, float y1, float size, Color color, TextAlignmentOptions align)
    {
        var go = new GameObject("Yazı", typeof(TextMeshProUGUI)); go.transform.SetParent(parent, false);
        var tt = go.GetComponent<TextMeshProUGUI>(); tt.font = font; tt.text = value; tt.fontSize = size; tt.color = color; tt.alignment = align; tt.raycastTarget = false;
        tt.enableAutoSizing = true; tt.fontSizeMin = 12; tt.fontSizeMax = size; Fit(go, x0, y0, x1, y1); return tt;
    }

    Row MakeRow(RectTransform content, int index)
    {
        var a = PlayerProgress.Achievements[index]; var row = new Row();
        var go = new GameObject("Başarım " + (index + 1), typeof(Image)); go.transform.SetParent(content, false);
        var rect = go.GetComponent<RectTransform>(); rect.anchorMin = new Vector2(.05f, 1); rect.anchorMax = new Vector2(.95f, 1); rect.pivot = new Vector2(.5f, 1);
        rect.anchoredPosition = new Vector2(0, -(20 + index * RowGap)); rect.sizeDelta = new Vector2(0, RowHeight);
        row.Card = go.GetComponent<Image>(); row.Card.raycastTarget = false;
        if (plate) { row.Card.sprite = plate; row.Card.type = Image.Type.Sliced; }
        Text(go.transform, a.title, .04f, .56f, .62f, .96f, 30, Soft, TextAlignmentOptions.Left);
        Text(go.transform, a.desc, .04f, .30f, .62f, .60f, 21, Muted, TextAlignmentOptions.Left);
        var back = new GameObject("Çubuk", typeof(Image)); back.transform.SetParent(go.transform, false); Fit(back, .04f, .08f, .62f, .26f);
        back.GetComponent<Image>().color = new Color(.05f, .03f, .06f, .9f); back.GetComponent<Image>().raycastTarget = false;
        var fill = new GameObject("Dolgu", typeof(Image)); fill.transform.SetParent(back.transform, false);
        fill.GetComponent<Image>().color = Gold; fill.GetComponent<Image>().raycastTarget = false; row.Fill = Fit(fill, 0, 0, 0, 1);
        row.Progress = Text(back.transform, "", 0, 0, 1, 1, 17, Color.white, TextAlignmentOptions.Center);
        Text(go.transform, "+" + a.reward, .64f, .52f, .76f, .94f, 28, Gold, TextAlignmentOptions.Center);
        var bgo = new GameObject("Düğme", typeof(Image), typeof(Button)); bgo.transform.SetParent(go.transform, false); Fit(bgo, .78f, .16f, .97f, .84f);
        row.ButtonFace = bgo.GetComponent<Image>();
        if (plate) { row.ButtonFace.sprite = plate; row.ButtonFace.type = Image.Type.Sliced; }
        row.Button = bgo.GetComponent<Button>(); row.Button.targetGraphic = row.ButtonFace;
        int i = index;
        row.Button.onClick.AddListener(() => { SesAtolyesi.Ses.UiClick(); if (PlayerProgress.ClaimAchievement(i)) { SesAtolyesi.Ses.Pickup(); Refresh(); } });
        row.ButtonText = Text(bgo.transform, "", .05f, .05f, .95f, .95f, 24, Soft, TextAlignmentOptions.Center);
        return row;
    }

    void Refresh()
    {
        stats.text = "TOPLAM COIN  " + PlayerProgress.Stat("Coins") + "   •   EN YÜKSEK KAT  " + PlayerPrefs.GetInt("BestFloor", 0)
            + "\nBİTEN BÖLÜM  " + PlayerProgress.Stat("Levels") + "   •   YILDIZ  " + LevelDefs.TotalStars() + " / " + LevelDefs.Count * 3
            + "   •   TEKMELENEN DÜŞMAN  " + PlayerProgress.Stat("Kicks") + "\nCÜZDAN  " + PlayerProgress.Wallet + " COIN";
        var list = PlayerProgress.Achievements;
        for (int i = 0; i < rows.Count; i++)
        {
            var a = list[i]; var row = rows[i];
            int value = Mathf.Min(a.value(), a.target); bool done = value >= a.target, claimed = PlayerProgress.AchievementClaimed(i);
            row.Progress.text = value + " / " + a.target; row.Fill.anchorMax = new Vector2(Mathf.Clamp01((float)value / a.target), 1);
            row.Card.color = plate ? (done && !claimed ? new Color(1.25f, 1f, .75f) : new Color(.85f, .82f, .88f)) : new Color(.12f, .08f, .11f, .95f);
            bool active = done && !claimed;
            row.ButtonText.text = claimed ? "ALINDI" : done ? "ÖDÜLÜ AL" : "KİLİTLİ";
            row.Button.interactable = active;
            row.ButtonFace.color = active ? new Color(1.35f, .85f, .5f) : new Color(.55f, .52f, .58f);
            row.ButtonText.color = active ? Color.white : Muted;
        }
    }

    public void Open() { Refresh(); root.SetActive(true); scroll.verticalNormalizedPosition = 1; }
    public void Close() { root.SetActive(false); }
}
