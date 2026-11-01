using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class GameFlow : MonoBehaviour
{
    public Sprite icon;
    public static GameFlow Instance {get;private set;}
    public static bool IsMenu {get;private set;}
    bool menuVisible,holding;LevelMap map;SettingsScreen settings;CostumeScreen costumes;LevelRun run;PlayerHealth health;
    Canvas canvas;GameObject overlay;TextMeshProUGUI heading,subtitle,status;Button start;float splashUntil;bool splash=true;bool begun;
    InfiniteTower tower;
    void Awake(){Instance=this;}
    void Start()
    {
        tower=FindFirstObjectByType<InfiniteTower>();
        canvas=new GameObject("Ana Menü",typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster)).GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=1000;
        var scaler=canvas.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1080,1080);scaler.matchWidthOrHeight=.5f;
        overlay=new GameObject("Menu Perdesi",typeof(Image));overlay.transform.SetParent(canvas.transform,false);var rect=overlay.GetComponent<RectTransform>();rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;overlay.GetComponent<Image>().color=new Color(.035f,.025f,.045f,1f);
        BuildMenu();
        var pause=new GameObject("Duraklat",typeof(Image),typeof(Button));pause.transform.SetParent(canvas.transform,false);pause.GetComponent<Image>().color=new Color(.16f,.1f,.15f,.8f);var pr=pause.GetComponent<RectTransform>();pr.anchorMin=pr.anchorMax=new Vector2(.5f,.96f);pr.sizeDelta=new Vector2(70,52);
        var pt=new GameObject("II",typeof(TextMeshProUGUI));pt.transform.SetParent(pause.transform,false);var textPause=pt.GetComponent<TextMeshProUGUI>();textPause.text="II";textPause.fontSize=26;textPause.alignment=TextAlignmentOptions.Center;textPause.raycastTarget=false;var ptr=textPause.rectTransform;ptr.anchorMin=Vector2.zero;ptr.anchorMax=Vector2.one;ptr.offsetMin=ptr.offsetMax=Vector2.zero;
        pause.GetComponent<Button>().onClick.AddListener(()=>{if(holding)return;SesAtolyesi.Ses.UiClick();if(settings.IsOpen)settings.Close();else if(costumes.IsOpen)costumes.Close();else if(map.IsOpen)map.Close();else if(IsMenu&&begun)Begin(true);else Show(true);});pause.transform.SetAsFirstSibling();
        map=new LevelMap(canvas.transform,tower.floorLabel.font,StartLevel,StartEndless,()=>map.Close());
        settings=new SettingsScreen(canvas.transform,tower.floorLabel.font,()=>settings.Close());
        costumes=new CostumeScreen(canvas.transform,tower.floorLabel.font,()=>costumes.Close());
        health=new GameObject("Can Yöneticisi").AddComponent<PlayerHealth>();health.Init(tower);health.Died+=()=>{if(!run.Active)tower.player.RestartRun();};
        run=new GameObject("Bölüm Yöneticisi").AddComponent<LevelRun>();run.Init(this,tower);
        new GameObject("Mezar Süsü").AddComponent<GraveDecor>().Init(tower);
        splashUntil=Time.unscaledTime+1.1f;Show(true);
#if UNITY_EDITOR
        if(UnityEditor.SessionState.GetBool("NextVerify",false)||UnityEditor.SessionState.GetBool("PresentationVerify",false)||UnityEditor.SessionState.GetBool("TowerVerify",false)){splash=false;Begin(false);}
