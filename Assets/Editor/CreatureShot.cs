using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

// Toplu mod: mezarlık arka planı + yeni ambient canlıların (baykuş, kedi, yarasa, ruh ışıkları) görüntüsünü SHOT_OUT klasörüne yazar.
// Kullanım: Unity.exe -batchmode -projectPath <depo> -executeMethod CreatureShot.Run -quit   (-nographics OLMADAN)
public static class CreatureShot
{
    public static void Run()
    {
        string outDir = Environment.GetEnvironmentVariable("SHOT_OUT"); Directory.CreateDirectory(outDir);
        FixImport();
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/CarGameArt/Graveyard.png");
        var bgGo = new GameObject("Mezarlik", typeof(SpriteRenderer)); var sr = bgGo.GetComponent<SpriteRenderer>();
        sr.sprite = sprite; sr.sortingOrder = -40;
        var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/CarGameArt/FlatSprites.mat");
        sr.sharedMaterial = material;
        // Oyundaki ölçek: arka plan 21.7 x 12.2 birime gerilir, kamera 6.1 boyunda ve y = .3'te durur (GameDeviceLayout / DarkGameSetup).
        bgGo.transform.position = new Vector3(0, .3f, 0);
        bgGo.transform.localScale = new Vector3(21.7f / sprite.bounds.size.x, 12.2f / sprite.bounds.size.y, 1);
        var cam = new GameObject("Cam", typeof(Camera)).GetComponent<Camera>();
        cam.orthographic = true; cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = Color.black;
        cam.orthographicSize = 6.1f; cam.transform.position = new Vector3(0, .3f, -10);
        var rt = new RenderTexture(1920, 1080, 24); cam.targetTexture = rt;
        var decor = new GameObject("Mezar Süsü").AddComponent<GraveDecor>();
        decor.Init(material);
        typeof(GraveDecor).GetMethod("LateUpdate", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(decor, null);
        cam.Render(); cam.Render(); cam.Render(); RenderTexture.active = rt;
        var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0); tex.Apply();
        File.WriteAllBytes(Path.Combine(outDir, "yaratiklar.png"), tex.EncodeToPNG());
        RenderTexture.active = null;
    }

    // Yeni PNG'ler mevcut dekor/düşman şeritleriyle aynı içe aktarma ayarlarını alır (alfa şeffaflık, mipmap kapalı, sıkıştırmasız).
    [MenuItem("Cihazlar/Yeni Canlı Dokularını Düzelt")]
    public static void FixImport()
    {
        foreach (var path in new[] { "Assets/Resources/Decor/Baykus.png", "Assets/Resources/Decor/KaraKedi.png", "Assets/Resources/Decor/Yarasa.png", "Assets/Resources/Decor/Kuzgun.png", "Assets/Resources/Decor/ZombiMezar.png", "Assets/Resources/Enemies/Karakoncolos.png", "Assets/Resources/Enemies/KaraKurt.png" })
        {
            var imp = AssetImporter.GetAtPath(path) as TextureImporter; if (imp == null) continue;
            if (!path.EndsWith("Kuzgun.png") && !path.EndsWith("ZombiMezar.png")) imp.textureType = TextureImporterType.Default; imp.alphaSource = TextureImporterAlphaSource.FromInput; imp.alphaIsTransparency = true;
            bool decor = path.Contains("/Decor/");
            imp.mipmapEnabled = decor; imp.textureCompression = TextureImporterCompression.Uncompressed; imp.maxTextureSize = 2048; imp.filterMode = decor ? FilterMode.Trilinear : FilterMode.Bilinear;
            imp.SaveAndReimport();
        }
    }
}
