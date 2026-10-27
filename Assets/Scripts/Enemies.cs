using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum EnemyKind { Zombie, Goblin }

// Resources/Enemies altındaki sprite sayfasını (EnemyBake ile üretilir) kare kare Sprite'a böler.
class EnemyArt
{
    [System.Serializable] class Meta { public int frames, cols, cellW, cellH; }
    public Sprite[] frames;
    public float height; // ayakta duran karenin oyundaki yüksekliği (birim)
    static readonly Dictionary<EnemyKind, EnemyArt> cache = new Dictionary<EnemyKind, EnemyArt>();

    // Ölçek: referans karenin piksel yüksekliği, hedef yüksekliğe denk gelsin.
    public static EnemyArt Get(EnemyKind kind)
    {
        if (cache.TryGetValue(kind, out var art) && art.frames[0]) return art;
        string name = kind == EnemyKind.Zombie ? "Zombie" : "Goblin";
        float referencePixels = kind == EnemyKind.Zombie ? 305 : 229; // 21. / 22. karenin sıkı yüksekliği
        float target = kind == EnemyKind.Zombie ? 1.35f : 1.0f;
        var texture = Resources.Load<Texture2D>("Enemies/" + name);
        var meta = JsonUtility.FromJson<Meta>(Resources.Load<TextAsset>("Enemies/" + name).text);
        int rows = Mathf.CeilToInt(meta.frames / (float)meta.cols);
        art = new EnemyArt { frames = new Sprite[meta.frames], height = target };
        for (int i = 0; i < meta.frames; i++)
            art.frames[i] = Sprite.Create(texture, new Rect((i % meta.cols) * meta.cellW, (rows - 1 - i / meta.cols) * meta.cellH, meta.cellW, meta.cellH), new Vector2(.5f, 0), referencePixels / target);
        cache[kind] = art; return art;
    }

    public static void Release() { foreach (var a in cache.Values) foreach (var f in a.frames) if (f) Object.Destroy(f); cache.Clear(); }
}

class Enemy
{
    public const int Stand = 0, Leap = 1, Climb = 2, Land = 3;
    public EnemyKind kind; public GameObject go; public SpriteRenderer sr; public EnemyArt art;
    public int floor, state, toFloor; public float offset, toOffset, side = 1, t0, dur, readyAt, staggerUntil, spawnedAt, waitSince;
    public Vector3 from; public bool faceRight = true, dead;
}

class EnemySlot { public EnemyKind kind; public Enemy enemy; public float readyAt; }

// Bölümün düşmanlarını doğurur, yönlendirir ve oyuncuya değince can götürür.
// Zombi: basamağın kenarına zıplayıp asılır, tırmanır. Goblin: basamaktan basamağa doğrudan zıplar.
public class EnemyManager : MonoBehaviour
{
    const float FirstDelay = 6f, RespawnDelay = 2.5f;
    InfiniteTower tower; Material material; Canvas canvas; Image[] markers;
    readonly List<EnemySlot> slots = new List<EnemySlot>();
    LevelDef def; bool active; float startAt; int playerFloor;
    public int AliveCount { get { int n = 0; foreach (var s in slots) if (s.enemy != null) n++; return n; } }
    public int SlotCount => slots.Count;

    public void Init(InfiniteTower t)
    {
        tower = t; material = t.coins.coin.GetComponentInChildren<SpriteRenderer>().sharedMaterial;
        canvas = UiArt.NewCanvas("Düşman Göstergeleri", 870);
        markers = new Image[6];
        for (int i = 0; i < markers.Length; i++) { markers[i] = UiArt.Pic(canvas.transform, UiArt.Disc, new Color(1, .2f, .2f, .9f), 0, 0, 34, 34); markers[i].gameObject.SetActive(false); }
    }

    public void Begin(LevelDef level)
    {
        Clear(); def = level; active = level.HasEnemies; startAt = Time.time + FirstDelay; playerFloor = 0;
        for (int i = 0; i < level.zombies; i++) slots.Add(new EnemySlot { kind = EnemyKind.Zombie, readyAt = startAt + i * 1.5f });
        for (int i = 0; i < level.goblins; i++) slots.Add(new EnemySlot { kind = EnemyKind.Goblin, readyAt = startAt + .75f + i * 1.5f });
    }

