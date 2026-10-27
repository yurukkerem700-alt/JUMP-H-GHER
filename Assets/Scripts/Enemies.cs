using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum EnemyKind { CinderCrow, CloudMoth, StormLeech, RootStalker, StarWisp }

// Four-pose, 2x2 creature sheets are loaded from Resources/Enemies. A drawn fallback
// keeps the game playable until a matching art sheet is present.
class EnemyArt
{
    public Sprite[] frames;
    public float height;
    static readonly Dictionary<EnemyKind, EnemyArt> cache = new Dictionary<EnemyKind, EnemyArt>();
    static readonly List<Texture2D> OwnedTextures = new List<Texture2D>();
    static readonly string[] Names = { "CinderCrow", "CloudMoth", "StormLeech", "RootStalker", "StarWisp" };
    static readonly float[] Heights = { 1.18f, 1.12f, 1.05f, 1.38f, 1.0f };
    static readonly Color[] Colors = {
        new Color(.48f,.22f,.19f), new Color(.74f,.75f,.86f), new Color(.31f,.55f,.78f),
        new Color(.49f,.34f,.22f), new Color(.62f,.46f,.91f)
    };

    public static EnemyArt Get(EnemyKind kind)
    {
        if (cache.TryGetValue(kind, out var cached) && cached.frames != null && cached.frames[0]) return cached;
        int index = Mathf.Clamp((int)kind, 0, Names.Length - 1);
        var texture = Resources.Load<Texture2D>("Enemies/" + Names[index]);
        if (!texture || texture.width < 2 || texture.height < 2) return cache[kind] = MakeFallback(kind, index);

        int columns = kind == EnemyKind.RootStalker ? 4 : 2;
        int cellW = texture.width / columns, cellH = texture.height / 2;
        var art = new EnemyArt { frames = new Sprite[4], height = Heights[index] };
        // Visual order is idle, move, leap, landing. RootWandererSheet uses four columns;
        // its leap and landing poses are the second and third cells in the bottom row.
        int[] cells = kind == EnemyKind.RootStalker ? new[] { 0, 1, 5, 6 } : new[] { 0, 1, 2, 3 };
        for (int i = 0; i < art.frames.Length; i++)
        {
            int cell = cells[i];
            int x = (cell % columns) * cellW, y = (1 - cell / columns) * cellH;
            art.frames[i] = Sprite.Create(texture, new Rect(x, y, cellW, cellH), new Vector2(.5f, 0), cellH / art.height);
        }
        return cache[kind] = art;
    }

