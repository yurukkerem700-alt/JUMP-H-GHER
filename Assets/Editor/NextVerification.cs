using System;
using System.IO;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class NextVerification
{
    static int phase,target,lastSupport;static float began,phaseAt,jumpAt;static InfiniteTower tower;static Rigidbody2D body;
    static NextVerification(){EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool("NextVerify",false)){phase=0;began=phaseAt=Time.realtimeSinceStartup;EditorApplication.update+=Tick;}};}
    public static void Run(){SessionState.SetBool("NextVerify",true);EditorApplication.isPlaying=true;EditorApplication.isPaused=false;}
    static void Tick()
    {
        try
        {
            if(!EditorApplication.isPlaying)return;
            float now=Time.realtimeSinceStartup;
            if(now-began>160)throw new Exception("Climbing timeout, target="+target);
            if(phase==0)
            {
                if(Time.timeSinceLevelLoad<.3f)return;
                Application.runInBackground=true;Time.timeScale=1;PlayerController.VerificationControl=true;PlayerController.VerificationMovement=0;
                tower=UnityEngine.Object.FindFirstObjectByType<InfiniteTower>();body=tower.player.GetComponent<Rigidbody2D>();
                var coins=tower.coins;var collider=coins.coin.GetComponent<Collider2D>();
                int last=-1;float lastY=-100;
                for(int n=0;n<100;n++)
                {
                    if(coins.SpawnFloor<=last||coins.GetCoinPosition().y<=lastY)throw new Exception("Coin heights not strictly increasing");
                    last=coins.SpawnFloor;lastY=coins.GetCoinPosition().y;
                    if(!coins.TryCollect(collider))throw new Exception("Collect rejected at "+n);
                    typeof(CoinSequence).GetField("nextCollectTime",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).SetValue(coins,0f);
                }
                if(TowerThemes.ThemeAtFloor(19)!=0||TowerThemes.ThemeAtFloor(20)!=1||TowerThemes.ThemeAtFloor(40)!=2||TowerThemes.ThemeAtFloor(100)!=0)throw new Exception("20-floor theme boundary incorrect");
                var powers=TowerPowers.Instance;
                var powerStep=tower.GetPlatform(4);
                body.position=new Vector2(powerStep.transform.position.x,powerStep.Top+.8f);
                body.transform.position=body.position; powers.TickGameplay();
                if(!powers.DoubleJumpActive)throw new Exception("Double jump pickup did not activate: player="+body.transform.position+" pickup="+GameObject.Find("Güçlendirme").transform.position+" floor="+tower.HighestReachedFloor);
                body.position=new Vector2(0,powerStep.Top+4);body.transform.position=body.position;Physics2D.SyncTransforms();body.linearVelocity=Vector2.down;
                typeof(PlayerController).GetField("lastGroundContact",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).SetValue(tower.player,-100f);
                tower.player.ZiplamayiDene();
                if(body.linearVelocity.y<7.5f)throw new Exception("Powered air jump failed");
                body.linearVelocity=Vector2.down;tower.player.ZiplamayiDene();
                if(body.linearVelocity.y>0)throw new Exception("Third jump was allowed");
                typeof(TowerPowers).GetField("magnetUntil",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).SetValue(powers,Time.time+15);
                body.position=coins.GetCoinPosition();body.transform.position=body.position;
                int before=tower.player.Score;powers.TickGameplay();
                if(tower.player.Score!=before+1)throw new Exception("Magnet collection failed");
                tower.player.RestartRun();tower.EnsureThroughFloor(22);
                var p=tower.GetPlatform(7);body.position=new Vector2(p.transform.position.x,p.Top+.58f);body.transform.position=body.position;Physics2D.SyncTransforms();body.linearVelocity=Vector2.zero;
                collider.enabled=false;target=8;lastSupport=7;phase=1;phaseAt=now;jumpAt=-100;
            }
            else if(phase==1)
            {
                var support=tower.player.GroundPlatform(out bool grounded);
                var destination=tower.GetPlatform(target);
                if(!destination)throw new Exception("Next platform missing at "+target+" body="+body.position+" top="+tower.HighestGeneratedFloor+" active="+tower.ActivePlatformCount);
                if(grounded&&support)
                {
                    PlayerController.VerificationMovement=0;
                    if(support.floorIndex>=target){lastSupport=support.floorIndex;target=lastSupport+1;phaseAt=now;if(target>21){Finish("PASS: 100 strictly rising coin spawns; double jump pickup and single extra air jump; magnet coin collection; 20-floor theme boundaries; actual physics climb from floor 8 through 22, including floor 9; continuous generation and upward camera.");return;}}
                    else if(now-jumpAt>.7f&&Mathf.Abs(support.transform.position.x)<.35f){tower.player.ZiplamayiDene();jumpAt=now;}
                }
                else
                {
                    float delta=destination.transform.position.x+destination.CurrentVelocity.x*.12f-body.position.x;
                    PlayerController.VerificationMovement=Mathf.Clamp(delta*5,-1,1);
                    if(body.position.y<InfiniteTower.SurfaceFor(lastSupport)-1.5f)throw new Exception("Fell below route at target "+target);
                }
            }
        }
        catch(Exception e){Finish("FAIL: "+e);}
    }
    static void Finish(string result){File.WriteAllText("Library/NextVerification.txt",result);SessionState.SetBool("NextVerify",false);PlayerController.VerificationControl=false;PlayerController.VerificationMovement=0;EditorApplication.update-=Tick;EditorApplication.isPlaying=false;}
}

