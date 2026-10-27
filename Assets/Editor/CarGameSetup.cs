using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

// Editor-only scene construction. Gameplay stays in the teacher's PlayerController.
[InitializeOnLoad]
public static class CarGameSetup
{
    const string Marker = "Library/CarGameSetup.v6.done";
    const string Report = "Library/CarGameVerification.txt";
    const string Art = "Assets/CarGameArt/";
    static Sprite square, circle, triangle;
    static Material spriteMaterial;
    static TMP_FontAsset font;
    static int phase;
    static double phaseTime;
    static Vector3 originalPosition;
    static Vector3 oldGoldPosition;
    static string results;
    static PlayerController player;
    static float maxJumpHeight;
    static bool airJumpChecked;

    static CarGameSetup()
    {
        // Scene construction is manual after the initial setup.
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool("CarGameVerify", false))
            {
                phase = 0;
                results = "";
                phaseTime = EditorApplication.timeSinceStartup;
                EditorApplication.update -= TestTick;
                EditorApplication.update += TestTick;
            }
        };
    }

    static void AutoSetup()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            // Scene construction is manual after the initial setup.
            return;
        }
        if (File.Exists(Marker)) return;
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorApplication.isPlaying = false;
            // Scene construction is manual after the initial setup.
            return;
        }
        try { Build(); }
        catch (Exception e) { File.WriteAllText(Report, "SETUP FAILED\n" + e); Debug.LogException(e); }
    }

    [MenuItem("Araba Oyunu/Temel Sahneyi Kur")]
    public static void Build()
    {
        if (EditorApplication.isPlaying) throw new Exception("Exit Play mode before building.");
        Directory.CreateDirectory(Art);
        if (!File.Exists("Assets/TextMesh Pro/Resources/TMP Settings.asset"))
        {
            var resources = Directory.GetFiles("Library/PackageCache", "TMP Essential Resources.unitypackage", SearchOption.AllDirectories);
            if (resources.Length == 0) throw new Exception("Bundled TMP essential resources not found.");
            AssetDatabase.ImportPackage(resources[0], false);
            // Scene construction is manual after the initial setup.
            return;
        }
        Directory.CreateDirectory("Assets/Scenes/Backups");
        if (!File.Exists("Assets/Scenes/Backups/OriginalSampleScene.unity"))
            AssetDatabase.CopyAsset("Assets/Scenes/SampleScene.unity", "Assets/Scenes/Backups/OriginalSampleScene.unity");

        square = MakeSprite("Square", 0);
        circle = MakeSprite("Circle", 1);
        triangle = MakeSprite("Triangle", 2);
        spriteMaterial = AssetDatabase.LoadAssetAtPath<Material>(Art + "FlatSprites.mat");
        if (!spriteMaterial)
        {
            spriteMaterial = new Material(Shader.Find("Sprites/Default"));
            AssetDatabase.CreateAsset(spriteMaterial, Art + "FlatSprites.mat");
        }
        font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(Art + "GameFont.asset");
        if (!font)
        {
            var sourceFont = AssetDatabase.LoadAssetAtPath<Font>(Art + "Arial.ttf");
            if (!sourceFont) throw new Exception("Game font source missing.");
            font = TMP_FontAsset.CreateFontAsset(sourceFont);
            font.name = "GameFont";
            font.TryAddCharacters("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789 :/+-.,!?ÇĞİÖŞÜçğıöşü←→");
            AssetDatabase.CreateAsset(font, Art + "GameFont.asset");
            AssetDatabase.AddObjectToAsset(font.material, font);
            foreach (var atlas in font.atlasTextures) if (atlas) AssetDatabase.AddObjectToAsset(atlas, font);
        }

        var tags = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        var list = tags.FindProperty("tags");
        bool found = false;
        for (int i = 0; i < list.arraySize; i++) if (list.GetArrayElementAtIndex(i).stringValue == "altin") found = true;
        if (!found) { list.InsertArrayElementAtIndex(list.arraySize); list.GetArrayElementAtIndex(list.arraySize - 1).stringValue = "altin"; tags.ApplyModifiedProperties(); }
        int groundLayer = LayerMask.NameToLayer("Zemin");
        if (groundLayer < 0)
        {
            var layers = tags.FindProperty("layers");
            for (int i = 8; i < 32; i++)
                if (string.IsNullOrEmpty(layers.GetArrayElementAtIndex(i).stringValue))
                {
                    groundLayer = i;
                    layers.GetArrayElementAtIndex(i).stringValue = "Zemin";
                    tags.ApplyModifiedProperties();
                    break;
                }
            if (groundLayer < 0) throw new Exception("No free ground layer.");
        }

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        cameraObject.tag = "MainCamera";
        var camera = cameraObject.GetComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 5.4f;
        camera.backgroundColor = Hex("A6DDEA");
        camera.clearFlags = CameraClearFlags.SolidColor;
        cameraObject.transform.position = new Vector3(0, 0.3f, -10);

        var backdrop = new GameObject("Arka Plan").transform;
        const string backgroundPath = Art + "CartoonWoodland.png";
        AssetDatabase.ImportAsset(backgroundPath);
        var backgroundImporter = (TextureImporter)AssetImporter.GetAtPath(backgroundPath);
        backgroundImporter.textureType = TextureImporterType.Sprite;
        backgroundImporter.spriteImportMode = SpriteImportMode.Single;
        var backgroundSettings = new TextureImporterSettings();
        backgroundImporter.ReadTextureSettings(backgroundSettings);
        backgroundSettings.spriteMeshType = SpriteMeshType.FullRect;
        backgroundImporter.SetTextureSettings(backgroundSettings);
        backgroundImporter.spritePixelsPerUnit = 100;
        backgroundImporter.maxTextureSize = 4096;
        backgroundImporter.mipmapEnabled = false;
        backgroundImporter.filterMode = FilterMode.Bilinear;
        backgroundImporter.textureCompression = TextureImporterCompression.Uncompressed;
        backgroundImporter.SaveAndReimport();
        var landscape = AssetDatabase.LoadAssetAtPath<Sprite>(backgroundPath);
        float landscapeScale = Mathf.Max(19.2f / landscape.bounds.size.x, 10.8f / landscape.bounds.size.y);
        Shape("Cizgi Orman Manzarasi", landscape, new Vector2(0, .3f), new Vector2(landscapeScale, landscapeScale), Color.white, -30, backdrop);
        var world = new GameObject("Zemin ve Sinirlar").transform;
        var ground = Shape("Zemin", square, new Vector2(0, -4.4f), new Vector2(20, 2f), Hex("493F35"), 0, world);
        ground.layer = groundLayer;
        ground.AddComponent<BoxCollider2D>();
        Shape("Cimen", square, new Vector2(0, -3.39f), new Vector2(20, .16f), Hex("8BAE49"), 3, world);
        Shape("Yumusak Toprak", square, new Vector2(0, -3.62f), new Vector2(20, .3f), Hex("8A6D47"), 2, world);
        Shape("Toprak Katmani", square, new Vector2(0, -3.92f), new Vector2(20, .3f), Hex("675438"), 1, world);
        for (int i = 0; i < 38; i++)
        {
            float x = -9.6f + i * .52f;
            Shape("Kucuk Tas", circle, new Vector2(x, -3.92f - (i % 4) * .15f), new Vector2(.11f + (i % 3) * .04f, .07f), Hex("A38D67"), 2, world);
            Shape("Ot", triangle, new Vector2(x + .1f, -3.27f), new Vector2(.06f, .16f + (i % 3) * .035f), Hex("718E38"), 3, world);
        }
        Barrier("Sol Sinir", new Vector2(-9, 0), new Vector2(.3f, 20), world);
        Barrier("Sag Sinir", new Vector2(9, 0), new Vector2(.3f, 20), world);
        Barrier("Ust Sinir", new Vector2(0, 5.35f), new Vector2(20, .25f), world);

        var car = new GameObject("Oyuncu");
        car.transform.position = new Vector3(-4, -2.65f, 0);
        var body = car.AddComponent<Rigidbody2D>();
        body.gravityScale = 2;
        body.constraints = RigidbodyConstraints2D.FreezeRotation;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        var collider = car.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(1f, 1.6f);
        collider.offset = new Vector2(0, .3f);
        player = car.AddComponent<PlayerController>();
        player.jumpForce = 8f;
        var t = car.transform;
        LocalShape("Kare Govde", square, Vector2.zero, new Vector2(1f, 1f), Hex("397AC0"), 10, t);
        LocalShape("Yuvarlak Kafa", circle, new Vector2(0, .78f), new Vector2(.65f, .65f), Hex("FFD89A"), 11, t);

        var coins = new GameObject("Altinlar").transform;
        for (int i = 0; i < 5; i++)
        {
            var gold = Shape("Altin " + (i + 1), circle, new Vector2(-1 + i * 2, -2.5f), new Vector2(.6f, .6f), Hex("FFBE35"), 6, coins);
            gold.tag = "altin";
            gold.AddComponent<CircleCollider2D>().isTrigger = true;
            LocalShape("Altin Ic", circle, Vector2.zero, new Vector2(.72f, .72f), Hex("FFDE73"), 7, gold.transform);
            LocalShape("Altin Isareti", square, Vector2.zero, new Vector2(.12f, .47f), Hex("D99526"), 8, gold.transform);
        }

        var canvasObject = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1600, 900);
        scaler.matchWidthOrHeight = .5f;
        Panel("Baslik Paneli", canvas.transform, new Vector2(0, 1), new Vector2(38, -34), new Vector2(510, 108), Hex("183B49"));
        Label("Baslik", canvas.transform, "ALTIN PEŞİNDE", 38, new Vector2(0, 1), new Vector2(60, -46), new Vector2(470, 54), Hex("FFF0C6"));
        Label("Alt Baslik", canvas.transform, "2D ALTIN TOPLAMA OYUNU", 19, new Vector2(0, 1), new Vector2(63, -94), new Vector2(450, 40), Hex("B9D7DC"));
        Panel("Skor Paneli", canvas.transform, new Vector2(1, 1), new Vector2(-38, -34), new Vector2(245, 85), Hex("183B49"));
        var score = Label("Skor Yazisi", canvas.transform, "Skor: 0", 36, new Vector2(1, 1), new Vector2(-60, -49), new Vector2(205, 55), Hex("FFDA76"));
        score.alignment = TextAlignmentOptions.Right;
        player.skorYazisi = score;
        Panel("Kontrol Paneli", canvas.transform, new Vector2(.5f, 0), new Vector2(0, 26), new Vector2(800, 58), Hex("183B49"));
        var help = Label("Kontroller", canvas.transform, "A / D veya OK TUŞLARI: Yürü    •    BOŞLUK: Zıpla", 23, new Vector2(.5f, 0), new Vector2(0, 34), new Vector2(780, 42), Hex("E6F0E9"));
        help.alignment = TextAlignmentOptions.Center;
        // Add the bullet used by the help label to the dynamic atlas before saving.
        font.TryAddCharacters("•");

        EditorSceneManager.SaveScene(scene, "Assets/Scenes/SampleScene.unity");
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene("Assets/Scenes/SampleScene.unity", true) };
        AssetDatabase.SaveAssets();
        File.WriteAllText(Marker, DateTime.Now.ToString("O"));
        File.WriteAllText(Report, "SETUP PASS: scene, sprites, physics, altin tag, UI and teacher controller saved.\n");
        Selection.activeGameObject = car;
        EditorApplication.ExecuteMenuItem("Window/General/Game");
        EditorApplication.delayCall += Verify;
    }

    static Color Hex(string value) { ColorUtility.TryParseHtmlString("#" + value, out var c); return c; }

    static Sprite MakeSprite(string name, int mode)
    {
        string path = Art + name + ".png";
        if (!File.Exists(path))
        {
            var texture = new Texture2D(64, 64, TextureFormat.RGBA32, false);
            for (int y = 0; y < 64; y++) for (int x = 0; x < 64; x++)
            {
                float u = (x + .5f) / 64f, v = (y + .5f) / 64f;
                bool inside = mode == 0 || (mode == 1 ? Vector2.Distance(new Vector2(u, v), new Vector2(.5f, .5f)) <= .49f : Mathf.Abs(u - .5f) <= (1 - v) * .5f);
                texture.SetPixel(x, y, inside ? Color.white : Color.clear);
            }
            texture.Apply();
            File.WriteAllBytes(path, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
        }
        AssetDatabase.ImportAsset(path);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spritePixelsPerUnit = 64;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    static GameObject Shape(string name, Sprite sprite, Vector2 pos, Vector2 scale, Color color, int order, Transform parent)
    {
        var go = new GameObject(name, typeof(SpriteRenderer));
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(pos.x, pos.y, 0);
        go.transform.localScale = new Vector3(scale.x, scale.y, 1);
        var renderer = go.GetComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = color;
        renderer.sortingOrder = order;
        renderer.sharedMaterial = spriteMaterial;
        return go;
    }

    static void LocalShape(string name, Sprite sprite, Vector2 pos, Vector2 scale, Color color, int order, Transform parent)
    {
        var go = Shape(name, sprite, Vector2.zero, scale, color, order, parent);
        go.transform.localPosition = new Vector3(pos.x, pos.y, 0);
    }

    static void Barrier(string name, Vector2 pos, Vector2 size, Transform parent)
    {
        var go = new GameObject(name, typeof(BoxCollider2D));
        go.transform.SetParent(parent);
        go.transform.position = pos;
        go.GetComponent<BoxCollider2D>().size = size;
    }

    static RectTransform Rect(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = anchor;
        rect.pivot = new Vector2(anchor.x, anchor.y);
        rect.anchoredPosition = pos;
        rect.sizeDelta = size;
        return rect;
    }

    static void Panel(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size, Color color)
    {
        Rect(name, parent, anchor, pos, size).gameObject.AddComponent<Image>().color = color;
    }

    static TextMeshProUGUI Label(string name, Transform parent, string text, int size, Vector2 anchor, Vector2 pos, Vector2 dimensions, Color color)
    {
        var label = Rect(name, parent, anchor, pos, dimensions).gameObject.AddComponent<TextMeshProUGUI>();
        label.font = font;
        label.text = text;
        label.fontSize = size;
        label.color = color;
        label.raycastTarget = false;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.alignment = TextAlignmentOptions.Left;
        return label;
    }

    [MenuItem("Araba Oyunu/Temel Oyunu Test Et")]
    public static void Verify()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        results = "";
        SessionState.SetBool("CarGameVerify", true);
        phase = 0;
        phaseTime = EditorApplication.timeSinceStartup;
        EditorApplication.update -= TestTick;
        EditorApplication.update += TestTick;
        EditorApplication.isPlaying = true;
    }

    static void TestTick()
    {
        try
        {
            if (EditorApplication.timeSinceStartup - phaseTime > 45) throw new Exception("Play mode verification timed out at phase " + phase);
            if (!EditorApplication.isPlaying || EditorApplication.isCompiling) return;
            if (phase == 0)
            {
                player = UnityEngine.Object.FindFirstObjectByType<PlayerController>();
                if (!player || !player.skorYazisi || player.skorYazisi.text != "Skor: 0") return;
                originalPosition = player.transform.position;
                var gold = GameObject.FindWithTag("altin");
                oldGoldPosition = gold.transform.position;
                if (!gold.GetComponent<Collider2D>().isTrigger) throw new Exception("Gold collider is not a trigger.");
                var movement = typeof(PlayerController).GetField("movementX", BindingFlags.Instance | BindingFlags.NonPublic);
                movement.SetValue(player, 1f);
                typeof(PlayerController).GetMethod("FixedUpdate", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(player, null);
                if (Mathf.Abs(player.GetComponent<Rigidbody2D>().linearVelocity.x - 7) > .01f) throw new Exception("Horizontal motion failed.");
                movement.SetValue(player, 0f);
                results += "PASS: PlayerController horizontal motion is 7 units/s.\nPASS: initial score UI is Skor: 0.\n";
                player.GetComponent<Rigidbody2D>().position = oldGoldPosition;
                player.GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
                Physics2D.SyncTransforms();
                phase = 1;
                phaseTime = EditorApplication.timeSinceStartup;
            }
            else if (phase == 1)
            {
                int score = (int)typeof(PlayerController).GetField("skor", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(player);
                if (score < 1) return;
                if (player.skorYazisi.text != "Skor: " + score) throw new Exception("Score UI update failed.");
                var position = GameObject.FindWithTag("altin").transform.position;
                if (position == oldGoldPosition || position.x < -6 || position.x > 6 || position.y < -2.6f || position.y > -.6f) throw new Exception("Gold relocation failed.");
                results += "PASS: real Physics2D trigger collected gold, updated score UI and relocated gold inside reachable bounds.\n";
                var rb = player.GetComponent<Rigidbody2D>();
                rb.position = originalPosition;
                rb.linearVelocity = Vector2.zero;
                phase = 2;
                phaseTime = EditorApplication.timeSinceStartup;
            }
            else if (phase == 2)
            {
                if (!player.GetComponent<Collider2D>().IsTouchingLayers(LayerMask.GetMask("Zemin"))) return;
                originalPosition = player.GetComponent<Rigidbody2D>().position;
                typeof(PlayerController).GetMethod("ZiplamayiDene", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(player, null);
                if (Mathf.Abs(player.GetComponent<Rigidbody2D>().linearVelocity.y - 8) > .01f) throw new Exception("Grounded jump did not start.");
                maxJumpHeight = 0;
                airJumpChecked = false;
                phase = 3;
                phaseTime = EditorApplication.timeSinceStartup;
            }
            else if (phase == 3)
            {
                float elapsed = (float)(EditorApplication.timeSinceStartup - phaseTime);
                maxJumpHeight = Mathf.Max(maxJumpHeight, player.GetComponent<Rigidbody2D>().position.y - originalPosition.y);
                if (!airJumpChecked && elapsed > .16f)
                {
                    float before = player.GetComponent<Rigidbody2D>().linearVelocity.y;
                    typeof(PlayerController).GetMethod("ZiplamayiDene", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(player, null);
                    if (Mathf.Abs(player.GetComponent<Rigidbody2D>().linearVelocity.y - before) > .01f) throw new Exception("Air jump was allowed.");
                    airJumpChecked = true;
                }
                if (elapsed < .3f || !player.GetComponent<Collider2D>().IsTouchingLayers(LayerMask.GetMask("Zemin"))) return;
                if (!airJumpChecked || maxJumpHeight < 1.2f || maxJumpHeight > 1.9f || elapsed > 1.3f) throw new Exception("Jump arc outside desired range: height=" + maxJumpHeight + " duration=" + elapsed);
                results += "PASS: grounded jump, blocked air jump, peak height=" + maxJumpHeight.ToString("F2") + " units, air time=" + elapsed.ToString("F2") + " seconds.\n";
                File.AppendAllText(Report, results);
                SessionState.SetBool("CarGameVerify", false);
                EditorApplication.update -= TestTick;
                EditorApplication.isPlaying = false;
                Debug.Log("Car game verification passed. " + Report);
            }
        }
        catch (Exception e)
        {
            File.AppendAllText(Report, results + "FAIL: " + e + "\n");
            SessionState.SetBool("CarGameVerify", false);
            EditorApplication.update -= TestTick;
            EditorApplication.isPlaying = false;
            Debug.LogException(e);
        }
    }
}


