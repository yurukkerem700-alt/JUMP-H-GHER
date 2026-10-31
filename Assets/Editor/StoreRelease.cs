using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

// Mağaza sürümleri (Google Play AAB, App Store Xcode projesi, Steam Windows klasörü).
// Menü: Cihazlar ▸ Magaza ▸ ...   Komut satırı: Unity -batchmode -quit -projectPath . -executeMethod StoreRelease.Android
// İmza bilgileri ortam değişkenlerinden okunur; dosyaya yazılmaz:
//   ANDROID_KEYSTORE_PATH, ANDROID_KEYSTORE_PASSWORD, ANDROID_KEY_ALIAS, ANDROID_KEY_PASSWORD
public static class StoreRelease
{
    public const string Version = "1.0.0";
    public const int BuildNumber = 32;
    const string AppId = "com.keremyuruk.kokyankisi";
    static readonly string[] Scenes = { "Assets/Scenes/SampleScene.unity" };
    static string Out(string sub) { var p = Environment.GetEnvironmentVariable("STORE_OUT") ?? "Builds"; Directory.CreateDirectory(Path.Combine(p, sub)); return Path.Combine(p, sub); }

    static void Common()
    {
        PlayerSettings.productName = "Kök Yankısı";
        PlayerSettings.companyName = "Kerem Yuruk";
        PlayerSettings.bundleVersion = Version;
    }

    [MenuItem("Cihazlar/Magaza/Android AAB (Google Play)")]
    public static void Android()
    {
        Common();
        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
        EditorUserBuildSettings.buildAppBundle = true;
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, AppId);
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto; // en yüksek kurulu SDK (Play güncel hedef API ister)
        PlayerSettings.Android.bundleVersionCode = BuildNumber;
        var ks = Environment.GetEnvironmentVariable("ANDROID_KEYSTORE_PATH");
        if (!string.IsNullOrEmpty(ks))
        {
            PlayerSettings.Android.useCustomKeystore = true;
            PlayerSettings.Android.keystoreName = ks;
            PlayerSettings.Android.keystorePass = Environment.GetEnvironmentVariable("ANDROID_KEYSTORE_PASSWORD");
            PlayerSettings.Android.keyaliasName = Environment.GetEnvironmentVariable("ANDROID_KEY_ALIAS");
            PlayerSettings.Android.keyaliasPass = Environment.GetEnvironmentVariable("ANDROID_KEY_PASSWORD");
        }
        else Console_Warn("Keystore ortam değişkenleri yok: AAB imzasız/debug imzalı üretilir, Play'e yüklenemez.");
        Build(BuildTarget.Android, Path.Combine(Out("android"), "KokYankisi.aab"), BuildOptions.CompressWithLz4HC);
    }

    [MenuItem("Cihazlar/Magaza/iOS Xcode Projesi (App Store)")]
    public static void IOS()
    {
        Common();
        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.iOS, BuildTarget.iOS);
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, AppId);
        PlayerSettings.iOS.buildNumber = BuildNumber.ToString();
        PlayerSettings.iOS.targetOSVersionString = "13.0";
        var team = Environment.GetEnvironmentVariable("APPLE_TEAM_ID");
        if (!string.IsNullOrEmpty(team)) { PlayerSettings.iOS.appleDeveloperTeamID = team; PlayerSettings.iOS.appleEnableAutomaticSigning = true; }
        Build(BuildTarget.iOS, Out("ios"), BuildOptions.None); // Release yapılandırması Xcode'da seçilir; Archive için macOS gerekir
    }

    [MenuItem("Cihazlar/Magaza/Windows (Steam)")]
    public static void SteamWindows()
    {
        Common();
        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64);
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Standalone, AppId);
        Build(BuildTarget.StandaloneWindows64, Path.Combine(Out("steam-windows"), "KokYankisi.exe"), BuildOptions.CompressWithLz4HC);
    }

    static void Console_Warn(string m) { UnityEngine.Debug.LogWarning(m); }

    static void Build(BuildTarget target, string path, BuildOptions opt)
    {
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = Scenes, target = target, locationPathName = path, options = opt });
        var s = report.summary;
        File.WriteAllText("Library/Store-" + target + "-Build.txt", "Result: " + s.result + "\nErrors: " + s.totalErrors + "\nSize: " + s.totalSize + "\nOutput: " + path);
        if (s.result != BuildResult.Succeeded) throw new Exception(target + " mağaza derlemesi başarısız: " + s.totalErrors + " hata");
    }
}