    static EnemyArt MakeFallback(EnemyKind kind, int index)
    {
        var art = new EnemyArt { frames = new Sprite[4], height = Heights[index] };
        for (int frame = 0; frame < art.frames.Length; frame++)
        {
            const int w = 96, h = 112;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color32[w * h];
            Color body = Colors[index], dark = Color.Lerp(body, new Color(.08f,.06f,.12f), .72f);
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float px = x - w * .5f, py = y;
                    bool torso = Ellipse(px, py, 0, 47, 20, 29);
                    bool head = Ellipse(px, py, 0, 78, 16, 16);
                    bool shape = torso || head;
                    if (kind == EnemyKind.CinderCrow)
                        shape |= Ellipse(px, py, -22, 58, 16, 7) || Ellipse(px, py, 22, 58, 16, 7) || Triangle(px, py, 12, 78, 27, 73, 14, 68);
                    else if (kind == EnemyKind.CloudMoth)
                        shape |= Ellipse(px, py, -28, 56, 17, 24) || Ellipse(px, py, 28, 56, 17, 24) || (Mathf.Abs(px) < 2 && py > 91 && py < 108);
                    else if (kind == EnemyKind.StormLeech)
                        shape |= Triangle(px, py, -8, 88, -20, 108, -3, 94) || Triangle(px, py, 8, 88, 20, 108, 3, 94) || Ellipse(px, py, 0, 18, 7, 17);
                    else if (kind == EnemyKind.RootStalker)
                        shape |= Triangle(px, py, -7, 86, -24, 109, -3, 95) || Triangle(px, py, 7, 86, 24, 109, 3, 95) || Ellipse(px, py, -23, 30, 8, 19) || Ellipse(px, py, 23, 30, 8, 19);
                    else
                        shape |= Ellipse(px, py, 0, 22, 8, 19) || Ellipse(px, py, -27, 78, 4, 4) || Ellipse(px, py, 28, 52, 3, 3);
                    if (frame == 1) shape &= !(torso && py < 32); // moving pose lifts the lower edge
                    if (frame == 2) shape |= Ellipse(px, py, -7, 28, 6, 9) || Ellipse(px, py, 8, 23, 6, 8);
                    if (frame == 3) shape |= Ellipse(px, py, -19, 46, 7, 8) || Ellipse(px, py, 19, 46, 7, 8);
                    bool eye = Ellipse(px, py, -6, 80, 2.2f, 2.2f) || Ellipse(px, py, 6, 80, 2.2f, 2.2f);
                    if (!shape && !eye) continue;
                    pixels[y * w + x] = eye ? new Color32(255, 223, 132, 255) : (torso || head ? body : dark);
                }
            tex.SetPixels32(pixels); tex.Apply(false, true); OwnedTextures.Add(tex);
            art.frames[frame] = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(.5f, 0), h / art.height);
        }
        return art;
    }

    static bool Ellipse(float x, float y, float cx, float cy, float rx, float ry)
    {
        float dx = (x - cx) / rx, dy = (y - cy) / ry;
        return dx * dx + dy * dy <= 1;
    }

    static bool Triangle(float x, float y, float ax, float ay, float bx, float by, float cx, float cy)
    {
        float d1 = Sign(x, y, ax, ay, bx, by), d2 = Sign(x, y, bx, by, cx, cy), d3 = Sign(x, y, cx, cy, ax, ay);
        return !((d1 < 0 || d2 < 0 || d3 < 0) && (d1 > 0 || d2 > 0 || d3 > 0));
    }

    static float Sign(float x, float y, float ax, float ay, float bx, float by) => (x - bx) * (ay - by) - (ax - bx) * (y - by);

    public static void Release()
    {
        foreach (var art in cache.Values)
            foreach (var frame in art.frames) if (frame) Object.Destroy(frame);
        foreach (var texture in OwnedTextures) if (texture) Object.Destroy(texture);
        OwnedTextures.Clear();
        cache.Clear();
    }
}

class Enemy
{
    public const int Stand = 0, Leap = 1, Land = 2;
    public EnemyKind kind; public GameObject go; public SpriteRenderer sr; public EnemyArt art;
    public int floor, state, toFloor; public float offset, toOffset, t0, dur, readyAt, staggerUntil, spawnedAt;
    public Vector3 from; public bool faceRight = true, dead;
}

class EnemySlot { public Enemy enemy; public float readyAt; }

