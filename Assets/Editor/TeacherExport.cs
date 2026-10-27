using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class TeacherExport
{
    const string Root = "C:/Users/Acer/Documents/Codex/2026-10-26/un/outputs/teslim-kok-yankisi/KEREM YÜRÜK 254309044";
    const string Request = "Library/TeacherExport.request";
    const string Report = "Library/TeacherExport.result.txt";
    static TeacherExport() { EditorApplication.delayCall += RunIfRequested; }

    static void RunIfRequested()
    {
        if (!File.Exists(Request)) return;
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += RunIfRequested;
            return;
        }
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorApplication.isPlaying = false;
            EditorApplication.delayCall += RunIfRequested;
            return;
        }
        File.Delete(Request);
        try { Export(); }
        catch (Exception e) { File.WriteAllText(Report, "FAILED\n" + e); Debug.LogException(e); }
    }

    [MenuItem("Araba Oyunu/Ogretmen Icin EXE Olustur")]
    public static void Export()
    {
        Directory.CreateDirectory(Root);
        EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
        PlayerSettings.companyName = "Kerem Yuruk";
        PlayerSettings.productName = "Kok Yankisi";
        PlayerSettings.bundleVersion = "3.0";
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
        PlayerSettings.defaultScreenWidth = 1280;
        PlayerSettings.defaultScreenHeight = 720;
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        PlayerSettings.resizableWindow = true;
        PlayerSettings.runInBackground = false;
        var options = new BuildPlayerOptions
        {
            scenes = new[] { "Assets/Scenes/SampleScene.unity" },
            locationPathName = Root + "/KokYankisi.exe",
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        };
        var result = BuildPipeline.BuildPlayer(options);
        var summary = result.summary;
        File.WriteAllText(Report, "Result: " + summary.result + "\nErrors: " + summary.totalErrors + "\nWarnings: " + summary.totalWarnings + "\nSize: " + summary.totalSize + "\nDuration: " + summary.totalTime + "\nOutput: " + Root);
        if (summary.result != BuildResult.Succeeded) throw new Exception("Windows build failed. See Build report and Console.");
        Debug.Log("Teacher export succeeded: " + Root);
    }
}

