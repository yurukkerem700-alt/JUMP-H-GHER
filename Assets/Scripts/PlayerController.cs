using UnityEngine;
using TMPro;

public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 4.8f;
    public float jumpForce = 8f;
    private Rigidbody2D rb;
    private Collider2D oyuncuCollider;
    private float movementX;
    bool airJumpUsed;
    private int zeminMask;
    private Vector2 startPosition;
    private CoinSequence coinSequence;
    private MovingPlatform2D standingPlatform;
    private Collider2D supportCollider;
    private float lastGroundContact = -100;

    // --- Oyun hissi ayarları ---
    const float CoyoteTime = .10f;        // Kenardan ayrıldıktan sonra bu kadar süre hâlâ yerden zıplanabilir.
    const float JumpBufferTime = .12f;    // Yere inmeden hemen önce basılan zıplama, inince otomatik uygulanır.
    const float FallGravityMultiplier = 1.4f; // Düşüş yükselişten daha tok ve hızlı.
    const float ApexGravityMultiplier = .75f; // Zıplamanın tepesinde kısa bir süzülme: konmak kolaylaşır.
    const float ApexVelocity = 1.1f;
    const float MaxFallSpeed = 20f;
    const float ShortHopVelocity = 7.1f;  // Tuş erken bırakılırsa yükseliş hızı en az bu kadar kalır (kat aralığı aşılır).
    private float baseGravity = 2f;
    private float lastGroundedTime = -100;
    private float jumpBufferedUntil = -100;
    private bool jumpRising;
    public event System.Action Jumped;
#if UNITY_EDITOR
    public static bool VerificationControl;
    public static float VerificationMovement;
