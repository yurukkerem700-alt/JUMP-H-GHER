using UnityEngine;

public class PlayerVisual2D : MonoBehaviour
{
    const float VisualScale=1.25f; // karakterin görünür boyutu (1 = eski boyut)
    public Texture2D animationSheet;
    public Material spriteMaterial;
    Sprite[] frames;
    SpriteRenderer display;
    Rigidbody2D body;
    PlayerController controller;
    bool wasGrounded = true;
    float landedAt = -100;
    public int CurrentFrame { get; private set; }
    // Squash & stretch: ayaklardan sabit, yay gibi geri döner.
    Vector2 squash = Vector2.one, squashVelocity;
    float previousVy;

    void Start()
    {
        body=GetComponent<Rigidbody2D>(); controller=GetComponent<PlayerController>();
        foreach(var old in GetComponentsInChildren<SpriteRenderer>()) old.enabled=false;
        var visual=new GameObject("Kok Gezgin Gorseli",typeof(SpriteRenderer));
        visual.transform.SetParent(transform,false); visual.transform.localPosition=new Vector3(0,-.3f,0);
        display=visual.GetComponent<SpriteRenderer>(); display.sharedMaterial=spriteMaterial; display.sortingOrder=20;
        // Sprite tabanı ayakta olduğu için ayaklardan yukarı büyür; çarpışma kutusu ve fizik değişmez.
        visual.transform.localScale=Vector3.one*VisualScale;
        Juice.Ensure(spriteMaterial);
        controller.Jumped+=OnJumped;
        Costumes.DefaultSheet=animationSheet;
        Costumes.Changed+=ApplyCostume; ApplyCostume();
    }

    // Seçili kostümün sayfasından 8 kareyi kurar; sayfa yoksa ya da okunamıyorsa varsayılan gezgin kullanılır.
    void ApplyCostume()
    {
        var sheet=Costumes.Sheet(Costumes.SelectedId); if(!sheet) sheet=animationSheet;
        if(frames!=null) foreach(var old in frames) if(old) Destroy(old);
        int w=sheet.width/4,h=sheet.height/2;
        var bottoms=new int[8]; int tallest=1;
        for(int i=0;i<8;i++)
        {
            var pixels=sheet.GetPixels((i%4)*w,(1-i/4)*h,w,h);
            int low=h-1,high=0;
            for(int y=0;y<h;y++)
            {
                int count=0; for(int x=0;x<w;x++) if(pixels[y*w+x].a>.5f) count++;
                if(count>=5) {low=Mathf.Min(low,y);high=Mathf.Max(high,y);}
            }
            bottoms[i]=low; tallest=Mathf.Max(tallest,high-low+1);
        }
        frames=new Sprite[8]; float ppu=tallest/1.08f;
        for(int i=0;i<8;i++) frames[i]=Sprite.Create(sheet,new Rect((i%4)*w,(1-i/4)*h,w,h),new Vector2(.5f,(bottoms[i]+.5f)/h),ppu);
        display.sprite=frames[CurrentFrame];
    }

    void Update()
    {
        var support=controller.GroundPlatform(out bool grounded);
        float relative=body.linearVelocity.x-(support?support.CurrentVelocity.x:0);
        if(grounded&&!wasGrounded)
        {
            landedAt=Time.time;
            float impact=Mathf.Clamp01(-previousVy/14f); // sert inişte daha çok ezilir
            if(impact>.2f) Juice.Dust(display.transform.position,impact);
            squash=new Vector2(1f+.28f*impact+.06f,1f-.30f*impact-.06f); squashVelocity=Vector2.zero;
        }
        previousVy=body.linearVelocity.y;
        if(!grounded) CurrentFrame=body.linearVelocity.y>.15f?5:6;
        else if(Time.time-landedAt<.12f) CurrentFrame=7;
        else if(Mathf.Abs(relative)>.3f) CurrentFrame=1+Mathf.FloorToInt(Time.time*8)%4;
        else CurrentFrame=0;
        if(GameInput.Horizontal>.1f) display.flipX=false;
        if(GameInput.Horizontal<-.1f) display.flipX=true;
        display.sprite=frames[CurrentFrame]; wasGrounded=grounded;
        // Sönümlü yay: scale 1'e döner (unscaledDeltaTime değil; duraklatınca zaten donar).
        float dt=Mathf.Min(Time.deltaTime,.05f);
        Vector2 diff=squash-Vector2.one;
        squashVelocity+=(-diff*320f-squashVelocity*16f)*dt;
        squash+=squashVelocity*dt;
        display.transform.localScale=new Vector3(VisualScale*squash.x,VisualScale*squash.y,VisualScale);
    }

    void OnJumped()
    {
        Juice.Dust(display.transform.position,0f);
        squash=new Vector2(.82f,1.22f); squashVelocity=Vector2.zero;
    }
    void OnDestroy() {if(controller) controller.Jumped-=OnJumped; Costumes.Changed-=ApplyCostume; if(frames!=null) foreach(var frame in frames) if(frame) Destroy(frame);}
}
