using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameFlow : MonoBehaviour
{
    public Sprite icon;
    public static GameFlow Instance {get;private set;}
    public static bool IsMenu {get;private set;}
    bool menuVisible,holding,settingsOpen;LevelMap map;LevelRun run;PlayerHealth health;RewardedAdBridge adBridge;
    Canvas canvas;GameObject overlay;TextMeshProUGUI heading,subtitle,status;Button start;float splashUntil;bool splash=true;bool begun;
    GameObject settingsPanel;TextMeshProUGUI settingsHint;Button gestureModeButton,buttonModeButton;GameDeviceLayout deviceLayout;
    InfiniteTower tower;GameObject endlessResult;Button endlessReviveButton;TextMeshProUGUI endlessAdStatus;bool endlessReviveUsed,endlessReviveOfferReady;
    void Awake(){Instance=this;}
    void Start()
    {
        tower=FindFirstObjectByType<InfiniteTower>();
        deviceLayout=FindFirstObjectByType<GameDeviceLayout>();
        canvas=new GameObject("Ana Menü",typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster)).GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=1000;
        var scaler=canvas.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1080,1080);scaler.matchWidthOrHeight=.5f;
        overlay=new GameObject("Menu Perdesi",typeof(Image));overlay.transform.SetParent(canvas.transform,false);var rect=overlay.GetComponent<RectTransform>();rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;overlay.GetComponent<Image>().color=new Color(.035f,.025f,.045f,.94f);
        if(icon){var art=new GameObject("Gezgin İkonu",typeof(Image));art.transform.SetParent(overlay.transform,false);art.GetComponent<Image>().sprite=icon;art.GetComponent<Image>().preserveAspect=true;var ar=art.GetComponent<RectTransform>();ar.sizeDelta=new Vector2(250,250);ar.anchorMin=ar.anchorMax=new Vector2(.5f,.72f);ar.anchoredPosition=Vector2.zero;}
        heading=Text("KÖK YANKISI",.55f,62,new Color(1,.77f,.5f));
        subtitle=Text("100 BÖLÜMDE ZİRVEYE YÜKSEL",.48f,23,new Color(.75f,.66f,.62f));
        status=Text("",.41f,18,new Color(.65f,.6f,.65f));
        start=Button("OYNA",.33f,()=>{if(begun)Begin(true);else map.Open();});
        Button("BÖLÜM HARİTASI",.235f,()=>map.Open());
        Button("AYARLAR",.14f,OpenSettings);
        Button(Application.platform==RuntimePlatform.WebGLPlayer?"TAM EKRAN":"ÇIKIŞ",.045f,()=>{if(Application.platform==RuntimePlatform.WebGLPlayer)Screen.fullScreen=!Screen.fullScreen;else Application.Quit();});
        var pause=new GameObject("Duraklat",typeof(Image),typeof(Button));pause.transform.SetParent(canvas.transform,false);pause.GetComponent<Image>().color=new Color(.16f,.1f,.15f,.8f);var pr=pause.GetComponent<RectTransform>();pr.anchorMin=pr.anchorMax=new Vector2(.5f,.96f);pr.sizeDelta=new Vector2(70,52);
        var pt=new GameObject("II",typeof(TextMeshProUGUI));pt.transform.SetParent(pause.transform,false);var textPause=pt.GetComponent<TextMeshProUGUI>();textPause.text="II";textPause.fontSize=26;textPause.alignment=TextAlignmentOptions.Center;textPause.raycastTarget=false;var ptr=textPause.rectTransform;ptr.anchorMin=Vector2.zero;ptr.anchorMax=Vector2.one;ptr.offsetMin=ptr.offsetMax=Vector2.zero;
        pause.GetComponent<Button>().onClick.AddListener(()=>{if(holding)return;if(map.IsOpen)map.Close();else if(IsMenu&&begun)Begin(true);else Show(true);});pause.transform.SetAsFirstSibling();
        BuildSettings(tower.floorLabel.font);
        var world=FindFirstObjectByType<AltitudeWorld>();
        map=new LevelMap(canvas.transform,tower.floorLabel.font,world ? world.skySprite : null,world ? world.themeAtlas : null,StartLevel,StartEndless,()=>map.Close());
        adBridge=new GameObject("RewardedAdBridge").AddComponent<RewardedAdBridge>();
        health=new GameObject("Can Yöneticisi").AddComponent<PlayerHealth>();health.Init(tower);health.Died+=OnPlayerDied;
        run=new GameObject("Bölüm Yöneticisi").AddComponent<LevelRun>();run.Init(this,tower);
        splashUntil=Time.unscaledTime+1.1f;Show(true);
