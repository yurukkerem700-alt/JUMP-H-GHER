using System.Collections.Generic;
using UnityEngine;

public enum EnemyKind { Zombie, Goblin, Karakoncolos, KaraKurt }

// Tür yardımcıları: Karakoncolos zombinin, Kara Kurt goblinin yerini alır (eski ikisi kodda durur, EnemyManager.UseLegacyEnemies ile açılır).
static class EnemyKinds
{
    public static bool Climber(EnemyKind k) => k == EnemyKind.Zombie || k == EnemyKind.Karakoncolos;
    public static EnemySound Spawn(EnemyKind k) => k == EnemyKind.Karakoncolos ? EnemySound.KarakoncolosSpawn : k == EnemyKind.KaraKurt ? EnemySound.KaraKurtSpawn : Climber(k) ? EnemySound.ZombieSpawn : EnemySound.GoblinSpawn;
    public static EnemySound Attack(EnemyKind k) => k == EnemyKind.Karakoncolos ? EnemySound.KarakoncolosAttack : k == EnemyKind.KaraKurt ? EnemySound.KaraKurtAttack : Climber(k) ? EnemySound.ZombieAttack : EnemySound.GoblinAttack;
    public static EnemySound Hurt(EnemyKind k) => k == EnemyKind.Karakoncolos ? EnemySound.KarakoncolosHurt : k == EnemyKind.KaraKurt ? EnemySound.KaraKurtHurt : Climber(k) ? EnemySound.ZombieHurt : EnemySound.GoblinHurt;
}

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
        string name; float referencePixels, target; // referencePixels: yürüme karesinin sıkı piksel yüksekliği
        switch (kind)
        {
            case EnemyKind.Karakoncolos: name = "Karakoncolos"; referencePixels = 235; target = 2.2f; break;
            case EnemyKind.KaraKurt: name = "KaraKurt"; referencePixels = 164; target = 1.2f; break;
            case EnemyKind.Zombie: name = "Zombie"; referencePixels = 305; target = 2.2f; break;
            default: name = "Goblin"; referencePixels = 229; target = 1.65f; break;
        }
        var texture = Resources.Load<Texture2D>("Enemies/" + name);
        var meta = JsonUtility.FromJson<Meta>(Resources.Load<TextAsset>("Enemies/" + name).text);
        int rows = Mathf.CeilToInt(meta.frames / (float)meta.cols);
        float pixelScale = texture.width / (float)(meta.cols * meta.cellW);
        float cellW = texture.width / (float)meta.cols, cellH = texture.height / (float)rows;
        art = new EnemyArt { frames = new Sprite[meta.frames], height = target };
        for (int i = 0; i < meta.frames; i++)
            art.frames[i] = Sprite.Create(texture, new Rect((i % meta.cols) * cellW, (rows - 1 - i / meta.cols) * cellH, cellW, cellH), new Vector2(.5f, 0), referencePixels * pixelScale / target, 0, SpriteMeshType.FullRect);
        cache[kind] = art; return art;
    }

    public static void Release() { foreach (var a in cache.Values) foreach (var f in a.frames) if (f) Object.Destroy(f); cache.Clear(); }
}

class Enemy
{
    public float attackSoundAt;
    public const int Stand = 0, Leap = 1, Climb = 2, Land = 3, Fall = 4, Attack = 5;
    public EnemyKind kind; public GameObject go; public SpriteRenderer sr; public EnemyArt art;
    public int floor, state, toFloor; public float offset, toOffset, side = 1, t0, dur, readyAt, staggerUntil, spawnedAt, waitSince;
    public Vector3 from; public bool faceRight = true, dead, moving, attackResolved;
    public float attackReadyAt;
    public int hits;
    public bool defeated;
    public float knockDirection;
    public SpriteRenderer claw;
}

class EnemyCoin
{
    public SpriteRenderer sprite;
    public Vector3 from;
    public float offset, spawnedAt;
    public int floor;
}

class EnemySlot { public EnemyKind kind; public Enemy enemy; public float readyAt; }

