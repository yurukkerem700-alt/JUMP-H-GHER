using UnityEngine;
using TMPro;

public class PlayerController : MonoBehaviour
{
    const float EdgeJumpGrace = .10f;
    public float moveSpeed = 4.8f;
    public float jumpForce = 8f;
    private Rigidbody2D rb;
    private Collider2D oyuncuCollider;
    private float movementX;
    bool airJumpUsed;
    float jumpBufferedUntil = -1;
    private int zeminMask;
    private Vector2 startPosition;
    private CoinSequence coinSequence;
    private MovingPlatform2D standingPlatform;
    private Collider2D supportCollider;
    private float lastGroundContact = -100;
    private float edgeJumpUntil = -1;
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
        if (GameFlow.IsMenu)
        {
            movementX = 0;
            jumpBufferedUntil = edgeJumpUntil = -1;
            GameInput.Clear();
            return;
        }
        movementX = GameInput.Horizontal;
        if (GameInput.ConsumeJump()) jumpBufferedUntil = Time.time + .12f;
        GroundPlatform(out bool onGround);
        bool canAirJump = TowerPowers.Instance && TowerPowers.Instance.DoubleJumpActive && !airJumpUsed;
        if ((Time.time <= jumpBufferedUntil && (onGround || CanEdgeJump || canAirJump)) || (GameInput.JumpHeld && onGround))
        {
            ZiplamayiDene(); jumpBufferedUntil = -1;
        }
        if (Input.GetKeyDown(KeyCode.R)) RestartRun();
        if (rb.position.y < -5.5f) FallToGround();
    }

    public MovingPlatform2D GroundPlatform(out bool grounded)
    {
        if ((!supportCollider || supportCollider.enabled) && Time.fixedTime - lastGroundContact <= .06f && rb.linearVelocity.y <= .15f)
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

    private void OnCollisionEnter2D(Collision2D collision)
    {
        bool wasAirborne = Time.fixedTime - lastGroundContact > .06f;
        UpdateSupport(collision);
        float impactSpeed = Mathf.Abs(collision.relativeVelocity.y);
        if (wasAirborne && supportCollider == collision.collider && lastGroundContact == Time.fixedTime && impactSpeed >= 4f)
        {
            int floor = standingPlatform ? standingPlatform.floorIndex : 0;
            SesAtolyesi.Ses.Impact(SesWorld.Surface(floor), Mathf.Clamp(impactSpeed * .13f, .5f, 1.4f), impactSpeed);
        }
    }
    private void OnCollisionStay2D(Collision2D collision) => UpdateSupport(collision);

    private void UpdateSupport(Collision2D collision)
    {
        if ((zeminMask & (1 << collision.gameObject.layer)) == 0) return;
        foreach (var contact in collision.contacts)
            if (contact.normal.y > .5f)
            {
                lastGroundContact = Time.fixedTime;
                if (rb.linearVelocity.y <= .15f && !GameFlow.IsMenu)
                    edgeJumpUntil = Time.fixedTime + EdgeJumpGrace;
                supportCollider = collision.collider;
                standingPlatform = collision.rigidbody ? collision.rigidbody.GetComponent<MovingPlatform2D>() : null;
                if (standingPlatform) standingPlatform.StartCrumble(this);
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

    bool CanEdgeJump => Time.time <= edgeJumpUntil && rb.linearVelocity.y <= .15f;

    public void ZiplamayiDene()
    {
        GroundPlatform(out bool grounded);
        if (!grounded && !CanEdgeJump)
        {
            if (!TowerPowers.Instance || !TowerPowers.Instance.DoubleJumpActive || airJumpUsed) return;
            airJumpUsed = true;
            SesAtolyesi.Ses.Swoosh(.8f);
        }
        else airJumpUsed = false;
        edgeJumpUntil = -1;
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
    }

    void FixedUpdate()
    {
        var platform = GroundPlatform(out bool grounded);
        if (grounded) airJumpUsed=false;
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
        AwardCoin();
    }

    public void AwardCoin()
    {
        SesAtolyesi.Ses.Coin();
        skor++;
        PlayerProgress.AddCoin();
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
        airJumpUsed = false;
        jumpBufferedUntil = -1;
        edgeJumpUntil = -1;
        supportCollider = null;
        lastGroundContact = -100;
        rb.position = startPosition;
        rb.linearVelocity = Vector2.zero;
    }
}
