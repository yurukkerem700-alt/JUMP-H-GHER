using UnityEngine;

public class PlayerVisual2D : MonoBehaviour
{
    const float VisualScale=1.25f; // karakterin görünür boyutu (1 = eski boyut)
    public Texture2D animationSheet;
    public Material spriteMaterial;
    Sprite[] frames;
    SpriteRenderer display;
    CostumeRig2D rig;
    Rigidbody2D body;
    PlayerController controller;
    bool wasGrounded = true;
    float landedAt = -100;
    int kickStyle;
    float kickDirection;
    float kickStarted = -100;
    public int CurrentFrame { get; private set; }

    void Start()
    {
        body=GetComponent<Rigidbody2D>(); controller=GetComponent<PlayerController>();
        foreach(var old in GetComponentsInChildren<SpriteRenderer>()) old.enabled=false;
        var visual=new GameObject("Kok Gezgin Gorseli",typeof(SpriteRenderer));
        visual.transform.SetParent(transform,false); visual.transform.localPosition=new Vector3(0,-.3f,0);
        display=visual.GetComponent<SpriteRenderer>(); display.sharedMaterial=spriteMaterial; display.sortingOrder=20;
        // Sprite tabanı ayakta olduğu için ayaklardan yukarı büyür; çarpışma kutusu ve fizik değişmez.
        visual.transform.localScale=Vector3.one*VisualScale;
        if (!GetComponent<CostumeEffects>()) gameObject.AddComponent<CostumeEffects>();
        Costumes.DefaultSheet=animationSheet;
        Costumes.Changed+=ApplyCostume; GameInput.Kicked+=OnKick; ApplyCostume();
    }

    // Seçili kostümün sayfasından 8 kareyi kurar; sayfa yoksa ya da okunamıyorsa varsayılan gezgin kullanılır.
    void ApplyCostume()
    {
        CurrentFrame=0; kickStarted=-100; landedAt=-100; wasGrounded=true;
        if(rig) { rig.gameObject.SetActive(false); Destroy(rig.gameObject); rig=null; }
        var selectedSheet=Costumes.Sheet(Costumes.SelectedId);
        if(!selectedSheet && Costumes.FrameRows(Costumes.SelectedId)==3)
            throw new System.InvalidOperationException("Missing illustrated costume: "+Costumes.SelectedId);
        int rigIndex=selectedSheet?-1:CostumeRig2D.Index(Costumes.SelectedId);
        display.enabled=rigIndex<0;
        if(rigIndex>=0)
        {
            var go=new GameObject("Kostum Iskeleti",typeof(CostumeRig2D));
            go.transform.SetParent(transform,false); go.transform.localPosition=new Vector3(0,-.3f,0);
            rig=go.GetComponent<CostumeRig2D>(); rig.Init(controller,spriteMaterial,rigIndex);
            return;
        }
        var sheet=selectedSheet; if(!sheet) sheet=animationSheet;
        if(frames!=null) foreach(var old in frames) if(old) Destroy(old);
        int rows=sheet==animationSheet?2:Costumes.FrameRows(Costumes.SelectedId);
        frames=BuildFrames(sheet,rows);
        display.sprite=frames[CurrentFrame];
    }

    public static Sprite[] BuildFrames(Texture2D sheet,int rows)
    {
        int count=rows*4;
        int w=sheet.width/4,h=sheet.height/rows;
        var bottoms=new int[count]; int tallest=1;
        for(int i=0;i<count;i++)
        {
            var pixels=sheet.GetPixels((i%4)*w,(rows-1-i/4)*h,w,h);
            int low=h-1,high=0;
            for(int y=0;y<h;y++)
            {
                int opaque=0; for(int x=0;x<w;x++) if(pixels[y*w+x].a>.5f) opaque++;
                if(opaque>=5) {low=Mathf.Min(low,y);high=Mathf.Max(high,y);}
            }
            bottoms[i]=low; tallest=Mathf.Max(tallest,high-low+1);
        }
        var result=new Sprite[count]; float ppu=tallest/1.08f;
        for(int i=0;i<count;i++) result[i]=Sprite.Create(sheet,new Rect((i%4)*w,(rows-1-i/4)*h,w,h),new Vector2(.5f,(bottoms[i]+.5f)/h),ppu);
        return result;
    }

    void Update()
    {
        if(rig) return;
        var support=controller.GroundPlatform(out bool grounded);
        float relative=body.linearVelocity.x-(support?support.CurrentVelocity.x:0);
        if(grounded&&!wasGrounded) landedAt=Time.time;
        if(frames.Length>8 && kickStyle != 3 && Time.time-kickStarted<.3f) CurrentFrame=8+Mathf.Clamp(Mathf.FloorToInt((Time.time-kickStarted)*10),0,2);
        else if(!grounded) CurrentFrame=body.linearVelocity.y>.15f?5:6;
        else if(Time.time-landedAt<.12f) CurrentFrame=7;
        else if(Mathf.Abs(relative)>.3f) CurrentFrame=1+Mathf.FloorToInt(Time.time*(frames.Length>8?Mathf.Clamp(Mathf.Abs(relative)*2,6,12):8))%4;
        else CurrentFrame=frames.Length>8 && Mathf.Repeat(Time.time,3.4f)>3.1f?11:0;
        if (Time.time - kickStarted < EnemyManager.StrikeDuration) display.flipX = kickDirection < 0;
        else if (Mathf.Abs(GameInput.Horizontal) > .1f) display.flipX = GameInput.Horizontal < 0;
        float strike = Mathf.Sin(Mathf.PI * Mathf.Clamp01((Time.time - kickStarted) / EnemyManager.StrikeDuration));
        float direction = display.flipX ? -1 : 1;
        float phase = Mathf.Clamp01((Time.time - kickStarted) / EnemyManager.StrikeDuration);
        float angle = kickStyle == 2 ? -360 * Mathf.SmoothStep(0, 1, phase) : (kickStyle == 1 ? 55 : -22) * strike;
        display.transform.localPosition = new Vector3(direction * strike * (kickStyle == 3 ? .30f : .17f),
            -.3f + strike * (kickStyle == 1 ? .62f : kickStyle == 2 ? .22f : 0), 0);
        display.transform.localRotation = Quaternion.Euler(0, 0, angle * direction);
        display.transform.localScale = new Vector3(1 + .22f * strike, 1 - .12f * strike, 1) * VisualScale;
        display.sprite=frames[CurrentFrame]; wasGrounded=grounded;
    }
    void OnKick(int style, float direction) { kickStyle = style; kickDirection = direction; kickStarted = Time.time; }
    void OnDestroy() {Costumes.Changed-=ApplyCostume; GameInput.Kicked-=OnKick; if(frames!=null) foreach(var frame in frames) if(frame) Destroy(frame);}
}