// The threat species change with the biome and keep a readable, shared movement pattern.
public class EnemyManager : MonoBehaviour
{
    const float FirstDelay = 5f, RespawnDelay = 2.4f;
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
        for (int i = 0; i < markers.Length; i++) { markers[i] = UiArt.Pic(canvas.transform, UiArt.Disc, new Color(1, .46f, .24f, .9f), 0, 0, 34, 34); markers[i].gameObject.SetActive(false); }
    }

    public void Begin(LevelDef level)
    {
        Clear(); def = level; active = level.HasEnemies; startAt = Time.time + FirstDelay; playerFloor = 0;
        for (int i = 0; i < level.enemies; i++) slots.Add(new EnemySlot { readyAt = startAt + i * 1.45f });
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
        var platform = tower.GetPlatform(floor);
        if (!platform) { slot.readyAt = Time.time + 1; return; }
        var kind = (EnemyKind)TowerThemes.ThemeAtFloor(floor);
        var art = EnemyArt.Get(kind);
        var go = new GameObject(NameFor(kind), typeof(SpriteRenderer));
        var sr = go.GetComponent<SpriteRenderer>(); sr.sharedMaterial = material; sr.sortingOrder = 18; sr.sprite = art.frames[0];
        float half = Half(platform);
        var enemy = new Enemy { kind = kind, go = go, sr = sr, art = art, floor = floor, offset = Random.Range(-half + .3f, half - .3f), spawnedAt = Time.time, readyAt = Time.time + .45f };
        slot.enemy = enemy; Place(enemy, StandPos(platform, enemy.offset));
    }

    static string NameFor(EnemyKind kind)
    {
        switch (kind)
        {
            case EnemyKind.CinderCrow: return "Kül Kargası";
            case EnemyKind.CloudMoth: return "Bulut Güvesi";
            case EnemyKind.StormLeech: return "Fırtına Sülüğü";
            case EnemyKind.RootStalker: return "Kök Pusucusu";
            default: return "Yıldız Cini";
        }
    }

    static float Half(MovingPlatform2D platform)
    {
        var collider = platform.GetComponent<Collider2D>(); if (!collider) collider = platform.GetComponentInChildren<Collider2D>();
        return collider ? collider.bounds.extents.x : 1.2f;
    }
    static Vector3 StandPos(MovingPlatform2D platform, float offset) => new Vector3(platform.transform.position.x + offset, platform.Top, 0);
    static void Place(Enemy enemy, Vector3 pos) { enemy.go.transform.position = pos; }

    void Tick(Enemy enemy, float dt, float now)
    {
        float speed = def.enemySpeed;
        var player = tower.player.transform.position;
        float alpha = Mathf.Clamp01((now - enemy.spawnedAt) / .55f);
        enemy.sr.color = new Color(1, 1, 1, alpha);
        if (enemy.state == Enemy.Stand)
        {
            var platform = tower.GetPlatform(enemy.floor); if (!platform) { enemy.dead = true; return; }
            float half = Half(platform), limit = half - .25f;
            float posX = platform.transform.position.x + enemy.offset;
            bool staggered = now < enemy.staggerUntil;
            if (enemy.floor == playerFloor || staggered)
            {
                float want = Mathf.Clamp(player.x - platform.transform.position.x, -limit, limit);
                float dir = Mathf.Abs(want - enemy.offset) < .06f ? 0 : Mathf.Sign(want - enemy.offset);
                if (staggered) dir = -dir;
                float run = RunSpeed(enemy.kind) * speed * (staggered ? 1.3f : 1);
                enemy.offset = Mathf.Clamp(enemy.offset + dir * run * dt, -limit, limit);
                if (dir != 0) enemy.faceRight = staggered ? dir < 0 : dir > 0; else enemy.faceRight = player.x > posX;
            }
            else if (now >= enemy.readyAt)
            {
                int to = enemy.floor < playerFloor ? enemy.floor + 1 : enemy.floor - 1;
                var next = tower.GetPlatform(to);
                if (!next) { enemy.dead = true; return; }
                float toLimit = Half(next) - .25f;
                float toOffset = Mathf.Clamp(player.x - next.transform.position.x, -toLimit, toLimit);
                StartLeap(enemy, to, toOffset, now, LeapDuration(enemy.kind) / speed);
            }
            if (enemy.state == Enemy.Stand)
            {
                var p = tower.GetPlatform(enemy.floor);
                if (p) Place(enemy, StandPos(p, enemy.offset) + Vector3.up * Bob(enemy.kind, now));
            }
        }
        else if (enemy.state == Enemy.Leap) TickLeap(enemy, now);
        else TickLand(enemy, now);
        enemy.sr.flipX = !enemy.faceRight;
        enemy.sr.sprite = enemy.art.frames[FrameFor(enemy, now)];
    }

    static float RunSpeed(EnemyKind kind)
    {
        switch (kind)
        {
            case EnemyKind.CinderCrow: return 1.55f;
            case EnemyKind.CloudMoth: return 1.3f;
            case EnemyKind.StormLeech: return 1.7f;
            case EnemyKind.RootStalker: return 1.15f;
            default: return 1.45f;
        }
    }

    static float LeapDuration(EnemyKind kind) => kind == EnemyKind.RootStalker ? .95f : kind == EnemyKind.StormLeech ? .74f : .88f;
    static float Bob(EnemyKind kind, float now) => kind == EnemyKind.CloudMoth ? Mathf.Abs(Mathf.Sin(now * 7f)) * .12f : kind == EnemyKind.StarWisp ? Mathf.Sin(now * 4f) * .09f : Mathf.Sin(now * 4f) * .025f;

    void StartLeap(Enemy enemy, int to, float toOffset, float now, float duration)
    {
        enemy.state = Enemy.Leap; enemy.t0 = now; enemy.dur = Mathf.Max(.3f, duration); enemy.toFloor = to; enemy.toOffset = toOffset;
        enemy.from = enemy.go.transform.position;
    }

    void TickLeap(Enemy enemy, float now)
    {
        var dest = tower.GetPlatform(enemy.toFloor); if (!dest) { enemy.dead = true; return; }
        Vector3 target = StandPos(dest, enemy.toOffset);
        float s = Mathf.Clamp01((now - enemy.t0) / enemy.dur);
        float arc = enemy.toFloor > enemy.floor ? 1.05f : .55f;
        Vector3 pos = Vector3.Lerp(enemy.from, target, s * s * (3 - 2 * s)); pos.y += Mathf.Sin(Mathf.PI * s) * arc;
        Place(enemy, pos); enemy.faceRight = target.x >= enemy.from.x;
        if (s < 1) return;
        enemy.floor = enemy.toFloor; enemy.offset = enemy.toOffset; enemy.state = Enemy.Land; enemy.t0 = now; enemy.dur = .38f;
        enemy.readyAt = now + .65f / def.enemySpeed;
    }

    void TickLand(Enemy enemy, float now)
    {
        var platform = tower.GetPlatform(enemy.floor); if (!platform) { enemy.dead = true; return; }
        Place(enemy, StandPos(platform, enemy.offset));
        if (now - enemy.t0 >= enemy.dur) enemy.state = Enemy.Stand;
    }

    static int FrameFor(Enemy enemy, float now)
    {
        if (enemy.state == Enemy.Leap) return 2;
        if (enemy.state == Enemy.Land) return 3;
        return Mathf.FloorToInt(now * 5f) % 2;
    }

    void Touch(Enemy enemy, float now)
    {
        var health = PlayerHealth.Instance;
        if (health == null || now < enemy.staggerUntil || now - enemy.spawnedAt < .7f) return;
        var pos = enemy.go.transform.position;
        float centerY = pos.y + enemy.art.height * .45f;
        var p = tower.player.transform.position;
        if (Mathf.Abs(p.x - pos.x) < .56f && Mathf.Abs(p.y - centerY) < .72f && health.Hit()) enemy.staggerUntil = now + 1.15f;
    }

    // The warm warning dots point to creatures just below the camera.
    void UpdateMarkers()
    {
        var cam = Camera.main; int used = 0;
        foreach (var slot in slots)
        {
            if (slot.enemy == null || used >= markers.Length) continue;
            var viewport = cam.WorldToViewportPoint(slot.enemy.go.transform.position);
            if (viewport.y >= .01f) continue;
            var marker = markers[used++]; marker.gameObject.SetActive(true);
            marker.rectTransform.anchorMin = marker.rectTransform.anchorMax = new Vector2(Mathf.Clamp(viewport.x, .05f, .95f), 0);
            marker.rectTransform.anchoredPosition = new Vector2(0, 170);
            marker.color = new Color(1, .46f, .24f, .55f + .4f * Mathf.Sin(Time.time * 8));
        }
        for (int i = used; i < markers.Length; i++) markers[i].gameObject.SetActive(false);
    }

    void OnDestroy() { Clear(); EnemyArt.Release(); if (canvas) Destroy(canvas.gameObject); }
}
