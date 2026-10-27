using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Seçili bölümün hedefini, süresini, kapısını, arayüzünü ve sonucunu yönetir.
public class LevelRun : MonoBehaviour
{
    public static LevelRun Instance { get; private set; }
    public bool Active { get; private set; }
    public bool Finished => finished;
    public LevelDef Def { get; private set; }
    public float Elapsed { get; private set; }
    GameFlow flow; InfiniteTower tower; TMP_FontAsset font;
    Canvas canvas; TextMeshProUGUI hud; GameObject result;
    Transform door; SpriteRenderer doorGlow;
    EnemyManager enemies;
    bool finished;
    static readonly Color Gold = new Color(1f, .77f, .3f), Dim = new Color(.25f, .2f, .26f);

    void Awake() { Instance = this; }

    public void Init(GameFlow owner, InfiniteTower t)
    {
        flow = owner; tower = t; font = t.floorLabel.font;
        canvas = UiArt.NewCanvas("Bölüm Arayüzü", 900);
        hud = UiArt.Label(canvas.transform, font, "", .5f, .815f, 900, 90, 24, new Color(1, .86f, .66f));
        hud.richText = true; hud.gameObject.SetActive(false);
        BuildDoor();
        enemies = gameObject.AddComponent<EnemyManager>(); enemies.Init(t);
        if (PlayerHealth.Instance) PlayerHealth.Instance.Died += OnPlayerDied;
    }

    void OnPlayerDied() { if (Active && !finished) Finish(false, "CAN BİTTİ"); }