#endif
    [Header("Skor Sistemi")]
    public TextMeshProUGUI skorYazisi;
    private int skor;
    public int Score => skor;

    SesAtolyesi.SesFootsteps footsteps; float stepAcc; InfiniteTower sesTower;

    void Start()
    {
        SesWorld.Ensure();
        footsteps = gameObject.AddComponent<SesAtolyesi.SesFootsteps>();
        footsteps.autoStride = false; footsteps.useDefaultOnly = true; footsteps.weight = .9f;
        sesTower = FindFirstObjectByType<InfiniteTower>();
        rb = GetComponent<Rigidbody2D>();
        oyuncuCollider = GetComponent<Collider2D>();
        baseGravity = rb.gravityScale;
        zeminMask = LayerMask.GetMask("Zemin");
        startPosition = rb.position;
        coinSequence = FindFirstObjectByType<CoinSequence>();
        SkoruGuncelle();
    }

    void Update()
    {
#if UNITY_EDITOR
        if (VerificationControl) { movementX = VerificationMovement; return; }
#endif
        if (GameFlow.IsMenu) {movementX=0;GameInput.Clear();return;}
        movementX = GameInput.Horizontal;
        if (GameInput.ConsumeJump()) { if (!ZiplamayiDene()) jumpBufferedUntil = Time.time + JumpBufferTime; }
        else if (GameInput.JumpHeld) { GroundPlatform(out bool onGround); if (onGround) ZiplamayiDene(); }
        if (Input.GetKeyDown(KeyCode.R)) RestartRun();
        if (rb.position.y < -5.5f) FallToGround();
    }

    public MovingPlatform2D GroundPlatform(out bool grounded)
    {
        if (Time.fixedTime - lastGroundContact <= .06f && rb.linearVelocity.y <= .15f)
        {
            grounded = true;
            return standingPlatform;
        }
        var bounds = oyuncuCollider.bounds;
        var hit = Physics2D.BoxCast(new Vector2(bounds.center.x, bounds.min.y + .065f),
            new Vector2(bounds.size.x * .72f, .06f), 0, Vector2.down, .14f, zeminMask);
        grounded = hit.collider != null && hit.normal.y > .5f && rb.linearVelocity.y <= .15f;
        return grounded && hit.rigidbody ? hit.rigidbody.GetComponent<MovingPlatform2D>() : null;
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if ((zeminMask & (1 << collision.gameObject.layer)) == 0) return;
        foreach (var contact in collision.contacts)
            if (contact.normal.y > .5f)
            {
                lastGroundContact = Time.fixedTime;
                supportCollider = collision.collider;
                standingPlatform = collision.rigidbody ? collision.rigidbody.GetComponent<MovingPlatform2D>() : null;
                break;
            }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.collider != supportCollider) return;
        standingPlatform = null;
        supportCollider = null;
        lastGroundContact = -100;
    }

    // Zıplama gerçekleştiyse true döner. Yerde, ya da kenardan yeni ayrılmışken (coyote) yer zıplaması; yoksa güç varsa havada zıplama.
    public bool ZiplamayiDene()
    {
        GroundPlatform(out bool grounded);
        bool coyote = !grounded && Time.time - lastGroundedTime <= CoyoteTime && rb.linearVelocity.y <= .15f;
        if (!grounded && !coyote)
        {
            if (!TowerPowers.Instance || !TowerPowers.Instance.DoubleJumpActive || airJumpUsed) return false;
            airJumpUsed = true;
            SesAtolyesi.Ses.Swoosh(.8f);
        }
        else airJumpUsed = false;
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        lastGroundedTime = -100; jumpBufferedUntil = -100; jumpRising = true;
        Jumped?.Invoke();
        return true;
    }

    // Değişken zıplama yüksekliği, tok düşüş, tepe süzülmesi ve düşüş hızı sınırı.
    void ApplyGameFeel(bool grounded)
    {
        float vy = rb.linearVelocity.y;
        if (jumpRising)
        {
            if (vy <= 0) jumpRising = false;
            else if (!GameInput.JumpButtonDown)
            {
                jumpRising = false;
                if (vy > ShortHopVelocity) rb.linearVelocity = new Vector2(rb.linearVelocity.x, ShortHopVelocity);
                vy = rb.linearVelocity.y;
            }
        }
        float scale = baseGravity;
        if (!grounded)
        {
            if (vy < -.1f) scale *= FallGravityMultiplier;
            else if (Mathf.Abs(vy) < ApexVelocity) scale *= ApexGravityMultiplier;
        }
        rb.gravityScale = scale;
        if (vy < -MaxFallSpeed) rb.linearVelocity = new Vector2(rb.linearVelocity.x, -MaxFallSpeed);
    }

    void FixedUpdate()
    {
        var platform = GroundPlatform(out bool grounded);
        if (grounded) { airJumpUsed = false; lastGroundedTime = Time.time; jumpRising = false; }
        // Zıplama tamponu: yere değdiği anda bekleyen zıplama uygulanır.
        if (grounded && Time.time <= jumpBufferedUntil) { jumpBufferedUntil = -100; ZiplamayiDene(); }
        ApplyGameFeel(grounded);
        footsteps.grounded = grounded;
        footsteps.defaultSurface = SesWorld.Surface(sesTower ? sesTower.HighestReachedFloor : 0);
        if (grounded && Mathf.Abs(movementX) > .1f)
        {
            stepAcc += Mathf.Abs(movementX) * moveSpeed * Time.fixedDeltaTime;
            if (stepAcc >= .9f) { stepAcc -= .9f; footsteps.Step(); }
        }
        if (TowerPowers.Instance && TowerPowers.Instance.IsInWind(rb.position))
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, Mathf.MoveTowards(rb.linearVelocity.y, 6f, 35f * Time.fixedDeltaTime));
        float carry = grounded && platform ? platform.CurrentVelocity.x : 0;
        float vy = rb.linearVelocity.y;
        if (!grounded && GameInput.DownHeld && vy < 2f) vy = Mathf.Min(vy, -9f); // joystick aşağı: hızlı iniş
        rb.linearVelocity = new Vector2(movementX * moveSpeed + carry, vy);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        CollectCoin(collision);
    }

    public void CollectCoin(Collider2D collision)
    {
        if (!collision.CompareTag("altin")) return;
        if (coinSequence && !coinSequence.TryCollect(collision)) return;
        SesAtolyesi.Ses.Coin();
        skor++;
        SkoruGuncelle();
    }

    private void SkoruGuncelle()
    {
        if (skorYazisi) skorYazisi.text = "COIN  " + skor;
    }

    public void RestartRun()
    {
        if (TowerPowers.Instance) TowerPowers.Instance.ResetPowers();
        skor = 0;
        SkoruGuncelle();
        var tower = FindFirstObjectByType<InfiniteTower>();
        if (tower) tower.ResetTower();
        if (coinSequence) coinSequence.ResetSequence();
        if (PlayerHealth.Instance) PlayerHealth.Instance.Refill();
        if (LevelRun.Instance) LevelRun.Instance.OnRunRestarted();
        Respawn();
    }

    // Zemine kadar düşmek 1 can götürür ve başlangıca döndürür.
    void FallToGround()
    {
        SesAtolyesi.Ses.Impact(SesWorld.Surface(sesTower ? sesTower.HighestReachedFloor : 0), 2f, 9f);
        Respawn();
        if (PlayerHealth.Instance) PlayerHealth.Instance.Hit(true);
    }

    private void Respawn()
    {
        standingPlatform = null;
        supportCollider = null;
        lastGroundContact = -100;
        lastGroundedTime = -100; jumpBufferedUntil = -100; jumpRising = false;
        rb.position = startPosition;
        rb.linearVelocity = Vector2.zero;
    }
}
