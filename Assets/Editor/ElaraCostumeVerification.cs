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
                Assert.IsTrue(frames[i].pivot.y>=0 && frames[i].pivot.y<r.height);
                var foot=sheet.GetPixels((int)r.x,(int)(r.y+frames[i].pivot.y-.5f),(int)r.width,1);
                int supported=0; foreach(var p in foot) if(p.a>.5f) supported++;
                Assert.IsTrue(supported>=5);
                Debug.Log("ELARA FRAME PASS: "+i+" solid="+solid+" transparent="+clear);
            }
        }
        finally { foreach(var frame in frames) Object.DestroyImmediate(frame); }
        var preview=Costumes.Preview(Costumes.Elara);
        Assert.IsNotNull(preview); Assert.AreEqual(sheet.height*2/3,preview.rect.y);
        Object.DestroyImmediate(preview);
        CostumeRigVerification.Run();
        Debug.Log("ELARA COSTUME VERIFICATION PASSED: 12 transparent frames and legacy compatibility");
    }
}
