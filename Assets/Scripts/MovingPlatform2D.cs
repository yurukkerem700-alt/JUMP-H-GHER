using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-100)]
[RequireComponent(typeof(Rigidbody2D))]
public class MovingPlatform2D : MonoBehaviour
{
    public float amplitude = 2.9f;
    public float frequency = .65f;
    public float phase;
    public float topOffset = .17f;
    public SpriteRenderer centerLight;
    public int floorIndex;
    private Rigidbody2D rb;
    private Vector2 origin;
    const float FirstChips = .42f, MoreChips = .73f, RebuildDelay = 2.25f;
    static readonly Color Stone = new Color(.52f, .32f, .19f);
    static readonly Color Dust = new Color(.85f, .71f, .51f, .72f);
    static Sprite shardArt, dustArt;
    readonly List<SpriteRenderer> cracks = new List<SpriteRenderer>();
    readonly List<Fragment> fragments = new List<Fragment>();
    SpriteRenderer[] surfaces;
    Collider2D[] solidColliders;
    PlayerController rider;
    Material fragmentMaterial;
    float crumbleAt, rebuildAt, halfWidth;
    int crumbleStage;

    struct Fragment
    {
        public SpriteRenderer sprite;
        public Vector3 origin, velocity;
        public Color color;
        public float started, lifetime, size, spin;
        public bool dust;
    }
    public Vector2 CurrentVelocity { get; private set; }
    public float Top => transform.position.y + topOffset;

    void Awake() { rb = GetComponent<Rigidbody2D>(); origin = rb.position; }

    public void Configure(int floor, float surfaceY)
    {
        if (!rb) rb = GetComponent<Rigidbody2D>();
        floorIndex = floor;
        origin = new Vector2(0, surfaceY - topOffset);

        phase = floor % 2 == 0 ? 0 : Mathf.PI;
        amplitude = Mathf.Clamp(GameDeviceLayout.HorizontalLimit - 1.3f, .35f, 2.9f);
        Vector2 initial = origin + Vector2.right * (amplitude * Mathf.Sin(Time.fixedTime * frequency + phase));
        rb.position = initial; transform.position = initial;
        CurrentVelocity = Vector2.right * (amplitude * frequency * Mathf.Cos(Time.fixedTime * frequency + phase));
    }

    void FixedUpdate()
    {
        Vector2 next = origin + Vector2.right * (amplitude * Mathf.Sin(Time.fixedTime * frequency + phase));
        CurrentVelocity = Vector2.right * (amplitude * frequency * Mathf.Cos(Time.fixedTime * frequency + phase));
        rb.MovePosition(next);
    }

    void Update()
    {
        if (centerLight) centerLight.color = new Color(.85f, .13f, .06f, .45f + Mathf.Sin(Time.time * 2) * .12f);
        if (crumbleStage == 0) return;
        if (!LevelRun.Instance || !LevelRun.Instance.CrumblingPlatforms)
        {
            ResetCrumble();
            return;
        }

        if (GameFlow.IsMenu) return;
        if (crumbleStage < 4 && !HasRider())
        {
            ResetCrumble();
            return;
        }

        float progress = (Time.time - crumbleAt) / LevelRun.Instance.PlatformCrumbleSeconds;
        if (crumbleStage == 1 && progress >= FirstChips)
        {
            crumbleStage = 2;
            AddCracks(2, .2f);
            Scatter(4, false, false);
        }
        if (crumbleStage == 2 && progress >= MoreChips)
        {
            crumbleStage = 3;
            AddCracks(3, .29f);
            Scatter(9, false, false);
        }
        if (crumbleStage == 3 && progress >= 1) Explode();
        if (crumbleStage == 4 && Time.time >= rebuildAt)
        {
            ResetCrumble();
            return;
        }
        TickFragments();
    }

    public void StartCrumble(PlayerController player)
    {
        if (crumbleStage != 0 || GameFlow.IsMenu || !LevelRun.Instance || !LevelRun.Instance.CrumblingPlatforms) return;
        var solid = new List<Collider2D>();
        foreach (var collider in GetComponentsInChildren<Collider2D>())
            if (collider.enabled && !collider.isTrigger) solid.Add(collider);
        if (solid.Count == 0) return;

        solidColliders = solid.ToArray();
        surfaces = GetComponentsInChildren<SpriteRenderer>();
        fragmentMaterial = surfaces.Length > 0 ? surfaces[0].sharedMaterial : null;
        halfWidth = Mathf.Max(.25f, solid[0].bounds.extents.x);
        rider = player;
        crumbleAt = Time.time;
        crumbleStage = 1;
        AddCracks(2, .14f);
    }

