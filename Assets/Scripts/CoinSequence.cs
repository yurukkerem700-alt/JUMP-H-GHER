using UnityEngine;
using TMPro;

public class CoinSequence : MonoBehaviour
{
    public MovingPlatform2D[] steps;
    public Transform coin;
    public TextMeshProUGUI stageText;
    public TextMeshProUGUI hintText;
    public GameObject winPanel;
    public int ActiveIndex { get; private set; }
    public int SpawnMode { get; private set; }
    public int SpawnFloor { get; private set; } = -1;
    public int TargetStep => targetPlatform ? System.Array.IndexOf(steps, targetPlatform) : -1;
    MovingPlatform2D targetPlatform;
    float nextCollectTime;
    Vector3 coinScale, staticPosition;
    float horizontalOffset, heightOffset;
    const float RingDuration = .30f;
    readonly SpriteRenderer[] rings = new SpriteRenderer[3];
    readonly float[] ringEnds = new float[3];
    Sprite ringSprite;
    int nextRing;

    void Awake() { coinScale = coin.localScale; }
    void Start() { ResetSequence(); }
    void LateUpdate()
    {
        UpdatePickupRings();
        coin.position = GetCoinPosition();
        coin.localScale = coinScale * (1 + Mathf.Sin(Time.time * 2.5f) * .035f);
        if (hintText)
        {
            float bottom = Camera.main.transform.position.y - Camera.main.orthographicSize;
            if (coin.position.y < bottom + .3f) hintText.text = "Coin aşağıda - Geri dönüp topla";
            else if (coin.position.y > bottom + Camera.main.orthographicSize * 2 - .3f) hintText.text = "Coin yukarıda - Yükselmeye devam et";
            else hintText.text = "Her coin bir üst katta - Yükselmeye devam et";
            if(TowerPowers.Instance && TowerPowers.Instance.Status.Length>0) hintText.text += "  •  " + TowerPowers.Instance.Status;
        }
    }
    static float CoinLimit => Mathf.Max(.1f, GameDeviceLayout.HorizontalLimit - .5f);
    public Vector3 GetCoinPosition()
    {
        if (SpawnMode == 1 && targetPlatform)
            return new Vector3(Mathf.Clamp(targetPlatform.transform.position.x + horizontalOffset, -CoinLimit, CoinLimit), targetPlatform.Top + heightOffset + Mathf.Sin(Time.time * 3) * .035f, 0);
        return staticPosition + Vector3.up * (Mathf.Sin(Time.time * 3) * .035f);
    }
    public bool TryCollect(Collider2D candidate)
    {
        if (!coin.gameObject.activeSelf || candidate.transform != coin || Time.time < nextCollectTime) return false;
        ShowPickupRing(coin.position);
        ActiveIndex++; nextCollectTime = Time.time + .15f; SpawnNext(); return true;
    }

    void ShowPickupRing(Vector3 position)
    {
        if (!ringSprite)
            ringSprite = UiArt.Build(48, 48, 48, (x, y) =>
                Mathf.Clamp01((.055f - Mathf.Abs(Mathf.Sqrt(x * x + y * y) - .39f)) * 36));

        int index = nextRing;
        nextRing = (nextRing + 1) % rings.Length;
        var ring = rings[index];
        if (!ring)
        {
            ring = new GameObject("Coin Işıması", typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
            ring.transform.SetParent(transform, false);
            ring.sprite = ringSprite;
            ring.sharedMaterial = coin.GetComponentInChildren<SpriteRenderer>().sharedMaterial;
            ring.sortingOrder = 27;
            rings[index] = ring;
        }
        ring.transform.position = position;
        ringEnds[index] = Time.time + RingDuration;
        UpdatePickupRings();
    }

    void UpdatePickupRings()
    {
        for (int i = 0; i < rings.Length; i++)
        {
            var ring = rings[i];
            if (!ring) continue;
            float remaining = Mathf.Clamp01((ringEnds[i] - Time.time) / RingDuration);
            ring.enabled = remaining > 0;
            if (!ring.enabled) continue;
            ring.transform.localScale = Vector3.one * Mathf.Lerp(1.25f, .35f, remaining);
            ring.color = new Color(1f, .75f, .3f, remaining);
        }
    }

    void OnDestroy()
    {
        if (!ringSprite) return;
        Destroy(ringSprite.texture);
        Destroy(ringSprite);
    }
    void SpawnNext()
    {
        SpawnMode = 1; // her coin basamağın tam ortasında durur ve basamakla birlikte gider
        int nextFloor = SpawnFloor + 1;
        var tower = FindFirstObjectByType<InfiniteTower>();
        tower.EnsureThroughFloor(nextFloor + 7);
        var chosen = tower.GetPlatform(nextFloor);
        if (!chosen) throw new System.InvalidOperationException("Coin platform missing: " + nextFloor);
        targetPlatform = null;
        SpawnFloor = nextFloor;
        horizontalOffset = 0;
        heightOffset = .75f;
        targetPlatform = chosen;
        coin.gameObject.SetActive(true); coin.position = GetCoinPosition();
    }
    public void ResetSequence()
    {
        System.Array.Clear(ringEnds, 0, ringEnds.Length);
        UpdatePickupRings();
        ActiveIndex = 0; SpawnFloor = -1; nextCollectTime = 0;
        if (winPanel) winPanel.SetActive(false);
        SpawnNext();
    }
    public void ClampCurrentCoin()
    {
        float limit = Mathf.Max(.5f, GameDeviceLayout.HorizontalLimit - .5f);
        staticPosition.x = Mathf.Clamp(staticPosition.x, -limit, limit);
        if (coin && coin.gameObject.activeSelf) coin.position = GetCoinPosition();
    }
}

