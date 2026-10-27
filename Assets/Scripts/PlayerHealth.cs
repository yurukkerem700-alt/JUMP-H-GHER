using System;
using UnityEngine;
using UnityEngine.UI;

// 3 canlık can barı. Düşman darbesi ve zemine düşmek 1 can götürür; darbeden sonra kısa süre dokunulmazlık vardır.
public class PlayerHealth : MonoBehaviour
{
    public static PlayerHealth Instance { get; private set; }
    public const int Max = 3;
    public const float InvulnerableSeconds = 1.6f;
    public int Hearts { get; private set; } = Max;
    public bool Invulnerable => Time.time < invulnerableUntil;
    public event Action Died;
    float invulnerableUntil;
    Canvas canvas; Image[] icons; SpriteRenderer body; InfiniteTower tower;
    static readonly Color Full = new Color(.95f, .22f, .28f), Empty = new Color(.22f, .15f, .2f, .85f);

    void Awake() { Instance = this; }

    public void Init(InfiniteTower t)
    {
        tower = t;
        canvas = UiArt.NewCanvas("Can Barı", 880);
        icons = new Image[Max];
        for (int i = 0; i < Max; i++)
        {
            icons[i] = UiArt.Pic(canvas.transform, UiArt.Heart, Full, 0, 1, 46, 46);
            icons[i].rectTransform.anchoredPosition = new Vector2(46 + i * 54, -140);
        }
        Refresh();
    }

    // ignoreInvulnerability: zemine düşmek dokunulmazlığa takılmaz.
    public bool Hit(bool ignoreInvulnerability = false)
    {
        if (Hearts <= 0 || (Invulnerable && !ignoreInvulnerability)) return false;
        Hearts--; invulnerableUntil = Time.time + InvulnerableSeconds; Refresh();
        if (Hearts <= 0) Died?.Invoke();
        return true;
    }

    public void Refill() { Hearts = Max; invulnerableUntil = 0; Refresh(); }

    void Refresh()
    {
        for (int i = 0; i < Max; i++) icons[i].color = i < Hearts ? Full : Empty;
    }

    void LateUpdate()
    {
        if (!body) { foreach (var r in tower.player.GetComponentsInChildren<SpriteRenderer>(true)) if (r.name == "Kok Gezgin Gorseli") body = r; if (!body) return; }
        float alpha = Invulnerable && Hearts > 0 ? (Mathf.Sin(Time.time * 26f) > 0 ? 1f : .3f) : 1f;
        var c = body.color; if (!Mathf.Approximately(c.a, alpha)) { c.a = alpha; body.color = c; }
    }

    void OnDestroy() { Instance = null; if (canvas) Destroy(canvas.gameObject); }
}
