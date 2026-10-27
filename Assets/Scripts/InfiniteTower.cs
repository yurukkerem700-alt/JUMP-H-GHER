using System.Collections.Generic;
using UnityEngine;
using TMPro;

[DefaultExecutionOrder(-80)]
public class InfiniteTower : MonoBehaviour
{
    public const int FloorsPerSection = 7;
    public const float FloorGap = 1.15f;
    public const float FirstSurface = -2.25f;
    public PlayerController player;
    public CoinSequence coins;
    public TextMeshProUGUI floorLabel;
    public float scrollSmoothTime = .65f;
    readonly Dictionary<int, MovingPlatform2D> active = new Dictionary<int, MovingPlatform2D>();
    readonly Queue<MovingPlatform2D> pool = new Queue<MovingPlatform2D>();
    GameObject template;
    Transform sectionParent;
    Camera gameCamera;
    float desiredCameraY = .3f;
    float cameraVelocity;
    public int HighestGeneratedFloor { get; private set; } = 6;
    public int HighestReachedFloor { get; private set; } = -1;
    public int ActivePlatformCount => active.Count;
    public int PooledPlatformCount => pool.Count;
    public float DesiredCameraY => desiredCameraY;

    void Awake()
    {
        gameCamera = Camera.main;
        sectionParent = coins.steps[0].transform.parent;
        for (int i = 0; i < coins.steps.Length; i++)
        {
            coins.steps[i].floorIndex = i;
            active.Add(i, coins.steps[i]);
        }
        template = Instantiate(coins.steps[0].gameObject, sectionParent);
        template.name = "Basamak Sablonu";
        template.SetActive(false);
    }

    void LateUpdate()
    {
        var support = player.GroundPlatform(out bool grounded);
        if (grounded && support)
        {
            HighestReachedFloor = Mathf.Max(HighestReachedFloor, support.floorIndex);
            EnsureThroughFloor(support.floorIndex + 10);
        }
        float playerY = player.GetComponent<Rigidbody2D>().position.y;
        EnsureThroughFloor(Mathf.Max(6, Mathf.FloorToInt((playerY - FirstSurface) / FloorGap) + 9));
        int nearFloor = Mathf.Clamp(Mathf.FloorToInt((playerY - .3f - FirstSurface) / FloorGap), 0, HighestGeneratedFloor);
        int nearSection = nearFloor / FloorsPerSection;
        bool changed = EnsureSection(nearSection);
        if (nearSection > 0) changed |= EnsureSection(nearSection - 1);
        if ((nearSection + 1) * FloorsPerSection <= HighestGeneratedFloor) changed |= EnsureSection(nearSection + 1);
        if (changed) RefreshCoinPlatforms();

        // Aşağı geri dönmek mümkündür: bekleyen coin ve bağlı basamak silinmez.
        if (playerY < gameCamera.transform.position.y - gameCamera.orthographicSize + 1.2f)
            desiredCameraY = Mathf.Max(.3f, playerY + 2.5f);
        if (playerY > gameCamera.transform.position.y - .8f)
            desiredCameraY = Mathf.Max(desiredCameraY, playerY + .8f);
        float y = Mathf.SmoothDamp(gameCamera.transform.position.y, desiredCameraY, ref cameraVelocity, scrollSmoothTime, 75, Time.deltaTime);
        if (Mathf.Abs(gameCamera.transform.position.y - desiredCameraY) > gameCamera.orthographicSize * 3) y = desiredCameraY;
        gameCamera.transform.position = new Vector3(0, y, -10);
        if (floorLabel) floorLabel.text = "KAT " + Mathf.Max(0, HighestReachedFloor + 1);
        RecycleDistantSections(nearSection);
    }

    public void AdvanceFromFloor(int floor) { EnsureThroughFloor(floor + FloorsPerSection); }

    public void EnsureThroughFloor(int floor)
    {
        int lastSection = Mathf.Max(0, floor / FloorsPerSection);
        bool changed = false;
        if (floor > HighestGeneratedFloor) HighestGeneratedFloor = (lastSection + 1) * FloorsPerSection - 1;
        int firstSection = Mathf.Max(0, lastSection - 2);
        for (int section = firstSection; section <= lastSection; section++) changed |= EnsureSection(section);
        if (changed) RefreshCoinPlatforms();
    }

    bool EnsureSection(int section)
    {
        bool changed = false;
        int first = section * FloorsPerSection;
        for (int floor = first; floor < first + FloorsPerSection && floor <= HighestGeneratedFloor; floor++)
        {
            if (active.ContainsKey(floor)) continue;
            MovingPlatform2D step;
            if (pool.Count > 0) step = pool.Dequeue();
            else step = Instantiate(template, sectionParent).GetComponent<MovingPlatform2D>();
            step.Configure(floor, SurfaceFor(floor));
            step.gameObject.name = "Basamak " + (floor + 1);
            step.gameObject.SetActive(true);
            active.Add(floor, step); changed = true;
        }
        return changed;
    }

    void RecycleDistantSections(int currentSection)
    {
        int coinSection = Mathf.Max(0, coins.SpawnFloor) / FloorsPerSection;
        var remove = new List<int>();
        foreach (var pair in active)
        {
            int section = pair.Key / FloorsPerSection;
            if (Mathf.Abs(section - currentSection) > 1 && section != coinSection) remove.Add(pair.Key);
        }
        if (remove.Count == 0) return;
        foreach (int floor in remove)
        {
            var step = active[floor]; active.Remove(floor);
            step.gameObject.SetActive(false); pool.Enqueue(step);
        }
        RefreshCoinPlatforms();
    }

    void RefreshCoinPlatforms()
    {
        var keys = new List<int>(active.Keys); keys.Sort();
        var steps = new MovingPlatform2D[keys.Count];
        for (int i = 0; i < keys.Count; i++) steps[i] = active[keys[i]];
        coins.steps = steps;
    }

    public MovingPlatform2D GetPlatform(int floor) { active.TryGetValue(floor, out var step); return step; }
    public static float SurfaceFor(int floor) => FirstSurface + floor * FloorGap;

    public void ResetTower()
    {
        desiredCameraY = .3f; cameraVelocity = 0;
        gameCamera.transform.position = new Vector3(0, .3f, -10);
        HighestReachedFloor = -1;
        HighestGeneratedFloor = 6;
        var remove = new List<int>();
        foreach (var pair in active) if (pair.Key >= 7) remove.Add(pair.Key);
        foreach (int floor in remove)
        {
            var step = active[floor]; active.Remove(floor);
            step.gameObject.SetActive(false); pool.Enqueue(step);
        }
        EnsureSection(0); RefreshCoinPlatforms();
    }
}
