using UnityEngine;

// Fixed-size pool: no per-frame allocations or gameplay changes.
public class CostumeEffects : MonoBehaviour
{
    const int Count = 18;
    readonly SpriteRenderer[] motes = new SpriteRenderer[Count];
    readonly Vector3[] positions = new Vector3[Count];
    readonly float[] ages = new float[Count];
    Sprite sprite;
    Texture2D texture;
    Rigidbody2D body;
    Color tint;
    float clock;
    int cursor, style;

    void Start()
    {
        body = GetComponent<Rigidbody2D>();
        texture = new Texture2D(16,16,TextureFormat.RGBA32,false);
        for(int y=0;y<16;y++) for(int x=0;x<16;x++)
        {
            float d = Vector2.Distance(new Vector2(x,y),new Vector2(7.5f,7.5f))/7.5f;
            texture.SetPixel(x,y,new Color(1,1,1,Mathf.Clamp01(1-d)));
        }
        texture.Apply();
        sprite = Sprite.Create(texture,new Rect(0,0,16,16),Vector2.one*.5f,16);
        for(int i=0;i<Count;i++)
        {
            var go = new GameObject("Kostum Pariltisi",typeof(SpriteRenderer));
            go.transform.SetParent(transform,false);
            motes[i]=go.GetComponent<SpriteRenderer>(); motes[i].sprite=sprite;
            motes[i].sortingOrder=19; motes[i].enabled=false; ages[i]=1;
        }
        Costumes.Changed+=Refresh; Refresh();
    }
    void Refresh()
    {
        style=0;
        for(int i=0;i<Costumes.All.Length;i++) if(Costumes.All[i].Id==Costumes.SelectedId) style=i;
        Color[] colors={new Color(.6f,.85f,.4f),new Color(1,.45f,.12f),new Color(.65f,.9f,1),new Color(.45f,.65f,1),new Color(1,.18f,.3f),new Color(.8f,.65f,1)};
        tint=colors[style]; clock=0;
        for(int i=0;i<Count;i++){ages[i]=1; motes[i].enabled=false;}
    }
    void LateUpdate()
    {
        if(GameFlow.IsMenu) return;
        float dt=Time.deltaTime;
        clock+=dt;
        if(body && body.linearVelocity.sqrMagnitude>.2f && clock>=.06f)
        {
            clock=0; int i=cursor++%Count; ages[i]=0;
            positions[i]=transform.position+new Vector3(Mathf.Sin(cursor*2.4f)*.22f,-.2f,0);
        }
        for(int i=0;i<Count;i++)
        {
            ages[i]+=dt; float a=ages[i]; var m=motes[i]; m.enabled=a<.8f;
            if(!m.enabled) continue;
            float drift=style==2?.7f:style==4?.15f:.4f;
            positions[i]+=new Vector3(Mathf.Sin(i*3+a*8)*.15f,drift,0)*dt;
            m.transform.position=positions[i];
            float size=style==2?.22f:style==3?.065f:.11f;
            m.transform.localScale=Vector3.one*size*(1+a);
            m.color=new Color(tint.r,tint.g,tint.b,(1-a/.8f)*.65f);
        }
    }
    void OnDestroy()
    {
        Costumes.Changed-=Refresh;
        if(sprite) Destroy(sprite); if(texture) Destroy(texture);
    }
}