// Bölümün düşmanlarını doğurur, yönlendirir ve oyuncuya değince can götürür.
// Zombi: basamağın kenarına zıplayıp asılır, tırmanır. Goblin: basamaktan basamağa doğrudan zıplar.
public class EnemyManager : MonoBehaviour
{
    // Eski zombi ve goblini geri getirmek için true yap (kod ve dosyalar silinmedi, sadece devre dışı).
    public static bool UseLegacyEnemies = false;
    static EnemyKind Climbers => UseLegacyEnemies ? EnemyKind.Zombie : EnemyKind.Karakoncolos;
    static EnemyKind Leapers => UseLegacyEnemies ? EnemyKind.Goblin : EnemyKind.KaraKurt;
    const float FirstDelay = 8f, RespawnDelay = 7f, SlotGap = 4f, SpeedScale = .6f, KickCooldown = .5f, KickReach = 1.7f;
    InfiniteTower tower;
    Material material;
    SpriteRenderer coinArt, kickTrail;
    Sprite clawArt, sweepArt, fistArt;
    float kickReadyAt, kickStarted, facing = 1, kickDirection;
    int kickStyle = 3;
    public const float StrikeDuration = .44f;
    bool kickPending;
    readonly List<EnemyCoin> drops = new List<EnemyCoin>();
    static readonly Color[] KickColors = { new Color(1f,.8f,.3f), new Color(.3f,.9f,1f), new Color(.94f,.45f,1f), new Color(1f,.46f,.25f) };
    float Speed => def.enemySpeed * SpeedScale; // tüm düşman hızları bu çarpanla yavaşlatılır
    readonly List<EnemySlot> slots = new List<EnemySlot>();
    LevelDef def; bool active, endless; float startAt; int playerFloor;
    public int AliveCount { get { int n = 0; foreach (var s in slots) if (s.enemy != null) n++; return n; } }
    public int SlotCount => slots.Count;

    public void Init(InfiniteTower t)
    {
        tower = t;
        coinArt = t.coins.coin.GetComponentInChildren<SpriteRenderer>();
        material = coinArt.sharedMaterial;
        clawArt = UiArt.Build(96, 96, 96, (x, y) => SweepAlpha(x, y, .48f, .018f) +
            SweepAlpha(x + .12f, y + .04f, .49f, .015f) + SweepAlpha(x - .12f, y - .04f, .47f, .013f));
        sweepArt = UiArt.Build(96, 96, 96, (x, y) => SweepAlpha(x, y, .52f, .053f));
        fistArt = UiArt.Build(64, 64, 64, (x, y) =>
        {
            float fingers = Mathf.Max(Mathf.Abs(x - .08f) - .25f, Mathf.Abs(y - .07f) - .22f);
            float wrist = Mathf.Max(Mathf.Abs(x + .22f) - .15f, Mathf.Abs(y + .04f) - .15f);
            return Mathf.Clamp01(-Mathf.Min(fingers, wrist) * 64);
        });
        kickTrail = MakeEffect("Tekme izi", sweepArt);
    }

    static float SweepAlpha(float x, float y, float radius, float width)
    {
        float taper = Mathf.Clamp01((.45f - Mathf.Abs(y)) * 6);
        float curve = Mathf.Sqrt(Mathf.Max(0, radius * radius - y * y)) - radius * .63f;
        return Mathf.Clamp01((width * taper - Mathf.Abs(x - curve)) * 95) * taper;
    }

