using System;
using UnityEngine;

public class TowerPowers : MonoBehaviour
{
    public static TowerPowers Instance {get;private set;}
    public bool DoubleJumpActive=>Time.time<doubleUntil;
    public bool MagnetActive=>Time.time<magnetUntil;
    public string Status=>DoubleJumpActive?"ÇİFT ZIPLAMA "+Mathf.CeilToInt(doubleUntil-Time.time)+" sn":MagnetActive?"MIKNATIS "+Mathf.CeilToInt(magnetUntil-Time.time)+" sn":"";
    float doubleUntil,magnetUntil;bool wasDouble,wasMagnet;
    int pickupFloor=4,pickupKind;
    const float DoubleSeconds=10,MagnetSeconds=7.5f,MagnetRadius=2.5f;
    static readonly Color DoubleColor=new Color(1f,.62f,.22f),MagnetColor=new Color(.72f,.45f,1f);
    InfiniteTower tower; Transform pickup;
    SpriteRenderer halo,rim,ring,icon,tips,aura,pulse,burst;
    Sprite haloSprite,discSprite,ringSprite,dashSprite,chevronSprite,magnetSprite,magnetTipSprite;
    float burstStart=-9;Color burstColor;Vector3 burstPos;
    Transform wind;SpriteRenderer windArt;Transform[] windStreaks;
    void Awake(){Instance=this;}
    void Start()
    {
        tower=FindFirstObjectByType<InfiniteTower>();
        var coin=tower.coins.coin.GetComponentInChildren<SpriteRenderer>();var material=coin.sharedMaterial;
        BuildSprites();
        pickup=new GameObject("Güçlendirme").transform;
        halo=Part("Hale",pickup,haloSprite,material,24,Vector3.one*1.7f);
        rim=Part("Halka Zemini",pickup,discSprite,material,25,Vector3.one*.72f);
        ring=Part("Dönen Halka",pickup,dashSprite,material,26,Vector3.one*.9f);
        var core=Part("Çekirdek",pickup,discSprite,material,26,Vector3.one*.6f);core.color=new Color(.09f,.06f,.12f);
        icon=Part("Simge",pickup,chevronSprite,material,28,Vector3.one*.5f);
        tips=Part("Uç",pickup,magnetTipSprite,material,29,Vector3.one*.5f);
        aura=Part("Güç Aurası",null,haloSprite,material,19,Vector3.one);aura.gameObject.SetActive(false);
        pulse=Part("Mıknatıs Dalgası",null,ringSprite,material,18,Vector3.one);pulse.gameObject.SetActive(false);
        burst=Part("Toplama Patlaması",null,ringSprite,material,30,Vector3.one);burst.gameObject.SetActive(false);
        var texture=Texture2D.whiteTexture;
        wind=new GameObject("Yukarı Hava Akımı").transform;
        windArt=wind.gameObject.AddComponent<SpriteRenderer>();windArt.sprite=Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),Vector2.one*.5f,texture.width);
        windStreaks=new Transform[8];
        for(int i=0;i<windStreaks.Length;i++){var s=new GameObject("Hava Çizgisi",typeof(SpriteRenderer));s.transform.SetParent(wind,false);s.transform.localScale=new Vector3(.018f,.055f,1);var r=s.GetComponent<SpriteRenderer>();r.sprite=windArt.sprite;r.sharedMaterial=material;r.color=new Color(.6f,.8f,1,.35f);r.sortingOrder=-4;windStreaks[i]=s.transform;}
        windArt.sharedMaterial=material;windArt.sortingOrder=-5;windArt.color=new Color(.45f,.8f,1,.12f);
    }
    void Update() { TickGameplay(); }
    public void TickGameplay()
    {
        while(pickupFloor < tower.HighestReachedFloor - 2){pickupFloor+=10;pickupKind=1-pickupKind;}
        tower.EnsureThroughFloor(pickupFloor+1);
        var platform=tower.GetPlatform(pickupFloor);
        if(platform)
        {
            var tint=pickupKind==0?DoubleColor:MagnetColor;
            float t=Time.time;
            pickup.position=new Vector3(platform.transform.position.x,platform.Top+.8f+Mathf.Sin(t*3f)*.07f,0);
            float beat=.5f+.5f*Mathf.Sin(t*4f);
            halo.color=new Color(tint.r,tint.g,tint.b,.22f+.18f*beat);halo.transform.localScale=Vector3.one*(1.6f+.2f*beat);
            rim.color=tint;ring.color=Color.Lerp(tint,Color.white,.35f);
            ring.transform.localRotation=Quaternion.Euler(0,0,-t*70f);
            icon.sprite=pickupKind==0?chevronSprite:magnetSprite;icon.color=Color.Lerp(tint,Color.white,.55f);
            tips.gameObject.SetActive(pickupKind==1);tips.color=new Color(1f,.95f,.95f);
            icon.transform.localScale=Vector3.one*(.5f+.03f*beat);
            if(Vector2.Distance(tower.player.transform.position,pickup.position)<.75f)
            {
                if(pickupKind==0)doubleUntil=Time.time+DoubleSeconds;else magnetUntil=Time.time+MagnetSeconds;
                burstStart=Time.time;burstColor=tint;burstPos=pickup.position;SesAtolyesi.Ses.Pickup();
                pickupFloor+=10;pickupKind=1-pickupKind;
            }
        }
        UpdateEffects();
        if(MagnetActive)
        {
            var collider=tower.coins.coin.GetComponent<Collider2D>();
            if(Vector2.Distance(tower.player.transform.position,tower.coins.coin.position)<MagnetRadius)tower.player.CollectCoin(collider);
        }
        int section=Mathf.Max(0,tower.HighestReachedFloor)/TowerThemes.FloorsPerTheme;
        bool inClouds=section%5==1||section%5==2;
        wind.gameObject.SetActive(inClouds);
        if(inClouds)
        {
            int floor=section*TowerThemes.FloorsPerTheme+8;
            wind.position=new Vector3(0,InfiniteTower.SurfaceFor(floor)+1.4f,0);
            wind.localScale=new Vector3(1.6f,3.8f,1);
            windArt.color=new Color(.5f,.85f,1,.06f+.025f*Mathf.Sin(Time.time*4));
            for(int i=0;i<windStreaks.Length;i++)windStreaks[i].localPosition=new Vector3(Mathf.Sin(i*2.4f)*.38f,Mathf.Repeat(Time.time*.8f+i*.14f,1)-.5f,0);
        }
    }

    static SpriteRenderer Part(string name,Transform parent,Sprite sprite,Material material,int order,Vector3 scale)
    {
        var go=new GameObject(name,typeof(SpriteRenderer));if(parent)go.transform.SetParent(parent,false);go.transform.localScale=scale;
        var r=go.GetComponent<SpriteRenderer>();r.sprite=sprite;r.sharedMaterial=material;r.sortingOrder=order;return r;
    }
    void UpdateEffects()
    {
        var playerPos=tower.player.transform.position;float t=Time.time;
        bool dbl=DoubleJumpActive,mag=MagnetActive;
        if((wasDouble&&!dbl)||(wasMagnet&&!mag))SesAtolyesi.Ses.Swoosh(.7f); // buff süresi doldu
        wasDouble=dbl;wasMagnet=mag;
        aura.gameObject.SetActive(dbl||mag);
        if(dbl||mag)
        {
            float left=dbl?doubleUntil-t:magnetUntil-t;
            var tint=dbl?DoubleColor:MagnetColor;
            float blink=left<3f?(Mathf.Sin(t*14f)>0?1f:.35f):1f;
            aura.transform.position=playerPos;aura.transform.localScale=Vector3.one*(1.9f+.15f*Mathf.Sin(t*5f));
            aura.color=new Color(tint.r,tint.g,tint.b,.38f*blink);
        }
        pulse.gameObject.SetActive(mag);
        if(mag)
        {
            float phase=Mathf.Repeat(t*.9f,1f);
            pulse.transform.position=playerPos;pulse.transform.localScale=Vector3.one*(MagnetRadius*2f*phase);
            pulse.color=new Color(MagnetColor.r,MagnetColor.g,MagnetColor.b,.5f*(1f-phase));
        }
        float age=t-burstStart;
        burst.gameObject.SetActive(age>=0&&age<.5f);
        if(age>=0&&age<.5f)
        {
            float k=age/.5f;burst.transform.position=burstPos;burst.transform.localScale=Vector3.one*(.8f+2.2f*k);
            burst.color=new Color(burstColor.r,burstColor.g,burstColor.b,1f-k);
        }
    }

    // ---- Prosedürel simgeler (görsel dosyası gerekmez) ----
    static Sprite Make(Func<float,float,float> alpha)
    {
        const int N=128;var tex=new Texture2D(N,N,TextureFormat.RGBA32,false){filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp};
        var px=new Color32[N*N];
        for(int y=0;y<N;y++)for(int x=0;x<N;x++)
        {
            float a=Mathf.Clamp01(alpha((x+.5f)/N*2f-1f,(y+.5f)/N*2f-1f));
            px[y*N+x]=new Color32(255,255,255,(byte)Mathf.RoundToInt(a*255f));
        }
        tex.SetPixels32(px);tex.Apply();
        return Sprite.Create(tex,new Rect(0,0,N,N),Vector2.one*.5f,N);
    }
    static float Edge(float signedDistance)=>.5f-signedDistance*64f; // 1 px = 1/64 birim
    static float SegDist(float px,float py,float ax,float ay,float bx,float by)
    {
        float dx=bx-ax,dy=by-ay,t=Mathf.Clamp01(((px-ax)*dx+(py-ay)*dy)/(dx*dx+dy*dy));
        return Mathf.Sqrt((px-ax-t*dx)*(px-ax-t*dx)+(py-ay-t*dy)*(py-ay-t*dy));
    }
    static float Chevron(float x,float y,float yOff)=>Mathf.Min(SegDist(x,y,-.62f,-.2f+yOff,0,.3f+yOff),SegDist(x,y,.62f,-.2f+yOff,0,.3f+yOff))-.11f;
    // Nal mıknatıs: U gövde (legs y<0..), uçlar ayrı sprite
    static float MagnetBody(float x,float y)
    {
        const float r=.46f,h=.1f;
        float arc=y<.05f?Mathf.Abs(Mathf.Sqrt(x*x+(y-.05f)*(y-.05f))-r)-h:9f;
        float legs=Mathf.Min(SegDist(x,y,-r,.05f,-r,.3f),SegDist(x,y,r,.05f,r,.3f))-h;
        return Mathf.Min(arc,legs);
    }
    static float MagnetTips(float x,float y)
    {
        const float r=.46f,h=.1f;
        return Mathf.Min(SegDist(x,y,-r,.32f,-r,.72f),SegDist(x,y,r,.32f,r,.72f))-h;
    }
    void BuildSprites()
    {
        haloSprite=Make((x,y)=>{float r=Mathf.Sqrt(x*x+y*y);return r>=1?0:Mathf.Pow(1-r,2f)*.95f;});
        discSprite=Make((x,y)=>Edge(Mathf.Sqrt(x*x+y*y)-.97f));
        ringSprite=Make((x,y)=>Edge(Mathf.Abs(Mathf.Sqrt(x*x+y*y)-.93f)-.045f));
        dashSprite=Make((x,y)=>{float r=Mathf.Sqrt(x*x+y*y);float ang=Mathf.Atan2(y,x);float dash=Mathf.Sin(ang*6f)>-.15f?1f:0f;return Edge(Mathf.Abs(r-.93f)-.05f)*dash;});
        chevronSprite=Make((x,y)=>Edge(Mathf.Min(Chevron(x,y+.0f,.2f),Chevron(x,y,-.22f))*.9f));
        magnetSprite=Make((x,y)=>Edge(MagnetBody(x,y+.12f)));
        magnetTipSprite=Make((x,y)=>Edge(MagnetTips(x,y+.12f)));
    }
    public bool IsInWind(Vector2 pos)=>wind&&wind.gameObject.activeSelf&&Mathf.Abs(pos.x-wind.position.x)<.8f&&Mathf.Abs(pos.y-wind.position.y)<1.9f;
    public void ResetPowers(){doubleUntil=magnetUntil=0;pickupFloor=4;pickupKind=0;}
    void OnDestroy(){if(pickup)Destroy(pickup.gameObject);foreach(var r in new[]{aura,pulse,burst})if(r)Destroy(r.gameObject);if(wind)Destroy(wind.gameObject);}
}

