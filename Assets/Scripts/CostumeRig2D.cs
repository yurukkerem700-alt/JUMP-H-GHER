using UnityEngine;

// Transform bones keep the four new costumes independent of sprite-sheet sampling.
public class CostumeRig2D : MonoBehaviour
{
    public static readonly string[] Ids = { "gece_zirhcisi", "mercan_cadisi", "kum_saati_bekcisi", "buz_pervanesi", "bordo_kovboy" };
    public static readonly Color[] Tints = { new Color(.35f,.5f,1), new Color(1,.4f,.55f), new Color(1,.75f,.25f), new Color(.45f,1,1), new Color(.58f,.035f,.14f) };
    public static int Index(string id) { return System.Array.IndexOf(Ids,id); }
    Transform skeleton, chest, head, armL, armR, elbowL, elbowR, legL, legR, kneeL, kneeR, accessoryL, accessoryR;
    PlayerController controller;
    Rigidbody2D body;
    SpriteRenderer[] renderers;
    int style;
    float phase, landUntil, kickUntil;
    bool wasGrounded;
    float facing=1;
    Material material;
    Transform bag, artRoot;

    public void Init(PlayerController owner, Material mat, int index)
    {
        controller=owner; body=owner.GetComponent<Rigidbody2D>();
        Build(mat,index);
        GameInput.Kicked+=Kick;
    }
    // İskeleti ve parçaları kurar; oyuncuya bağlamaz (kostüm seçim ekranı önizlemesi de bunu kullanır).
    void Build(Material mat, int index)
    {
        material=mat; style=index;
        skeleton=Bone("Iskelet",transform,Vector2.zero);
        var dark=Color.Lerp(Tints[style],new Color(.07f,.06f,.13f),.7f);
        var skin=new Color(.98f,.91f,.86f);
        legL=Limb("Sol Kalca",skeleton,new Vector2(-.09f,.43f),style==4?skin:dark,.13f,.22f,out kneeL);
        legR=Limb("Sag Kalca",skeleton,new Vector2(.09f,.43f),style==4?skin:dark,.13f,.22f,out kneeR);
        chest=Bone("Gogus",skeleton,new Vector2(0,.65f));
        Part("Govde",chest,Vector2.zero,new Vector2(.36f,.42f),dark,22);
        Part("Zirh",chest,new Vector2(0,.04f),new Vector2(.28f,.29f),Tints[style],23);
        Part("Ruh",chest,new Vector2(0,.08f),Vector2.one*.1f,Color.white,25);
        head=Bone("Boyun",chest,new Vector2(0,.32f));
        Part("Bas",head,Vector2.zero,new Vector2(.29f,.28f),style==4?skin:dark,24);
        Color eyes=style==4?new Color(1,.025f,.025f):Tints[style]*1.3f;
        Part("Goz Sol",head,new Vector2(-.06f,.025f),new Vector2(.045f,.055f),eyes,26);
        Part("Goz Sag",head,new Vector2(.06f,.025f),new Vector2(.045f,.055f),eyes,26);
        armL=Limb("Sol Omuz",chest,new Vector2(-.21f,.12f),Tints[style],.11f,.19f,out elbowL);
        armR=Limb("Sag Omuz",chest,new Vector2(.21f,.12f),Tints[style],.11f,.19f,out elbowR);
        accessoryL=Bone("Sol Eklenti",chest,new Vector2(-.22f,.2f));
        accessoryR=Bone("Sag Eklenti",chest,new Vector2(.22f,.2f));
        if(style==0)
        {
            Part("Sol Boynuz",head,new Vector2(-.15f,.17f),new Vector2(.09f,.3f),Tints[style],23);
            Part("Sag Boynuz",head,new Vector2(.15f,.17f),new Vector2(.09f,.3f),Tints[style],23);
            Part("Pelerin",accessoryL,new Vector2(0,-.22f),new Vector2(.18f,.55f),dark,18);
            Part("Pelerin",accessoryR,new Vector2(0,-.22f),new Vector2(.18f,.55f),dark,18);
        }
        else if(style==1)
        {
            for(int i=0;i<5;i++) Part("Mercan Taci",head,new Vector2((i-2)*.07f,.15f),new Vector2(.055f,.16f+(i%2)*.12f),Tints[style],23);
            Part("Yuzgec",accessoryL,new Vector2(-.08f,-.08f),new Vector2(.27f,.38f),Tints[style],19);
            Part("Yuzgec",accessoryR,new Vector2(.08f,-.08f),new Vector2(.27f,.38f),Tints[style],19);
        }
        else if(style==2)
        {
            Part("Saat Halkasi",head,new Vector2(0,.04f),new Vector2(.44f,.44f),Tints[style],19);
            Part("Kum Ust",chest,new Vector2(0,.1f),new Vector2(.17f,.1f),Color.white,24);
            Part("Kum Alt",chest,new Vector2(0,-.1f),new Vector2(.17f,.1f),Tints[style],24);
            Part("Saat Sarkaci",accessoryR,new Vector2(.04f,-.24f),new Vector2(.09f,.45f),Tints[style],23);
        }
        else if(style==3)
        {
            Part("Sol Kanat",accessoryL,new Vector2(-.13f,0),new Vector2(.4f,.65f),Tints[style],18);
            Part("Sag Kanat",accessoryR,new Vector2(.13f,0),new Vector2(.4f,.65f),Tints[style],18);
            Part("Anten",head,new Vector2(-.09f,.2f),new Vector2(.04f,.25f),Color.white,24);
            Part("Anten",head,new Vector2(.09f,.2f),new Vector2(.04f,.25f),Color.white,24);
        }
        else
        {
            var blonde=new Color(1,.76f,.25f);
            Part("Sol Uzun Sari Sac",accessoryL,new Vector2(.03f,.04f),new Vector2(.16f,.65f),blonde,21);
            Part("Sag Uzun Sari Sac",accessoryR,new Vector2(-.03f,.04f),new Vector2(.16f,.65f),blonde,21);
            Part("Kakul",head,new Vector2(0,.1f),new Vector2(.29f,.1f),blonde,25);
            Part("Bordo Fotr Kenari",head,new Vector2(0,.17f),new Vector2(.55f,.085f),Tints[style],27);
            Part("Bordo Fotr Tepesi",head,new Vector2(0,.26f),new Vector2(.31f,.21f),Tints[style],27);
            Part("Siyah Sapka Bandi",head,new Vector2(0,.205f),new Vector2(.32f,.035f),dark,28);
            Part("Dekolte",chest,new Vector2(0,.15f),new Vector2(.14f,.19f),skin,24);
            Part("Bordo Etek Sol",chest,new Vector2(-.09f,-.24f),new Vector2(.22f,.42f),Tints[style],25);
            Part("Yirtmacli Etek Sag",chest,new Vector2(.13f,-.19f),new Vector2(.1f,.31f),Tints[style],25);
            foreach(var knee in new[]{kneeL,kneeR})
            {
                Part("Bordo Kovboy Cizmesi",knee,new Vector2(0,-.13f),new Vector2(.16f,.25f),Tints[style],26);
                Part("Cizme Sivri Burun",knee,new Vector2(.045f,-.22f),new Vector2(.23f,.09f),Tints[style],26);
                Part("Siyah Topuk",knee,new Vector2(-.04f,-.25f),new Vector2(.06f,.07f),dark,25);
            }
            foreach(var elbow in new[]{elbowL,elbowR})
            {
                Part("Beyaz Tenli El",elbow,new Vector2(0,-.19f),new Vector2(.09f,.1f),skin,26);
                for(int i=0;i<4;i++) Part("Uzun Beyaz Tirnak",elbow,new Vector2((i-1.5f)*.018f,-.26f),new Vector2(.012f,.1f),Color.white,27);
            }
            bag=Bone("Mini Canta Askisi",elbowR,new Vector2(.08f,-.2f));
            Part("Kirmizi Canta Sapi",bag,new Vector2(0,-.045f),new Vector2(.15f,.2f),new Color(.75f,.02f,.06f),25);
            Part("Sap Boslugu",bag,new Vector2(0,-.035f),new Vector2(.1f,.13f),dark,26);
            Part("Kirmizi Mini Canta",bag,new Vector2(0,-.14f),new Vector2(.22f,.17f),new Color(.95f,.035f,.08f),27);
            Part("Canta Tokasi",bag,new Vector2(0,-.1f),Vector2.one*.035f,new Color(1,.8f,.4f),28);
        }
        if(style==4) AttachArt();
        renderers=GetComponentsInChildren<SpriteRenderer>();
    }
    // Bordo Kovboy için boyalı çizim (Resources/Costumes/bordo_kovboy_art.png); yoksa kod ile çizilen iskelet kalır.
    static Sprite art; static bool artTried;
    public static Sprite Art()
    {
        if(!artTried)
        {
            artTried=true;
            var tex=Resources.Load<Texture2D>("Costumes/bordo_kovboy_art");
            if(tex){ tex.filterMode=FilterMode.Bilinear; art=Sprite.Create(tex,new Rect(0,0,tex.width,tex.height),new Vector2(.5f,0),tex.height/1.5f); }
        }
        return art;
    }
    void AttachArt()
    {
        var picture=Art(); if(!picture) return;
        foreach(var r in GetComponentsInChildren<SpriteRenderer>(true)) r.enabled=false;
        artRoot=Bone("Boyali Resim",skeleton,Vector2.zero);
        var sr=artRoot.gameObject.AddComponent<SpriteRenderer>(); sr.sprite=picture; sr.sharedMaterial=material; sr.sortingOrder=24;
    }
    static Transform Bone(string name, Transform parent, Vector2 at)
    {
        var t=new GameObject(name).transform; t.SetParent(parent,false); t.localPosition=at; return t;
    }
    void Part(string name, Transform parent, Vector2 at, Vector2 size, Color color, int order)
    {
        var t=Bone(name,parent,at); t.localScale=new Vector3(size.x,size.y,1);
        var r=t.gameObject.AddComponent<SpriteRenderer>(); r.sprite=UiArt.Disc; r.sharedMaterial=material; r.color=color; r.sortingOrder=order;
    }
    Transform Limb(string name, Transform parent, Vector2 at, Color color, float width, float length, out Transform joint)
    {
        var root=Bone(name,parent,at);
        Part("Ust",root,new Vector2(0,-length*.5f),new Vector2(width,length),color,23);
        joint=Bone("Eklem",root,new Vector2(0,-length));
        Part("Alt",joint,new Vector2(0,-length*.5f),new Vector2(width*.8f,length),color,24);
        return root;
    }
    static void Angle(Transform t,float angle) { t.localRotation=Quaternion.Euler(0,0,angle); }
    void Kick() { if(isActiveAndEnabled) kickUntil=Time.time+.25f; }
    void LateUpdate()
    {
        if(!controller || GameFlow.IsMenu) return;
        var support=controller.GroundPlatform(out bool ground);
        float speed=body.linearVelocity.x-(support?support.CurrentVelocity.x:0);
        if(Mathf.Abs(GameInput.Horizontal)>.1f) facing=Mathf.Sign(GameInput.Horizontal);
        transform.localScale=new Vector3(1.25f*facing,1.25f,1);
        if(ground&&!wasGrounded) landUntil=Time.time+.14f;
        phase+=Time.deltaTime*(Mathf.Abs(speed)>.3f?10+style:2);
        float stride=ground?Mathf.Clamp01(Mathf.Abs(speed)/3)*Mathf.Sin(phase)*30:0;
        float jump=ground?0:body.linearVelocity.y>0?1:-1;
        Angle(legL,stride+(jump>0?-28:jump<0?15:0)); Angle(legR,-stride+(jump>0?20:jump<0?-12:0));
        Angle(kneeL,Mathf.Max(0,-stride)*1.2f+(jump>0?50:0)); Angle(kneeR,Mathf.Max(0,stride)*1.2f+(jump>0?35:0));
        Angle(armL,-stride*.8f+jump*35); Angle(armR,stride*.8f-jump*35);
        Angle(elbowL,-15-Mathf.Abs(stride)*.4f); Angle(elbowR,15+Mathf.Abs(stride)*.4f);
        if(Time.time<kickUntil) { Angle(legR,-85); Angle(kneeR,10); Angle(armL,45); }
        bool land=Time.time<landUntil;
        skeleton.localScale=new Vector3(land?1.12f:1,land?.85f:1,1);
        skeleton.localPosition=new Vector3(0,ground?Mathf.Abs(Mathf.Sin(phase))*.025f:0,0);
        if(artRoot) Angle(skeleton,(Mathf.Abs(speed)>.3f?Mathf.Sin(phase)*3f:Mathf.Sin(phase)*.6f)-jump*4+(Time.time<kickUntil?-8:0));
        Angle(chest,Mathf.Sin(phase)* (style==1?6:2)); Angle(head,-Mathf.Sin(phase)*3);
        Angle(accessoryL,Mathf.Sin(phase*(style==3?2:1))*(style==3?35:12));
        Angle(accessoryR,-Mathf.Sin(phase*(style==3?2:1))*(style==3?35:12));
        if(bag) Angle(bag,Mathf.Sin(phase)*18);
        float alpha=PlayerHealth.Instance && PlayerHealth.Instance.Invulnerable ? (Mathf.Sin(Time.time*26)>0?1:.3f):1;
        foreach(var r in renderers) { var c=r.color; c.a=alpha; r.color=c; }
        wasGrounded=ground;
    }
    void OnDestroy() { GameInput.Kicked-=Kick; }

