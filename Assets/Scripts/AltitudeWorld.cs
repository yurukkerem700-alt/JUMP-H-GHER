using UnityEngine;

public class AltitudeWorld : MonoBehaviour
{
    public Sprite skySprite;
    public Material skyMaterial;
    public Texture2D birdSheet;
    public Material birdMaterial;
    public Sprite mistSprite;
    public Texture2D themeAtlas;
    Sprite[] themeFrames;
    MaterialPropertyBlock skyProps;
    const int SkyTiles = 8; // her bölge arka planı orantısı bozulmadan yukarı doğru yan yana dizilir
    SpriteRenderer[] skies;
    SpriteRenderer[] birds;
    float[] birth, baseY, startX, speed, direction;
    Sprite[] frames;
    int[] model;
    Camera view;
    public int ActiveBirdCount { get; private set; }

    void Start()
    {
        skies=new SpriteRenderer[3*SkyTiles];birds=new SpriteRenderer[6];skyProps=new MaterialPropertyBlock();
        birth=new float[6];baseY=new float[6];startX=new float[6];speed=new float[6];direction=new float[6];
        model=new int[6];
        view = Camera.main;
        themeFrames = new Sprite[4];
        if(themeAtlas)for(int n=0;n<4;n++)themeFrames[n]=Sprite.Create(themeAtlas,new Rect((n%2)*(themeAtlas.width/2),(1-n/2)*(themeAtlas.height/2),themeAtlas.width/2,themeAtlas.height/2),Vector2.one*.5f,100);
        frames = new Sprite[4];
        int w = birdSheet.width / 2, h = birdSheet.height / 2;
        for (int i = 0; i < 4; i++)
        {
            int x = (i%2)*w, y = (1-i/2)*h;
            var pixels = birdSheet.GetPixels(x,y,w,h);
            float px=0,py=0,n=0;
            for(int row=0;row<h;row++) for(int col=0;col<w;col++)
            {
                var c=pixels[row*w+col];
                if(c.a>.5f && c.r>.55f && c.r>c.g*2 && c.r>c.b*1.6f) {px+=col;py+=row;n++;}
            }
            var pivot=n>0?new Vector2(px/n/w,py/n/h):new Vector2(.8f,.5f);
            frames[i]=Sprite.Create(birdSheet,new Rect(x,y,w,h),pivot,w);
        }
        for (int i = 0; i < skies.Length; i++)
        {
            skies[i] = new GameObject("Dunya Bulut Katmani " + i, typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
            skies[i].transform.SetParent(transform); skies[i].sprite = skySprite;
            skies[i].sharedMaterial = skyMaterial; skies[i].sortingOrder = -39;
        }
        for (int i = 0; i < birds.Length; i++)
        {
            birds[i] = new GameObject("Mutant Kus " + (i+1), typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
            birds[i].transform.SetParent(transform); birds[i].sprite = frames[0];
            birds[i].sharedMaterial = birdMaterial; birds[i].sortingOrder = -12;
            birds[i].gameObject.SetActive(false); birth[i] = -100;
        }
        if (mistSprite)
            for (int i = 0; i < 3; i++)
            {
                var mist = new GameObject("Mezarlik Ust Sisi", typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
                mist.transform.SetParent(transform); mist.sprite = mistSprite; mist.sharedMaterial = birdMaterial;
                mist.sortingOrder = -35; mist.color = new Color(.29f,.17f,.12f,.38f);
                mist.transform.position = new Vector3(-7 + i*7, 6.15f, 0);
                mist.transform.localScale = new Vector3(13,1.6f,1);
            }
    }

    void LateUpdate()
    {
        const float bandHeight = TowerThemes.FloorsPerTheme * InfiniteTower.FloorGap;
        int band = Mathf.Max(0, Mathf.FloorToInt((view.transform.position.y - 8) / bandHeight));
        float width = Mathf.Max(21.7f, view.orthographicSize * view.aspect * 2 + 1);
        for (int i = 0; i < 3; i++)
        {
            int worldBand = band + i - 1;
            int theme = Mathf.Max(0, worldBand) % 5;
            var sprite = theme > 0 && themeFrames[theme-1] ? themeFrames[theme-1] : skySprite;
            // 0. bölge 4. birimden ilk bölge sınırının 3,25 birim üstüne kadar uzanır
            float top0 = InfiniteTower.FirstSurface + bandHeight + 3.25f;
            float center = worldBand == 0 ? (4 + top0) / 2 : InfiniteTower.FirstSurface + worldBand * bandHeight + bandHeight * .5f;
            float height = worldBand == 0 ? top0 - 4 : bandHeight + 5;
            float scale = width / sprite.bounds.size.x, tileHeight = sprite.bounds.size.y * scale;
            int tiles = Mathf.Min(SkyTiles, Mathf.CeilToInt(height / tileHeight));
            Vector4 uv = theme > 0 ? new Vector4(((theme-1)%2)*.5f,(1-(theme-1)/2)*.5f,.5f,.5f) : new Vector4(0,0,1,1);
            skyProps.SetVector("_UVRect",uv);
            for (int t = 0; t < SkyTiles; t++)
            {
                var tile = skies[i * SkyTiles + t];
                tile.gameObject.SetActive(worldBand >= 0 && t < tiles);
                if (!tile.gameObject.activeSelf) continue;
                tile.sprite = sprite; tile.color = Color.white;
                tile.flipY = t % 2 == 1; // bitişik karolar aynalanır, dikiş görünmez
                tile.transform.position = new Vector3(0, center - height / 2 + tileHeight * (t + .5f), 0);
                tile.transform.localScale = new Vector3(scale, scale, 1);
                tile.SetPropertyBlock(skyProps);
            }
        }
        float blend = Mathf.SmoothStep(0,1,(view.transform.position.y - 2)/12);
        view.backgroundColor = Color.Lerp(new Color(.14f,.10f,.09f), new Color(.075f,.075f,.13f), blend);
        ActiveBirdCount = 0;
        float halfWidth = view.orthographicSize * view.aspect;
        for (int i = 0; i < birds.Length; i++)
        {
            if (view.transform.position.y < 3.8f) { birds[i].gameObject.SetActive(false); continue; }
            float age = Time.time - birth[i];
            if (age > 13 || Mathf.Abs(baseY[i] - view.transform.position.y) > 8)
            {
                direction[i] = i % 2 == 0 ? 1 : -1;
                model[i]=SkyCreatureArt.VariantFor(i,view.transform.position.y);
                startX[i] = direction[i] > 0 ? -halfWidth-1.2f : halfWidth+1.2f;
                baseY[i] = Mathf.Max(7, view.transform.position.y + Random.Range(-2f,4.5f));
                speed[i] = Random.Range(1.5f,2.6f); birth[i] = Time.time - i * .4f; age = i * .4f;
                birds[i].transform.localScale = Vector3.one * Random.Range(.6f,.95f);
                birds[i].flipX = direction[i] < 0;
            }
            birds[i].gameObject.SetActive(true); ActiveBirdCount++;
            birds[i].transform.position = new Vector3(startX[i] + direction[i] * speed[i] * age, baseY[i] + Mathf.Sin(Time.time*3+i)*.16f, 0);
            var variants=model[i]>0?SkyCreatureArt.Get(model[i]-1):null;
            int pose=(Mathf.FloorToInt(Time.time*(model[i]>=3?10:7))+i)%4;
            birds[i].sprite = variants!=null?variants[pose]:frames[pose];
        }
    }

    void OnDestroy() { SkyCreatureArt.Release(); if(themeFrames!=null)foreach(var f in themeFrames)if(f)Destroy(f); if (frames != null) foreach (var frame in frames) if(frame) Destroy(frame); }
}
