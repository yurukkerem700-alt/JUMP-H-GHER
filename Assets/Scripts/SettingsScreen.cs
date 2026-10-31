using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Ayarlar ekranı: joystick açma/kapama, görünürlük, konum, boyut, şeffaflık ve hassasiyet.
// Joystick halkası değişiklikleri canlı gösterir.
public class SettingsScreen
{
    readonly GameObject root;
    readonly TMP_FontAsset font;
    TextMeshProUGUI enabledText, visibleText;
    Slider x, y, size, opacity, sens;
    TextMeshProUGUI xv, yv, sizev, opv, sensv;
    public bool IsOpen => root.activeSelf;

    public SettingsScreen(Transform parent, TMP_FontAsset f, Action back)
    {
        font = f;
        root = new GameObject("Ayarlar", typeof(Image)); root.transform.SetParent(parent, false);
        var r = root.GetComponent<RectTransform>(); r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero;
        root.GetComponent<Image>().color = new Color(.05f, .03f, .07f, .86f);
        var t = root.transform;
        UiArt.Label(t, font, "AYARLAR", .5f, .95f, 500, 70, 40, new Color(1, .77f, .5f));
        UiArt.Btn(t, font, "GERİ", .14f, .95f, 190, 66, () => back());
        UiArt.Btn(t, font, "SIFIRLA", .86f, .95f, 220, 66, () => { GameSettings.ResetJoystick(); Refresh(); });

        var legend = UiArt.Label(t, font, "JOYSTICK  (telefon ve tablet)", .5f, .875f, 800, 40, 22, new Color(.75f, .66f, .62f));
        legend.rectTransform.anchorMin = new Vector2(.05f, .875f); legend.rectTransform.anchorMax = new Vector2(.95f, .875f); legend.rectTransform.sizeDelta = new Vector2(0, 40);

        enabledText = Toggle(t, .81f, "Joystick", () => GameSettings.JoyEnabled = !GameSettings.JoyEnabled);
        visibleText = Toggle(t, .74f, "Görünüm", () => GameSettings.JoyVisible = !GameSettings.JoyVisible);
        x = Row(t, .67f, "Konum yatay", .15f, .85f, v => GameSettings.JoyX = v, out xv);
        y = Row(t, .60f, "Konum dikey", .12f, .32f, v => GameSettings.JoyY = v, out yv);
        size = Row(t, .53f, "Boyut", .6f, 1.5f, v => GameSettings.JoySize = v, out sizev);
        opacity = Row(t, .46f, "Şeffaflık", .1f, 1f, v => GameSettings.JoyOpacity = v, out opv);
        sens = Row(t, .39f, "Hassasiyet", .5f, 1.5f, v => GameSettings.JoySensitivity = v, out sensv);
        root.SetActive(false);
    }

    TextMeshProUGUI Toggle(Transform parent, float cy, string label, Action flip)
    {
        Caption(parent, cy, label);
        var button = UiArt.Btn(parent, font, "", .7f, cy, 330, 60, () => { flip(); Refresh(); });
        return button.GetComponentInChildren<TextMeshProUGUI>();
    }

    void Caption(Transform parent, float cy, string text)
    {
        var l = UiArt.Label(parent, font, text, .05f, cy, 330, 50, 24, new Color(1, .86f, .66f));
        l.alignment = TextAlignmentOptions.Left; l.rectTransform.pivot = new Vector2(0, .5f);
    }

    Slider Row(Transform parent, float cy, string label, float min, float max, Action<float> set, out TextMeshProUGUI value)
    {
        Caption(parent, cy, label);
        value = UiArt.Label(parent, font, "", .92f, cy, 120, 50, 22, new Color(1, .81f, .59f));
        var s = MakeSlider(parent, cy, min, max);
        s.onValueChanged.AddListener(v => { set(v); Refresh(); });
        return s;
    }