    SpriteRenderer MakeEffect(string label, Sprite sprite)
    {
        var effect = new GameObject(label, typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
        effect.transform.SetParent(transform, false);
        effect.sprite = sprite;
        effect.sharedMaterial = material;
        effect.sortingOrder = 31;
        effect.enabled = false;
        return effect;
    }

    void StartKick(float now)
    {
        if (now < kickReadyAt) return;
        kickReadyAt = now + KickCooldown;
        kickStarted = now;
        kickPending = true;
        kickDirection = facing;
        kickStyle = (kickStyle + 1) % KickColors.Length;
        GameInput.PlayKick(kickStyle, kickDirection);
        SesAtolyesi.Ses.Swoosh(1f);
    }

    void TickKick(float now)
    {
        float elapsed = now - kickStarted;
        kickTrail.enabled = now < kickReadyAt && elapsed < StrikeDuration;
        if (kickTrail.enabled)
        {
            float phase = elapsed / StrikeDuration;
            kickTrail.sprite = kickStyle == 3 ? fistArt : sweepArt;
            var p = tower.player.transform.position;
            float height = kickStyle == 0 ? .03f : kickStyle == 1 ? Mathf.Lerp(-.12f, .62f, phase) : -.18f;
            kickTrail.transform.position = p + new Vector3(kickDirection * Mathf.Lerp(.32f, 1.15f, phase), height, 0);
            kickTrail.transform.localScale = new Vector3(kickStyle == 2 ? 1.65f : 1.1f, 1.2f, 1);
            if (kickStyle == 3) kickTrail.transform.localScale = new Vector3(.68f, .48f, 1);
            float angle = kickStyle == 0 ? -25 : kickStyle == 1 ? 45 : kickStyle == 2 ? -85 : 0;
            kickTrail.transform.rotation = Quaternion.Euler(0, 0, kickDirection * (angle - 40 * phase));
            kickTrail.flipX = kickDirection < 0;
            var color = KickColors[kickStyle];
            color.a = Mathf.Sin(Mathf.PI * phase);
            kickTrail.color = color;
        }
        if (!kickPending || elapsed < .17f) return;
        kickPending = false;
        var pHit = tower.player.transform.position;
        foreach (var slot in slots)
        {
            var e = slot.enemy;
            if (e == null || e.dead || e.state == Enemy.Leap || e.state == Enemy.Fall) continue;
            var pos = e.go.transform.position;
            float dx = pos.x - pHit.x, centerY = pos.y + e.art.height * .45f;
            if (dx * kickDirection < -.2f || Mathf.Abs(dx) > KickReach || Mathf.Abs(pHit.y - centerY) > 1.4f) continue;
            Knock(e, kickDirection, now);
        }
    }

    void Knock(Enemy e, float dir, float now)
    {
        PlayerProgress.AddKick();
        e.staggerUntil = now + 1.8f;
        EnemySfx.Play(EnemyKinds.Hurt(e.kind), 1f);
        e.knockDirection = dir;
        var below = e.floor > 0 ? tower.GetPlatform(e.floor - 1) : null;
        e.hits++;
        e.defeated = e.hits >= 3 || !below;
        e.state = Enemy.Fall;
        e.t0 = now;
        e.dur = .48f;
        e.toFloor = e.defeated ? e.floor : e.floor - 1;
        e.from = e.go.transform.position;
        e.faceRight = dir < 0;
    }

    void TickFall(Enemy e, float now)
    {
        var dest = tower.GetPlatform(e.toFloor);
        float phase = Mathf.Clamp01((now - e.t0) / e.dur);
        float shoveX = e.from.x + e.knockDirection * Mathf.Lerp(0, 2.1f, 1 - (1 - phase) * (1 - phase));
        float y = e.from.y + Mathf.Sin(Mathf.PI * phase) * .62f;
        if (dest)
        {
            float limit = Half(dest) - .25f;
            e.toOffset = Mathf.Clamp(shoveX - dest.transform.position.x, -limit, limit);
            y += (dest.Top - e.from.y) * phase * phase;
            // Travel away first, then meet the lower moving platform.
            shoveX = Mathf.Lerp(shoveX, dest.transform.position.x + e.toOffset, Mathf.InverseLerp(.5f, 1, phase));
        }
        Place(e, new Vector3(shoveX, y, 0));
        if (phase < 1) return;
        if (e.defeated || !dest)
        {
            if (e.defeated) DropCoins(e.go.transform.position, dest, now);
            e.dead = true;
            return;
        }
        e.floor = e.toFloor; e.offset = e.toOffset; e.state = Enemy.Stand; e.waitSince = now;
        e.readyAt = now + 1f / Speed;
        SesWorld.EnemyLand(e.go.transform.position, tower.player.transform.position, e.floor);
    }

    public void Begin(LevelDef level)
    {
        Clear(); def = level; active = level.HasEnemies; startAt = Time.time + FirstDelay; playerFloor = 0;
        for (int i = 0; i < level.zombies; i++) slots.Add(new EnemySlot { kind = Climbers, readyAt = startAt + i * SlotGap });
        for (int i = 0; i < level.goblins; i++) slots.Add(new EnemySlot { kind = Leapers, readyAt = startAt + SlotGap / 2 + i * SlotGap });
    }

    // Sonsuz mod: zombi 10. kattan, goblin 25. kattan sonra gelir; yükseldikçe sayı (en çok 5) ve hız artar.
    public void BeginEndless()
    {
        Clear(); endless = true; active = true; startAt = Time.time + FirstDelay; playerFloor = 0; def = default;
        def.enemySpeed = 1;
    }

    void GrowEndless(float now)
    {
        int floor = Mathf.Max(0, tower.HighestReachedFloor);
        int wantZombies = floor < 10 ? 0 : Mathf.Min(3, 1 + (floor - 10) / 20);
        int wantGoblins = floor < 25 ? 0 : Mathf.Min(2, 1 + (floor - 25) / 30);
        def.enemySpeed = Mathf.Min(1.6f, 1 + floor * .005f);
        int zombies = 0, goblins = 0;
        foreach (var s in slots) { if (EnemyKinds.Climber(s.kind)) zombies++; else goblins++; }
        for (; zombies < wantZombies; zombies++) slots.Add(new EnemySlot { kind = Climbers, readyAt = now + 6f + zombies * SlotGap });
        for (; goblins < wantGoblins; goblins++) slots.Add(new EnemySlot { kind = Leapers, readyAt = now + 8f + goblins * SlotGap });
    }

    void DropCoins(Vector3 point, MovingPlatform2D platform, float now)
    {
        float span = platform ? Mathf.Max(.3f, Half(platform) - .15f) : 1;
        for (int i = 0; i < 5; i++)
        {
            var sprite = MakeEffect("Düşman coini", coinArt.sprite);
            sprite.sortingOrder = 24;
            sprite.color = new Color(1f, .82f, .28f);
            sprite.transform.localScale = Vector3.one * (.28f / Mathf.Max(.01f, coinArt.sprite.bounds.size.x));
            sprite.transform.position = point;
            sprite.enabled = true;
            drops.Add(new EnemyCoin { sprite = sprite, floor = platform ? platform.floorIndex : -1, from = point,
                offset = Mathf.Lerp(-span, span, i / 4f), spawnedAt = now });
        }
    }

    void TickCoins(float now)
    {
        for (int i = drops.Count - 1; i >= 0; i--)
        {
            var coin = drops[i];
            float age = now - coin.spawnedAt;
            float phase = Mathf.Clamp01(age / .48f);
            var platform = tower.GetPlatform(coin.floor);
            var target = platform ? StandPos(platform, coin.offset) : coin.from + Vector3.right * coin.offset;
            target.y += .35f + Mathf.Sin(now * 4 + coin.offset) * .06f;
            var pos = Vector3.Lerp(coin.from, target, phase);
            pos.y += Mathf.Sin(phase * Mathf.PI) * .85f;
            coin.sprite.transform.position = pos;
            bool collect = age >= .25f && (tower.player.transform.position - pos).sqrMagnitude < .48f * .48f;
            if (!collect && age < 24f) continue;
            drops.RemoveAt(i);
            Destroy(coin.sprite.gameObject);
            if (collect) tower.player.AwardCoin();
        }
    }

    public void Clear()
    {
        endless = false;
        foreach (var s in slots) if (s.enemy != null && s.enemy.go) Destroy(s.enemy.go);
        slots.Clear(); active = false;
        foreach (var coin in drops) if (coin.sprite) Destroy(coin.sprite.gameObject);
        drops.Clear();
        kickPending = false;
        kickReadyAt = 0;
        if (kickTrail) kickTrail.enabled = false;
    }

    void Update()
    {
        var run = LevelRun.Instance;
        bool pressed = GameInput.ConsumeKick();
        if (!run || !run.CombatPlaying || GameFlow.IsMenu)
        {
            kickPending = false;
            if (kickTrail) kickTrail.enabled = false;
            return;
        }
        float now = Time.time, dt = Time.deltaTime;
        float movement = GameInput.Horizontal;
        if (Mathf.Abs(movement) > .1f) facing = Mathf.Sign(movement);
        if (pressed) StartKick(now);
        TickKick(now);
        TickCoins(now);
        if (!active) return;
        if (endless) GrowEndless(now);
        var support = tower.player.GroundPlatform(out bool grounded);
        if (grounded && support) playerFloor = support.floorIndex;
        for (int i = 0; i < slots.Count; i++)
        {
            var slot = slots[i];
            if (slot.enemy == null)
            {
                if (now >= slot.readyAt && (playerFloor >= 3 || def.kind == GoalKind.Survive)) Spawn(slot);
                continue;
            }
            var e = slot.enemy;
            Tick(e, dt, now);
            if (slots.Count == 0) return;
            if (!e.dead && !e.defeated && (e.floor < playerFloor - 9 || e.floor > playerFloor + 7)) e.dead = true;
            if (e.dead) { Destroy(e.go); slot.enemy = null; slot.readyAt = now + RespawnDelay; continue; }
            Touch(e, now);
            if (slots.Count == 0) return; // darbe son canı aldıysa liste temizlenmiş olabilir
        }
    }

    void Spawn(EnemySlot slot)
    {
        int floor = Mathf.Max(0, playerFloor - Random.Range(3, 5));
        var plat = tower.GetPlatform(floor);
        if (!plat) { slot.readyAt = Time.time + 1; return; }
        var art = EnemyArt.Get(slot.kind);
        var go = new GameObject(slot.kind.ToString(), typeof(SpriteRenderer));
        var sr = go.GetComponent<SpriteRenderer>(); sr.sharedMaterial = material; sr.sortingOrder = 18; sr.sprite = art.frames[IdleFrame(slot.kind)];
        float half = Half(plat);
        var e = new Enemy { kind = slot.kind, go = go, sr = sr, art = art, floor = floor, offset = Random.Range(-half + .3f, half - .3f), spawnedAt = Time.time, readyAt = Time.time + .4f, waitSince = Time.time };
        e.claw = MakeEffect("Pençe izi", clawArt);
        e.claw.transform.SetParent(go.transform, false);
        slot.enemy = e; Place(e, StandPos(plat, e.offset));
        EnemySfx.Play(EnemyKinds.Spawn(slot.kind), 1f);
    }

    static float Half(MovingPlatform2D plat)
    {
        var c = plat.GetComponent<Collider2D>(); if (!c) c = plat.GetComponentInChildren<Collider2D>();
        return c ? c.bounds.extents.x : 1.2f;
    }
    static Vector3 StandPos(MovingPlatform2D plat, float offset) => new Vector3(plat.transform.position.x + offset, plat.Top, 0);
    static Vector3 HangPos(MovingPlatform2D plat, float side, float height) => new Vector3(plat.transform.position.x + side * (Half(plat) + height * .30f), plat.Top - height * .94f, 0);
    static void Place(Enemy e, Vector3 pos) { e.go.transform.position = pos; }

    void Tick(Enemy e, float dt, float now)
    {
        float speed = Speed;
        var player = tower.player.transform.position;
        bool zombie = EnemyKinds.Climber(e.kind);
        e.moving=false;
        float alpha = Mathf.Clamp01((now - e.spawnedAt) / .6f);
        float pulse = .5f + .5f * Mathf.Sin(now * 6f);
        e.sr.color = new Color(1f, 1f - .35f * pulse, 1f - .35f * pulse, alpha);
        if (e.state == Enemy.Stand)
        {
            var plat = tower.GetPlatform(e.floor); if (!plat) { e.dead = true; return; }
            float half = Half(plat), limit = half - .25f;
            bool staggered = now < e.staggerUntil;
            float posX = plat.transform.position.x + e.offset;
            float previousOffset=e.offset;
            if (e.floor == playerFloor || staggered)
            {
                float want = Mathf.Clamp(player.x - plat.transform.position.x, -limit, limit);
                float dir = Mathf.Abs(want - e.offset) < .06f ? 0 : Mathf.Sign(want - e.offset);
                if (staggered) dir = -dir; // darbeden sonra geri çekilir
                float run = (zombie ? 2.0f : 3.4f) * speed * (staggered ? 1.3f : 1);
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
                    e.offset = Mathf.Clamp(e.offset + dir * 2.0f * speed * dt, -limit, limit);
                    if (dir != 0) e.faceRight = dir > 0;
                    if (Mathf.Abs(hangX - (plat.transform.position.x + e.offset)) < 3.2f || now - e.waitSince > 1.5f) StartLeap(e, to, side, 0, now, .55f / speed);
                }
                else
                {
                    float toLimit = Half(next) - .25f;
                    float toOffset = Mathf.Clamp(player.x - next.transform.position.x, -toLimit, toLimit);
                    StartLeap(e, to, 0, toOffset, now, (zombie ? .5f : .6f) / speed);
                }
            }
            e.moving=Mathf.Abs(e.offset-previousOffset)>.001f;
            if (e.state == Enemy.Stand) { var p = tower.GetPlatform(e.floor); if (p) Place(e, StandPos(p, e.offset)); }
        }
        else if (e.state == Enemy.Leap) TickLeap(e, now);
        else if (e.state == Enemy.Climb) TickClimb(e, now);
        else if (e.state == Enemy.Fall) TickFall(e, now);
        else if (e.state == Enemy.Attack) TickAttack(e, now);
        else TickLand(e, now);
        e.sr.flipX = !e.faceRight;
        float attackPhase = e.state == Enemy.Attack ? Mathf.Clamp01((now - e.t0) / e.dur) : 0;
        float swing = Mathf.Sin(Mathf.PI * Mathf.InverseLerp(.3f, .75f, attackPhase));
        float windup = Mathf.Sin(Mathf.PI * Mathf.InverseLerp(0, .43f, attackPhase));
        float direction = e.faceRight ? 1 : -1;
        float lean = e.state == Enemy.Fall ? -direction * 65 : direction * (windup * 16 - swing * 34);
        float stretch = e.state == Enemy.Leap ? 1.06f : 1 - .16f * windup + .12f * swing;
        e.claw.enabled = swing > .015f;
        if (e.claw.enabled)
        {
            e.claw.transform.localPosition = new Vector3(direction * (.32f + swing * .36f), e.art.height * .55f, 0);
            e.claw.transform.localScale = Vector3.one * (zombie ? 1.55f : 1.08f);
            e.claw.transform.localRotation = Quaternion.Euler(0, 0, direction * Mathf.Lerp(55, -60, Mathf.InverseLerp(.3f, .75f, attackPhase)));
            e.claw.flipX = !e.faceRight;
            e.claw.color = zombie ? new Color(1f,.22f,.16f,swing) : new Color(1f,.68f,.25f,swing);
        }
        e.go.transform.rotation = Quaternion.Euler(0, 0, lean);
        e.go.transform.localScale = new Vector3(1f / stretch, stretch, 1);
        e.sr.sprite = e.art.frames[FrameFor(e, now)];
    }

