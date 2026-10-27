using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameFlow : MonoBehaviour
{
    public Sprite icon;
    public static bool IsMenu {get;private set;}
    Canvas canvas;GameObject overlay;TextMeshProUGUI heading,subtitle,status;Button start;float splashUntil;bool splash=true;bool begun;
    InfiniteTower tower;
    void Start()
    {
        tower=FindFirstObjectByType<InfiniteTower>();
        canvas=new GameObject("Ana Menü",typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster)).GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=1000;
        var scaler=canvas.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1080,1080);scaler.matchWidthOrHeight=.5f;
        overlay=new GameObject("Menu Perdesi",typeof(Image));overlay.transform.SetParent(canvas.transform,false);var rect=overlay.GetComponent<RectTransform>();rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;overlay.GetComponent<Image>().color=new Color(.035f,.025f,.045f,.94f);
        if(icon){var art=new GameObject("Gezgin İkonu",typeof(Image));art.transform.SetParent(overlay.transform,false);art.GetComponent<Image>().sprite=icon;art.GetComponent<Image>().preserveAspect=true;var ar=art.GetComponent<RectTransform>();ar.sizeDelta=new Vector2(250,250);ar.anchorMin=ar.anchorMax=new Vector2(.5f,.72f);ar.anchoredPosition=Vector2.zero;}
        heading=Text("KÖK YANKISI",.53f,62,new Color(1,.77f,.5f));
        subtitle=Text("SONSUZA DOĞRU YÜKSEL",.46f,23,new Color(.75f,.66f,.62f));
        start=Button("OYNA",.35f,()=>Begin(begun));
        Button("NASIL OYNANIR",.25f,()=>{subtitle.text="A / D veya oklar: yürü   •   W / Boşluk: zıpla\nHer coin bir üst katta. Güçleri topla, yüksel!\nTelefon: ekrandaki sol / sağ / zıpla düğmeleri";subtitle.fontSize=18;});
        Button(Application.platform==RuntimePlatform.WebGLPlayer?"TAM EKRAN":"ÇIKIŞ",.15f,()=>{if(Application.platform==RuntimePlatform.WebGLPlayer)Screen.fullScreen=!Screen.fullScreen;else Application.Quit();});
        status=Text("",.065f,18,new Color(.65f,.6f,.65f));
        var pause=new GameObject("Duraklat",typeof(Image),typeof(Button));pause.transform.SetParent(canvas.transform,false);pause.GetComponent<Image>().color=new Color(.16f,.1f,.15f,.8f);var pr=pause.GetComponent<RectTransform>();pr.anchorMin=pr.anchorMax=new Vector2(.5f,.96f);pr.sizeDelta=new Vector2(70,52);
        var pt=new GameObject("II",typeof(TextMeshProUGUI));pt.transform.SetParent(pause.transform,false);var textPause=pt.GetComponent<TextMeshProUGUI>();textPause.text="II";textPause.fontSize=26;textPause.alignment=TextAlignmentOptions.Center;textPause.raycastTarget=false;var ptr=textPause.rectTransform;ptr.anchorMin=Vector2.zero;ptr.anchorMax=Vector2.one;ptr.offsetMin=ptr.offsetMax=Vector2.zero;
        pause.GetComponent<Button>().onClick.AddListener(()=>{if(IsMenu&&begun)Begin(true);else Show(true);});pause.transform.SetAsFirstSibling();
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
        if(Input.GetKeyDown(KeyCode.Escape)||Input.GetKeyDown(KeyCode.P)){if(IsMenu&&begun)Begin(true);else Show(true);}
        if(IsMenu){int best=PlayerPrefs.GetInt("BestFloor",0);status.text="EN YÜKSEK KAT  "+best;}
        else if(tower.HighestReachedFloor+1>PlayerPrefs.GetInt("BestFloor",0))PlayerPrefs.SetInt("BestFloor",tower.HighestReachedFloor+1);
    }
    public void Begin(bool resume){if(!resume&&begun)tower.player.RestartRun();begun=true;Show(false);}
    public void Show(bool visible){IsMenu=visible;overlay.SetActive(visible);Time.timeScale=visible?0:1;GameInput.Clear();if(begun)start.GetComponentInChildren<TextMeshProUGUI>().text="DEVAM ET";}
    void OnDestroy(){Time.timeScale=1;IsMenu=false;if(canvas)Destroy(canvas.gameObject);PlayerPrefs.Save();}
}

