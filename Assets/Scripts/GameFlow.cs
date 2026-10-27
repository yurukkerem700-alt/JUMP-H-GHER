using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameFlow : MonoBehaviour
{
    public Sprite icon;
    public static GameFlow Instance {get;private set;}
    public static bool IsMenu {get;private set;}
    bool menuVisible,holding;LevelMap map;LevelRun run;PlayerHealth health;
    Canvas canvas;GameObject overlay;TextMeshProUGUI heading,subtitle,status;Button start;float splashUntil;bool splash=true;bool begun;
    InfiniteTower tower;
    void Awake(){Instance=this;}
    void Start()
    {
        tower=FindFirstObjectByType<InfiniteTower>();
        canvas=new GameObject("Ana Menü",typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster)).GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=1000;
        var scaler=canvas.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1080,1080);scaler.matchWidthOrHeight=.5f;
        overlay=new GameObject("Menu Perdesi",typeof(Image));overlay.transform.SetParent(canvas.transform,false);var rect=overlay.GetComponent<RectTransform>();rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;overlay.GetComponent<Image>().color=new Color(.035f,.025f,.045f,.94f);
        if(icon){var art=new GameObject("Gezgin İkonu",typeof(Image));art.transform.SetParent(overlay.transform,false);art.GetComponent<Image>().sprite=icon;art.GetComponent<Image>().preserveAspect=true;var ar=art.GetComponent<RectTransform>();ar.sizeDelta=new Vector2(250,250);ar.anchorMin=ar.anchorMax=new Vector2(.5f,.72f);ar.anchoredPosition=Vector2.zero;}
        heading=Text("KÖK YANKISI",.55f,62,new Color(1,.77f,.5f));
        subtitle=Text("100 BÖLÜMDE ZİRVEYE YÜKSEL",.48f,23,new Color(.75f,.66f,.62f));
        status=Text("",.41f,18,new Color(.65f,.6f,.65f));
        start=Button("OYNA",.33f,()=>{if(begun)Begin(true);else map.Open();});
        Button("BÖLÜM HARİTASI",.235f,()=>map.Open());
        Button("NASIL OYNANIR",.14f,()=>{subtitle.text="A / D veya oklar: yürü   •   W / Boşluk: zıpla\nBölümün hedefini tamamla, hızlı bitirerek 3 yıldız topla.\nTelefon: ekrandaki sol / sağ / zıpla düğmeleri";subtitle.fontSize=18;});
        Button(Application.platform==RuntimePlatform.WebGLPlayer?"TAM EKRAN":"ÇIKIŞ",.045f,()=>{if(Application.platform==RuntimePlatform.WebGLPlayer)Screen.fullScreen=!Screen.fullScreen;else Application.Quit();});
        var pause=new GameObject("Duraklat",typeof(Image),typeof(Button));pause.transform.SetParent(canvas.transform,false);pause.GetComponent<Image>().color=new Color(.16f,.1f,.15f,.8f);var pr=pause.GetComponent<RectTransform>();pr.anchorMin=pr.anchorMax=new Vector2(.5f,.96f);pr.sizeDelta=new Vector2(70,52);
        var pt=new GameObject("II",typeof(TextMeshProUGUI));pt.transform.SetParent(pause.transform,false);var textPause=pt.GetComponent<TextMeshProUGUI>();textPause.text="II";textPause.fontSize=26;textPause.alignment=TextAlignmentOptions.Center;textPause.raycastTarget=false;var ptr=textPause.rectTransform;ptr.anchorMin=Vector2.zero;ptr.anchorMax=Vector2.one;ptr.offsetMin=ptr.offsetMax=Vector2.zero;
        pause.GetComponent<Button>().onClick.AddListener(()=>{if(holding)return;if(map.IsOpen)map.Close();else if(IsMenu&&begun)Begin(true);else Show(true);});pause.transform.SetAsFirstSibling();
        map=new LevelMap(canvas.transform,tower.floorLabel.font,StartLevel,StartEndless,()=>map.Close());
        health=new GameObject("Can Yöneticisi").AddComponent<PlayerHealth>();health.Init(tower);health.Died+=()=>{if(!run.Active)tower.player.RestartRun();};
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
    void Update()
    {
        if(splash){bool ready=Time.unscaledTime>=splashUntil;start.gameObject.SetActive(ready);if(ready)splash=false;}
        if(!holding&&(Input.GetKeyDown(KeyCode.Escape)||Input.GetKeyDown(KeyCode.P))){if(map.IsOpen)map.Close();else if(IsMenu&&begun)Begin(true);else Show(true);}
        if(IsMenu){int best=PlayerPrefs.GetInt("BestFloor",0);status.text="EN YÜKSEK KAT  "+best+"   •   YILDIZ  "+LevelDefs.TotalStars()+" / "+LevelDefs.Count*3;}
        else if(tower.HighestReachedFloor+1>PlayerPrefs.GetInt("BestFloor",0))PlayerPrefs.SetInt("BestFloor",tower.HighestReachedFloor+1);
    }
    public void Begin(bool resume){if(!resume&&begun)tower.player.RestartRun();begun=true;Show(false);}
    public void Show(bool visible){menuVisible=visible;overlay.SetActive(visible);Apply();if(begun)start.GetComponentInChildren<TextMeshProUGUI>().text="DEVAM ET";}
    // Sonuç ekranı açıkken oyun donar ama ana menü görünmez.
    public void Hold(bool on){holding=on;Apply();}
    void Apply(){IsMenu=menuVisible||holding;Time.timeScale=IsMenu?0:1;GameInput.Clear();}
    public void StartLevel(int n){map.Close();begun=true;Show(false);run.Begin(n);}
    void StartEndless(){map.Close();run.Stop();begun=true;Show(false);tower.player.RestartRun();}
    public void OpenMap(){Show(true);map.Open();}
    void OnDestroy(){Time.timeScale=1;IsMenu=false;Instance=null;if(run)Destroy(run.gameObject);if(health)Destroy(health.gameObject);if(canvas)Destroy(canvas.gameObject);PlayerPrefs.Save();}
}