#if UNITY_EDITOR
        if(UnityEditor.SessionState.GetBool("NextVerify",false)||UnityEditor.SessionState.GetBool("PresentationVerify",false)||UnityEditor.SessionState.GetBool("TowerVerify",false)){splash=false;Begin(false);}
#endif
    }
    TextMeshProUGUI Text(string value,float y,float size,Color color)
    {
        var go=new GameObject(value,typeof(TextMeshProUGUI));go.transform.SetParent(overlay.transform,false);var t=go.GetComponent<TextMeshProUGUI>();t.font=tower.floorLabel.font;t.text=value;t.fontSize=size;t.color=color;t.alignment=TextAlignmentOptions.Center;t.raycastTarget=false;var r=t.rectTransform;r.anchorMin=new Vector2(.07f,y);r.anchorMax=new Vector2(.93f,y);r.sizeDelta=new Vector2(0,105);return t;
    }
    Button Button(string title,float y,UnityEngine.Events.UnityAction action)
    {
        var go=new GameObject(title,typeof(Image),typeof(Button));go.transform.SetParent(overlay.transform,false);var r=go.GetComponent<RectTransform>();r.anchorMin=new Vector2(.28f,y);r.anchorMax=new Vector2(.72f,y);r.sizeDelta=new Vector2(0,70);go.GetComponent<Image>().color=new Color(.22f,.12f,.14f,.95f);var b=go.GetComponent<Button>();b.onClick.AddListener(action);
        var text=new GameObject("Başlık",typeof(TextMeshProUGUI));text.transform.SetParent(go.transform,false);var t=text.GetComponent<TextMeshProUGUI>();t.font=tower.floorLabel.font;t.text=title;t.fontSize=25;t.color=new Color(1,.81f,.59f);t.alignment=TextAlignmentOptions.Center;t.raycastTarget=false;var tr=t.rectTransform;tr.anchorMin=Vector2.zero;tr.anchorMax=Vector2.one;tr.offsetMin=tr.offsetMax=Vector2.zero;return b;
    }
    void BuildSettings(TMP_FontAsset font)
    {
        settingsPanel=new GameObject("Kontrol Ayarları",typeof(Image));settingsPanel.transform.SetParent(canvas.transform,false);
        var rect=settingsPanel.GetComponent<RectTransform>();rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
        settingsPanel.GetComponent<Image>().color=new Color(.035f,.025f,.045f,.97f);
        UiArt.Label(settingsPanel.transform,font,"KONTROL AYARLARI",.5f,.78f,900,90,42,new Color(1,.77f,.5f));
        settingsHint=UiArt.Label(settingsPanel.transform,font,"",.5f,.64f,900,120,23,new Color(.86f,.79f,.76f));
        gestureModeButton=UiArt.Btn(settingsPanel.transform,font,"PARMAKLA KONTROL",.5f,.49f,600,76,()=>ChooseControlMode(false));
        buttonModeButton=UiArt.Btn(settingsPanel.transform,font,"BUTONLU KONTROL",.5f,.37f,600,76,()=>ChooseControlMode(true));
        UiArt.Btn(settingsPanel.transform,font,"GERİ",.5f,.22f,360,66,CloseSettings);
        settingsPanel.SetActive(false);
    }
    void RefreshSettings()
    {
        bool usingButtons=deviceLayout && deviceLayout.ButtonMode;
        settingsHint.text=usingButtons
            ? "Telefon: ekrandaki sol, sağ ve zıpla düğmelerini kullan.\nBilgisayar: A / D veya oklar ile yürü, W / Boşluk ile zıpla."
            : "Parmağını yatay sürükle: o yöne ilerle.\nYukarı kaydır veya hızlıca dokun: zıpla.\nBölüm hedefini tamamla; hızlı bitirerek 3 yıldız topla.";
        var gestureImage=gestureModeButton.GetComponent<Image>();
        var buttonImage=buttonModeButton.GetComponent<Image>();
        gestureImage.color=usingButtons?new Color(.17f,.11f,.15f,.95f):new Color(.36f,.19f,.14f,.98f);
        buttonImage.color=usingButtons?new Color(.36f,.19f,.14f,.98f):new Color(.17f,.11f,.15f,.95f);
    }
    void OpenSettings()
    {
        if(map!=null)map.Close();
        settingsOpen=true;overlay.SetActive(false);settingsPanel.SetActive(true);RefreshSettings();
    }
    void CloseSettings()
    {
        settingsOpen=false;
        if(settingsPanel)settingsPanel.SetActive(false);
        if(overlay)overlay.SetActive(true);
    }
    void ChooseControlMode(bool useButtons)
    {
        if(deviceLayout)deviceLayout.SetButtonControls(useButtons);
        else PlayerPrefs.SetInt("MobileControlMode",useButtons?1:0);
        RefreshSettings();
    }
    void Update()
    {
        if(splash){bool ready=Time.unscaledTime>=splashUntil;start.gameObject.SetActive(ready);if(ready)splash=false;}
        if(!holding&&(Input.GetKeyDown(KeyCode.Escape)||Input.GetKeyDown(KeyCode.P))){if(settingsOpen)CloseSettings();else if(map.IsOpen)map.Close();else if(IsMenu&&begun)Begin(true);else Show(true);}
        if(IsMenu){int best=PlayerPrefs.GetInt("BestFloor",0);status.text="EN YÜKSEK KAT  "+best+"   •   YILDIZ  "+LevelDefs.TotalStars()+" / "+LevelDefs.Count*3;}
        else if(tower.HighestReachedFloor+1>PlayerPrefs.GetInt("BestFloor",0))PlayerPrefs.SetInt("BestFloor",tower.HighestReachedFloor+1);
    }
    public void Begin(bool resume){if(!resume&&begun)tower.player.RestartRun();begun=true;Show(false);}
    public void Show(bool visible){if(settingsOpen)CloseSettings();menuVisible=visible;overlay.SetActive(visible);Apply();if(begun)start.GetComponentInChildren<TextMeshProUGUI>().text="DEVAM ET";}
    // Sonuç ekranı açıkken oyun donar ama ana menü görünmez.
    public void Hold(bool on){holding=on;Apply();}
    void Apply(){IsMenu=menuVisible||holding;Time.timeScale=IsMenu?0:1;GameInput.Clear();}
    public void StartLevel(int n){CloseSettings();CloseEndlessResult();if(adBridge)adBridge.CancelOffer();map.Close();begun=true;Show(false);run.Begin(n);}
    void StartEndless(){CloseSettings();CloseEndlessResult();if(adBridge)adBridge.CancelOffer();map.Close();run.Stop();begun=true;Show(false);endlessReviveUsed=false;tower.player.RestartRun();}
    public void OpenMap(){CloseSettings();Show(true);map.Open();}

    void OnPlayerDied()
    {
        if (run && run.Active) return;
        ShowEndlessResult();
    }

    void ShowEndlessResult()
    {
        if (endlessResult) Destroy(endlessResult);
        Hold(true);
        endlessResult = new GameObject("Sonsuz Tur Sonucu", typeof(Image)); endlessResult.transform.SetParent(canvas.transform, false);
        var rect = endlessResult.GetComponent<RectTransform>(); rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
        endlessResult.GetComponent<Image>().color = new Color(.035f, .025f, .045f, .92f);
        var font = tower.floorLabel.font;
        UiArt.Label(endlessResult.transform, font, "BU TUR BURADA BİTTİ", .5f, .76f, 900, 100, 48, new Color(1, .62f, .46f));
        UiArt.Label(endlessResult.transform, font, "KAT " + Mathf.Max(1, tower.HighestReachedFloor + 1) + "   •   COIN " + tower.player.Score, .5f, .66f, 900, 58, 28, new Color(1, .84f, .66f));
        UiArt.Label(endlessResult.transform, font, "Son basamağından bir kez devam edebilirsin.", .5f, .57f, 900, 50, 22, new Color(.85f, .75f, .7f));
        endlessAdStatus = UiArt.Label(endlessResult.transform, font, "Reklam seçeneği kontrol ediliyor…", .5f, .46f, 900, 42, 18, new Color(.75f, .67f, .62f));
        endlessReviveButton = UiArt.Btn(endlessResult.transform, font, "REKLAMLA DEVAM ET  •  +1 CAN", .5f, .38f, 520, 70, () =>
        {
            if (endlessReviveOfferReady)
            {
                endlessAdStatus.text = "Reklam açılıyor…";
                endlessReviveButton.interactable = false;
                if (!adBridge || !adBridge.ShowOffer()) SetEndlessAdUnavailable();
            }
            else RequestEndlessReviveOffer();
        });
        endlessReviveButton.gameObject.SetActive(false);
        UiArt.Btn(endlessResult.transform, font, "YENİDEN BAŞLA", .5f, .26f, 440, 68, () => { CloseEndlessResult(); Hold(false); StartEndless(); });
        UiArt.Btn(endlessResult.transform, font, "BÖLÜM HARİTASI", .5f, .16f, 440, 68, () => { CloseEndlessResult(); Hold(false); OpenMap(); });
        RequestEndlessReviveOffer();
    }

    void RequestEndlessReviveOffer()
    {
        endlessReviveOfferReady = false;
        if (endlessAdStatus) endlessAdStatus.text = "Reklam aranıyor…";
        if (endlessReviveButton) { endlessReviveButton.interactable = false; endlessReviveButton.GetComponentInChildren<TextMeshProUGUI>().text = "REKLAM ARANIYOR…"; }
        if (!adBridge) { SetEndlessAdUnavailable(); return; }
        adBridge.RequestOffer(
            () =>
            {
                endlessReviveOfferReady = true;
                if (endlessAdStatus) endlessAdStatus.text = "Reklam hazır. İzleyerek bir kez devam edebilirsin.";
                if (endlessReviveButton && !endlessReviveUsed) { endlessReviveButton.GetComponentInChildren<TextMeshProUGUI>().text = "REKLAMLA DEVAM ET  •  +1 CAN"; endlessReviveButton.interactable = true; endlessReviveButton.gameObject.SetActive(true); }
            },
            OnEndlessReviveRewarded,
            SetEndlessAdUnavailable);
    }

    void OnEndlessReviveRewarded()
    {
        if (!endlessResult || endlessReviveUsed) return;
        endlessReviveUsed = true;
        endlessReviveOfferReady = false;
        if (!tower.player.ReviveAtCheckpoint())
        {
            SetEndlessContinueUnavailable();
            return;
        }
        if (health) health.ContinueAfterRewardedAd();
        CloseEndlessResult(); Hold(false);
    }

    void SetEndlessAdUnavailable()
    {
        endlessReviveOfferReady = false;
        if (endlessAdStatus) endlessAdStatus.text = "Şu an reklam yok. İlerlemen korunuyor; yeniden arayabilirsin.";
        if (endlessReviveButton && !endlessReviveUsed) { endlessReviveButton.GetComponentInChildren<TextMeshProUGUI>().text = "REKLAMI YENİDEN DENE"; endlessReviveButton.interactable = true; endlessReviveButton.gameObject.SetActive(true); }
    }

    void SetEndlessContinueUnavailable()
    {
        if (endlessAdStatus) endlessAdStatus.text = "Devam basamağı bulunamadı. Yeni tur başlatabilirsin.";
        if (endlessReviveButton) endlessReviveButton.gameObject.SetActive(false);
    }

    void CloseEndlessResult()
    {
        if (adBridge) adBridge.CancelOffer();
        if (endlessResult) Destroy(endlessResult);
        endlessResult = null; endlessReviveButton = null; endlessAdStatus = null; endlessReviveOfferReady = false;
    }

    public void OnRunRestarted()
    {
        if (run && run.Active) return;
        endlessReviveUsed = false;
        CloseEndlessResult();
    }

    void OnDestroy(){Time.timeScale=1;IsMenu=false;Instance=null;if(map!=null)map.Dispose();if(run)Destroy(run.gameObject);if(health)Destroy(health.gameObject);if(adBridge)Destroy(adBridge.gameObject);if(canvas)Destroy(canvas.gameObject);PlayerPrefs.Save();}
}

