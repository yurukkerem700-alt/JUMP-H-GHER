using System;
using System.IO;
using UnityEditor;

[InitializeOnLoad]
public static class AndroidBuildQueue
{
    static double nextPoll;
    static AndroidBuildQueue(){EditorApplication.update+=Tick;}
    static void Tick()
    {
        if(EditorApplication.timeSinceStartup<nextPoll)return;nextPoll=EditorApplication.timeSinceStartup+1;
        if(!File.Exists("Library/AndroidBuild.request")||EditorApplication.isCompiling||EditorApplication.isUpdating)return;
        if(EditorApplication.isPlayingOrWillChangePlaymode){EditorApplication.isPlaying=false;return;}
        if(EditorUserBuildSettings.activeBuildTarget!=BuildTarget.Android)
        {
            if(!SessionState.GetBool("AndroidSwitchPending",false))
            {
                SessionState.SetBool("AndroidSwitchPending",true);
                File.WriteAllText("Library/Android-Build.txt","Status: switching editor to Android");
                if(!EditorUserBuildSettings.SwitchActiveBuildTargetAsync(BuildTargetGroup.Android,BuildTarget.Android))
                {SessionState.SetBool("AndroidSwitchPending",false);File.Delete("Library/AndroidBuild.request");File.WriteAllText("Library/Android-Build.txt","FAILED: Android target switch could not start.");}
            }
            return;
        }
        File.Delete("Library/AndroidBuild.request");SessionState.SetBool("AndroidSwitchPending",false);
        EditorApplication.delayCall+=()=>{
            try{MobileExports.Android();}catch(Exception e){File.WriteAllText("Library/Android-Build.txt","FAILED: "+e);}
        };
    }
}
