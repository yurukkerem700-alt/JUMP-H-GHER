using UnityEngine;
using UnityEngine.Assertions;

public static class ElaraCostumeVerification
{
    public static void Run()
    {
        var sheet=Costumes.Sheet(Costumes.Elara);
        Assert.IsNotNull(sheet); Assert.IsTrue(sheet.isReadable);
        Assert.AreEqual(0,sheet.width%4); Assert.AreEqual(0,sheet.height%3);
        Assert.AreEqual(12,Costumes.FrameCount(Costumes.Elara));
        Assert.AreEqual(8,Costumes.FrameCount(Costumes.Default));
        var frames=PlayerVisual2D.BuildFrames(sheet,3);
        Assert.AreEqual(12,frames.Length);
        try
        {
            for(int i=0;i<frames.Length;i++)
            {
                var r=frames[i].rect;
                var pixels=sheet.GetPixels((int)r.x,(int)r.y,(int)r.width,(int)r.height);
                int solid=0,clear=0;
                foreach(var p in pixels) { if(p.a>.5f) solid++; if(p.a<.01f) clear++; }
                Assert.IsTrue(solid>500); Assert.IsTrue(clear>pixels.Length/4);
                for(int x=0;x<(int)r.width;x++) { Assert.IsTrue(pixels[x].a<.5f); Assert.IsTrue(pixels[(int)(r.height-1)*(int)r.width+x].a<.5f); }
                for(int y=0;y<(int)r.height;y++) { Assert.IsTrue(pixels[y*(int)r.width].a<.5f); Assert.IsTrue(pixels[y*(int)r.width+(int)r.width-1].a<.5f); }
                Assert.IsTrue(frames[i].pivot.y>=0 && frames[i].pivot.y<r.height);
                var foot=sheet.GetPixels((int)r.x,(int)(r.y+frames[i].pivot.y-.5f),(int)r.width,1);
                int supported=0; foreach(var p in foot) if(p.a>.5f) supported++;
                Assert.IsTrue(supported>=5);
                Debug.Log("ELARA FRAME PASS: "+i+" solid="+solid+" transparent="+clear);
            }
        }
        finally { foreach(var frame in frames) Object.DestroyImmediate(frame); }
        var preview=Costumes.Preview(Costumes.Elara);
        Assert.IsNotNull(preview); Assert.IsTrue(preview.rect.y>=sheet.height*2/3 && preview.rect.yMax<=sheet.height);
        Object.DestroyImmediate(preview);
        CostumeRigVerification.Run();
        Debug.Log("ELARA COSTUME VERIFICATION PASSED: 12 transparent frames and legacy compatibility");
    }
}
