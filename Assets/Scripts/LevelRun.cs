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
    Canvas canvas; TextMeshProUGUI hud; GameObject result; Button reviveButton; TextMeshProUGUI reviveStatus;
    Transform door; SpriteRenderer doorGlow;
    EnemyManager enemies;
    bool finished, reviveUsed, reviveOfferReady;
    float hardLimitGraceUntil;
    static readonly Color Gold = new Color(1f, .77f, .3f), Dim = new Color(.25f, .2f, .26f);

    void Awake() { Instance = this; }

    public void Init(GameFlow owner, InfiniteTower t)
    {
        flow = owner; tower = t; font = t.floorLabel.font;
        canvas = UiArt.NewCanvas("Bölüm Arayüzü", 900);
        hud = UiArt.Label(canvas.transform, font, "", .5f, .815f, 900, 90, 24, new Color(1, .86f, .66f));
        hud.richText = true; hud.gameObject.SetActive(false);
        // Okunabilirlik için HUD'un arkasına yarı saydam koyu şerit
        var hudBack = new GameObject("HUD Zemin", typeof(Image)); hudBack.transform.SetParent(hud.transform, false);
        hudBack.transform.SetAsFirstSibling();
        var hudBackImage = hudBack.GetComponent<Image>(); hudBackImage.color = new Color(.03f, .04f, .07f, .5f); hudBackImage.raycastTarget = false;
        var hudBackRect = hudBack.GetComponent<RectTransform>(); hudBackRect.anchorMin = hudBackRect.anchorMax = new Vector2(.5f, .5f); hudBackRect.sizeDelta = new Vector2(900, 84);
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
        f.transform.SetParent(door, false); f.sprite = frame; f.sharedMaterial = material; f.sortingOrder = 15; f.color = new Color(.78f, .55f, .24f);
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
        Elapsed = 0; finished = false; reviveUsed = false; hardLimitGraceUntil = 0;
    }

    public void Stop() { Active = false; finished = false; enemies.Clear(); CloseResult(); hud.gameObject.SetActive(false); door.gameObject.SetActive(false); }

    // R ile yeniden başlatma bölümün süresini ve durumunu da sıfırlar.
    public void OnRunRestarted() { if (!Active) return; CloseResult(); Elapsed = 0; finished = false; reviveUsed = false; hardLimitGraceUntil = 0; enemies.Begin(Def); }

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
            // Kapıyı atlayıp hedef katın üstüne çıkmak bölümü bitirir; sonsuz tırmanış yalnızca Sonsuz Mod'da.
            if (!done && tower.HighestReachedFloor >= Def.target) done = true;
        }
        if (done) Finish(true);
        else if (Def.HardLimit && Elapsed >= Def.limit && Time.time >= hardLimitGraceUntil) Finish(false, "SÜRE DOLDU");
    }

    void LateUpdate()
    {
        hud.gameObject.SetActive(Active && !finished);
        // Bölümde kat bilgisi HUD'da var; eski kat etiketi üst üste binmesin. Menüde de gizli kalsın.
        if (tower.floorLabel) tower.floorLabel.enabled = !(Active && !finished) && !GameFlow.IsMenu;
        var platform = Active && !finished && Def.kind == GoalKind.Floor ? tower.GetPlatform(Def.target - 1) : null;
        door.gameObject.SetActive(platform);
        if (platform)
        {
            var nextPlatform = tower.GetPlatform(Def.target);
            var platformCollider = platform.GetComponent<Collider2D>();
            float platformHalfWidth = platformCollider ? platformCollider.bounds.extents.x : .65f;
            float doorHalfWidth = .38f;
            float maxDoorX = Mathf.Max(.4f, GameDeviceLayout.HorizontalLimit - doorHalfWidth - .05f);
            float leftX = Mathf.Clamp(platform.transform.position.x - platformHalfWidth - doorHalfWidth - .2f, -maxDoorX, maxDoorX);
            float rightX = Mathf.Clamp(platform.transform.position.x + platformHalfWidth + doorHalfWidth + .2f, -maxDoorX, maxDoorX);
            float doorX = Mathf.Abs(leftX - platform.transform.position.x) >= Mathf.Abs(rightX - platform.transform.position.x) ? leftX : rightX;
            float doorY = platform.Top + .75f;

            if (nextPlatform)
            {
                var nextCollider = nextPlatform.GetComponent<Collider2D>();
                float nextHalfWidth = nextCollider ? nextCollider.bounds.extents.x : .65f;
                float separation = nextPlatform.transform.position.x - platform.transform.position.x;
                if (Mathf.Abs(separation) > platformHalfWidth + nextHalfWidth + doorHalfWidth + .25f)
                {
                    // Büyük yatay açıklık varsa kapı iki basamağın arasındaki boşlukta kalır.
                    doorX = (platform.transform.position.x + nextPlatform.transform.position.x) * .5f;
                }
                else
                {
                    // Açıklık darsa kapıyı üst basamaktan uzak olan yana taşı.
                    float leftDistance = Mathf.Abs(leftX - nextPlatform.transform.position.x);
                    float rightDistance = Mathf.Abs(rightX - nextPlatform.transform.position.x);
                    doorX = leftDistance > rightDistance ? leftX : rightX;
                }
                doorY = (platform.Top + nextPlatform.Top) * .5f + .18f;
            }

            // Kapı artık basamağın üstüne oturmaz; iki basamak arasındaki havada asılı durur.
            door.position = new Vector3(doorX, doorY, 0);
            doorGlow.color = new Color(.45f, .95f, 1f, .6f + .25f * Mathf.Sin(Time.time * 3f));
        }
        if (!Active || finished) return;
        string foes = Def.HasEnemies ? "  •  " + Def.EnemyText : "";
        string goal = Def.kind == GoalKind.Floor
            ? "BÖLÜM " + Def.number + "  •  KAT " + Mathf.Max(0, tower.HighestReachedFloor + 1) + " / " + Def.target + foes
            : "BÖLÜM " + Def.number + "  •  COIN " + tower.player.Score + " / " + Def.target + foes;
        string time;
        if (Def.HardLimit && Time.time < hardLimitGraceUntil) time = "DEVAM SÜRESİ " + LevelDefs.Clock(hardLimitGraceUntil - Time.time);
        else if (Def.HardLimit) time = (Def.limit - Elapsed <= 10 ? "<color=#ff6b5e>" : "") + "KALAN SÜRE " + LevelDefs.Clock(Def.limit - Elapsed) + (Def.limit - Elapsed <= 10 ? "</color>" : "");
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
        else UiArt.Label(t, font, reason == "CAN BİTTİ" ? "Son güvenli basamaktan bir kez devam edebilirsin." : reason == "SÜRE DOLDU" ? "Reklam izleyip son güvenli basamağından devam et." : "Bu bölümü " + LevelDefs.Clock(Def.limit) + " içinde bitirmelisin.", .5f, .50f, 900, 60, 24, new Color(.85f, .75f, .7f));
        int n = Def.number;
        bool canContinue = !win && !reviveUsed && (reason == "CAN BİTTİ" || reason == "SÜRE DOLDU");
        if (canContinue)
        {
            reviveStatus = UiArt.Label(t, font, "Reklam seçeneği kontrol ediliyor…", .5f, .355f, 900, 42, 18, new Color(.75f, .67f, .62f));
            reviveButton = UiArt.Btn(t, font, "REKLAMLA DEVAM ET  •  +1 CAN", .5f, .29f, 500, 70, () =>
            {
                if (reviveOfferReady)
                {
                    reviveStatus.text = "Reklam açılıyor…";
                    reviveButton.interactable = false;
                    if (!RewardedAdBridge.Instance || !RewardedAdBridge.Instance.ShowOffer()) SetReviveUnavailable();
                }
                else RequestReviveOffer();
            });
            reviveButton.gameObject.SetActive(false);
            RequestReviveOffer();
        }
        float y = canContinue ? .19f : .24f;
        if (win && n < LevelDefs.Count) { UiArt.Btn(t, font, "SONRAKİ BÖLÜM", .5f, y, 440, 70, () => { flow.Hold(false); flow.StartLevel(n + 1); }); y -= .09f; }
        UiArt.Btn(t, font, "TEKRAR", .5f, y, 440, 70, () => { flow.Hold(false); flow.StartLevel(n); }); y -= .09f;
        UiArt.Btn(t, font, "HARİTA", .5f, y, 440, 70, () => { flow.Hold(false); flow.OpenMap(); });
    }

    void RequestReviveOffer()
    {
        reviveOfferReady = false;
        if (reviveStatus) reviveStatus.text = "Reklam aranıyor…";
        if (reviveButton) { reviveButton.interactable = false; reviveButton.GetComponentInChildren<TextMeshProUGUI>().text = "REKLAM ARANIYOR…"; }
        if (!RewardedAdBridge.Instance) { SetReviveUnavailable(); return; }
        RewardedAdBridge.Instance.RequestOffer(
            () =>
            {
                reviveOfferReady = true;
                if (reviveStatus) reviveStatus.text = "Reklam hazır. İzleyerek bir kez devam edebilirsin.";
                if (reviveButton && !reviveUsed) { reviveButton.GetComponentInChildren<TextMeshProUGUI>().text = "REKLAMLA DEVAM ET  •  +1 CAN"; reviveButton.interactable = true; reviveButton.gameObject.SetActive(true); }
            },
            OnReviveRewarded,
            SetReviveUnavailable);
    }

    void SetReviveUnavailable()
    {
        reviveOfferReady = false;
        if (reviveStatus) reviveStatus.text = "Şu an reklam yok. İstersen yeniden arayabilirsin.";
        if (reviveButton && !reviveUsed) { reviveButton.GetComponentInChildren<TextMeshProUGUI>().text = "REKLAMI YENİDEN DENE"; reviveButton.interactable = true; reviveButton.gameObject.SetActive(true); }
    }

    void OnReviveRewarded()
    {
        if (!finished || reviveUsed || !result) return;
        reviveUsed = true;
        reviveOfferReady = false;
        if (!tower.player.ReviveAtCheckpoint())
        {
            if (reviveStatus) reviveStatus.text = "Devam basamağı bulunamadı. Bölümü yeniden başlat.";
            if (reviveButton) reviveButton.gameObject.SetActive(false);
            return;
        }
        if (Def.HardLimit) { Elapsed += 5f; hardLimitGraceUntil = Time.time + 8f; }
        if (PlayerHealth.Instance) PlayerHealth.Instance.ContinueAfterRewardedAd();
        finished = false;
        CloseResult();
        enemies.Begin(Def);
        flow.Hold(false);
    }

    void Condition(Transform parent, float y, string text, bool met)
    {
        UiArt.Pic(parent, UiArt.Star, met ? Gold : Dim, .27f, y, 38, 38);
        var label = UiArt.Label(parent, font, text, .5f, y, 500, 50, 24, met ? new Color(1, .9f, .7f) : new Color(.55f, .5f, .55f));
        label.alignment = TextAlignmentOptions.Left; label.rectTransform.anchoredPosition = new Vector2(20, 0);
    }

    void CloseResult()
    {
        if (RewardedAdBridge.Instance) RewardedAdBridge.Instance.CancelOffer();
        if (result) { Destroy(result); result = null; }
        reviveButton = null; reviveStatus = null; reviveOfferReady = false;
    }

    void OnDestroy() { Instance = null; if (PlayerHealth.Instance) PlayerHealth.Instance.Died -= OnPlayerDied; if (canvas) Destroy(canvas.gameObject); if (door) Destroy(door.gameObject); }
}
