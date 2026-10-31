using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Ekranın alt ortasındaki yüzen joystick. Parmağı sürüklediğin yöne karakter gider:
// sağ / sol yürür, yukarı zıplar, aşağı hızlı iner, çaprazlar ikisini birden yapar.
public class TouchJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    const float BaseSize = 230;
    Canvas canvas; RectTransform zone, visual, knob; CanvasGroup group;
    GameObject[] legacyButtons = new GameObject[0];
    int pointer; bool active; Vector2 origin; bool preview;
    GameDeviceLayout layout;

    public static TouchJoystick Create(GameDeviceLayout owner)
    {
        var canvas = UiArt.NewCanvas("Joystick", 850);
        var zoneGo = new GameObject("Joystick Alanı", typeof(Image), typeof(TouchJoystick));
        zoneGo.transform.SetParent(canvas.transform, false);
        zoneGo.GetComponent<Image>().color = new Color(0, 0, 0, 0);
        var joy = zoneGo.GetComponent<TouchJoystick>();
        joy.canvas = canvas; joy.zone = zoneGo.GetComponent<RectTransform>(); joy.layout = owner;
        joy.Build();
        GameSettings.Changed += joy.Apply;
        GameInput.Cleared += joy.ResetState;
        joy.Apply();
        return joy;
    }

    void Build()
    {
        var go = new GameObject("Joystick Görünümü", typeof(RectTransform), typeof(CanvasGroup)); go.transform.SetParent(canvas.transform, false);
        visual = go.GetComponent<RectTransform>(); group = go.GetComponent<CanvasGroup>();
        group.blocksRaycasts = false; group.interactable = false;
        visual.anchorMin = visual.anchorMax = Vector2.zero; visual.sizeDelta = new Vector2(BaseSize, BaseSize);
        UiArt.Pic(visual, UiArt.Disc, new Color(0, 0, 0, .35f), .5f, .5f, BaseSize, BaseSize);
        UiArt.Pic(visual, Ring, Color.white, .5f, .5f, BaseSize, BaseSize);
        UiArt.Pic(visual, InnerRing, new Color(1, 1, 1, .6f), .5f, .5f, BaseSize * .8f, BaseSize * .8f);
        for (int i = 0; i < 4; i++)
        {
            var arrow = UiArt.Pic(visual, Chevron, new Color(1, 1, 1, .85f), .5f, .5f, 30, 30);
            var dir = i == 0 ? Vector2.up : i == 1 ? Vector2.right : i == 2 ? Vector2.down : Vector2.left;
            arrow.rectTransform.anchoredPosition = dir * BaseSize * .4f;
            arrow.rectTransform.localRotation = Quaternion.Euler(0, 0, -90 * i);
        }
        knob = UiArt.Pic(visual, UiArt.Disc, new Color(.85f, .85f, .85f, .95f), .5f, .5f, BaseSize * .42f, BaseSize * .42f).rectTransform;
    }

    static Sprite ring, innerRing, chevron;
    static Sprite Ring => ring ? ring : ring = UiArt.Build(128, 128, 128, (u, v) => { float r = Mathf.Sqrt(u * u + v * v) * 2; return Mathf.Clamp01((.03f - Mathf.Abs(r - .96f)) * 64 + .5f); });
    static Sprite InnerRing => innerRing ? innerRing : innerRing = UiArt.Build(128, 128, 128, (u, v) => { float r = Mathf.Sqrt(u * u + v * v) * 2; return Mathf.Clamp01((.012f - Mathf.Abs(r - .96f)) * 64 + .5f); });
    static Sprite Chevron => chevron ? chevron : chevron = UiArt.Build(64, 64, 64, (u, v) =>
    {
        float d = Mathf.Abs(v - (.12f - Mathf.Abs(u))) * .7071f;
        return Mathf.Abs(u) < .34f ? Mathf.Clamp01((.04f - d) * 64 + .5f) : 0;
    });

    // Ayarlar açıkken halkayı ayar paneli üstünde göstermek için.
    public void SetPreview(bool on) { preview = on; canvas.sortingOrder = on ? 1001 : 850; Apply(); }

    // Eski sol / sağ / zıpla düğmeleri joystick kapalıyken geri gelir.
    public void SetLegacyButtons(GameObject[] buttons) { legacyButtons = buttons; Apply(); }

    bool Wanted => GameSettings.JoyEnabled && layout && layout.TouchEnabled;

    public void Apply()
    {
        if (!this || !canvas) return;
        bool on = Wanted || (preview && GameSettings.JoyEnabled);
        float size = GameSettings.JoySize, x = GameSettings.JoyX, y = GameSettings.JoyY;
        zone.gameObject.SetActive(Wanted && !preview);
        visual.gameObject.SetActive(on);
        // Yakalama alanı: halkanın yatayda iki yanı, aşağıda ekranın tabanına kadar
        zone.anchorMin = new Vector2(Mathf.Max(0, x - .36f), 0);
        zone.anchorMax = new Vector2(Mathf.Min(1, x + .36f), Mathf.Min(.7f, y + .26f));
        zone.offsetMin = zone.offsetMax = Vector2.zero;
        visual.anchorMin = visual.anchorMax = new Vector2(x, y);
        visual.anchoredPosition = Vector2.zero;
        visual.sizeDelta = new Vector2(BaseSize, BaseSize);
        visual.localScale = Vector3.one * size;
        group.alpha = GameSettings.JoyVisible || preview ? Mathf.Clamp01(GameSettings.JoyOpacity) : 0;
        foreach (var b in legacyButtons) if (b) b.SetActive(!GameSettings.JoyEnabled);
        if (!Wanted) Release();
    }

    float RadiusPixels => BaseSize * .5f * GameSettings.JoySize * canvas.scaleFactor;

    public void OnPointerDown(PointerEventData e)
    {
        if (active) ResetState(); // takılı kalmış eski parmak varsa bırak
        active = true; pointer = e.pointerId; origin = e.position;
        Evaluate(e.position);
    }
    public void OnDrag(PointerEventData e) { if (active && e.pointerId == pointer) Evaluate(e.position); }
    public void OnPointerUp(PointerEventData e) { if (active && e.pointerId == pointer) Release(); }

    void Evaluate(Vector2 position)
    {
        float radius = RadiusPixels;
        var delta = position - origin;
        // Parmak çok uzaklaşırsa başlangıç noktası ardından gelir; yön değiştirmek hızlı olur.
        if (delta.magnitude > radius * 1.6f) { origin = position - delta.normalized * radius * 1.6f; delta = position - origin; }
        float dead = radius * .3f / Mathf.Clamp(GameSettings.JoySensitivity, .4f, 2f);
        float ax = Mathf.Abs(delta.x), ay = Mathf.Abs(delta.y);
        float h = ax > dead && ax > ay * .35f ? Mathf.Sign(delta.x) : 0;
        bool up = delta.y > dead * 1.15f && delta.y > ax * .6f;
        bool down = delta.y < -dead * 1.15f && ay > ax * .6f;
        bool wasUp = lastUp; lastUp = up;
        GameInput.SetJoystick(h, up, down);
        if (up && !wasUp) GameInput.Jump();
        knob.anchoredPosition = Vector2.ClampMagnitude(delta / canvas.scaleFactor / GameSettings.JoySize, BaseSize * .36f);
    }
    bool lastUp;

    void Release()
    {
        active = false; lastUp = false;
        GameInput.SetJoystick(0, false, false);
        if (knob) knob.anchoredPosition = Vector2.zero;
    }

    // GameInput.Clear() (menü, odak kaybı) sırasında çağrılır.
    void ResetState() { active = false; lastUp = false; if (knob) knob.anchoredPosition = Vector2.zero; }

    void OnDisable() { if (active) Release(); }
    void OnDestroy() { GameSettings.Changed -= Apply; GameInput.Cleared -= ResetState; if (canvas) Destroy(canvas.gameObject); }
}
