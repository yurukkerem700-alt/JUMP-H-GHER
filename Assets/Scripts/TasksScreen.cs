using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Görevler sayfası: üç günlük görev (ilerleme çubuğu + ödül al), günlük giriş ödülü ve cüzdan.
public class TasksScreen
{
    class Row { public TextMeshProUGUI Text, Progress, Reward, ButtonText; public RectTransform Fill; public Button Button; public Image ButtonFace; }

    static readonly Color Gold = new Color(1f, .77f, .3f), Soft = new Color(1, .86f, .66f), Muted = new Color(.62f, .55f, .58f);
    readonly GameObject root;
    readonly TMP_FontAsset font;
    readonly Sprite plate;
    readonly Row[] rows = new Row[3];
    readonly Row login = new Row();
    TextMeshProUGUI wallet, loginInfo;
    public bool IsOpen => root.activeSelf;

    public TasksScreen(Transform parent, TMP_FontAsset font, Action back)
    {
        this.font = font;
        root = new GameObject("Görevler", typeof(Image)); root.transform.SetParent(parent, false);
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
        var title = UiArt.Label(t, font, "GÖREVLER", .5f, .95f, 500, 70, 40, new Color(1, .82f, .56f)); title.outlineColor = new Color32(30, 5, 5, 255); title.outlineWidth = .2f;
        var backBtn = UiArt.Btn(t, font, "GERİ", .14f, .95f, 190, 66, () => back());
        var backImg = backBtn.GetComponent<Image>(); if (plate) { backImg.sprite = plate; backImg.type = Image.Type.Sliced; backImg.color = new Color(.9f, .85f, .9f); }
        wallet = UiArt.Label(t, font, "", .5f, .86f, 700, 60, 32, Gold);
        wallet.outlineColor = new Color32(30, 5, 5, 255); wallet.outlineWidth = .15f;

        UiArt.Label(t, font, "BUGÜNÜN GÖREVLERİ", .5f, .80f, 700, 40, 22, Muted);
        for (int i = 0; i < 3; i++) rows[i] = MakeRow(t, .715f - i * .148f, i);
        UiArt.Label(t, font, "GÜNLÜK GİRİŞ ÖDÜLÜ", .5f, .292f, 700, 40, 22, Muted);
        MakeLogin(t, .185f);
        root.SetActive(false);
    }

    // Ortak kart: sol metin + ilerleme çubuğu, sağ ödül + düğme.
    RectTransform Card(Transform parent, float cy, float half)
    {
        var go = new GameObject("Kart", typeof(Image)); go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>(); rect.anchorMin = new Vector2(.05f, cy - half); rect.anchorMax = new Vector2(.95f, cy + half); rect.offsetMin = rect.offsetMax = Vector2.zero;
        var img = go.GetComponent<Image>(); img.raycastTarget = false;
        if (plate) { img.sprite = plate; img.type = Image.Type.Sliced; img.color = new Color(.85f, .82f, .88f); } else img.color = new Color(.12f, .08f, .11f, .95f);
        return rect;
    }

    static RectTransform Fit(GameObject go, float x0, float y0, float x1, float y1)
    {
        var r = go.GetComponent<RectTransform>(); r.anchorMin = new Vector2(x0, y0); r.anchorMax = new Vector2(x1, y1); r.offsetMin = r.offsetMax = Vector2.zero; return r;
    }

    TextMeshProUGUI Text(Transform parent, string value, float x0, float y0, float x1, float y1, float size, Color color, TextAlignmentOptions align)
    {
        var go = new GameObject("Yazı", typeof(TextMeshProUGUI)); go.transform.SetParent(parent, false);
        var t = go.GetComponent<TextMeshProUGUI>(); t.font = font; t.text = value; t.fontSize = size; t.color = color; t.alignment = align; t.raycastTarget = false;
        t.enableAutoSizing = true; t.fontSizeMin = 12; t.fontSizeMax = size; Fit(go, x0, y0, x1, y1); return t;
    }

