using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class MobileExports
{
    const string Output = "C:/Users/Acer/Documents/Codex/2026-10-26/un/outputs/mobile";
    [MenuItem("Cihazlar/Kisisel Windows Surumu Olustur")]
    public static void Windows()
    {
        string folder="C:/Users/Acer/Documents/Codex/2026-10-26/un/outputs/KokYankisi-Windows";
        Directory.CreateDirectory(folder);
        PlayerSettings.productName="Kök Yankısı";PlayerSettings.bundleVersion="0.4.0";
        var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions {
            scenes=new[]{"Assets/Scenes/SampleScene.unity"},target=BuildTarget.StandaloneWindows64,
            locationPathName=folder+"/KokYankisi.exe",options=BuildOptions.CompressWithLz4
        });
        WriteReport(report,"Personal-Windows");
    }
    public static void QueueAndroid(){File.WriteAllText("Library/AndroidBuild.request","build");}
    [MenuItem("Cihazlar/Android Deneme APK Olustur")]
    public static void Android()
    {
        if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android)) throw new Exception("Android Build Support is not installed.");
        Directory.CreateDirectory(Output);
        EditorUserBuildSettings.buildAppBundle = false;
        PlayerSettings.productName = "Kök Yankısı";
        PlayerSettings.bundleVersion = "0.4.0";
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.keremyuruk.kokyankisi");
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        PlayerSettings.Android.bundleVersionCode = 40;
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
            scenes = new[] { "Assets/Scenes/SampleScene.unity" },
            target = BuildTarget.Android,
            locationPathName = Output + "/KokYankisi-Android-Test.apk",
            options = BuildOptions.Development | BuildOptions.CompressWithLz4
        });
        WriteReport(report, "Android");
    }
    [MenuItem("Cihazlar/iOS Xcode Projesi Olustur")]
    public static void IOS()
    {
        if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.iOS, BuildTarget.iOS)) throw new Exception("iOS Build Support is not installed.");
        Directory.CreateDirectory(Output);
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, "com.keremyuruk.kokyankisi");
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
            scenes = new[] { "Assets/Scenes/SampleScene.unity" },
            target = BuildTarget.iOS,
            locationPathName = Output + "/iOS-Xcode",
            options = BuildOptions.Development
        });
        WriteReport(report, "iOS-Xcode");
    }
    static void WriteReport(BuildReport report, string name)
    {
        var s = report.summary;
        File.WriteAllText("Library/" + name + "-Build.txt", "Result: " + s.result + "\nErrors: " + s.totalErrors + "\nSize: " + s.totalSize + "\nTime: " + s.totalTime);
        if (s.result != BuildResult.Succeeded) throw new Exception(name + " build failed.");
    }
}
