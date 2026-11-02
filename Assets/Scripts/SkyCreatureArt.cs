using System;
using System.Collections.Generic;
using UnityEngine;

// Four painted species, each with a four-pose wing cycle. Transparent gaps
// define the row bands; no extended wing can be clipped by a fixed grid row.
public static class SkyCreatureArt
{
    public const int ModelCount=4, FramesPerModel=4;
    static Sprite[][] cache;
    public static Sprite[] Get(int model)
    {
        if(model<0 || model>=ModelCount) return null;
        if(cache==null || !cache[0][0]) Load();
        return cache==null?null:cache[model];
    }
    static void Load()
    {
        var texture=Resources.Load<Texture2D>("Decor/SkyWildlife");
#if UNITY_EDITOR
        // A freshly imported Resources entry can precede the editor's runtime
        // resource index. Load the same imported asset directly for batch checks.
        if(!texture) texture=UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Resources/Decor/SkyWildlife.png");
#endif
        if(!texture) return;
        int w=texture.width,h=texture.height,cell=w/4;
        var pixels=texture.GetPixels32();
        cache=new Sprite[ModelCount][];
        for(int row=0;row<ModelCount;row++) cache[row]=new Sprite[FramesPerModel];
        for(int col=0;col<4;col++)
        {
            var bands=new List<Vector2Int>(); int start=-1,last=-1;
            for(int y=0;y<h;y++)
            {
                int count=0;
                for(int x=col*cell;x<(col+1)*cell;x++) if(pixels[y*w+x].a>64) count++;
                if(count>=3){if(start<0)start=y;last=y;}
                else if(start>=0 && y-last>Mathf.Max(3,h/100)) {bands.Add(new Vector2Int(start,last));start=-1;}
            }
            if(start>=0)bands.Add(new Vector2Int(start,last));
            if(bands.Count!=4)throw new InvalidOperationException("Sky wildlife: expected four separated creatures in column "+col+", got "+bands.Count);
            for(int row=0;row<4;row++)
            {
                var band=bands[3-row]; int minX=(col+1)*cell,maxX=col*cell;
                for(int y=band.x;y<=band.y;y++)for(int x=col*cell;x<(col+1)*cell;x++)
                    if(pixels[y*w+x].a>64){minX=Mathf.Min(minX,x);maxX=Mathf.Max(maxX,x);}
                int left=Mathf.Max(col*cell,minX-3),right=Mathf.Min((col+1)*cell-1,maxX+3);
                int bottom=Mathf.Max(0,band.x-3),top=Mathf.Min(h-1,band.y+3);
                var rect=new Rect(left,bottom,right-left+1,top-bottom+1);
                float ex=0,ey=0,n=0;
                for(int y=band.x;y<=band.y;y++)for(int x=minX+(maxX-minX)*2/3;x<=maxX;x++)
                {
                    var c=pixels[y*w+x];
                    bool eye=row==0?c.g>180 && c.b>180 && c.r<c.b*.8f
                        : row==3?c.b>150 && c.g>80 && c.b>c.r*1.7f && c.b>c.g*1.12f
                        : c.r>150 && c.g>110 && c.b<90 && c.r>c.g*1.05f;
                    if(c.a>180 && eye){ex+=x+.5f;ey+=y+.5f;n++;}
                }
                var pivot=n>0?new Vector2((ex/n-left)/rect.width,(ey/n-bottom)/rect.height):new Vector2(.78f,.5f);
                cache[row][col]=Sprite.Create(texture,rect,pivot,cell/1.65f);
                cache[row][col].name="Sky species "+row+" frame "+col;
            }
        }
    }
    public static int VariantFor(int slot,float cameraY) => (slot+Mathf.Max(0,Mathf.FloorToInt(cameraY/(InfiniteTower.FloorGap*10))))%5;
    public static void Release()
    {
        if(cache!=null)foreach(var model in cache)foreach(var frame in model)if(frame)UnityEngine.Object.Destroy(frame);
        cache=null;
    }
}
