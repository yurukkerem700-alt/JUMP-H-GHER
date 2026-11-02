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
    Transform bag;

    public void Init(PlayerController owner, Material mat, int index)
    {
        controller=owner; body=owner.GetComponent<Rigidbody2D>(); material=mat; style=index;
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
        renderers=GetComponentsInChildren<SpriteRenderer>();
        GameInput.Kicked+=Kick;
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
        Angle(chest,Mathf.Sin(phase)* (style==1?6:2)); Angle(head,-Mathf.Sin(phase)*3);
        Angle(accessoryL,Mathf.Sin(phase*(style==3?2:1))*(style==3?35:12));
        Angle(accessoryR,-Mathf.Sin(phase*(style==3?2:1))*(style==3?35:12));
        if(bag) Angle(bag,Mathf.Sin(phase)*18);
        float alpha=PlayerHealth.Instance && PlayerHealth.Instance.Invulnerable ? (Mathf.Sin(Time.time*26)>0?1:.3f):1;
        foreach(var r in renderers) { var c=r.color; c.a=alpha; r.color=c; }
        wasGrounded=ground;
    }
    void OnDestroy() { GameInput.Kicked-=Kick; }

    // Matching palette and silhouette for the costume picker, no external texture dependency.
    public static Sprite Preview(int index)
    {
        var tex=new Texture2D(96,128,TextureFormat.RGBA32,false);
        for(int y=0;y<128;y++) for(int x=0;x<96;x++)
        {
            float u=(x-47.5f)/96, v=y/128f;
            bool head=u*u+(v-.78f)*(v-.78f)<.016f;
            bool torso=Mathf.Abs(u)<.14f && v>.34f && v<.67f;
            bool legs=Mathf.Abs(u)>.035f && Mathf.Abs(u)<.12f && v>.08f && v<.36f;
            bool arms=Mathf.Abs(u)>.15f && Mathf.Abs(u)<.21f && v>.3f && v<.61f;
            bool extra=index==4 ? (Mathf.Abs(u)<.27f && v>.89f && v<.94f) || (Mathf.Abs(u)<.15f && v>.93f) || (Mathf.Abs(u)>.11f && Mathf.Abs(u)<.18f && v>.56f && v<.85f)
                : index==3 ? Mathf.Abs(u)>.16f && Mathf.Abs(u)<.36f && v>.4f && v<.8f
                : index==0 ? Mathf.Abs(u)>.1f && Mathf.Abs(u)<.16f && v>.83f && v<.98f
                : index==1 ? Mathf.Abs(u)<.18f && v>.88f && v<.95f
                : u*u+(v-.78f)*(v-.78f)<.036f && u*u+(v-.78f)*(v-.78f)>.025f;
            Color color=head||torso||legs||arms||extra?Tints[index]:Color.clear;
            if(index==4 && head) color=new Color(.98f,.91f,.86f);
            if(index==4 && Mathf.Abs(u)>.11f && Mathf.Abs(u)<.18f && v>.56f && v<.87f) color=new Color(1,.76f,.25f);
            if(index==4 && Mathf.Abs(Mathf.Abs(u)-.055f)<.018f && Mathf.Abs(v-.79f)<.02f) color=Color.red;
            tex.SetPixel(x,y,color);
        }
        tex.Apply(); return Sprite.Create(tex,new Rect(0,0,96,128),Vector2.one*.5f,100);
    }
}
