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
        if (GameFlow.IsMenu) {movementX=0;GameInput.Clear();return;}
        movementX = GameInput.Horizontal;
        if (GameInput.ConsumeJump()) ZiplamayiDene();
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

    public void ZiplamayiDene()
    {
        GroundPlatform(out bool grounded);
        if (!grounded)
        {
            if (!TowerPowers.Instance || !TowerPowers.Instance.DoubleJumpActive || airJumpUsed) return;
            airJumpUsed = true;
            SesAtolyesi.Ses.Swoosh(.8f);
        }
        else airJumpUsed = false;
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
        rb.linearVelocity = new Vector2(movementX * moveSpeed + carry, rb.linearVelocity.y);
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
        Respawn();
        if (PlayerHealth.Instance) PlayerHealth.Instance.Hit(true);
    }

    private void Respawn()
    {
        standingPlatform = null;
        supportCollider = null;
        lastGroundContact = -100;
        rb.position = startPosition;
        rb.linearVelocity = Vector2.zero;
    }
}
