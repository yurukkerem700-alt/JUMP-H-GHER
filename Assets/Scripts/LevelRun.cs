using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Seçili bölümün hedefini, süresini, kapısını, arayüzünü ve sonucunu yönetir.
public class LevelRun : MonoBehaviour
{
    public static LevelRun Instance { get; private set; }
    public bool Active { get; private set; }
    public bool Finished => finished;
    public bool CombatPlaying => (Active && !finished) || (Endless && !endlessOver);
    public bool CrumblingPlatforms => Active && !finished && Def.number > 20;
    public float PlatformCrumbleSeconds => Def.number switch
    {
        <= 30 => 5f,
        <= 50 => 3.5f,
        <= 70 => 3f,
        <= 90 => 2.75f,
        _ => 2.5f
    };
    public bool Endless { get; private set; }
    public LevelDef Def { get; private set; }
    public float Elapsed { get; private set; }
    GameFlow flow; InfiniteTower tower; TMP_FontAsset font;
    Canvas canvas; TextMeshProUGUI hud; GameObject result, hudBack;
    Transform door; SpriteRenderer doorGlow;
    EnemyManager enemies;
    bool finished, endlessOver;
    TextMeshProUGUI toast; float toastUntil, nextPoll;
    float endlessElapsed; int bestAtStart;
    int exitFloor=-1;
    bool wasInsideDoor, wasDoorReady;
    public bool ExitReady => LevelExitRules.ObjectiveReady(Def,tower.player.Score,Elapsed);
    static readonly Color Gold = new Color(1f, .77f, .3f), Dim = new Color(.25f, .2f, .26f);

    void Awake() { Instance = this; }

    public void Init(GameFlow owner, InfiniteTower t)
    {
        flow = owner; tower = t; font = t.floorLabel.font;
        canvas = UiArt.NewCanvas("Bölüm Arayüzü", 900);
        // Yazının arkasından geçen platformlar okunurluğu bozmasın diye koyu yarı saydam zemin.
        hudBack = new GameObject("HUD Zemin", typeof(Image)); hudBack.transform.SetParent(canvas.transform, false);
        hudBack.AddComponent<SafeHudAnchor>();
        var hb = hudBack.GetComponent<Image>(); hb.color = new Color(.05f, .03f, .05f, .80f); hb.raycastTarget = false;
        var hr = hb.rectTransform; hr.anchorMin = hr.anchorMax = new Vector2(.5f, .815f); hr.sizeDelta = new Vector2(780, 80);
        hud = UiArt.Label(canvas.transform, font, "", .5f, .815f, 780, 80, 24, new Color(1, .86f, .66f));
        hud.enableAutoSizing = true; hud.fontSizeMin = 15; hud.fontSizeMax = 24;
        hud.margin = new Vector4(12, 3, 12, 3);
        hud.richText = true; hud.gameObject.SetActive(false); hudBack.SetActive(false);
        BuildDoor();
        toast = UiArt.Label(canvas.transform, font, "", .5f, .70f, 900, 70, 30, new Color(.6f, 1f, .75f));
        toast.outlineColor = new Color32(10, 30, 15, 255); toast.outlineWidth = .25f; toast.gameObject.SetActive(false);
        enemies = gameObject.AddComponent<EnemyManager>(); enemies.Init(t);
        if (PlayerHealth.Instance) PlayerHealth.Instance.Died += OnPlayerDied;
    }

    public void DebugKill() { if (PlayerHealth.Instance) for (int i = 0; i < PlayerHealth.Max; i++) PlayerHealth.Instance.Hit(true); }