    void AddCracks(int count, float depth)
    {
        for (int i = 0; i < count; i++)
        {
            float x = Random.Range(-halfWidth * .86f, halfWidth * .86f);
            var from = new Vector3(x, topOffset - .02f, 0);
            for (int segment = 0; segment < 3; segment++)
            {
                var to = from + new Vector3(Random.Range(-.1f, .1f), -depth / 3, 0);
                var mark = DebrisSprite("Çatlak", new Color(.12f, .065f, .036f), 19);
                mark.transform.localPosition = (from + to) * .5f;
                mark.transform.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(to.y - from.y, to.x - from.x) * Mathf.Rad2Deg);
                mark.transform.localScale = new Vector3(Vector3.Distance(from, to) + .025f, .035f, 1);
                cracks.Add(mark);
                from = to;
            }
        }
    }

    SpriteRenderer DebrisSprite(string label, Color color, int order)
    {
        var sprite = new GameObject(label, typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
        sprite.transform.SetParent(transform, false);
        sprite.sprite = UiArt.Disc;
        if (fragmentMaterial) sprite.sharedMaterial = fragmentMaterial;
        sprite.sortingOrder = order;
        sprite.color = color;
        return sprite;
    }

    void Scatter(int count, bool burst, bool dust)
    {
        if (!shardArt) shardArt = UiArt.Build(32, 32, 32, (u, v) =>
            Mathf.Clamp01((.43f - Mathf.Max(Mathf.Abs(u + v * .42f), Mathf.Abs(v - u * .25f))) * 28));
        if (!dustArt) dustArt = UiArt.Build(32, 32, 32, (u, v) =>
            Mathf.Pow(Mathf.Clamp01(1 - 4 * (u * u + v * v)), 2));
        for (int i = 0; i < count; i++)
        {
            float x = Random.Range(-halfWidth, halfWidth);
            var tint = dust ? Dust : Color.Lerp(Stone, Dust, Random.Range(0, .48f));
            var sprite = DebrisSprite(dust ? "Toz" : "Taş parçası", tint, dust ? 21 : 19);
            sprite.sprite = dust ? dustArt : shardArt;
            var position = new Vector3(transform.position.x + x, Top - .12f, 0);
            sprite.transform.position = position;
            fragments.Add(new Fragment
            {
                sprite = sprite, origin = position, color = tint, started = Time.time, dust = dust,
                lifetime = dust ? Random.Range(.65f, 1.2f) : Random.Range(.4f, .92f),
                size = dust ? Random.Range(.28f, .6f) : Random.Range(.06f, burst ? .25f : .16f),
                velocity = new Vector3(x * (burst ? 2 : .4f) + CurrentVelocity.x * .5f, burst ? Random.Range(.6f, 2.6f) : -.45f, 0),
                spin = Random.Range(-360f, 360f)
            });
        }
    }

    void TickFragments()
    {
        for (int i = fragments.Count - 1; i >= 0; i--)
        {
            var piece = fragments[i];
            float age = Time.time - piece.started;
            if (age >= piece.lifetime)
            {
                Destroy(piece.sprite.gameObject);
                fragments.RemoveAt(i);
                continue;
            }
            float phase = age / piece.lifetime;
            piece.sprite.transform.position = piece.origin + piece.velocity * age + Vector3.down * (piece.dust ? .55f : 7f) * age * age;
            piece.sprite.transform.localRotation = Quaternion.Euler(0, 0, piece.spin * age);
            float scale = piece.size * (piece.dust ? Mathf.Lerp(.65f, 2.8f, phase) : 1);
            piece.sprite.transform.localScale = new Vector3(scale, scale * (piece.dust ? .7f : .58f), 1);
            var color = piece.color;
            color.a *= 1 - Mathf.SmoothStep(0, 1, phase);
            piece.sprite.color = color;
        }
    }

    bool HasRider() => rider && rider.GroundPlatform(out bool grounded) == this && grounded;

    void Explode()
    {
        crumbleStage = 4;
        rebuildAt = Time.time + RebuildDelay;
        foreach (var collider in solidColliders) collider.enabled = false;
        foreach (var sprite in surfaces) sprite.forceRenderingOff = true;
        foreach (var crack in cracks) Destroy(crack.gameObject);
        cracks.Clear();
        Scatter(18, true, false);
        Scatter(16, true, true);
        SesAtolyesi.Ses.Impact(SesWorld.Surface(floorIndex), 2.3f, 9f);
        PlayerHealth.Instance?.Hit(true);
    }

    public void ResetCrumble()
    {
        if (crumbleStage == 0) return;
        if (crumbleStage == 4)
        {
            foreach (var collider in solidColliders) if (collider) collider.enabled = true;
            foreach (var sprite in surfaces) if (sprite) sprite.forceRenderingOff = false;
        }
        foreach (var crack in cracks) if (crack) Destroy(crack.gameObject);
        foreach (var piece in fragments) if (piece.sprite) Destroy(piece.sprite.gameObject);
        cracks.Clear();
        fragments.Clear();
        rider = null;
        crumbleStage = 0;
    }

    void OnDisable() => ResetCrumble();
}