    static Slider MakeSlider(Transform parent, float cy, float min, float max)
    {
        var go = new GameObject("Kaydırıcı", typeof(RectTransform), typeof(Slider)); go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>(); rect.anchorMin = new Vector2(.42f, cy); rect.anchorMax = new Vector2(.84f, cy); rect.sizeDelta = new Vector2(0, 56);
        var bg = new GameObject("Arka", typeof(Image)); bg.transform.SetParent(go.transform, false);
        var br = bg.GetComponent<RectTransform>(); br.anchorMin = new Vector2(0, .35f); br.anchorMax = new Vector2(1, .65f); br.offsetMin = br.offsetMax = Vector2.zero;
        bg.GetComponent<Image>().color = new Color(.22f, .14f, .17f, 1);
        var area = new GameObject("Dolgu Alanı", typeof(RectTransform)); area.transform.SetParent(go.transform, false);
        var ar = area.GetComponent<RectTransform>(); ar.anchorMin = new Vector2(0, .35f); ar.anchorMax = new Vector2(1, .65f); ar.offsetMin = new Vector2(8, 0); ar.offsetMax = new Vector2(-8, 0);
        var fill = new GameObject("Dolgu", typeof(Image)); fill.transform.SetParent(area.transform, false);
        fill.GetComponent<Image>().color = new Color(.95f, .55f, .3f, 1);
        var fr = fill.GetComponent<RectTransform>(); fr.anchorMin = Vector2.zero; fr.anchorMax = Vector2.one; fr.offsetMin = fr.offsetMax = Vector2.zero;
        var slide = new GameObject("Tutamak Alanı", typeof(RectTransform)); slide.transform.SetParent(go.transform, false);
        var sr = slide.GetComponent<RectTransform>(); sr.anchorMin = Vector2.zero; sr.anchorMax = Vector2.one; sr.offsetMin = new Vector2(24, 0); sr.offsetMax = new Vector2(-24, 0);
        var handle = new GameObject("Tutamak", typeof(Image)); handle.transform.SetParent(slide.transform, false);
        var hi = handle.GetComponent<Image>(); hi.sprite = UiArt.Disc; hi.color = new Color(1, .86f, .66f, 1);
        handle.GetComponent<RectTransform>().sizeDelta = new Vector2(48, 48);
        var s = go.GetComponent<Slider>();
        s.fillRect = fill.GetComponent<RectTransform>(); s.handleRect = handle.GetComponent<RectTransform>(); s.targetGraphic = hi;
        s.direction = Slider.Direction.LeftToRight; s.minValue = min; s.maxValue = max;
        return s;
    }

    void Refresh()
    {
        enabledText.text = GameSettings.JoyEnabled ? "AÇIK" : "KAPALI (düğmeler)";
        visibleText.text = GameSettings.JoyVisible ? "GÖRÜNÜR" : "GİZLİ";
        Set(x, xv, GameSettings.JoyX, v => Mathf.RoundToInt(v * 100) + "%");
        Set(y, yv, GameSettings.JoyY, v => Mathf.RoundToInt(v * 100) + "%");
        Set(size, sizev, GameSettings.JoySize, v => Mathf.RoundToInt(v * 100) + "%");
        Set(opacity, opv, GameSettings.JoyOpacity, v => Mathf.RoundToInt(v * 100) + "%");
        Set(sens, sensv, GameSettings.JoySensitivity, v => v.ToString("0.0") + "x");
    }

    static void Set(Slider s, TextMeshProUGUI label, float value, Func<float, string> format)
    {
        s.SetValueWithoutNotify(Mathf.Clamp(value, s.minValue, s.maxValue));
        label.text = format(s.value);
    }

    TouchJoystick Joystick { get { var l = UnityEngine.Object.FindFirstObjectByType<GameDeviceLayout>(); return l ? l.Joystick : null; } }

    public void Open() { Refresh(); root.SetActive(true); var j = Joystick; if (j) j.SetPreview(true); }
    public void Close() { root.SetActive(false); var j = Joystick; if (j) j.SetPreview(false); }
}
