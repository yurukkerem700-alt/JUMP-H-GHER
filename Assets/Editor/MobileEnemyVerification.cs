using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.Assertions;

public static class MobileEnemyVerification
{
    const BindingFlags Private=BindingFlags.NonPublic|BindingFlags.Instance;
    public static void Check()
    {
        var layout=UnityEngine.Object.FindFirstObjectByType<GameDeviceLayout>();
        var flow=GameFlow.Instance;
        layout.SetTouchPreview(true); flow.Show(true);
        foreach(var size in new[]{new Vector2Int(393,724),new Vector2Int(390,650),new Vector2Int(844,390)})
        {
            layout.ApplyLayout(size.x,size.y,new Rect(0,0,size.x,size.y));
            Assert.AreEqual(new Rect(0,0,1,1),Camera.main.rect);
            Assert.IsNull(GameObject.Find("Zemin Kamerası"));
        }
        flow.Begin(true); layout.SetTouchPreview(false);
        GameInput.Hold(false,991,true);
        typeof(GameDeviceLayout).GetMethod("Update",Private).Invoke(layout,null); GameInput.Clear();
        layout.ApplyLayout(1600,900,new Rect(0,0,1600,900));
        Assert.IsFalse(layout.desktopHelp.activeSelf);
        Assert.IsFalse(layout.desktopControlsText.activeSelf);
        var tower=UnityEngine.Object.FindFirstObjectByType<InfiniteTower>();
        var manager=new GameObject("Enemy test").AddComponent<EnemyManager>(); manager.Init(tower); manager.BeginEndless();
        var assembly=typeof(EnemyManager).Assembly; var enemyType=assembly.GetType("Enemy"); var artType=assembly.GetType("EnemyArt");
        foreach(var kind in new[]{EnemyKind.Karakoncolos,EnemyKind.KaraKurt})
        {
            var e=Activator.CreateInstance(enemyType); var go=new GameObject("Test enemy",typeof(SpriteRenderer));
            void Set(string n,object v)=>enemyType.GetField(n).SetValue(e,v);
            var art=artType.GetMethod("Get").Invoke(null,new object[]{kind});
            Set("kind",kind); Set("go",go); Set("sr",go.GetComponent<SpriteRenderer>()); Set("art",art);
            Set("floor",0); Set("t0",10f); Set("dur",1f); Set("side",1f); Set("state",2);
            var climb=typeof(EnemyManager).GetMethod("TickClimb",Private);
            climb.Invoke(manager,new[]{e,(object)10.05f}); var first=go.transform.position;
            var plat=tower.GetPlatform(0); var original=plat.transform.position;
            plat.transform.position+=Vector3.right*.4f;
            climb.Invoke(manager,new[]{e,(object)10.15f});
            Assert.IsTrue(Vector3.Distance(go.transform.position,first+Vector3.right*.4f)<.001f,"Grip must follow moving ledge without bobbing");
            plat.transform.position=original;
            Set("state",0); Set("moving",false);
            var frame=typeof(EnemyManager).GetMethod("FrameFor",BindingFlags.NonPublic|BindingFlags.Static);
            Assert.AreEqual(0,(int)frame.Invoke(null,new[]{e,(object)10.4f}));
            var health=PlayerHealth.Instance; health.Refill();
            float height=(float)artType.GetField("height").GetValue(art);
            tower.player.transform.position=new Vector3(plat.transform.position.x+.1f,plat.Top+height*.45f,0);
            Set("offset",0f); Set("state",5); Set("t0",10f); Set("dur",1f); Set("faceRight",true); Set("attackResolved",false);
            var attack=typeof(EnemyManager).GetMethod("TickAttack",Private);
            attack.Invoke(manager,new[]{e,(object)10.2f}); Assert.AreEqual(3,health.Hearts);
            attack.Invoke(manager,new[]{e,(object)10.5f}); Assert.AreEqual(2,health.Hearts);
            attack.Invoke(manager,new[]{e,(object)10.7f}); Assert.AreEqual(2,health.Hearts);
            health.Refill(); Set("attackResolved",false); tower.player.transform.position+=Vector3.right*5;
            attack.Invoke(manager,new[]{e,(object)10.5f}); Assert.AreEqual(3,health.Hearts);
            UnityEngine.Object.DestroyImmediate(go);
            Debug.Log("ENEMY PASS: "+kind+"; moving ledge grip, idle, windup, one strike, dodge");
        }
        UnityEngine.Object.DestroyImmediate(manager.gameObject);
        Debug.Log("MOBILE PASS: full-screen single camera; help stays hidden after first movement");
    }
}