    public void Clear()
    {
        foreach (var s in slots) if (s.enemy != null && s.enemy.go) Destroy(s.enemy.go);
        slots.Clear(); active = false;
        if (markers != null) foreach (var m in markers) if (m) m.gameObject.SetActive(false);
    }

    void Update()
    {
        if (!active || GameFlow.IsMenu || !LevelRun.Instance || LevelRun.Instance.Finished) return;
        float now = Time.time, dt = Time.deltaTime;
        var support = tower.player.GroundPlatform(out bool grounded);
        if (grounded && support) playerFloor = support.floorIndex;
        foreach (var slot in slots)
        {
            if (slot.enemy == null)
            {
                if (now >= slot.readyAt && playerFloor >= 3) Spawn(slot);
                continue;
            }
            var e = slot.enemy;
            Tick(e, dt, now);
            if (!e.dead && (e.floor < playerFloor - 9 || e.floor > playerFloor + 7)) e.dead = true;
            if (e.dead) { Destroy(e.go); slot.enemy = null; slot.readyAt = now + RespawnDelay; continue; }
            Touch(e, now);
        }
        UpdateMarkers();
    }

    void Spawn(EnemySlot slot)
    {
        int floor = Mathf.Max(0, playerFloor - Random.Range(3, 5));
        var plat = tower.GetPlatform(floor);
        if (!plat) { slot.readyAt = Time.time + 1; return; }
        var art = EnemyArt.Get(slot.kind);
        var go = new GameObject(slot.kind == EnemyKind.Zombie ? "Zombi" : "Goblin", typeof(SpriteRenderer));
        var sr = go.GetComponent<SpriteRenderer>(); sr.sharedMaterial = material; sr.sortingOrder = 18; sr.sprite = art.frames[slot.kind == EnemyKind.Zombie ? 21 : 22];
        float half = Half(plat);
        var e = new Enemy { kind = slot.kind, go = go, sr = sr, art = art, floor = floor, offset = Random.Range(-half + .3f, half - .3f), spawnedAt = Time.time, readyAt = Time.time + .4f, waitSince = Time.time };
        slot.enemy = e; Place(e, StandPos(plat, e.offset));
    }

    static float Half(MovingPlatform2D plat)
    {
        var c = plat.GetComponent<Collider2D>(); if (!c) c = plat.GetComponentInChildren<Collider2D>();
        return c ? c.bounds.extents.x : 1.2f;
    }
    static Vector3 StandPos(MovingPlatform2D plat, float offset) => new Vector3(plat.transform.position.x + offset, plat.Top, 0);
    static Vector3 HangPos(MovingPlatform2D plat, float side, float height) => new Vector3(plat.transform.position.x + side * (Half(plat) + .12f), plat.Top - height * .95f, 0);
    static void Place(Enemy e, Vector3 pos) { e.go.transform.position = pos; }

