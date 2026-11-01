using System;
using System.IO;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// Toplu mod: bölüm haritasının, bölüm kartının ve ayarlar ekranının PNG görüntüsünü SHOT_OUT klasörüne yazar.
// Kullanım: Unity.exe -batchmode -projectPath <depo> -executeMethod MapShot.Run -quit   (-nographics OLMADAN)
public static class MapShot
{
    public static void Run()
    {
        string outDir = Environment.GetEnvironmentVariable("SHOT_OUT"); Directory.CreateDirectory(outDir);
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/CarGameArt/GameFont.asset");
        var camGo = new GameObject("Cam", typeof(Camera)); var cam = camGo.GetComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = Color.black; cam.orthographic = true; cam.orthographicSize = 5;
        var rt = new RenderTexture(1600, 900, 24); cam.targetTexture = rt;
        var canvas = new GameObject("C", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)).GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = cam; canvas.planeDistance = 1;
        var scaler = canvas.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1080, 1080); scaler.matchWidthOrHeight = .5f;

        for (int i = 1; i <= 4; i++) PlayerPrefs.SetInt("LvStars" + i, 3);
        try
        {
            var map = new LevelMap(canvas.transform, font, n => { }, () => { }, () => { });
            map.Open(); Canvas.ForceUpdateCanvases();
            // Toplu modda LateUpdate çalışmaz: yerleşimi elle tetikle (güncel bölüm, sonra biyom geçişi görünümü).
            var relayout = typeof(LevelMap).GetMethod("Relayout", BindingFlags.NonPublic | BindingFlags.Instance);
            var scrollTo = typeof(LevelMap).GetMethod("ScrollTo", BindingFlags.NonPublic | BindingFlags.Instance);
            relayout.Invoke(map, new object[] { new Vector2(1600, 900) }); Canvas.ForceUpdateCanvases();
            Shot(cam, rt, Path.Combine(outDir, "harita.png"));
            scrollTo.Invoke(map, new object[] { 7 }); Canvas.ForceUpdateCanvases();
            Shot(cam, rt, Path.Combine(outDir, "harita-gecis.png"));
            typeof(LevelMap).GetMethod("ShowCard", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(map, new object[] { 5 });
            Canvas.ForceUpdateCanvases();
            Shot(cam, rt, Path.Combine(outDir, "kart.png"));
            map.Close();
            var settings = new SettingsScreen(canvas.transform, font, () => { });
            settings.Open(); Canvas.ForceUpdateCanvases();
            Shot(cam, rt, Path.Combine(outDir, "ayarlar.png"));
        }
        finally { for (int i = 1; i <= 4; i++) PlayerPrefs.DeleteKey("LvStars" + i); }
    }

    static void Shot(Camera cam, RenderTexture rt, string path)
    {
        cam.Render();
        RenderTexture.active = rt;
        var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0); tex.Apply();
        File.WriteAllBytes(path, tex.EncodeToPNG());
        RenderTexture.active = null;
    }
}
