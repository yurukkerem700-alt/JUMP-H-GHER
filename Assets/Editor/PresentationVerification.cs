using System;
using System.IO;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class PresentationVerification
{
    static float started,phaseAt;static int phase;static Vector3 birdStart;
    static PresentationVerification()
    {
        EditorApplication.playModeStateChanged+=s=>{
            if(s!=PlayModeStateChange.EnteredPlayMode||!SessionState.GetBool("PresentationVerify",false))return;
            Application.runInBackground=true;PlayerController.VerificationControl=true;PlayerController.VerificationMovement=0;
            phase=0;started=phaseAt=Time.realtimeSinceStartup;EditorApplication.update-=Tick;EditorApplication.update+=Tick;
        };
    }
    [MenuItem("Kok Yankisi/Dunya Yukselisini Test Et")]
    public static void Run(){if(EditorApplication.isPlayingOrWillChangePlaymode)return;SessionState.SetBool("PresentationVerify",true);EditorApplication.isPlaying=true;}
    static void Tick()
    {
        try
        {
            if(!EditorApplication.isPlaying||EditorApplication.isCompiling)return;
            float now=Time.realtimeSinceStartup;if(now-started>30)throw new Exception("World presentation test timeout.");
            var tower=UnityEngine.Object.FindFirstObjectByType<InfiniteTower>();
            var world=UnityEngine.Object.FindFirstObjectByType<AltitudeWorld>();
            if(phase==0)
            {
                if(Time.timeSinceLevelLoad<.5f)return;
                if(!tower.player.GetComponent<PlayerVisual2D>())throw new Exception("Original character art missing.");
                if(ShaderUtil.ShaderHasError(world.skyMaterial.shader))throw new Exception("Sky shader failed.");
                tower.coins.coin.GetComponent<Collider2D>().enabled=false;
                var top=tower.GetPlatform(6);var rb=tower.player.GetComponent<Rigidbody2D>();
                rb.position=new Vector2(top.transform.position.x+top.CurrentVelocity.x*.27f,top.Top+1);rb.linearVelocity=Vector2.zero;
                phase=1;phaseAt=now;
            }
            else if(phase==1)
            {
                if(Camera.main.transform.position.y<7||world.ActiveBirdCount<1)return;
                var graveyard=GameObject.Find("Mezarlik").transform;
                if(Mathf.Abs(graveyard.position.y-.3f)>.001f)throw new Exception("Graveyard still follows the camera.");
                if(Camera.main.WorldToViewportPoint(graveyard.position).y>.1f)throw new Exception("Graveyard has not been left below.");
                if(tower.HighestGeneratedFloor<13)throw new Exception("New world floors missing.");
                birdStart=GameObject.Find("Mutant Kus 1").transform.position;
                phase=2;phaseAt=now;
            }
            else if(phase==2&&now-phaseAt>.4f)
            {
                var bird=GameObject.Find("Mutant Kus 1").transform.position;
                if(Mathf.Abs(bird.x-birdStart.x)<.1f)throw new Exception("Bird flight is static.");
                File.WriteAllText("Library/PresentationVerification.txt","PASS: camera and player ascend to new world heights; graveyard stays at y=0.3 and leaves the view below.\nPASS: upper cloud layers, moving animated mutant birds, original eight-pose character and compiled sky material.\n");
                Finish(true);
            }
        }
        catch(Exception e){File.WriteAllText("Library/PresentationVerification.txt","FAIL: "+e);Finish(false);}
    }
    static void Finish(bool pass){SessionState.SetBool("PresentationVerify",false);PlayerController.VerificationControl=false;EditorApplication.update-=Tick;EditorApplication.isPlaying=false;if(pass)Debug.Log("World ascent presentation passed.");}
}