    void OnPlayerDied()
    {
        if (Endless) { if (!endlessOver) FinishEndless(); return; }
        if (Active && !finished) Finish(false, "CAN BİTTİ");
    }

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
        Def = LevelDefs.Get(number); Active = true; Endless = false;
        CloseResult();
        if (PlayerHealth.Instance) PlayerHealth.Instance.SetAllowed(Def.hearts);
        tower.player.RestartRun();
        Elapsed = 0; finished = false; ResetExit();
    }

    // Sonsuz mod: düşmanlar yüksekliğe göre kendiliğinden artar (EnemyManager.BeginEndless).
    public void BeginEndless() { Endless = true; StartEndlessRun(); enemies.BeginEndless(); }
    void StartEndlessRun() { endlessOver = false; endlessElapsed = 0; bestAtStart = PlayerPrefs.GetInt("BestFloor", 0); CloseResult(); }

    public void Stop() { Active = false; Endless = false; finished = false; endlessOver = false; if (PlayerHealth.Instance) PlayerHealth.Instance.SetAllowed(PlayerHealth.Max); enemies.Clear(); CloseResult(); hud.gameObject.SetActive(false); hudBack.SetActive(false); door.gameObject.SetActive(false); }

    // R ile yeniden başlatma bölümün süresini ve durumunu da sıfırlar.
    public void OnRunRestarted() { if (Endless) { StartEndlessRun(); enemies.BeginEndless(); return; } if (!Active) return; Elapsed = 0; finished = false; ResetExit(); enemies.Begin(Def); }

    void ResetExit() { exitFloor=-1; wasInsideDoor=false; wasDoorReady=false; if(door) door.gameObject.SetActive(false); }

    void UpdateExit()
    {
        if(Def.kind!=GoalKind.Survive) exitFloor=LevelExitRules.FixedExitFloor(Def);
        else if(exitFloor<0 && ExitReady)
        {
            exitFloor=Mathf.Max(0,Mathf.FloorToInt((tower.player.transform.position.y-InfiniteTower.FirstSurface)/InfiniteTower.FloorGap)+1);
            tower.EnsureThroughFloor(exitFloor);
        }
        if(exitFloor<0){door.gameObject.SetActive(false);return;}
        var platform=tower.GetPlatform(exitFloor);
        door.gameObject.SetActive(platform);
        if(!platform) return;
        door.position=new Vector3(platform.transform.position.x,platform.Top+.57f,0);
        doorGlow.color=ExitReady?new Color(.45f,.95f,1f,.6f+.25f*Mathf.Sin(Time.time*3f)):new Color(.3f,.27f,.3f,.55f);
    }

    void Update()
    {
        if (GameFlow.IsMenu) return;
        if (Endless && !endlessOver) { endlessElapsed += Time.deltaTime; PlayerProgress.ReportFloor(tower.HighestReachedFloor + 1); }
        if (!Active || finished) return;
        PlayerProgress.ReportFloor(tower.HighestReachedFloor + 1);
        Elapsed += Time.deltaTime;
        UpdateExit();
        var p=tower.player.transform.position;
        bool inside=door.gameObject.activeSelf && Mathf.Abs(p.x-door.position.x)<.4f && Mathf.Abs(p.y-door.position.y)<.7f;
        bool done=LevelExitRules.EnteredOpenDoor(ExitReady,wasDoorReady,inside,wasInsideDoor);
        wasInsideDoor=inside; wasDoorReady=ExitReady;
        if (done) Finish(true);
        else if (Def.HardLimit && Elapsed >= Def.limit) Finish(false, "SÜRE DOLDU");
    }

    void LateUpdate()
    {
        if (!GameFlow.IsMenu && (Active || Endless) && Time.unscaledTime >= nextPoll)
        {
            nextPoll = Time.unscaledTime + 1f;
            var msg = PlayerProgress.PollNotice();
            if (msg != null) { toast.text = msg; toastUntil = Time.unscaledTime + 3.2f; SesAtolyesi.Ses.Pickup(); }
        }
        toast.gameObject.SetActive(Time.unscaledTime < toastUntil && !GameFlow.IsMenu);
        bool showEndlessHud = Endless && !endlessOver;
        bool showHud = (Active && !finished) || showEndlessHud;
        hud.gameObject.SetActive(showHud); hudBack.SetActive(showHud);
        if (tower.floorLabel) tower.floorLabel.enabled = !showHud;
        if(!Active || finished) door.gameObject.SetActive(false);
        if (!showHud) return;
        string power = TowerPowers.Instance ? TowerPowers.Instance.Status : "";
        string powerHint = power.Length == 0 ? "" : "  •  " + power;
        if (showEndlessHud)
        {
            int floor = Mathf.Max(0, tower.HighestReachedFloor + 1);
            string theme = TowerThemes.Instance ? TowerThemes.Instance.CurrentName : "";
            string record = bestAtStart == 0 ? "İLK TIRMANIŞ"
                : floor > bestAtStart ? "<color=#8CFFBF>YENİ REKOR</color>"
                : "REKOR  " + bestAtStart;
            hud.text = "KAT " + floor + "  •  " + theme + "\n" + record + "  •  " + CoinTarget() + powerHint;
            return;
        }

        string foes = Def.HasEnemies ? "  •  " + Def.EnemyText : "";
        string head = "BÖLÜM " + Def.number + "  •  " + Def.TierName;
        string goal = Def.kind == GoalKind.Floor ? head + "  •  KAT " + Mathf.Max(0, tower.HighestReachedFloor + 1) + " / " + Def.target + foes
            : Def.kind == GoalKind.Coins ? head + "  •  COIN " + tower.player.Score + " / " + Def.target + (ExitReady ? "" : "  •  " + CoinTarget()) + foes
            : head + "  •  HAYATTA KAL" + foes;
        string time;
        bool exitOpen = Def.kind != GoalKind.Floor && ExitReady && exitFloor >= 0;
        if (Def.kind == GoalKind.Survive) time = "KALAN SÜRE " + LevelDefs.Clock(Def.survive - Elapsed);
        else if (Def.HardLimit) time = "KALAN SÜRE " + LevelDefs.Clock(Def.limit - Elapsed);
        else time = "SÜRE " + LevelDefs.Clock(Elapsed);
        if (exitOpen)
        {
            string exit = "<color=#85F3FF>KAPI AÇIK  •  " + (exitFloor + 1) + ". KAT</color>";
            time = Def.HardLimit ? exit + "  •  " + time : exit;
        }
        int lost = PlayerHealth.Instance ? PlayerHealth.Instance.Lost : 0;
        string next = "  •  ŞU AN " + Def.StarsFor(Elapsed, lost) + " YILDIZ";
        hud.text = goal + "\n" + time + next + powerHint;
    }

    string CoinTarget()
    {
        var coins = tower.coins;
        bool below = tower.player.transform.position.y - coins.coin.position.y > InfiniteTower.FloorGap;
        return "COIN " + (below ? "AŞAĞIDA  ·  " : "") + (coins.SpawnFloor + 1) + ". KAT";
    }

    void Finish(bool win, string reason = null)
    {
        finished = true; enemies.Clear(); if (win) SesAtolyesi.Ses.Pickup();
        int stars = win ? Def.StarsFor(Elapsed, PlayerHealth.Instance ? PlayerHealth.Instance.Lost : 0) : 0;
        if (win) { LevelDefs.SaveStars(Def.number, stars); PlayerProgress.AddLevel(); }
        flow.Hold(true);
        int finishTime = Mathf.Max(1, Mathf.RoundToInt(Elapsed * 100f));
        int previousTime = win ? LevelDefs.BestTime(Def.number) : 0;
        if (win) LevelDefs.SaveBestTime(Def.number, finishTime);
        ShowResult(win, stars, reason, finishTime, previousTime);
    }

    static string TimeRecordText(int finishTime, int previousTime)
    {
        string current = LevelDefs.RecordClock(finishTime);
        if (previousTime <= 0) return "İLK DERECE  " + current;
        int delta = finishTime - previousTime;
        if (delta == 0) return "REKORA EŞİT  " + current;
        if (delta < 0) return "YENİ REKOR  " + current + "  •  -" + LevelDefs.RecordClock(-delta);
        return "EN İYİ  " + LevelDefs.RecordClock(previousTime) + "  •  +" + LevelDefs.RecordClock(delta);
    }

    void ShowResult(bool win, int stars, string reason, int finishTime, int previousTime)
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
            UiArt.Label(t, font, "SÜRE  " + LevelDefs.RecordClock(finishTime) + "   •   COIN  " + tower.player.Score + "   •   KAT  " + Mathf.Max(0, tower.HighestReachedFloor + 1), .5f, .52f, 900, 50, 26, new Color(1, .86f, .66f));
            var rules = Def.StarRules;
            Condition(t, .45f, rules[0], true);
            Condition(t, .40f, rules[1], stars >= 2);
            Condition(t, .35f, rules[2], stars >= 3);
            bool timeRecord = previousTime <= 0 || finishTime < previousTime;
            UiArt.Label(t, font, TimeRecordText(finishTime, previousTime),
                .5f, .30f, 620, 28, 22, timeRecord ? new Color(.55f, 1f, .75f) : Gold);
        }
        else UiArt.Label(t, font, "COIN  " + tower.player.Score + "   •   KAT  " + Mathf.Max(0, tower.HighestReachedFloor + 1) + "   •   CÜZDAN  " + PlayerProgress.Wallet, .5f, .44f, 900, 50, 24, Gold);
        if (!win) UiArt.Label(t, font, reason == "CAN BİTTİ" ? "Tüm canlarını (" + Def.hearts + ") kaybettin. Bir daha dene." : "Bu bölümü " + LevelDefs.Clock(Def.limit) + " içinde bitirmelisin.", .5f, .50f, 900, 60, 24, new Color(.85f, .75f, .7f));
        int n = Def.number;
        float y = .24f;
        if (win && n < LevelDefs.Count) { UiArt.Btn(t, font, "SONRAKİ BÖLÜM", .5f, y, 440, 70, () => { flow.Hold(false); flow.StartLevel(n + 1); }); y -= .09f; }
        UiArt.Btn(t, font, "TEKRAR", .5f, y, 440, 70, () => { flow.Hold(false); flow.StartLevel(n); }); y -= .09f;
        UiArt.Btn(t, font, "HARİTA", .5f, y, 440, 70, () => { flow.Hold(false); flow.OpenMap(); });
    }

    // Sonsuz mod: can bitince sonuç paneli (kat, coin, süre, rekor) gösterilir.
    void FinishEndless()
    {
        endlessOver = true; enemies.Clear();
        int floor = Mathf.Max(0, tower.HighestReachedFloor + 1), coins = tower.player.Score;
        bool record = floor > 0 && floor > bestAtStart;
        if (floor > PlayerPrefs.GetInt("BestFloor", 0)) PlayerPrefs.SetInt("BestFloor", floor);
        PlayerPrefs.Save();
        flow.Hold(true);
        ShowEndlessResult(floor, coins, record);
    }

    void ShowEndlessResult(int floor, int coins, bool record)
    {
        CloseResult();
        result = new GameObject("Sonsuz Mod Sonucu", typeof(Image)); result.transform.SetParent(canvas.transform, false);
        var rr = result.GetComponent<RectTransform>(); rr.anchorMin = Vector2.zero; rr.anchorMax = Vector2.one; rr.offsetMin = rr.offsetMax = Vector2.zero;
        result.GetComponent<Image>().color = new Color(.035f, .025f, .045f, .9f);
        var t = result.transform;
        var soft = new Color(.85f, .75f, .7f);
        UiArt.Label(t, font, "CAN BİTTİ", .5f, .84f, 900, 110, 52, new Color(1, .42f, .38f));
        UiArt.Label(t, font, "KAT  " + floor, .5f, .70f, 900, 130, 84, Gold);
        if (record) UiArt.Label(t, font, "YENİ REKOR!", .5f, .59f, 900, 70, 40, new Color(.55f, 1f, .75f));
        else UiArt.Label(t, font, "EN İYİ KAT  " + Mathf.Max(floor, PlayerPrefs.GetInt("BestFloor", 0)), .5f, .59f, 900, 60, 28, soft);
        UiArt.Label(t, font, "COIN  " + coins + "   •   SÜRE  " + LevelDefs.Clock(endlessElapsed), .5f, .50f, 900, 56, 28, new Color(1, .86f, .66f));
        UiArt.Label(t, font, "CÜZDAN  " + PlayerProgress.Wallet + " COIN", .5f, .43f, 900, 50, 24, Gold);
        UiArt.Btn(t, font, "TEKRAR", .5f, .29f, 440, 70, () => { flow.Hold(false); CloseResult(); tower.player.RestartRun(); });
        UiArt.Btn(t, font, "ANA MENÜ", .5f, .20f, 440, 70, () => { flow.Hold(false); CloseResult(); Stop(); tower.player.RestartRun(); flow.ToMenu(); });
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
