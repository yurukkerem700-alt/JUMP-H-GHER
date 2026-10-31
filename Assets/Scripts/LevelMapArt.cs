using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Bölüm haritasının boyalı parçaları: Resources/UI altındaki PNG'ler (tools/make_map_art.py referans görselden üretir).
public partial class LevelMap
{
    static readonly Dictionary<string, Sprite> painted = new Dictionary<string, Sprite>();

    // Resources/UI/<ad> yoksa null döner.
    static Sprite Painted(string name)
    {
        if (painted.TryGetValue(name, out var cached) && cached) return cached;
        var tex = Resources.Load<Texture2D>("UI/" + name);
        var sprite = tex ? Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), Vector2.one * .5f, 1) : null;
        painted[name] = sprite;
        return sprite;
    }

    // Madeni levha: boyalı sprite varsa o, yoksa koyu iç + soluk çelik kenar
    static void Plate(Image img, string name = "MapPlateSide")
    {
        var art = Painted(name);
        if (art) { img.sprite = art; img.color = Color.white; return; }
        img.color = new Color(.07f, .05f, .06f, .95f);
        var steel = new Color(.62f, .56f, .5f, 1);
        var o = img.gameObject.AddComponent<Outline>(); o.effectColor = steel; o.effectDistance = new Vector2(3, 3);
        var o2 = img.gameObject.AddComponent<Outline>(); o2.effectColor = steel; o2.effectDistance = new Vector2(-3, -3);
    }
}