    // Kostüm seçim ekranı için önizleme: gerçek iskelet dinlenme pozunda kurulur ve her parça
    // (elips) CPU'da pürüzsüz olarak çizilir; böylece küçük resim oyundaki karakterle birebir aynıdır.
    public static Sprite Preview(int index)
    {
        if(index==4 && Art() && Art().texture.isReadable)
        {
            var source=Art().texture; var copy=new Texture2D(source.width,source.height,TextureFormat.RGBA32,false){filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp};
            copy.SetPixels(source.GetPixels()); copy.Apply();
            return Sprite.Create(copy,new Rect(0,0,copy.width,copy.height),Vector2.one*.5f,100);
        }
        const int W=260, H=296; const float MinX=-.65f, MinY=-.08f, PerUnit=200f;
        var go=new GameObject("Kostum Onizleme"); go.SetActive(false);
        var rig=go.AddComponent<CostumeRig2D>(); rig.Build(null,index);
        Angle(rig.elbowL,-15); Angle(rig.elbowR,15);
        var parts=new System.Collections.Generic.List<SpriteRenderer>(go.GetComponentsInChildren<SpriteRenderer>(true));
        var order=new System.Collections.Generic.Dictionary<SpriteRenderer,int>(); for(int i=0;i<parts.Count;i++) order[parts[i]]=i;
        parts.Sort((a,b)=>a.sortingOrder!=b.sortingOrder?a.sortingOrder.CompareTo(b.sortingOrder):order[a].CompareTo(order[b]));
        var pixels=new Color[W*H];
        foreach(var part in parts)
        {
            var sprite=part.sprite; if(!sprite) continue;
            var box=sprite.bounds; var tr=part.transform; var tint=part.color;
            // Renderer.bounds etkin olmayan nesnede güvenilmez; dünya sınırı sprite köşelerinden hesaplanır.
            Vector2 lo=new Vector2(float.MaxValue,float.MaxValue), hi=new Vector2(float.MinValue,float.MinValue);
            for(int c=0;c<4;c++)
            {
                var corner=tr.TransformPoint(box.center+new Vector3((c&1)==0?-box.extents.x:box.extents.x,(c&2)==0?-box.extents.y:box.extents.y,0));
                lo=Vector2.Min(lo,corner); hi=Vector2.Max(hi,corner);
            }
            int x0=Mathf.Max(0,Mathf.FloorToInt((lo.x-MinX)*PerUnit)-1), x1=Mathf.Min(W-1,Mathf.CeilToInt((hi.x-MinX)*PerUnit)+1);
            int y0=Mathf.Max(0,Mathf.FloorToInt((lo.y-MinY)*PerUnit)-1), y1=Mathf.Min(H-1,Mathf.CeilToInt((hi.y-MinY)*PerUnit)+1);
            for(int y=y0;y<=y1;y++) for(int x=x0;x<=x1;x++)
            {
                int hit=0;
                for(int sy=0;sy<2;sy++) for(int sx=0;sx<2;sx++)
                {
                    var local=tr.InverseTransformPoint(new Vector3(MinX+(x+.25f+sx*.5f)/PerUnit,MinY+(y+.25f+sy*.5f)/PerUnit,0));
                    float nx=(local.x-box.center.x)/box.extents.x, ny=(local.y-box.center.y)/box.extents.y;
                    if(nx*nx+ny*ny<=.94f) hit++;
                }
                if(hit==0) continue;
                float a=tint.a*hit/4f; var dst=pixels[y*W+x]; float outA=a+dst.a*(1-a);
                var rgb=(new Vector3(Mathf.Min(tint.r,1),Mathf.Min(tint.g,1),Mathf.Min(tint.b,1))*a+new Vector3(dst.r,dst.g,dst.b)*dst.a*(1-a))/outA;
                pixels[y*W+x]=new Color(rgb.x,rgb.y,rgb.z,outA);
            }
        }
        Object.Destroy(go);
        var tex=new Texture2D(W,H,TextureFormat.RGBA32,false){filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp};
        tex.SetPixels(pixels); tex.Apply();
        return Sprite.Create(tex,new Rect(0,0,W,H),Vector2.one*.5f,100);
    }
}
