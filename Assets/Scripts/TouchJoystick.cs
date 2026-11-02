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
    GameObject jumpButton;

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
        jumpButton = new GameObject("Mobil Zipla", typeof(Image), typeof(TouchControlButton));
        jumpButton.transform.SetParent(canvas.transform, false);
        jumpButton.GetComponent<TouchControlButton>().action = TouchControlButton.Action.Jump;
        jumpButton.GetComponent<Image>().color = new Color(.25f,.14f,.18f,.9f);
        var jr = jumpButton.GetComponent<RectTransform>();
        jr.anchorMin = jr.anchorMax = new Vector2(.87f,.24f); jr.sizeDelta = new Vector2(160,140);
        jumpButton.AddComponent<SafeHudAnchor>();
        UiArt.Label(jumpButton.transform, null, "ZIPLA", .5f,.5f,150,80,30,Color.white);

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
    public void SetPreview(bool on)
    {
        preview = on; canvas.sortingOrder = on ? 1001 : 850;
        // Düğmeler ayar panelinin üstünde canlı görünsün; ayar satırlarına dokunuşu engellemesinler.
        var tc = layout ? layout.touchControls : null;
        if (tc)
        {
            var c = tc.GetComponent<Canvas>();
            if (!c) { c = tc.AddComponent<Canvas>(); tc.AddComponent<GraphicRaycaster>(); }
            c.overrideSorting = on; c.sortingOrder = 1001;
            tc.GetComponent<GraphicRaycaster>().enabled = !on;
        }
        Apply();
    }

    // Eski sol / sağ / zıpla düğmeleri joystick kapalıyken geri gelir.
    public void SetLegacyButtons(GameObject[] buttons) { legacyButtons = buttons; Apply(); }

    struct Base { public Vector2 pos, size; }
    readonly System.Collections.Generic.Dictionary<RectTransform, Base> baseRects = new System.Collections.Generic.Dictionary<RectTransform, Base>();
    Vector2 lastParentSize;

    // < > ZIPLA düğmelerinin boyut, şeffaflık ve konum ayarı. Başlangıç değerleri ilk çağrıda sahneden okunur.
    void ApplyButtons()
    {
        float size = Mathf.Clamp(GameSettings.BtnSize, .5f, 1.8f), alpha = Mathf.Clamp(GameSettings.BtnOpacity, .1f, 1f);
        float originX = 0; bool haveOrigin = false;
        foreach (var go in legacyButtons)
        {
            if (!go) continue;
            var rt = (RectTransform)go.transform;
            if (!baseRects.ContainsKey(rt)) baseRects[rt] = new Base { pos = rt.anchoredPosition, size = rt.sizeDelta };
            var tcb = go.GetComponent<TouchControlButton>();
            if (tcb && tcb.action != TouchControlButton.Action.Jump) { float x = baseRects[rt].pos.x; originX = haveOrigin ? Mathf.Min(originX, x) : x; haveOrigin = true; }
        }
        foreach (var go in legacyButtons)
        {
            if (!go) continue;
            var rt = (RectTransform)go.transform; var parent = rt.parent as RectTransform; var b = baseRects[rt];
            float w = parent ? parent.rect.width : 0, h = parent ? parent.rect.height : 0;
            if (parent) lastParentSize = new Vector2(w, h);
            if (!go.TryGetComponent<CanvasGroup>(out var g)) g = go.AddComponent<CanvasGroup>();
            g.alpha = alpha;
            var tcb = go.GetComponent<TouchControlButton>();
            bool jump = tcb && tcb.action == TouchControlButton.Action.Jump;
            var size2 = b.size * size;
            Vector2 pos;
            if (jump) pos = new Vector2(b.pos.x - GameSettings.JumpX * w, b.pos.y + GameSettings.JumpY * h);
            else pos = new Vector2(originX + (b.pos.x - originX) * size + GameSettings.MoveX * w, b.pos.y + GameSettings.MoveY * h);
            if (w > 0)
            {
                // Her zaman ekranın içinde kal (jump sağa, diğerleri sola sabitli).
                if (jump) pos.x = Mathf.Clamp(pos.x, -(w - size2.x), 0);
                else pos.x = Mathf.Clamp(pos.x, 0, Mathf.Max(0, w - size2.x));
                pos.y = Mathf.Clamp(pos.y, 0, Mathf.Max(0, h - size2.y));
            }
            rt.sizeDelta = size2; rt.anchoredPosition = pos;
        }
    }

    void LateUpdate()
    {
        var p = legacyButtons.Length > 0 && legacyButtons[0] ? legacyButtons[0].transform.parent as RectTransform : null;
        if (p && (Vector2)p.rect.size != lastParentSize) ApplyButtons();
    }

    bool Wanted => GameSettings.JoyEnabled && layout && layout.TouchEnabled;

    public void Apply()
    {
        if (!this || !canvas) return;
        bool on = Wanted || (preview && GameSettings.JoyEnabled);
        float size = Mathf.Clamp(GameSettings.JoySize, .6f, 1.6f);
        var safe = Screen.safeArea;
        float insetX = BaseSize * size * canvas.scaleFactor * .5f / Mathf.Max(1, Screen.width);
        float insetY = BaseSize * size * canvas.scaleFactor * .5f / Mathf.Max(1, Screen.height);
        float x = Mathf.Clamp(GameSettings.JoyX, safe.xMin / Screen.width + insetX, safe.xMax / Screen.width - insetX);
        float y = Mathf.Clamp(GameSettings.JoyY, safe.yMin / Screen.height + insetY, safe.yMax / Screen.height - insetY);
        zone.gameObject.SetActive(Wanted && !preview);
        visual.gameObject.SetActive(on);
        if(jumpButton) { jumpButton.SetActive(Wanted && !preview); jumpButton.transform.SetAsLastSibling(); }
        // Yakalama alanı: halkanın yatayda iki yanı, aşağıda ekranın tabanına kadar
        zone.anchorMin = new Vector2(Mathf.Max(safe.xMin / Screen.width, x - .36f), safe.yMin / Screen.height);
        zone.anchorMax = new Vector2(Mathf.Min(safe.xMax / Screen.width, x + .36f), Mathf.Min(safe.yMax / Screen.height, y + .16f));
        zone.offsetMin = zone.offsetMax = Vector2.zero;
        visual.anchorMin = visual.anchorMax = new Vector2(x, y);
        visual.anchoredPosition = Vector2.zero;
        visual.sizeDelta = new Vector2(BaseSize, BaseSize);
        visual.localScale = Vector3.one * size;
        group.alpha = GameSettings.JoyVisible || preview ? Mathf.Clamp01(GameSettings.JoyOpacity) : 0;
        foreach (var b in legacyButtons) if (b) b.SetActive(!GameSettings.JoyEnabled);
        ApplyButtons();
        if (!Wanted) Release();
    }

    float RadiusPixels => BaseSize * .5f * Mathf.Clamp(GameSettings.JoySize, .6f, 1.6f) * canvas.scaleFactor;

    public void OnPointerDown(PointerEventData e)
    {
        if (active || GameFlow.IsMenu) return; // ikinci parmak joystick sahipliğini devralamaz
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
        knob.anchoredPosition = Vector2.ClampMagnitude(delta / canvas.scaleFactor / Mathf.Clamp(GameSettings.JoySize, .6f, 1.6f), BaseSize * .36f);
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
