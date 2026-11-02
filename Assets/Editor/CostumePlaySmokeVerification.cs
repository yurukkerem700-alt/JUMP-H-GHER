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
                GameInput.Kick(); GameInput.ConsumeKick(); update.Invoke(visual,null); Assert.AreEqual(8,visual.CurrentFrame);
                kick.SetValue(visual,Time.time-.15f); update.Invoke(visual,null); Assert.AreEqual(9,visual.CurrentFrame);
                kick.SetValue(visual,Time.time-.25f); update.Invoke(visual,null); Assert.AreEqual(10,visual.CurrentFrame);
                Debug.Log("PLAY COSTUME PASS: "+id+"; illustrated renderer, idle, walk, jump, fall, land, kick x3");
            }
            MobileEnemyVerification.Check();
            Finish(0,"PASS: 6 illustrated costumes x8 poses; mobile single-camera layout; help dismissal; enemy grip and attack timing");
        }
        catch(Exception error) {Debug.LogException(error); Finish(1,"FAIL: "+error);}
        finally {if(had) PlayerPrefs.SetString("Costume",saved); else PlayerPrefs.DeleteKey("Costume"); PlayerPrefs.Save();}
    }
    static void Finish(int code,string report)
    {
        File.WriteAllText("Library/CostumePlaySmoke.txt",report); SessionState.SetBool("CostumeSmokeActive",false);
        SessionState.SetInt("CostumeSmokeCode",code); SessionState.SetBool("CostumeSmokeFinished",true);
        EditorApplication.ExitPlaymode();
        if(!EditorApplication.isPlaying) EditorApplication.delayCall+=()=>EditorApplication.Exit(code);
    }
}