    static float Bob(Enemy e, float now) => !EnemyKinds.Climber(e.kind) ? Mathf.Abs(Mathf.Sin(now * 11f)) * .06f : Mathf.Sin(now * 4f) * .025f;

    void StartLeap(Enemy e, int to, float side, float toOffset, float now, float duration)
    {
        e.state = Enemy.Leap; e.t0 = now; e.dur = Mathf.Max(.25f, duration); e.toFloor = to; e.side = side; e.toOffset = toOffset;
        e.from = e.go.transform.position;
    }

    void TickLeap(Enemy e, float now)
    {
        var dest = tower.GetPlatform(e.toFloor); if (!dest) { e.dead = true; return; }
        bool hang = EnemyKinds.Climber(e.kind) && e.toFloor > e.floor;
        Vector3 target = hang ? HangPos(dest, e.side, e.art.height) : StandPos(dest, e.toOffset);
        float s = Mathf.Clamp01((now - e.t0) / e.dur);
        float arc = hang ? .55f : (e.toFloor > e.floor ? 1.15f : .6f);
        Vector3 pos = Vector3.Lerp(e.from, target, s * s * (3 - 2 * s)); pos.y += Mathf.Sin(Mathf.PI * s) * arc;
        Place(e, pos);
        e.faceRight = hang ? e.side < 0 : target.x >= e.from.x;
        if (s < 1) return;
        e.floor = e.toFloor;
        if (hang) { e.state = Enemy.Climb; e.t0 = now; e.dur = 1.0f / Speed; }
        else
        {
            SesWorld.EnemyLand(e.go.transform.position, tower.player.transform.position, e.toFloor); e.offset = e.toOffset; e.state = Enemy.Land; e.t0 = now; e.dur = .4f;
            e.readyAt = now + (!EnemyKinds.Climber(e.kind) ? .5f : .35f) / Speed;
        }
    }

