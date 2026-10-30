using UnityEngine;
using SesAtolyesi.Dsp;

namespace SesAtolyesi
{
    /// <summary>
    /// Bir zemini/nesneyi hangi yüzey olarak duyacağımızı belirtir. İsteğe bağlıdır:
    /// bileşen yoksa sırasıyla etiket (tag), fizik malzemesi adı, görsel malzeme adı ve nesne adına bakılır
    /// ("Grass", "Çim", "Taş", "Ahşap" gibi). Hiçbiri uymazsa Ses.DefaultSurface kullanılır.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SesSurface : MonoBehaviour
    {
        public SurfaceType surface = SurfaceType.Stone;

        static readonly string[] Names =
        {
            "grass", "çim", "cim", "ot", "stone", "rock", "concrete", "tile", "taş", "tas", "beton",
            "dirt", "soil", "mud", "toprak", "çamur", "camur", "gravel", "pebble", "çakıl", "cakil",
            "wood", "plank", "ahşap", "ahsap", "tahta", "parke", "snow", "kar", "water", "su", "sand", "kum",
            "metal", "iron", "steel", "demir", "çelik", "celik"
        };
        static readonly SurfaceType[] Types =
        {
            SurfaceType.Grass, SurfaceType.Grass, SurfaceType.Grass, SurfaceType.Grass,
            SurfaceType.Stone, SurfaceType.Stone, SurfaceType.Stone, SurfaceType.Stone, SurfaceType.Stone, SurfaceType.Stone, SurfaceType.Stone,
            SurfaceType.Dirt, SurfaceType.Dirt, SurfaceType.Dirt, SurfaceType.Dirt, SurfaceType.Dirt, SurfaceType.Dirt,
            SurfaceType.Gravel, SurfaceType.Gravel, SurfaceType.Gravel, SurfaceType.Gravel,
            SurfaceType.Wood, SurfaceType.Wood, SurfaceType.Wood, SurfaceType.Wood, SurfaceType.Wood, SurfaceType.Wood,
            SurfaceType.Snow, SurfaceType.Snow, SurfaceType.Water, SurfaceType.Water, SurfaceType.Sand, SurfaceType.Sand,
            SurfaceType.Metal, SurfaceType.Metal, SurfaceType.Metal, SurfaceType.Metal, SurfaceType.Metal, SurfaceType.Metal
        };

        /// <summary>İsimden yüzey bulur. Kısa adlar (ot, kar, su, tas) yalnızca tam eşleşmede, uzunlar içerirse eşleşir.</summary>
        public static bool FromName(string name, out SurfaceType result)
        {
            result = Ses.DefaultSurface;
            if (string.IsNullOrEmpty(name)) return false;
            string n = name.ToLowerInvariant().Replace(" (instance)", "").Trim();
            for (int i = 0; i < Names.Length; i++)
            {
                string k = Names[i];
                bool hit = k.Length <= 3 ? n == k : n.Contains(k);
                if (hit) { result = Types[i]; return true; }
            }
            return false;
        }

        static SurfaceType FromObject(GameObject go, string physicsMaterialName)
        {
            SurfaceType t;
            var marker = go.GetComponentInParent<SesSurface>();
            if (marker != null) return marker.surface;
            if (FromName(go.tag, out t)) return t;
            if (FromName(physicsMaterialName, out t)) return t;
            var r = go.GetComponent<Renderer>();
            if (r != null && r.sharedMaterial != null && FromName(r.sharedMaterial.name, out t)) return t;
            if (FromName(go.name, out t)) return t;
            return Ses.DefaultSurface;
        }

        public static SurfaceType Resolve(Collider col)
        {
            if (col == null) return Ses.DefaultSurface;
            var m = col.sharedMaterial;
            return FromObject(col.gameObject, m != null ? m.name : null);
        }

        public static SurfaceType Resolve(Collider2D col)
        {
            if (col == null) return Ses.DefaultSurface;
            var m = col.sharedMaterial;
            return FromObject(col.gameObject, m != null ? m.name : null);
        }
    }
}