#endif
    }
    // Ana menü: boyalı harita resmi arkada, çelik levha içinde ikon, başlık ve butonlar. Geniş ekranda iki sütun, dikeyde tek sütun.
    Sprite plate,glowSprite;RectTransform panel,halo;Image haloImg,iconGlow;TextMeshProUGUI titleGlow;
    const string Tagline="100 BÖLÜMDE ZİRVEYE YÜKSEL";
    const string HowTo="A / D veya oklar: yürü   •   W / Boşluk: zıpla   •   F / K: tekme (düşmanı bir kat aşağı düşürür)\nBölümün hedefini tamamla, hızlı bitirerek 3 yıldız topla.\nTelefon: ekranın altında parmağını sürükle, karakter o yöne gider (yukarı = zıpla)";
    struct SlotInfo{public RectTransform r;public Vector4 n,w;}
    readonly System.Collections.Generic.List<SlotInfo> slots=new System.Collections.Generic.List<SlotInfo>();
    Image[] embers;float[] emberSeed;Vector2 lastSize;float panelScale=1;
    public static Sprite Sliced(string name,Vector4 border)
    {
        var tex=Resources.Load<Texture2D>("UI/"+name);
        return tex?Sprite.Create(tex,new Rect(0,0,tex.width,tex.height),Vector2.one*.5f,100,0,SpriteMeshType.FullRect,border):null;
    }
    // Slot: (merkez x, üstten uzaklık, genişlik, yükseklik) dar ve geniş düzen için; ölçüler tuval birimidir.
    T Slot<T>(T c,Vector4 narrow,Vector4 wide) where T:Component{slots.Add(new SlotInfo{r=c.GetComponent<RectTransform>(),n=narrow,w=wide});return c;}
    Image Pic(string name,Transform parent,Sprite sprite,Color color){var go=new GameObject(name,typeof(Image));go.transform.SetParent(parent,false);var i=go.GetComponent<Image>();i.sprite=sprite;i.color=color;i.raycastTarget=false;return i;}
    void BuildMenu()
    {
        plate=Sliced("MapPlateSide",new Vector4(36,24,36,24));
        glowSprite=UiArt.Build(128,128,128,(u,v)=>{float a=Mathf.Clamp01(1-Mathf.Sqrt(u*u+v*v)*2);return a*a;});
        var back=Resources.Load<Texture2D>("UI/MapBackground");
        if(back)
        {
            var bg=new GameObject("Menü Resmi",typeof(Image),typeof(AspectRatioFitter));bg.transform.SetParent(overlay.transform,false);
            // Haritadaki düğüm halkalarını dışarıda bırakmak için yalnız üst bölüm (ay, kale, heykel) kullanılır.
            float cw=back.width*.68f,chh=back.height*.43f;
            var bi=bg.GetComponent<Image>();bi.sprite=Sprite.Create(back,new Rect(back.width-cw,back.height-chh,cw,chh),Vector2.one*.5f,100);bi.raycastTarget=false;
            var fit=bg.GetComponent<AspectRatioFitter>();fit.aspectMode=AspectRatioFitter.AspectMode.EnvelopeParent;fit.aspectRatio=cw/chh;
        }
        var dim=Pic("Karartma",overlay.transform,null,new Color(.03f,.01f,.04f,.5f));var dr=dim.rectTransform;dr.anchorMin=Vector2.zero;dr.anchorMax=Vector2.one;dr.offsetMin=dr.offsetMax=Vector2.zero;
        // Köz taneleri: yavaşça yükselen, titreyen turuncu noktalar.
        embers=new Image[34];emberSeed=new float[34];
        for(int i=0;i<embers.Length;i++){var e=Pic("Köz",overlay.transform,UiArt.Disc,new Color(1,.55f,.2f,0));e.rectTransform.anchorMin=e.rectTransform.anchorMax=Vector2.zero;float sz=Random.Range(4f,10f);e.rectTransform.sizeDelta=new Vector2(sz,sz);embers[i]=e;emberSeed[i]=Random.value*100;}
        haloImg=Pic("Işıma",overlay.transform,glowSprite,new Color(.9f,.2f,.1f,.35f));halo=haloImg.rectTransform;halo.anchorMin=halo.anchorMax=new Vector2(.5f,.5f);
        var pgo=new GameObject("Menü Levhası",typeof(Image));pgo.transform.SetParent(overlay.transform,false);panel=pgo.GetComponent<RectTransform>();
        panel.anchorMin=panel.anchorMax=new Vector2(.5f,.5f);
        var pi=pgo.GetComponent<Image>();pi.raycastTarget=false;
        if(plate){pi.sprite=plate;pi.type=Image.Type.Sliced;pi.color=Color.white;}else pi.color=new Color(.07f,.05f,.06f,.95f);
        if(icon)
        {
            iconGlow=Slot(Pic("İkon Işıması",panel,glowSprite,new Color(1,.45f,.15f,.55f)),new Vector4(0,-10,470,320),new Vector4(-270,50,520,440));
            var art=Slot(Pic("Gezgin İkonu",panel,icon,Color.white),new Vector4(0,68,180,180),new Vector4(-270,124,258,258));art.preserveAspect=true;
            var fr=Slot(Pic("İkon Çerçevesi",panel,plate,new Color(1,.9f,.8f)),new Vector4(0,44,252,228),new Vector4(-270,100,330,306));
            if(plate){fr.type=Image.Type.Sliced;fr.fillCenter=false;}
        }
        titleGlow=Slot(Label("KÖK YANKISI",62,new Color(1,.2f,.08f,.5f)),new Vector4(0,288,580,84),new Vector4(250,34,520,92));
        titleGlow.outlineColor=new Color32(255,70,20,255);titleGlow.outlineWidth=.5f;
        heading=Slot(Label("KÖK YANKISI",62,Color.white),new Vector4(0,288,580,84),new Vector4(250,34,520,92));
        heading.enableVertexGradient=true;heading.colorGradient=new VertexGradient(new Color(1,.95f,.72f),new Color(1,.95f,.72f),new Color(1,.5f,.2f),new Color(1,.5f,.2f));
        heading.outlineColor=new Color32(40,6,6,255);heading.outlineWidth=.22f;
        foreach(var h in new[]{heading,titleGlow}){h.enableAutoSizing=true;h.fontSizeMin=28;h.fontSizeMax=74;}
        // Başlık altı süs çizgisi ve elmas.
        Slot(Pic("Çizgi",panel,glowSprite,new Color(1,.6f,.3f,.9f)),new Vector4(0,380,420,10),new Vector4(250,130,400,10));
        var dia=Slot(Pic("Elmas",panel,null,new Color(1,.7f,.35f)),new Vector4(0,375,12,12),new Vector4(250,129,12,12));dia.rectTransform.localRotation=Quaternion.Euler(0,0,45);
        subtitle=Slot(Label(Tagline,23,new Color(.82f,.72f,.66f)),new Vector4(0,398,560,104),new Vector4(250,146,480,70));
        subtitle.enableAutoSizing=true;subtitle.fontSizeMin=11;subtitle.fontSizeMax=23;subtitle.outlineColor=new Color32(10,2,4,255);subtitle.outlineWidth=.15f;
        status=Slot(Label("",18,new Color(.9f,.75f,.5f)),new Vector4(0,510,560,30),new Vector4(-270,420,380,30));
        start=MenuButton("OYNA",()=>{if(begun)Begin(true);else map.Open();},44,true,new Vector4(0,556,540,96),new Vector4(250,226,480,84));
        MenuButton("BÖLÜM HARİTASI",()=>map.Open(),25,false,new Vector4(0,666,540,66),new Vector4(250,320,480,58));
        MenuButton("KOSTÜMLER",()=>costumes.Open(),23,false,new Vector4(-138,744,264,66),new Vector4(128,388,236,58));
        MenuButton("AYARLAR",()=>settings.Open(),23,false,new Vector4(138,744,264,66),new Vector4(372,388,236,58));
        MenuButton("NASIL OYNANIR",()=>{subtitle.text=subtitle.text==HowTo?Tagline:HowTo;},25,false,new Vector4(0,822,540,66),new Vector4(250,456,480,58));
        MenuButton(Application.platform==RuntimePlatform.WebGLPlayer?"TAM EKRAN":"ÇIKIŞ",()=>{if(Application.platform==RuntimePlatform.WebGLPlayer)Screen.fullScreen=!Screen.fullScreen;else Application.Quit();},25,false,new Vector4(0,900,540,66),new Vector4(250,524,480,58));
        lastSize=Vector2.zero;Layout();
    }
    void Layout()
    {
        var size=canvas.GetComponent<RectTransform>().rect.size;if(size.x<1||size.y<1)return;
        bool wide=size.x/size.y>1.25f;lastSize=size;
        panel.sizeDelta=wide?new Vector2(1060,620):new Vector2(640,1000);
        panelScale=Mathf.Min(1,size.x*.95f/panel.sizeDelta.x,size.y*.94f/panel.sizeDelta.y);panel.localScale=Vector3.one*panelScale;
        halo.sizeDelta=panel.sizeDelta*panelScale*1.28f+new Vector2(120,120);
        foreach(var s in slots){var v=wide?s.w:s.n;var r=s.r;r.anchorMin=r.anchorMax=new Vector2(.5f,1);r.pivot=new Vector2(.5f,1);r.anchoredPosition=new Vector2(v.x,-v.y);r.sizeDelta=new Vector2(v.z,v.w);}
    }
    void Animate()
    {
        var size=canvas.GetComponent<RectTransform>().rect.size;if(size!=lastSize)Layout();
        float t=Time.unscaledTime;
        haloImg.color=new Color(.9f,.2f,.1f,.3f+.07f*Mathf.Sin(t*1.3f));
        if(iconGlow)iconGlow.color=new Color(1,.45f,.15f,.45f+.15f*Mathf.Sin(t*1.7f+1));
        titleGlow.color=new Color(1,.2f,.08f,.35f+.2f*Mathf.Sin(t*1.6f));
        var area=overlay.GetComponent<RectTransform>().rect.size;
        for(int i=0;i<embers.Length;i++)
        {
            float sd=emberSeed[i],k=Mathf.Repeat(t*(.035f+.02f*Mathf.Repeat(sd,1))+sd*.37f,1),x=Mathf.Repeat(sd*.173f,1)+.03f*Mathf.Sin(t*.6f+sd);
            float a=Mathf.Sin(k*Mathf.PI)*(.55f+.4f*Mathf.Sin(t*5+sd*7));
            embers[i].rectTransform.anchoredPosition=new Vector2(x*area.x,k*area.y);embers[i].color=new Color(1,.55f+.25f*Mathf.Sin(sd),.2f,Mathf.Clamp01(a));
        }
    }
    TextMeshProUGUI Label(string value,float size,Color color)
    {
        var go=new GameObject(value,typeof(TextMeshProUGUI));go.transform.SetParent(panel,false);var t=go.GetComponent<TextMeshProUGUI>();
        t.font=tower.floorLabel.font;t.text=value;t.fontSize=size;t.color=color;t.alignment=TextAlignmentOptions.Center;t.raycastTarget=false;return t;
    }
    Button MenuButton(string title,UnityEngine.Events.UnityAction action,float fontSize,bool primary,Vector4 narrow,Vector4 wide)
    {
        var go=new GameObject(title,typeof(Image),typeof(Button));go.transform.SetParent(panel,false);Slot(go.GetComponent<RectTransform>(),narrow,wide);
        var img=go.GetComponent<Image>();
        if(plate){img.sprite=plate;img.type=Image.Type.Sliced;}else img.color=new Color(.22f,.12f,.14f,.95f);
        var b=go.GetComponent<Button>();b.targetGraphic=img;b.transition=Selectable.Transition.None;
        b.onClick.AddListener(()=>{SesAtolyesi.Ses.UiClick();action();});
        // İç ışıma: ana düğmede kızıl-amber ve nabız atar, diğerlerinde fareyle üzerine gelince yanar.
        var glow=Pic("Parlama",go.transform,glowSprite,Color.white);var gr=glow.rectTransform;gr.anchorMin=Vector2.zero;gr.anchorMax=Vector2.one;gr.offsetMin=new Vector2(22,14);gr.offsetMax=new Vector2(-22,-14);
        var fx=go.AddComponent<MenuFx>();fx.face=img;fx.glow=glow;
        fx.rest=primary?new Color(1,.78f,.7f):new Color(.82f,.8f,.86f);fx.lit=primary?new Color(1.25f,1,.85f):new Color(1.3f,1.1f,1.05f);
        fx.glowCol=primary?new Color(1,.3f,.1f):new Color(.9f,.35f,.15f);fx.glowRest=primary?.7f:0;fx.glowLit=primary?1f:.5f;fx.pulse=primary;
        var t=Label(title,fontSize,new Color(1,.86f,.62f));t.transform.SetParent(go.transform,false);
        t.outlineColor=new Color32(30,5,5,255);t.outlineWidth=primary?.25f:.2f;
        if(primary){t.enableVertexGradient=true;t.color=Color.white;t.colorGradient=new VertexGradient(new Color(1,.97f,.8f),new Color(1,.97f,.8f),new Color(1,.62f,.3f),new Color(1,.62f,.3f));}
        var tr=t.rectTransform;tr.anchorMin=Vector2.zero;tr.anchorMax=Vector2.one;tr.offsetMin=new Vector2(8,4);tr.offsetMax=new Vector2(-8,-4);
        t.enableAutoSizing=true;t.fontSizeMax=fontSize;t.fontSizeMin=12;
        return b;
    }
    void Update()
    {
        if(splash){bool ready=Time.unscaledTime>=splashUntil;start.gameObject.SetActive(ready);if(ready)splash=false;}
        if(!holding&&(Input.GetKeyDown(KeyCode.Escape)||Input.GetKeyDown(KeyCode.P))){if(settings.IsOpen)settings.Close();else if(costumes.IsOpen)costumes.Close();else if(map.IsOpen)map.Close();else if(IsMenu&&begun)Begin(true);else Show(true);}
        if(IsMenu){Animate();bool sub=settings.IsOpen||costumes.IsOpen||map.IsOpen;if(panel.gameObject.activeSelf==sub){panel.gameObject.SetActive(!sub);halo.gameObject.SetActive(!sub);}int best=PlayerPrefs.GetInt("BestFloor",0);status.text="EN YÜKSEK KAT  "+best+"   •   YILDIZ  "+LevelDefs.TotalStars()+" / "+LevelDefs.Count*3;}
        else if(tower.HighestReachedFloor+1>PlayerPrefs.GetInt("BestFloor",0))PlayerPrefs.SetInt("BestFloor",tower.HighestReachedFloor+1);
    }
    public void Begin(bool resume){if(!resume&&begun)tower.player.RestartRun();begun=true;Show(false);}
    public void Show(bool visible){menuVisible=visible;if(subtitle)subtitle.text=Tagline;overlay.SetActive(visible);Apply();if(begun)start.GetComponentInChildren<TextMeshProUGUI>(true).text="DEVAM ET";}
    // Sonuç ekranı açıkken oyun donar ama ana menü görünmez.
    public void Hold(bool on){holding=on;Apply();}
    void Apply(){IsMenu=menuVisible||holding;Time.timeScale=IsMenu?0:1;GameInput.Clear();}
    public void StartLevel(int n){map.Close();begun=true;Show(false);run.Begin(n);}
    void StartEndless(){map.Close();run.Stop();run.BeginEndless();begun=true;Show(false);tower.player.RestartRun();}
    public void OpenMap(){if(run.Finished)run.Stop();Show(true);map.Open();}
    void OnDestroy(){Time.timeScale=1;IsMenu=false;Instance=null;if(run)Destroy(run.gameObject);if(health)Destroy(health.gameObject);if(canvas)Destroy(canvas.gameObject);PlayerPrefs.Save();}
}