    void Tick(Enemy e, float dt, float now)
    {
        float speed = def.enemySpeed;
        var player = tower.player.transform.position;
        bool zombie = e.kind == EnemyKind.Zombie;
        float alpha = Mathf.Clamp01((now - e.spawnedAt) / .6f);
        e.sr.color = new Color(1, 1, 1, alpha);
        if (e.state == Enemy.Stand)
        {
            var plat = tower.GetPlatform(e.floor); if (!plat) { e.dead = true; return; }
            float half = Half(plat), limit = half - .25f;
            bool staggered = now < e.staggerUntil;
            float posX = plat.transform.position.x + e.offset;
            if (e.floor == playerFloor || staggered)
            {
                float want = Mathf.Clamp(player.x - plat.transform.position.x, -limit, limit);
                float dir = Mathf.Abs(want - e.offset) < .06f ? 0 : Mathf.Sign(want - e.offset);
                if (staggered) dir = -dir; // darbeden sonra geri çekilir
                float run = (zombie ? 1.35f : 2.7f) * speed * (staggered ? 1.3f : 1);
                e.offset = Mathf.Clamp(e.offset + dir * run * dt, -limit, limit);
                if (dir != 0) e.faceRight = staggered ? dir < 0 : dir > 0; else e.faceRight = player.x > posX;
            }
            else if (now >= e.readyAt)
            {
                int to = e.floor < playerFloor ? e.floor + 1 : e.floor - 1;
                var next = tower.GetPlatform(to);
                if (!next) { e.dead = true; return; }
                if (zombie && to > e.floor)
                {
                    // Zombi, üstteki basamağın yakın kenarına yaklaşıp o zaman sıçrar.
                    float side = posX >= next.transform.position.x ? 1 : -1;
                    float hangX = HangPos(next, side, e.art.height).x;
                    float want = Mathf.Clamp(hangX - plat.transform.position.x, -limit, limit);
                    float dir = Mathf.Abs(want - e.offset) < .06f ? 0 : Mathf.Sign(want - e.offset);
                    e.offset = Mathf.Clamp(e.offset + dir * 1.35f * speed * dt, -limit, limit);
                    if (dir != 0) e.faceRight = dir > 0;
                    if (Mathf.Abs(hangX - (plat.transform.position.x + e.offset)) < 3.2f || now - e.waitSince > 3f) StartLeap(e, to, side, 0, now, .75f / speed);
                }
                else
                {
                    float toLimit = Half(next) - .25f;
                    float toOffset = Mathf.Clamp(player.x - next.transform.position.x, -toLimit, toLimit);
                    StartLeap(e, to, 0, toOffset, now, (zombie ? .6f : .8f) / speed);
                }
            }
            if (e.state == Enemy.Stand) { var p = tower.GetPlatform(e.floor); if (p) Place(e, StandPos(p, e.offset) + Vector3.up * Bob(e, now)); }
        }
        else if (e.state == Enemy.Leap) TickLeap(e, now);
        else if (e.state == Enemy.Climb) TickClimb(e, now);
        else TickLand(e, now);
        e.sr.flipX = !e.faceRight;
        e.sr.sprite = e.art.frames[FrameFor(e, now)];
    }

    static float Bob(Enemy e, float now) => e.kind == EnemyKind.Goblin ? Mathf.Abs(Mathf.Sin(now * 11f)) * .06f : Mathf.Sin(now * 4f) * .025f;

    void StartLeap(Enemy e, int to, float side, float toOffset, float now, float duration)
    {
        e.state = Enemy.Leap; e.t0 = now; e.dur = Mathf.Max(.25f, duration); e.toFloor = to; e.side = side; e.toOffset = toOffset;
        e.from = e.go.transform.position;
    }

    void TickLeap(Enemy e, float now)
    {
        var dest = tower.GetPlatform(e.toFloor); if (!dest) { e.dead = true; return; }
        bool hang = e.kind == EnemyKind.Zombie && e.toFloor > e.floor;
        Vector3 target = hang ? HangPos(dest, e.side, e.art.height) : StandPos(dest, e.toOffset);
        float s = Mathf.Clamp01((now - e.t0) / e.dur);
        float arc = hang ? .55f : (e.toFloor > e.floor ? 1.15f : .6f);
        Vector3 pos = Vector3.Lerp(e.from, target, s * s * (3 - 2 * s)); pos.y += Mathf.Sin(Mathf.PI * s) * arc;
        Place(e, pos);
        e.faceRight = hang ? e.side < 0 : target.x >= e.from.x;
        if (s < 1) return;
        e.floor = e.toFloor;
        if (hang) { e.state = Enemy.Climb; e.t0 = now; e.dur = 1.5f / def.enemySpeed; }
        else
        {
            e.offset = e.toOffset; e.state = e.kind == EnemyKind.Goblin ? Enemy.Land : Enemy.Stand; e.t0 = now; e.dur = .55f;
            e.readyAt = now + (e.kind == EnemyKind.Goblin ? .95f : .6f) / def.enemySpeed;
        }
    }

