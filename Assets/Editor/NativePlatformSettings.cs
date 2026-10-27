using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

[InitializeOnLoad]
public static class NativePlatformSettings
{
    static NativePlatformSettings() { /* Platform settings are saved in ProjectSettings; explicit menu only. */ }
    [MenuItem("Cihazlar/Android ve iOS Ayarlarini Hazirla")]
    static void Configure()
    {
        const string marker = "Library/NativePlatformSettings.v1.done";
        if (File.Exists(marker)) return;
        if (EditorApplication.isCompiling || EditorApplication.isUpdating) { EditorApplication.delayCall += Configure; return; }
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.keremyuruk.kokyankisi");
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, "com.keremyuruk.kokyankisi");
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.iOS, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        PlayerSettings.Android.bundleVersionCode = 31;
        PlayerSettings.iOS.buildNumber = "31";
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
        PlayerSettings.allowedAutorotateToPortrait = true;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        PlayerSettings.allowedAutorotateToLandscapeLeft = true;
        PlayerSettings.allowedAutorotateToLandscapeRight = true;
        AssetDatabase.SaveAssets();
        File.WriteAllText(marker, "Native Android ARM64 and iOS configuration prepared; build modules and signing still required.");
    }
}

