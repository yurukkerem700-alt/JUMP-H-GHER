using UnityEngine;

// Başlangıç sahnesine canlılık: mezardan çıkmaya çalışan sallanan zombi ve dalda duran kuzgunlar.
// Grafikler Resources/Decor altındaki yatay şeritlerdir (kare = yükseklik x yükseklik); dosya yoksa hiçbir şey eklenmez.
//   Resources/Decor/ZombiMezar.png : kareler sırayla oynar (ileri-geri), taban orta noktası zeminde
//   Resources/Decor/Kuzgun.png     : kare 0 = durur, diğerleri = başını çevirir / gagalar / kanat silker
// Konumlar arka plan resminin kenarlarına göre oranlıdır; ekran oranı değişince birlikte kayar.
public class GraveDecor : MonoBehaviour
{
    // 0..1: arka plan resminin soldan / yukarıdan oranı (zombi için tabanın, kuzgunlar için ayak yerinin konumu)
    const float ZombieU = .182f, ZombieV = .71f, ZombieHeight = 1.9f;
    const float BranchU = .863f, BranchV = .18f, RavenHeight = .85f;
    static readonly Vector2[] RavenOffsets = { new Vector2(0, 0), new Vector2(-.02f, .008f), new Vector2(.015f, -.006f) };

    SpriteRenderer bg, zombie;
    Sprite[] zombieFrames, ravenFrames;
    SpriteRenderer[] ravens;
    float[] nextAct, actUntil;

    public void Init(InfiniteTower tower)
    {
        var background = GameObject.Find("Mezarlik");
        if (!background) { enabled = false; return; }
        bg = background.GetComponent<SpriteRenderer>();
        var material = tower.coins.coin.GetComponentInChildren<SpriteRenderer>().sharedMaterial;
        zombieFrames = Slice("ZombiMezar", ZombieHeight);
        ravenFrames = Slice("Kuzgun", RavenHeight);
        if (zombieFrames != null) zombie = Make("Mezardaki Zombi", zombieFrames[0], material, -20);
        if (ravenFrames != null)
        {
            ravens = new SpriteRenderer[RavenOffsets.Length]; nextAct = new float[ravens.Length]; actUntil = new float[ravens.Length];
            for (int i = 0; i < ravens.Length; i++)
            {
                ravens[i] = Make("Kuzgun " + (i + 1), ravenFrames[0], material, -19);
                ravens[i].flipX = i == 1; nextAct[i] = Time.time + Random.Range(1f, 5f);
            }
        }
        if (zombie == null && ravens == null) enabled = false;
    }

    static SpriteRenderer Make(string name, Sprite sprite, Material material, int order)
    {
        var sr = new GameObject(name, typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
        sr.sprite = sprite; sr.sharedMaterial = material; sr.sortingOrder = order; return sr;
    }

    static Sprite[] Slice(string name, float targetHeight)
    {
        var texture = Resources.Load<Texture2D>("Decor/" + name);
        if (!texture) return null;
        int h = texture.height, n = Mathf.Max(1, texture.width / h);
        // Küçük (piksel sanatı) kareler keskin kalsın
        if (h <= 128) texture.filterMode = FilterMode.Point;
        var frames = new Sprite[n];
        for (int i = 0; i < n; i++) frames[i] = Sprite.Create(texture, new Rect(i * h, 0, h, h), new Vector2(.5f, 0), h / targetHeight);
        return frames;
    }

    Vector3 At(float u, float v)
    {
        var b = bg.bounds;
        return new Vector3(b.min.x + u * b.size.x, b.max.y - v * b.size.y, 0);
    }

    void LateUpdate()
    {
        if (!bg) return;
        float t = Time.time;
        if (zombie)
        {
            // Mezardan sürünerek çıkmaya çalışır: kareler ileri-geri, gövde hafifçe sallanır.
            int n = zombieFrames.Length;
            float cycle = Mathf.PingPong(t * 3.2f, n - 1 + .999f);
            zombie.sprite = zombieFrames[Mathf.Min(n - 1, Mathf.FloorToInt(cycle))];
            var p = At(ZombieU, ZombieV);
            zombie.transform.position = p + new Vector3(Mathf.Sin(t * 1.7f) * .03f, 0, 0);
            zombie.transform.rotation = Quaternion.Euler(0, 0, Mathf.Sin(t * 1.3f) * 3.5f);
        }
        if (ravens != null)
            for (int i = 0; i < ravens.Length; i++)
            {
                int n = ravenFrames.Length;
                if (t >= nextAct[i] && n > 1) { actUntil[i] = t + Random.Range(.5f, 1.3f); nextAct[i] = actUntil[i] + Random.Range(3f, 9f); ravens[i].sprite = ravenFrames[Random.Range(1, n)]; }
                if (t >= actUntil[i]) ravens[i].sprite = ravenFrames[0];
                ravens[i].transform.position = At(BranchU + RavenOffsets[i].x, BranchV + RavenOffsets[i].y);
            }
    }
}