// Menü düğmesi geri bildirimi: üzerine gelince büyür ve ışır, basılınca çöker; ana düğme nabız atar.
public class MenuFx : MonoBehaviour,IPointerEnterHandler,IPointerExitHandler,IPointerDownHandler,IPointerUpHandler
{
    public Image face,glow;public Color rest,lit,glowCol;public float glowRest,glowLit;public bool pulse;
    bool over,down;float k;
    void OnEnable(){over=down=false;k=0;Tick(0);}
    public void OnPointerEnter(PointerEventData e){over=true;}
    public void OnPointerExit(PointerEventData e){over=down=false;}
    public void OnPointerDown(PointerEventData e){down=true;}
    public void OnPointerUp(PointerEventData e){down=false;}
    void Update(){Tick(Time.unscaledDeltaTime);}
    void Tick(float dt)
    {
        if(!face||!glow)return;
        k=Mathf.MoveTowards(k,over?1:0,dt*8);
        transform.localScale=Vector3.one*Mathf.Lerp(1,1.035f,k)*(down?.965f:1);
        var c=Color.Lerp(rest,lit,k);float d=down?.8f:1;face.color=new Color(c.r*d,c.g*d,c.b*d,1);
        float g=Mathf.Lerp(glowRest,glowLit,k)+(pulse?Mathf.Sin(Time.unscaledTime*2.4f)*.14f:0);
        glow.color=new Color(glowCol.r,glowCol.g,glowCol.b,Mathf.Clamp01(g));
    }
}
