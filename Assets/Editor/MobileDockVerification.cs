using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.EventSystems;
using UnityEditor;

public static class MobileDockVerification
{
    public static void RunAll() { Run(); CostumeRigVerification.Run(); }
    [MenuItem("Dogrulama/Mobil Kontrol Panelini Kontrol Et")]
    public static void Run()
    {
        var screens=new[]{new Vector2Int(320,568),new Vector2Int(360,640),new Vector2Int(390,844),new Vector2Int(412,915),new Vector2Int(768,1024),new Vector2Int(1536,2048),new Vector2Int(844,390)};
        foreach(var screen in screens)
        {
            float scale=TouchJoystick.ScaleFor(screen.x,screen.y);
            foreach(float inset in new[]{0f,24f}) foreach(float size in new[]{.5f,1f,1.8f})
            {
                var safe=new Rect(inset,24,screen.x-inset*2,screen.y-48);
                var targets=TouchJoystick.Targets(safe.width/scale,size,size);
                foreach(var target in targets)
                {
                    Assert.IsTrue(target.xMin>=0 && target.xMax<=safe.width/scale);
                    Assert.IsTrue(target.yMin>=0 && target.yMax<=TouchJoystick.DockHeight);
                    Assert.IsTrue(target.width*scale>=44);
                }
                Assert.IsFalse(targets[0].Overlaps(targets[3])); Assert.IsFalse(targets[0].Overlaps(targets[4]));
                Assert.IsFalse(targets[1].Overlaps(targets[2]));
                Assert.IsFalse(targets[2].Overlaps(targets[4])); Assert.IsFalse(targets[3].Overlaps(targets[4]));
                float bottom=TouchJoystick.ReservedBottom(screen.x,screen.y,safe);
                Assert.IsTrue(bottom>0 && bottom<.4f);
            }
            Debug.Log("MOBILE DOCK PASS: "+screen);
        }
        var ownerGo=new GameObject("Dock Verification Owner");
        var events=new GameObject("Dock Verification Events",typeof(EventSystem));
        TouchJoystick joy=null;
        try
        {
            var owner=ownerGo.AddComponent<GameDeviceLayout>(); joy=TouchJoystick.Create(owner);
            Canvas.ForceUpdateCanvases();
            var stick=joy.transform.Find("Kontrol Zemini/Hareket") as RectTransform;
            var center=RectTransformUtility.WorldToScreenPoint(null,stick.position);
            var first=new PointerEventData(events.GetComponent<EventSystem>()){pointerId=11,position=center+Vector2.right*100};
            var second=new PointerEventData(events.GetComponent<EventSystem>()){pointerId=22,position=center-Vector2.right*100};
            joy.Press(first); Assert.IsTrue(GameInput.Horizontal>0);
            joy.Press(second); joy.Release(second); Assert.IsTrue(GameInput.Horizontal>0);
            joy.Release(first); Assert.AreEqual(0f,GameInput.Horizontal);
            Debug.Log("MOBILE MULTITOUCH PASS: second pointer cannot steal or release movement");
        }
        finally
        {
            GameInput.Clear(); if(joy) Object.DestroyImmediate(joy.gameObject);
            Object.DestroyImmediate(ownerGo); Object.DestroyImmediate(events);
        }
        Debug.Log("MOBILE DOCK VERIFICATION PASSED");
    }
}
