using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Mixamo FBX modellerini, oyundaki 2D stile uyan küçük sprite sayfalarına çevirir.
// Kullanım: Unity menüsü Cihazlar > Düşman Sprite Üret (veya -executeMethod EnemyBake.Run).
// Kaynak FBX dosyaları depoda tutulmaz; yol ENEMY_FBX_DIR ortam değişkeniyle ya da varsayılan Downloads klasöründen alınır.
public static class EnemyBake
{
    const string Work = "Assets/_EnemyBake";
    const string Out = "Assets/Resources/Enemies";
    const int Frames = 24, Cols = 8;

    class Job { public string file, name; public float yaw; }

    [MenuItem("Cihazlar/Düşman Sprite Üret")]
    public static void Run()
    {
        string dir = System.Environment.GetEnvironmentVariable("ENEMY_FBX_DIR");
        if (string.IsNullOrEmpty(dir)) dir = System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile) + "/Downloads";
        var jobs = new[]
        {
            new Job { file = "Freehang Climb.fbx", name = "Zombie", yaw = 0 },
            new Job { file = "Hard Landing.fbx", name = "Goblin", yaw = 0 },
        };
        string only = System.Environment.GetEnvironmentVariable("ENEMY_ONLY");
        string yawOverride = System.Environment.GetEnvironmentVariable("ENEMY_YAW");
        Directory.CreateDirectory(Out);
        foreach (var job in jobs)
        {
            if (!string.IsNullOrEmpty(only) && only != job.name) continue;
            if (!string.IsNullOrEmpty(yawOverride)) job.yaw = float.Parse(yawOverride, System.Globalization.CultureInfo.InvariantCulture);
            Bake(Path.Combine(dir, job.file), job);
        }
        if (Directory.Exists(Work)) AssetDatabase.DeleteAsset(Work);
        AssetDatabase.Refresh();
        Debug.Log("EnemyBake tamam");
    }

    static void Bake(string source, Job job)
    {
        Directory.CreateDirectory(Work);
        string asset = Work + "/" + job.name + ".fbx";
        File.Copy(source, asset, true);
        AssetDatabase.ImportAsset(asset, ImportAssetOptions.ForceSynchronousImport);
        // Gömülü dokuları dosyaya çıkar ve modele bağla
        string texDir = Work + "/" + job.name + "_dokular";
        Directory.CreateDirectory(texDir);
        var modelImporter = (ModelImporter)AssetImporter.GetAtPath(asset);
        modelImporter.ExtractTextures(texDir);
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        foreach (var f in Directory.GetFiles(texDir, "*.png")) Debug.Log(job.name + " doku dosyası: " + Path.GetFileName(f));
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(asset);
        var clip = AssetDatabase.LoadAllAssetsAtPath(asset).OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview")).OrderByDescending(c => c.length).First();
        Debug.Log(job.name + " klip: " + clip.name + " süre=" + clip.length);

        foreach (var o in AssetDatabase.LoadAllAssetsAtPath(asset))
            if (o is Texture2D || o is Material) Debug.Log(job.name + " alt-varlik: " + o.GetType().Name + " " + o.name + (o is Material m ? " shader=" + m.shader.name + " harita=" + (m.HasProperty("_BaseMap") ? (m.GetTexture("_BaseMap") ? m.GetTexture("_BaseMap").name : "yok") : "?") : ""));
        if (System.Environment.GetEnvironmentVariable("ENEMY_DIAG") == "1") return;
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(.62f, .6f, .62f);
        var lightGo = new GameObject("Işık", typeof(Light)); var light = lightGo.GetComponent<Light>();
        light.type = LightType.Directional; light.intensity = 1.1f; light.color = new Color(1, .95f, .9f);
        lightGo.transform.rotation = Quaternion.Euler(35, 120, 0); // ışık kameradan, hafif üstten
        var model = (GameObject)Object.Instantiate(prefab);
        Dress(model, texDir, job.name);
        model.transform.position = Vector3.zero; model.transform.rotation = Quaternion.Euler(0, job.yaw, 0);
        var camGo = new GameObject("Kamera", typeof(Camera)); var cam = camGo.GetComponent<Camera>();
        cam.orthographic = true; cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0, 0, 0, 0);
        cam.nearClipPlane = .01f; cam.farClipPlane = 50; cam.allowMSAA = false; cam.allowHDR = false;
        camGo.transform.position = new Vector3(10, 0, 0); camGo.transform.rotation = Quaternion.Euler(0, -90, 0); // +X'ten bakar; +Z (karakterin yüzü) sağa düşer

        // Her kare sabit ölçekte (Ppu piksel/birim) çizilir, sonra içeriğe göre sıkı kırpılır. Kökün gezdiği yol kareyi küçültmesin diye.
        const int Ppu = 140, probe = 1400; const float half = probe / (float)Ppu / 2, centerY = 2.5f;
        var rt = new RenderTexture(probe, probe, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB) { antiAliasing = 1 };
        cam.targetTexture = rt; cam.orthographicSize = half; camGo.transform.position = new Vector3(10, centerY, 0);
        var read = new Texture2D(probe, probe, TextureFormat.RGBA32, false);
        var crops = new Color32[Frames][]; var cropW = new int[Frames]; var cropH = new int[Frames];
        for (int i = 0; i < Frames; i++)
        {
            clip.SampleAnimation(model, clip.length * i / (Frames - 1));
            cam.Render(); RenderTexture.active = rt; read.ReadPixels(new Rect(0, 0, probe, probe), 0, 0); RenderTexture.active = null;
            var px = read.GetPixels32(); int x0 = probe, x1 = -1, y0 = probe, y1 = -1;
            for (int y = 0; y < probe; y++)
                for (int x = 0; x < probe; x++)
                    if (px[y * probe + x].a > 8) { if (x < x0) x0 = x; if (x > x1) x1 = x; if (y < y0) y0 = y; if (y > y1) y1 = y; }
            if (x1 < 0) { cropW[i] = cropH[i] = 4; crops[i] = new Color32[16]; continue; }
            x0 = Mathf.Max(0, x0 - 2); y0 = Mathf.Max(0, y0 - 2); x1 = Mathf.Min(probe - 1, x1 + 2); y1 = Mathf.Min(probe - 1, y1 + 2);
            cropW[i] = x1 - x0 + 1; cropH[i] = y1 - y0 + 1; crops[i] = new Color32[cropW[i] * cropH[i]];
            for (int y = 0; y < cropH[i]; y++) System.Array.Copy(px, (y0 + y) * probe + x0, crops[i], y * cropW[i], cropW[i]);
        }
        Object.DestroyImmediate(rt); Object.DestroyImmediate(read);
        int cw = cropW.Max(), ch = cropH.Max(), rows = Mathf.CeilToInt(Frames / (float)Cols);
        var atlas = new Texture2D(cw * Cols, ch * rows, TextureFormat.RGBA32, false);
        atlas.SetPixels32(new Color32[atlas.width * atlas.height]);
        var sizes = new System.Text.StringBuilder();
        for (int i = 0; i < Frames; i++)
        {
            // karenin alt-ortasına yerleştir; sayfada 0. satır üstte
            atlas.SetPixels32((i % Cols) * cw + (cw - cropW[i]) / 2, (rows - 1 - i / Cols) * ch, cropW[i], cropH[i], crops[i]);
            sizes.Append(i > 0 ? "," : "").Append("[").Append(cropW[i]).Append(",").Append(cropH[i]).Append("]");
        }
        atlas.Apply();
        File.WriteAllBytes(Out + "/" + job.name + ".png", atlas.EncodeToPNG());
        string meta = "{\"frames\":" + Frames + ",\"cols\":" + Cols + ",\"cellW\":" + cw + ",\"cellH\":" + ch + ",\"ppu\":" + Ppu + ",\"duration\":" + clip.length.ToString(System.Globalization.CultureInfo.InvariantCulture) + ",\"sizes\":[" + sizes + "]}";
        File.WriteAllText(Out + "/" + job.name + ".json", meta);
        Debug.Log(job.name + " meta: " + meta);
        cam.targetTexture = null; Object.DestroyImmediate(atlas);
        Object.DestroyImmediate(model);
        AssetDatabase.Refresh();
        var importer = (TextureImporter)AssetImporter.GetAtPath(Out + "/" + job.name + ".png");
        importer.textureType = TextureImporterType.Default; importer.alphaIsTransparency = true; importer.mipmapEnabled = false;
        importer.textureCompression = TextureImporterCompression.Uncompressed; importer.maxTextureSize = 4096; importer.filterMode = FilterMode.Bilinear; importer.wrapMode = TextureWrapMode.Clamp;
        importer.SaveAndReimport();
    }

    // FBX malzemeleri doku bağlamadan geldiği için URP Lit malzemelerini elle kurar.
    static void Dress(GameObject model, string texDir, string name)
    {
        foreach (var r in model.GetComponentsInChildren<SkinnedMeshRenderer>())
        {
            var source = r.sharedMaterials; var made = new Material[source.Length];
            for (int i = 0; i < source.Length; i++)
            {
                var m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                string file = TextureFor(name, source[i].name);
                var tex = file == null ? null : AssetDatabase.LoadAssetAtPath<Texture2D>(texDir + "/" + file);
                Debug.Log(name + " malzeme " + source[i].name + " -> " + (tex ? tex.name : "düz renk"));
                if (tex) { m.SetTexture("_BaseMap", tex); m.SetColor("_BaseColor", Color.white); }
                else m.SetColor("_BaseColor", new Color(.95f, .85f, .3f));
                m.SetFloat("_Smoothness", .12f); m.SetFloat("_Metallic", 0);
                made[i] = m;
            }
            r.sharedMaterials = made;
        }
    }

    static string TextureFor(string name, string material)
    {
        if (name == "Zombie") return material == "Ch30_Body" ? "Ch30_1001_Diffuse.png" : "Ch30_1002_Diffuse.png";
        return material == "phong1" ? "goblin_diffuse.png" : null;
    }
}
