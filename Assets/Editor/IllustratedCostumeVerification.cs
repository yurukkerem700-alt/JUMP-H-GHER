using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

public static class IllustratedCostumeVerification
{
    public static void Run()
    {
        var ids=new List<string>(CostumeRig2D.Ids); ids.Add(Costumes.Elara);
        foreach(string id in ids)
        {
            var sheet=Costumes.Sheet(id); Assert.IsNotNull(sheet); Assert.IsTrue(sheet.isReadable);
            Assert.AreEqual(12,Costumes.FrameCount(id)); Assert.AreEqual(0,sheet.width%4); Assert.AreEqual(0,sheet.height%3);
            var colours=new HashSet<int>(); var pixels=sheet.GetPixels32();
            for(int i=0;i<pixels.Length;i+=7) if(pixels[i].a>127)
                colours.Add((pixels[i].r>>3)*1024+(pixels[i].g>>3)*32+(pixels[i].b>>3));
            Assert.IsTrue(colours.Count>64,"Costume must contain detailed painted colour data: "+id);
            var preview=Costumes.Preview(id); Assert.IsNotNull(preview); Assert.AreEqual(sheet,preview.texture);
            Assert.IsTrue(preview.rect.y>=sheet.height*2/3 && preview.rect.yMax<=sheet.height);
            Object.DestroyImmediate(preview);
            var frames=PlayerVisual2D.BuildFrames(sheet,3);
            try
            {
                foreach(var frame in frames)
                {
                    var r=frame.rect; var p=sheet.GetPixels((int)r.x,(int)r.y,(int)r.width,(int)r.height);
                    Assert.IsTrue(System.Array.Exists(p,c=>c.a>.5f));
                    for(int x=0;x<(int)r.width;x++) {Assert.IsTrue(p[x].a<.5f,id+" bottom border");Assert.IsTrue(p[((int)r.height-1)*(int)r.width+x].a<.5f,id+" top border");}
                    for(int y=0;y<(int)r.height;y++) {Assert.IsTrue(p[y*(int)r.width].a<.5f,id+" left border");Assert.IsTrue(p[y*(int)r.width+(int)r.width-1].a<.5f,id+" right border");}
                }
            }
            finally {foreach(var frame in frames) Object.DestroyImmediate(frame);}
            Debug.Log("ILLUSTRATED COSTUME PASS: "+id+"; shared preview texture; 12 frames; colours="+colours.Count);
        }
        ElaraCostumeVerification.Run();
        Debug.Log("ILLUSTRATED COSTUME VERIFICATION PASSED: 6/6, no geometric placeholders");
    }
}