    Row MakeRow(Transform parent, float cy, int index)
    {
        var card = Card(parent, cy, .068f); var row = new Row();
        row.Text = Text(card, "", .04f, .52f, .60f, .96f, 30, Soft, TextAlignmentOptions.Left);
        var back = new GameObject("Çubuk", typeof(Image)); back.transform.SetParent(card, false); Fit(back, .04f, .14f, .60f, .42f);
        back.GetComponent<Image>().color = new Color(.05f, .03f, .06f, .9f); back.GetComponent<Image>().raycastTarget = false;
        var fill = new GameObject("Dolgu", typeof(Image)); fill.transform.SetParent(back.transform, false);
        fill.GetComponent<Image>().color = Gold; fill.GetComponent<Image>().raycastTarget = false; row.Fill = Fit(fill, 0, 0, 0, 1);
        row.Progress = Text(back.transform, "", 0, 0, 1, 1, 20, Color.white, TextAlignmentOptions.Center);
        row.Reward = Text(card, "", .62f, .52f, .76f, .96f, 28, Gold, TextAlignmentOptions.Center);
        MakeButton(card, row, .78f, .16f, .97f, .84f, () => { if (PlayerProgress.Claim(index)) { SesAtolyesi.Ses.Pickup(); Refresh(); } });
        return row;
    }

    void MakeLogin(Transform parent, float cy)
    {
        var card = Card(parent, cy, .085f);
        loginInfo = Text(card, "", .04f, .15f, .66f, .85f, 28, Soft, TextAlignmentOptions.Left);
        MakeButton(card, login, .70f, .16f, .97f, .84f, () => { if (PlayerProgress.ClaimLogin()) { SesAtolyesi.Ses.Pickup(); Refresh(); } });
    }

    void MakeButton(RectTransform card, Row row, float x0, float y0, float x1, float y1, UnityEngine.Events.UnityAction action)
    {
        var go = new GameObject("Düğme", typeof(Image), typeof(Button)); go.transform.SetParent(card, false); Fit(go, x0, y0, x1, y1);
        row.ButtonFace = go.GetComponent<Image>();
        if (plate) { row.ButtonFace.sprite = plate; row.ButtonFace.type = Image.Type.Sliced; }
        row.Button = go.GetComponent<Button>(); row.Button.targetGraphic = row.ButtonFace;
        row.Button.onClick.AddListener(() => { SesAtolyesi.Ses.UiClick(); action(); });
        row.ButtonText = Text(go.transform, "", .05f, .05f, .95f, .95f, 26, Soft, TextAlignmentOptions.Center);
    }

    void Style(Row row, string label, bool active)
    {
        row.ButtonText.text = label; row.Button.interactable = active;
        row.ButtonFace.color = active ? new Color(1.35f, .85f, .5f) : new Color(.55f, .52f, .58f);
        row.ButtonText.color = active ? Color.white : Muted;
    }

    void Refresh()
    {
        wallet.text = "CÜZDAN  " + PlayerProgress.Wallet + " COIN";
        var tasks = PlayerProgress.Today();
        for (int i = 0; i < rows.Length; i++)
        {
            var t = tasks[i]; var row = rows[i];
            int p = Mathf.Min(PlayerProgress.Progress(t.kind), t.target);
            row.Text.text = t.text; row.Progress.text = p + " / " + t.target; row.Reward.text = "+" + t.reward;
            row.Fill.anchorMax = new Vector2(Mathf.Clamp01((float)p / t.target), 1);
            if (PlayerProgress.Claimed(t.kind)) Style(row, "ALINDI", false);
            else if (p >= t.target) Style(row, "ÖDÜLÜ AL", true);
            else Style(row, "DEVAM", false);
        }
        int streak = PlayerProgress.NextStreak;
        int reward = PlayerProgress.LoginReward(streak);
        if (PlayerProgress.LoginClaimable)
        {
            loginInfo.text = "SERİ: " + streak + ". GÜN\nBugünün ödülü: +" + reward + " COIN";
            Style(login, "AL  +" + reward, true);
        }
        else
        {
            loginInfo.text = "SERİ: " + streak + " GÜN\nYarın: +" + PlayerProgress.LoginReward(streak + 1) + " COIN";
            Style(login, "ALINDI", false);
        }
    }

    public void Open() { Refresh(); root.SetActive(true); }
    public void Close() { root.SetActive(false); }
}