    void TickClimb(Enemy e, float now)
    {
        var plat = tower.GetPlatform(e.floor); if (!plat) { e.dead = true; return; }
        float s = Mathf.Clamp01((now - e.t0) / e.dur), half = Half(plat);
        var hang=HangPos(plat,e.side,e.art.height);
        // First hold the ledge, then raise the body before moving over the lip.
        float lift=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.25f,.85f,s));
        float across=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.6f,1,s));
        var top=StandPos(plat,e.side*(half-.45f));
        Place(e,new Vector3(Mathf.Lerp(hang.x,top.x,across),Mathf.Lerp(hang.y,top.y,lift),0)); e.faceRight = e.side < 0;
        if (s < 1) return;
        e.offset = e.side * (half - .45f); e.state = Enemy.Land; e.t0=now; e.dur=.25f; e.readyAt = now + .3f / Speed; e.waitSince = now;
    }

    void TickLand(Enemy e, float now)
    {
        var plat = tower.GetPlatform(e.floor); if (!plat) { e.dead = true; return; }
        Place(e, StandPos(plat, e.offset));
        if (now - e.t0 >= e.dur) { e.state = Enemy.Stand; e.waitSince = now; }
    }

    static readonly int[] ClimbFrames = { 14, 15, 8, 9 }, WalkFrames = { 20, 21, 22, 21 };

    // Kare seçimi: sprite sayfasının kare numaraları EnemyBake çıktısına göredir.
    static int IdleFrame(EnemyKind k) => k == EnemyKind.Zombie ? 21 : k == EnemyKind.Goblin ? 22 : 0;

    // Yeni türlerin 8 karelik şeridi: 0-3 yürüyüş/koşu, 4 çömelme, 5 zıplama, 6-7 tırmanış (Karakoncolos) / havada (Kara Kurt).
    static int FrameForNew(Enemy e, float now)
    {
        bool climber = e.kind == EnemyKind.Karakoncolos;
        if (e.state == Enemy.Fall) return climber ? 5 : 7;
        if (e.state == Enemy.Leap) return climber ? 5 : (Mathf.Clamp01((now - e.t0) / e.dur) < .5f ? 6 : 7);
        if (e.state == Enemy.Climb) return (now-e.t0)/e.dur < .55f ? 6 : 7;
        if (e.state == Enemy.Attack) {float s=(now-e.t0)/e.dur; return s<.4f?4:s<.7f?5:0;}
        if (e.state == Enemy.Land) return Mathf.Clamp01((now - e.t0) / e.dur) < .35f ? 4 : 0;
        return e.moving ? Mathf.FloorToInt(now * (climber ? 4 : 9)) % 4 : 0;
    }

    static int FrameFor(Enemy e, float now)
    {
        if (e.kind == EnemyKind.Karakoncolos || e.kind == EnemyKind.KaraKurt) return FrameForNew(e, now);
        if (e.state == Enemy.Attack) return (now-e.t0)/e.dur<.4f ? IdleFrame(e.kind) : e.kind==EnemyKind.Zombie?14:5;
        if (e.state == Enemy.Fall) return e.kind == EnemyKind.Zombie ? 13 : 5; // havada savrulma karesi
        if (e.kind == EnemyKind.Zombie)
        {
            if (e.state == Enemy.Leap) return 10 + Mathf.Min(3, Mathf.FloorToInt(Mathf.Clamp01((now - e.t0) / e.dur) * 4));
            if (e.state == Enemy.Climb) return ClimbFrames[Mathf.FloorToInt(now * 6) % 4];
            return WalkFrames[Mathf.FloorToInt(now * 4) % 4];
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
        if (health == null || e.state!=Enemy.Stand || now<e.attackReadyAt || now < e.staggerUntil || now - e.spawnedAt < .7f) return;
        bool zombie = EnemyKinds.Climber(e.kind);
        var pos = e.go.transform.position;
        float centerY = pos.y + e.art.height * (zombie && e.state == Enemy.Climb ? .6f : .45f);
        var p = tower.player.transform.position;
        float scale = e.art.height / (zombie ? 1.35f : 1f);
        var attackSound = EnemyKinds.Attack(e.kind);
        float dx = Mathf.Abs(p.x - pos.x), dy = Mathf.Abs(p.y - centerY);
        // Yaklaşırken saldırı narası (yakın ve aynı hizada), arada bekleme ile.
        if (dx < 2.2f && dy < 1.4f * scale && now >= e.attackSoundAt) { e.attackSoundAt = now + 1.6f; EnemySfx.Play(attackSound, .8f); }
        if (dx < (zombie ? .55f : .6f) * scale && dy < (zombie ? .85f : .7f) * scale)
        { e.state=Enemy.Attack; e.t0=now; e.dur=.75f; e.attackResolved=false; e.faceRight=p.x>=pos.x; }
    }

    void TickAttack(Enemy e,float now)
    {
        var plat=tower.GetPlatform(e.floor); if(!plat){e.dead=true;return;}
        float phase=Mathf.Clamp01((now-e.t0)/e.dur);
        var strikePos = StandPos(plat, e.offset);
        float lunge = Mathf.Sin(Mathf.PI * Mathf.InverseLerp(.28f, .78f, phase));
        strikePos.x += (e.faceRight ? 1 : -1) * lunge * (EnemyKinds.Climber(e.kind) ? .28f : .44f);
        Place(e, strikePos);
        if(phase>=.45f && !e.attackResolved)
        {
            e.attackResolved=true;
            var p=tower.player.transform.position; var pos=e.go.transform.position;
            float scale=e.art.height/(EnemyKinds.Climber(e.kind)?1.35f:1);
            bool inFront=(p.x-StandPos(plat,e.offset).x)*(e.faceRight?1:-1)>=-.15f;
            if(inFront && Mathf.Abs(p.x-pos.x)<.65f*scale && Mathf.Abs(p.y-(pos.y+e.art.height*.45f))<.8f*scale)
                PlayerHealth.Instance?.Hit();
        }
        if(phase>=1){e.state=Enemy.Stand;e.attackReadyAt=now+.65f;e.waitSince=now;}
    }

    void OnDestroy() { Clear(); EnemyArt.Release(); EnemySfx.Release(); if (clawArt) { Destroy(clawArt.texture); Destroy(clawArt); } if (sweepArt) { Destroy(sweepArt.texture); Destroy(sweepArt); } if (fistArt) { Destroy(fistArt.texture); Destroy(fistArt); } }
}
