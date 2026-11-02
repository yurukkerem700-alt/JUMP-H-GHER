using UnityEngine;
using UnityEngine.Assertions;
public static class SkyWildlifeVerification
{
    public static void Run()
    {
        for(int row=0;row<SkyCreatureArt.ModelCount;row++)
        {
            var frames=SkyCreatureArt.Get(row);Assert.IsNotNull(frames);Assert.AreEqual(4,frames.Length);
            foreach(var frame in frames)
            {
                Assert.IsNotNull(frame);Assert.IsTrue(frame.rect.width>24 && frame.rect.height>24);
                Assert.IsTrue(frame.pivot.x>0 && frame.pivot.x<frame.rect.width);
                var texture=frame.texture;var r=frame.rect;
                for(int x=(int)r.xMin;x<(int)r.xMax;x++)
                {Assert.IsTrue(texture.GetPixel(x,(int)r.yMin).a<.5f);Assert.IsTrue(texture.GetPixel(x,(int)r.yMax-1).a<.5f);}
                for(int y=(int)r.yMin;y<(int)r.yMax;y++)
                {Assert.IsTrue(texture.GetPixel((int)r.xMin,y).a<.5f);Assert.IsTrue(texture.GetPixel((int)r.xMax-1,y).a<.5f);}
            }
        }
        foreach(float height in new[]{4f,30f,80f,150f})
        {
            var seen=new System.Collections.Generic.HashSet<int>();
            for(int slot=0;slot<6;slot++)seen.Add(SkyCreatureArt.VariantFor(slot,height));
            Assert.AreEqual(5,seen.Count,"Every altitude band must include the original and four new models");
        }
        Debug.Log("SKY WILDLIFE PASS: four painted models, sixteen unclipped frames, all five variants across altitude bands");
    }
}