    void TickClimb(Enemy e, float now)
    {
        var plat = tower.GetPlatform(e.floor); if (!plat) { e.dead = true; return; }
        float s = Mathf.Clamp01((now - e.t0) / e.dur), half = Half(plat);
        float x = plat.transform.position.x + e.side * Mathf.Lerp(half + .12f, half - .45f, s);
        float y = Mathf.Lerp(plat.Top - e.art.height * .95f, plat.Top, s);
        Place(e, new Vector3(x, y, 0)); e.faceRight = e.side < 0;
        if (s < 1) return;
        e.offset = e.side * (half - .45f); e.state = Enemy.Stand; e.readyAt = now + .6f / def.enemySpeed; e.waitSince = now;
    }

    void TickLand(Enemy e, float now)
    {
        var plat = tower.GetPlatform(e.floor); if (!plat) { e.dead = true; return; }
        Place(e, StandPos(plat, e.offset));
        if (now - e.t0 >= e.dur) { e.state = Enemy.Stand; e.waitSince = now; }
    }

    // Kare seçimi: sprite sayfasının kare numaraları EnemyBake çıktısına göredir.
    static int FrameFor(Enemy e, float now)
    {
        if (e.kind == EnemyKind.Zombie)
        {
            if (e.state == Enemy.Leap) return 10 + Mathf.Min(3, Mathf.FloorToInt(Mathf.Clamp01((now - e.t0) / e.dur) * 4));
            if (e.state == Enemy.Climb) return new[] { 14, 15, 8, 9 }[Mathf.FloorToInt(now * 6) % 4];
            return new[] { 20, 21, 22, 21 }[Mathf.FloorToInt(now * 4) % 4];
        }
        if (e.state == Enemy.Leap) return Mathf.Min(5, Mathf.FloorToInt(Mathf.Clamp01((now - e.t0) / e.dur) * 6));
        if (e.state == Enemy.Land)
        {
            float k = Mathf.Clamp01((now - e.t0) / e.dur);
            if (k < .2f) return 7;
            if (k < .35f) return 8;
            if (k < .7f) return 9 + Mathf.FloorToInt((k - .35f) / .35f * 4);
            return 16 + Mathf.Min(3, Mathf.FloorToInt((k - .7f) / .3f * 4));
        }
        return 20 + Mathf.FloorToInt(now * 9) % 4;
    }

    void Touch(Enemy e, float now)
    {
        var health = PlayerHealth.Instance;
        if (health == null || now < e.staggerUntil || now - e.spawnedAt < .7f) return;
        bool zombie = e.kind == EnemyKind.Zombie;
        var pos = e.go.transform.position;
        float centerY = pos.y + e.art.height * (zombie && e.state == Enemy.Climb ? .6f : .45f);
        var p = tower.player.transform.position;
        if (Mathf.Abs(p.x - pos.x) < (zombie ? .55f : .6f) && Mathf.Abs(p.y - centerY) < (zombie ? .85f : .7f) && health.Hit()) e.staggerUntil = now + 1.2f;
    }

    // Ekranın altında kalan düşmanlar için altta kırmızı uyarı noktası.
    void UpdateMarkers()
    {
        var cam = Camera.main; int used = 0;
        foreach (var slot in slots)
        {
            if (slot.enemy == null || used >= markers.Length) continue;
            var v = cam.WorldToViewportPoint(slot.enemy.go.transform.position);
            if (v.y >= .01f) continue;
            var m = markers[used++]; m.gameObject.SetActive(true);
            m.rectTransform.anchorMin = m.rectTransform.anchorMax = new Vector2(Mathf.Clamp(v.x, .05f, .95f), 0);
            m.rectTransform.anchoredPosition = new Vector2(0, 170);
            m.color = new Color(1, .2f, .2f, .5f + .4f * Mathf.Sin(Time.time * 8));
        }
        for (int i = used; i < markers.Length; i++) markers[i].gameObject.SetActive(false);
    }

    void OnDestroy() { Clear(); EnemyArt.Release(); if (canvas) Destroy(canvas.gameObject); }
}
