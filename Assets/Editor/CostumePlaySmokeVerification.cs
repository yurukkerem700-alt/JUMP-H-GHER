using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Assertions;

[InitializeOnLoad]
public static class CostumePlaySmokeVerification
{
    const BindingFlags Private=BindingFlags.NonPublic|BindingFlags.Instance;
    static CostumePlaySmokeVerification()
    {
        EditorApplication.update+=Tick;
        if(SessionState.GetBool("CostumeSmokeFinished",false)) EditorApplication.delayCall+=()=>EditorApplication.Exit(SessionState.GetInt("CostumeSmokeCode",1));
    }
    public static void Run()
    {
        SessionState.SetBool("CostumeSmokeFinished",false); SessionState.SetBool("CostumeSmokeActive",true);
        SessionState.SetFloat("CostumeSmokeDeadline",(float)EditorApplication.timeSinceStartup+180);
        EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
        EditorApplication.EnterPlaymode();
    }
    static void Tick()
    {
        if(!SessionState.GetBool("CostumeSmokeActive",false)) return;
        var player=UnityEngine.Object.FindFirstObjectByType<PlayerController>();
        if(!EditorApplication.isPlaying || !GameFlow.Instance || !player || !player.transform.Find("Kok Gezgin Gorseli"))
        {
            if(EditorApplication.timeSinceStartup>SessionState.GetFloat("CostumeSmokeDeadline",0)) Finish(1,"FAIL: play mode did not initialize");
            return;
        }
        SessionState.SetBool("CostumeSmokeActive",false);
        bool had=PlayerPrefs.HasKey("Costume"); string saved=PlayerPrefs.GetString("Costume",Costumes.Default);
        try
        {
            GameFlow.Instance.Begin(true); GameFlow.Instance.enabled=false;
            var tower=UnityEngine.Object.FindFirstObjectByType<InfiniteTower>(); if(tower) tower.enabled=false;
            player.enabled=false; var body=player.GetComponent<Rigidbody2D>(); body.simulated=false;
            typeof(PlayerController).GetField("zeminMask",Private).SetValue(player,0);
            var visual=player.GetComponent<PlayerVisual2D>();
            var update=typeof(PlayerVisual2D).GetMethod("Update",Private);
            var contact=typeof(PlayerController).GetField("lastGroundContact",Private);
            var kick=typeof(PlayerVisual2D).GetField("kickStarted",Private);
            var display=player.transform.Find("Kok Gezgin Gorseli").GetComponent<SpriteRenderer>();
            var ids=new System.Collections.Generic.List<string>(CostumeRig2D.Ids); ids.Add(Costumes.Elara);
            Costumes.SelectedId=Costumes.Default;
            foreach(var id in ids)
            {
                Costumes.SelectedId=id;
                Assert.IsTrue(display.enabled); Assert.AreEqual(Costumes.Sheet(id),display.sprite.texture);
                Assert.AreEqual(0,player.GetComponentsInChildren<CostumeRig2D>().Length);
                contact.SetValue(player,Time.fixedTime+1); body.linearVelocity=Vector2.zero; update.Invoke(visual,null);
                Assert.IsTrue(visual.CurrentFrame==0 || visual.CurrentFrame==11);
                body.linearVelocity=new Vector2(4,0); update.Invoke(visual,null); Assert.IsTrue(visual.CurrentFrame>=1 && visual.CurrentFrame<=4);
                contact.SetValue(player,-100f); body.linearVelocity=new Vector2(0,5); update.Invoke(visual,null); Assert.AreEqual(5,visual.CurrentFrame);
                body.linearVelocity=new Vector2(0,-5); update.Invoke(visual,null); Assert.AreEqual(6,visual.CurrentFrame);
                contact.SetValue(player,Time.fixedTime+1); body.linearVelocity=Vector2.zero; update.Invoke(visual,null); Assert.AreEqual(7,visual.CurrentFrame);
                GameInput.Kick(); Assert.IsTrue(GameInput.ConsumeKick()); GameInput.PlayKick(0, 1); update.Invoke(visual,null); Assert.IsFalse(display.enabled);
                Assert.AreEqual(1, player.GetComponentsInChildren<CostumeRig2D>().Length);
                kick.SetValue(visual,Time.time-EnemyManager.StrikeDuration-.01f); update.Invoke(visual,null); Assert.IsTrue(display.enabled);
                Debug.Log("PLAY COSTUME PASS: "+id+"; illustrated renderer, idle, walk, jump, fall, land, skeletal attack and recovery");
            }
            MobileEnemyVerification.Check();
            CheckCrumbling(tower, player);
            Finish(0,"PASS: 6 illustrated costumes x8 poses; mobile single-camera layout; help dismissal; enemy grip and attack timing; platform cracks, explosion damage and recovery");
        }
        catch(Exception error) {Debug.LogException(error); Finish(1,"FAIL: "+error);}
        finally {if(had) PlayerPrefs.SetString("Costume",saved); else PlayerPrefs.DeleteKey("Costume"); PlayerPrefs.Save();}
    }
    static void CheckCrumbling(InfiniteTower tower, PlayerController player)
    {
        var run = LevelRun.Instance;
        var platform = tower.GetPlatform(0);
        var collider = platform.GetComponentInChildren<Collider2D>();
        var stage = typeof(MovingPlatform2D).GetField("crumbleStage", Private);
        var started = typeof(MovingPlatform2D).GetField("crumbleAt", Private);
        var tick = typeof(MovingPlatform2D).GetMethod("Update", Private);
        var contact = typeof(PlayerController).GetField("lastGroundContact", Private);
        void StandOnPlatform()
        {
            typeof(PlayerController).GetField("supportCollider", Private).SetValue(player, collider);
            typeof(PlayerController).GetField("standingPlatform", Private).SetValue(player, platform);
            contact.SetValue(player, Time.fixedTime);
            player.GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
        }
        void Advance(float age, int expected)
        {
            started.SetValue(platform, Time.time - age);
            tick.Invoke(platform, null);
            Assert.AreEqual(expected, (int)stage.GetValue(platform));
        }

        run.Begin(20);
        platform.StartCrumble(player);
        Assert.AreEqual(0, (int)stage.GetValue(platform));
        run.Begin(21);
        Assert.AreEqual(5f, run.PlatformCrumbleSeconds);
        StandOnPlatform();
        platform.StartCrumble(player);
        Assert.AreEqual(1, (int)stage.GetValue(platform));
        Advance(2.2f, 2);
        contact.SetValue(player, -100f);
        Advance(3f, 0);
        Assert.IsTrue(collider.enabled);

        StandOnPlatform();
        platform.StartCrumble(player);
        Advance(3.8f, 3);
        int hearts = PlayerHealth.Instance.Hearts;
        Advance(4.99f, 3);
        Assert.AreEqual(hearts, PlayerHealth.Instance.Hearts);
        Advance(5.01f, 4);
        Assert.IsFalse(collider.enabled);
        Assert.AreEqual(hearts - 1, PlayerHealth.Instance.Hearts);
        Advance(6f, 4);
        Assert.AreEqual(hearts - 1, PlayerHealth.Instance.Hearts);
        typeof(MovingPlatform2D).GetField("rebuildAt", Private).SetValue(platform, Time.time - .01f);
        Advance(7.3f, 0);
        Assert.IsTrue(collider.enabled);
        run.Begin(30);
        Assert.AreEqual(5f, run.PlatformCrumbleSeconds);
        run.Begin(31);
        Assert.AreEqual(3.5f, run.PlatformCrumbleSeconds);
        run.Begin(91);
        Assert.AreEqual(2.5f, run.PlatformCrumbleSeconds);
        Debug.Log("CRUMBLE PASS: 20 stable; 21 heals after departure, bursts at 5 seconds, costs one heart and rebuilds; later levels accelerate");
    }

    static void Finish(int code,string report)
    {
        File.WriteAllText("Library/CostumePlaySmoke.txt",report); SessionState.SetBool("CostumeSmokeActive",false);
        SessionState.SetInt("CostumeSmokeCode",code); SessionState.SetBool("CostumeSmokeFinished",true);
        EditorApplication.ExitPlaymode();
        if(!EditorApplication.isPlaying) EditorApplication.delayCall+=()=>EditorApplication.Exit(code);
    }
}
