using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

[InitializeOnLoad]
public static class ReleaseBuildQueue
{
    static string OutputRoot=>(File.Exists("Library/ReleaseOutput.path")?File.ReadAllText("Library/ReleaseOutput.path").Trim():Path.GetDirectoryName(Application.dataPath)+"/Builds");
    static string WebFolder=>(File.Exists("Library/ReleaseWebOutput.path")?File.ReadAllText("Library/ReleaseWebOutput.path").Trim():Path.GetDirectoryName(Application.dataPath)+"/web");
    static double next;
    static ReleaseBuildQueue(){EditorApplication.update+=Tick;}
    [MenuItem("Cihazlar/Windows ve Web Surumlerini Olustur")]
    public static void Queue(){File.WriteAllText("Library/ReleaseBuild.stage","windows");}
    static void Tick()
    {
        if(EditorApplication.timeSinceStartup<next)return;next=EditorApplication.timeSinceStartup+2;
        if(!File.Exists("Library/ReleaseBuild.stage")||EditorApplication.isCompiling||EditorApplication.isUpdating||BuildPipeline.isBuildingPlayer)return;
        if(EditorApplication.isPlayingOrWillChangePlaymode){EditorApplication.isPlaying=false;return;}
        string stage=File.ReadAllText("Library/ReleaseBuild.stage").Trim();
        if(stage=="done"||stage.StartsWith("FAILED"))return;
        try
        {
            PlayerSettings.productName="Kök Yankısı";PlayerSettings.bundleVersion="0.5.0";PlayerSettings.runInBackground=true;
            if(stage=="windows")
            {
                if(EditorUserBuildSettings.activeBuildTarget!=BuildTarget.StandaloneWindows64){EditorUserBuildSettings.SwitchActiveBuildTargetAsync(BuildTargetGroup.Standalone,BuildTarget.StandaloneWindows64);return;}
                string folder=OutputRoot+"/KokYankisi-Windows";Directory.CreateDirectory(folder);
                Build(BuildTarget.StandaloneWindows64,folder+"/KokYankisi.exe","Windows");
                File.WriteAllText("Library/ReleaseBuild.stage","web-switch");
            }
            else if(stage=="web-switch")
            {
                if(!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.WebGL,BuildTarget.WebGL))return;
                if(EditorUserBuildSettings.activeBuildTarget!=BuildTarget.WebGL){EditorUserBuildSettings.SwitchActiveBuildTargetAsync(BuildTargetGroup.WebGL,BuildTarget.WebGL);return;}
                File.WriteAllText("Library/ReleaseBuild.stage","web-build");
            }
            else if(stage=="web-build")
            {
                string folder=WebFolder;Directory.CreateDirectory(folder);
                PlayerSettings.WebGL.compressionFormat=WebGLCompressionFormat.Disabled;PlayerSettings.WebGL.initialMemorySize=256;
                Build(BuildTarget.WebGL,folder,"WebGL");
                File.Copy(Application.dataPath+"/WebTemplates/web-index.html",folder+"/index.html",true);
                string html=File.ReadAllText(folder+"/index.html");
                foreach(string suffix in new[]{"loader.js","framework.js","data","wasm"})
                {string path=Directory.GetFiles(folder+"/Build","*."+suffix).Single();html=html.Replace("__"+suffix.ToUpperInvariant().Replace('.','_')+"__","Build/"+Path.GetFileName(path));}
                File.WriteAllText(folder+"/index.html",html);
                WebPwaExport.Apply(folder);
                File.WriteAllText("Library/ReleaseBuild.stage","done");
            }
        }
        catch(Exception e){File.WriteAllText("Library/ReleaseBuild.stage","FAILED: "+e);Debug.LogException(e);}
    }
    static void Build(BuildTarget target,string path,string label)
    {
        File.WriteAllText("Library/ReleaseBuild-status.txt","Building "+label+" at "+DateTime.Now);
        var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/Scenes/SampleScene.unity"},target=target,locationPathName=path,options=BuildOptions.CompressWithLz4});
        var s=report.summary;File.WriteAllText("Library/Release-"+label+".txt",s.result+" errors="+s.totalErrors+" size="+s.totalSize+" time="+s.totalTime);
        if(s.result!=BuildResult.Succeeded || s.totalErrors>0)throw new Exception(label+" build failed, errors="+s.totalErrors);
    }
}

