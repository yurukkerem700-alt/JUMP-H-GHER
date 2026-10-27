using System.Collections.Generic;
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

    void Awake() { coinScale = coin.localScale; }
    void Start() { ResetSequence(); }
    void LateUpdate()
    {
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
    public Vector3 GetCoinPosition()
    {
        if (SpawnMode == 1 && targetPlatform)
            return new Vector3(targetPlatform.transform.position.x + horizontalOffset, targetPlatform.Top + heightOffset + Mathf.Sin(Time.time * 3) * .035f, 0);
        return staticPosition + Vector3.up * (Mathf.Sin(Time.time * 3) * .035f);
    }
    public bool TryCollect(Collider2D candidate)
    {
        if (!coin.gameObject.activeSelf || candidate.transform != coin || Time.time < nextCollectTime) return false;
        ActiveIndex++; nextCollectTime = Time.time + .15f; SpawnNext(); return true;
    }
    void SpawnNext()
    {
        SpawnMode = ActiveIndex % 2 == 0 ? 1 : 2;
        int nextFloor = SpawnFloor + 1;
        var tower = FindFirstObjectByType<InfiniteTower>();
        tower.EnsureThroughFloor(nextFloor + 7);
        var chosen = tower.GetPlatform(nextFloor);
        if (!chosen) throw new System.InvalidOperationException("Coin platform missing: " + nextFloor);
        targetPlatform = null;
        SpawnFloor = nextFloor;
        horizontalOffset = Random.Range(-.68f, .68f);
        heightOffset = Random.Range(.65f, .9f);
        if (SpawnMode == 1) targetPlatform = chosen;
        else
        {
            float limit = Mathf.Min(2.65f, GameDeviceLayout.HorizontalLimit - .5f);
            staticPosition = new Vector3(Random.Range(-limit, limit), chosen.Top + heightOffset, 0);
        }
        coin.gameObject.SetActive(true); coin.position = GetCoinPosition();
    }
    public void ResetSequence()
    {
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