    void BuildDoor()
    {
        var material = tower.coins.coin.GetComponentInChildren<SpriteRenderer>().sharedMaterial;
        // 1 x 1,5 birim kemerli kapı: dış çerçeve + parlayan iç yüzey
        var frame = UiArt.Build(64, 96, 64, (u, v) => ArchAlpha(u, v, 0));
        var inner = UiArt.Build(64, 96, 64, (u, v) => ArchAlpha(u, v, .09f));
        door = new GameObject("Bölüm Kapısı").transform;
        var f = new GameObject("Çerçeve", typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
        f.transform.SetParent(door, false); f.sprite = frame; f.sharedMaterial = material; f.sortingOrder = 15; f.color = new Color(.45f, .27f, .13f);
        doorGlow = new GameObject("Işık", typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
        doorGlow.transform.SetParent(door, false); doorGlow.sprite = inner; doorGlow.sharedMaterial = material; doorGlow.sortingOrder = 16;
        door.localScale = Vector3.one * .75f; door.gameObject.SetActive(false);
    }

    static float ArchAlpha(float u, float v, float inset)
    {
        float d = Mathf.Max(Mathf.Abs(u) - .5f, -.75f - v);
        if (v > .25f) d = Mathf.Max(d, Mathf.Sqrt(u * u + (v - .25f) * (v - .25f)) - .5f);
        return Mathf.Clamp01(.5f - (d + inset) * 64);
    }

    public void Begin(int number)
    {
        Def = LevelDefs.Get(number); Active = true;
        CloseResult();
        tower.player.RestartRun();
        Elapsed = 0; finished = false;
    }

    public void Stop() { Active = false; finished = false; enemies.Clear(); CloseResult(); hud.gameObject.SetActive(false); door.gameObject.SetActive(false); }

    // R ile yeniden başlatma bölümün süresini ve durumunu da sıfırlar.
    public void OnRunRestarted() { if (!Active) return; Elapsed = 0; finished = false; enemies.Begin(Def); }

    void Update()
    {
        if (!Active || finished || GameFlow.IsMenu) return;
        Elapsed += Time.deltaTime;
        bool done;
        if (Def.kind == GoalKind.Coins) done = tower.player.Score >= Def.target;
        else
        {
            var p = tower.player.transform.position;
            done = door.gameObject.activeSelf && Mathf.Abs(p.x - door.position.x) < .4f && Mathf.Abs(p.y - door.position.y) < .7f;
        }
        if (done) Finish(true);
        else if (Def.HardLimit && Elapsed >= Def.limit) Finish(false, "SÜRE DOLDU");
    }

    void LateUpdate()
    {
        hud.gameObject.SetActive(Active && !finished);
        var platform = Active && !finished && Def.kind == GoalKind.Floor ? tower.GetPlatform(Def.target - 1) : null;
        door.gameObject.SetActive(platform);
        if (platform)
        {
            door.position = new Vector3(platform.transform.position.x, platform.Top + .57f, 0);
            doorGlow.color = new Color(.45f, .95f, 1f, .6f + .25f * Mathf.Sin(Time.time * 3f));
        }
        if (!Active || finished) return;
        string foes = Def.HasEnemies ? "  •  " + Def.EnemyText : "";
        string goal = Def.kind == GoalKind.Floor
            ? "BÖLÜM " + Def.number + "  •  KAT " + Mathf.Max(0, tower.HighestReachedFloor + 1) + " / " + Def.target + foes
            : "BÖLÜM " + Def.number + "  •  COIN " + tower.player.Score + " / " + Def.target + foes;
        string time;
        if (Def.HardLimit) time = "KALAN SÜRE " + LevelDefs.Clock(Def.limit - Elapsed);
        else time = "SÜRE " + LevelDefs.Clock(Elapsed);
        string next = Elapsed <= Def.threeStarTime ? "  •  3 YILDIZ: " + LevelDefs.Clock(Def.threeStarTime)
            : Elapsed <= Def.twoStarTime ? "  •  2 YILDIZ: " + LevelDefs.Clock(Def.twoStarTime) : "  •  1 YILDIZ";
        hud.text = goal + "\n" + time + next;
    }

    int StarsFor(float time) => 1 + (time <= Def.twoStarTime ? 1 : 0) + (time <= Def.threeStarTime ? 1 : 0);

    void Finish(bool win, string reason = null)
    {
        finished = true; enemies.Clear();
        int stars = win ? StarsFor(Elapsed) : 0;
        if (win) LevelDefs.SaveStars(Def.number, stars);
        flow.Hold(true);
        ShowResult(win, stars, reason);
    }

    void ShowResult(bool win, int stars, string reason)
    {
        CloseResult();
        result = new GameObject("Bölüm Sonucu", typeof(Image)); result.transform.SetParent(canvas.transform, false);
        var rr = result.GetComponent<RectTransform>(); rr.anchorMin = Vector2.zero; rr.anchorMax = Vector2.one; rr.offsetMin = rr.offsetMax = Vector2.zero;
        result.GetComponent<Image>().color = new Color(.035f, .025f, .045f, .9f);
        var t = result.transform;
        bool finale = win && Def.number == LevelDefs.Count;
        UiArt.Label(t, font, finale ? "OYUN TAMAMLANDI!" : win ? "BÖLÜM " + Def.number + " TAMAM" : reason, .5f, .84f, 900, 110, 52, win ? Gold : new Color(1, .42f, .38f));
        if (finale) UiArt.Label(t, font, "Kök Yankısı'nın en tepesine ulaştın.", .5f, .75f, 900, 60, 24, new Color(.85f, .75f, .7f));
        else UiArt.Label(t, font, Def.Goal, .5f, .75f, 900, 60, 24, new Color(.85f, .75f, .7f));
        for (int i = 0; i < 3; i++) UiArt.Pic(t, UiArt.Star, i < stars ? Gold : Dim, .5f + (i - 1) * .17f, .63f, 130, 130);
        if (win)
        {
            UiArt.Label(t, font, "SÜRE  " + LevelDefs.Clock(Elapsed), .5f, .52f, 800, 50, 26, new Color(1, .86f, .66f));
            Condition(t, .45f, "Bölümü tamamla", true);
            Condition(t, .40f, LevelDefs.Clock(Def.twoStarTime) + " içinde bitir", Elapsed <= Def.twoStarTime);
            Condition(t, .35f, LevelDefs.Clock(Def.threeStarTime) + " içinde bitir", Elapsed <= Def.threeStarTime);
        }
        else UiArt.Label(t, font, reason == "CAN BİTTİ" ? "Üç darbede düştün. Bir daha dene." : "Bu bölümü " + LevelDefs.Clock(Def.limit) + " içinde bitirmelisin.", .5f, .50f, 900, 60, 24, new Color(.85f, .75f, .7f));
        int n = Def.number;
        float y = .24f;
        if (win && n < LevelDefs.Count) { UiArt.Btn(t, font, "SONRAKİ BÖLÜM", .5f, y, 440, 70, () => { flow.Hold(false); flow.StartLevel(n + 1); }); y -= .09f; }
        UiArt.Btn(t, font, "TEKRAR", .5f, y, 440, 70, () => { flow.Hold(false); flow.StartLevel(n); }); y -= .09f;
        UiArt.Btn(t, font, "HARİTA", .5f, y, 440, 70, () => { flow.Hold(false); flow.OpenMap(); });
    }

    void Condition(Transform parent, float y, string text, bool met)
    {
        UiArt.Pic(parent, UiArt.Star, met ? Gold : Dim, .27f, y, 38, 38);
        var label = UiArt.Label(parent, font, text, .5f, y, 500, 50, 24, met ? new Color(1, .9f, .7f) : new Color(.55f, .5f, .55f));
        label.alignment = TextAlignmentOptions.Left; label.rectTransform.anchoredPosition = new Vector2(20, 0);
    }

    void CloseResult() { if (result) { Destroy(result); result = null; } }

    void OnDestroy() { Instance = null; if (PlayerHealth.Instance) PlayerHealth.Instance.Died -= OnPlayerDied; if (canvas) Destroy(canvas.gameObject); if (door) Destroy(door.gameObject); }
}
