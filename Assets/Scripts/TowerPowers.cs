using UnityEngine;
using TMPro;

public class TowerPowers : MonoBehaviour
{
    public static TowerPowers Instance {get;private set;}
    public bool DoubleJumpActive=>Time.time<doubleUntil;
    public bool MagnetActive=>Time.time<magnetUntil;
    public string Status=>DoubleJumpActive?"ÇİFT ZIPLAMA "+Mathf.CeilToInt(doubleUntil-Time.time)+" sn":MagnetActive?"MIKNATIS "+Mathf.CeilToInt(magnetUntil-Time.time)+" sn":"";
    float doubleUntil,magnetUntil;
    int pickupFloor=4,pickupKind;
    InfiniteTower tower; Transform pickup; TextMeshPro label; SpriteRenderer glow;
    Transform wind;SpriteRenderer windArt;Transform[] windStreaks;
    void Awake(){Instance=this;}
    void Start()
    {
        tower=FindFirstObjectByType<InfiniteTower>();
        pickup=new GameObject("Güçlendirme").transform;
        glow=pickup.gameObject.AddComponent<SpriteRenderer>();
        var coin=tower.coins.coin.GetComponentInChildren<SpriteRenderer>();glow.sprite=coin.sprite;glow.sharedMaterial=coin.sharedMaterial;glow.sortingOrder=25;
        pickup.localScale=Vector3.one*.58f;
        var face=new GameObject("Rozet Yüzü",typeof(SpriteRenderer));face.transform.SetParent(pickup,false);face.transform.localScale=Vector3.one*.78f;var faceArt=face.GetComponent<SpriteRenderer>();faceArt.sprite=coin.sprite;faceArt.sharedMaterial=coin.sharedMaterial;faceArt.color=new Color(.16f,.10f,.12f);faceArt.sortingOrder=26;
        var text=new GameObject("Güç Adı");text.transform.SetParent(pickup,false);
        label=text.AddComponent<TextMeshPro>();label.fontSize=4;label.alignment=TextAlignmentOptions.Center;label.rectTransform.sizeDelta=new Vector2(4,2);label.transform.localPosition=new Vector3(0,0,-.01f);label.GetComponent<MeshRenderer>().sortingOrder=27;
        var texture=Texture2D.whiteTexture;
        wind=new GameObject("Yukarı Hava Akımı").transform;
        windArt=wind.gameObject.AddComponent<SpriteRenderer>();windArt.sprite=Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),Vector2.one*.5f,texture.width);
        windStreaks=new Transform[8];
        for(int i=0;i<windStreaks.Length;i++){var s=new GameObject("Hava Çizgisi",typeof(SpriteRenderer));s.transform.SetParent(wind,false);s.transform.localScale=new Vector3(.018f,.055f,1);var r=s.GetComponent<SpriteRenderer>();r.sprite=windArt.sprite;r.sharedMaterial=coin.sharedMaterial;r.color=new Color(.6f,.8f,1,.35f);r.sortingOrder=-4;windStreaks[i]=s.transform;}
        windArt.sharedMaterial=coin.sharedMaterial;windArt.sortingOrder=-5;windArt.color=new Color(.45f,.8f,1,.12f);
    }
    void Update() { TickGameplay(); }
    public void TickGameplay()
    {
        while(pickupFloor < tower.HighestReachedFloor - 2){pickupFloor+=10;pickupKind=1-pickupKind;}
        tower.EnsureThroughFloor(pickupFloor+1);
        var platform=tower.GetPlatform(pickupFloor);
        if(platform)
        {
            pickup.position=new Vector3(platform.transform.position.x,platform.Top+.8f,0);
            glow.color=pickupKind==0?new Color(.82f,.54f,.28f):new Color(.67f,.42f,.7f);
            label.text=pickupKind==0?"2X":"M";
            if(Vector2.Distance(tower.player.transform.position,pickup.position)<.75f)
            {
                if(pickupKind==0)doubleUntil=Time.time+20;else magnetUntil=Time.time+15;
                pickupFloor+=10;pickupKind=1-pickupKind;
            }
        }
        if(MagnetActive)
        {
            var collider=tower.coins.coin.GetComponent<Collider2D>();
            if(Vector2.Distance(tower.player.transform.position,tower.coins.coin.position)<2.5f)tower.player.CollectCoin(collider);
        }
        int section=Mathf.Max(0,tower.HighestReachedFloor)/20;
        bool inClouds=section%5==1||section%5==2;
        wind.gameObject.SetActive(inClouds);
        if(inClouds)
        {
            int floor=section*20+8;
            wind.position=new Vector3(0,InfiniteTower.SurfaceFor(floor)+1.4f,0);
            wind.localScale=new Vector3(1.6f,3.8f,1);
            windArt.color=new Color(.5f,.85f,1,.06f+.025f*Mathf.Sin(Time.time*4));
            for(int i=0;i<windStreaks.Length;i++)windStreaks[i].localPosition=new Vector3(Mathf.Sin(i*2.4f)*.38f,Mathf.Repeat(Time.time*.8f+i*.14f,1)-.5f,0);
        }
    }
    public bool IsInWind(Vector2 pos)=>wind&&wind.gameObject.activeSelf&&Mathf.Abs(pos.x-wind.position.x)<.8f&&Mathf.Abs(pos.y-wind.position.y)<1.9f;
    public void ResetPowers(){doubleUntil=magnetUntil=0;pickupFloor=4;pickupKind=0;}
    void OnDestroy(){if(pickup)Destroy(pickup.gameObject);if(wind)Destroy(wind.gameObject);}
}

