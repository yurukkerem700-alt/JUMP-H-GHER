using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[InitializeOnLoad]
public static class DarkGameSetup
{
    const string Marker = "Library/DarkGameSetup.echo1.done";
    const string Art = "Assets/CarGameArt/";
    static Sprite square, circle, platformSprite;
    static Material material;
    static TMP_FontAsset font;
    static float platformTopFraction;
    static DarkGameSetup() { /* Manual menu action; never overwrite a cloned project scene. */ }

    static void AutoBuild()
    {
        if (File.Exists(Marker)) return;
        if (EditorApplication.isCompiling || EditorApplication.isUpdating) { EditorApplication.delayCall += AutoBuild; return; }
        if (EditorApplication.isPlayingOrWillChangePlaymode) { EditorApplication.isPlaying = false; EditorApplication.delayCall += AutoBuild; return; }
        try { Build(); }
        catch (Exception e) { File.WriteAllText("Library/DarkSetupError.txt", e.ToString()); Debug.LogException(e); }
    }

    [MenuItem("Karanlik Gecit/Sahneyi Kur")]
    public static void Build()
    {
        square = AssetDatabase.LoadAssetAtPath<Sprite>(Art + "Square.png");
        circle = AssetDatabase.LoadAssetAtPath<Sprite>(Art + "Circle.png");
        material = AssetDatabase.LoadAssetAtPath<Material>(Art + "FlatSprites.mat");
        font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(Art + "GameFont.asset");
        font.TryAddCharacters("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789 :/+-.,!?ÇĞİÖŞÜçğıöşü•");
        var background = ImportSprite(Art + "Graveyard.png", false);
        platformSprite = ImportSprite(Art + "RootPlatform.png", true);
        var texture = platformSprite.texture;
        int centerX = texture.width / 2;
        int top = texture.height - 1;
        for (; top >= 0; top--) if (texture.GetPixel(centerX, top).a > .5f) break;
        platformTopFraction = (top + .5f) / texture.height - .5f;
        int groundLayer = LayerMask.NameToLayer("Zemin");
        if (groundLayer < 0) throw new Exception("Zemin layer missing.");
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var camera = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener)).GetComponent<Camera>();
        camera.tag = "MainCamera";
        camera.orthographic = true;
        camera.orthographicSize = 6.1f;
        camera.backgroundColor = Hex("241A16");
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.transform.position = new Vector3(0, .3f, -10);
        float bgScale = Mathf.Max(21.7f / background.bounds.size.x, 12.2f / background.bounds.size.y);
        Shape("Mezarlik", background, new Vector2(0, .3f), new Vector2(bgScale, bgScale), Color.white, -40, null);

        var ground = Shape("Zemin", square, new Vector2(0, -4.6f), new Vector2(23, 2.4f), Hex("211713"), -1, null);
        ground.layer = groundLayer;
        ground.AddComponent<BoxCollider2D>();
        var groundArt = new GameObject("Toprak ve Kokler").transform;
        for (int i = 0; i < 8; i++) Slab("Koklu Toprak", new Vector2(-11.2f + i * 3.2f, -3.4f), 3.3f, groundArt, 1);
        for (int i = 0; i < 17; i++)
        {
            float x = -10.4f + i * 1.3f;
            var a = new Vector2(x, -3.7f);
            var b = new Vector2(x + Mathf.Sin(i * 2.4f) * .37f, -4.35f);
            var c = new Vector2(b.x + Mathf.Cos(i * 1.7f) * .32f, -5.05f);
            var d = new Vector2(c.x + Mathf.Sin(i) * .28f, -5.65f);
            RootLine(a, b, .12f, Hex("66412B"));
            RootLine(b, c, .085f, Hex("563621"));
            RootLine(c, d, .045f, Hex("422A1D"));
            RootLine(b, new Vector2(b.x + (i % 2 == 0 ? .58f : -.58f), -4.9f), .04f, Hex("68402A"));
            if (i % 3 == 0) RootLine(a + Vector2.down * .15f, b, .025f, Hex("B53E23"));
        }
        Barrier("Sol Sinir", new Vector2(-9.7f, 1), new Vector2(.3f, 16));
        Barrier("Sag Sinir", new Vector2(9.7f, 1), new Vector2(.3f, 16));

        var steps = new MovingPlatform2D[7];
        var stairs = new GameObject("Hareketli Basamaklar").transform;
        for (int i = 0; i < steps.Length; i++)
        {
            float topY = -2.25f + i * 1.15f;
            var go = new GameObject("Basamak " + (i + 1), typeof(Rigidbody2D), typeof(BoxCollider2D), typeof(PlatformEffector2D));
            go.transform.SetParent(stairs);
            go.transform.position = new Vector3(0, topY - .17f, 0);
            go.layer = groundLayer;
            var rb = go.GetComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            var box = go.GetComponent<BoxCollider2D>();
            box.size = new Vector2(2.05f, .22f);
            box.offset = new Vector2(0, .06f);
            box.usedByEffector = true;
            var effector = go.GetComponent<PlatformEffector2D>();
            effector.useOneWay = true;
            effector.useOneWayGrouping = true;
            effector.surfaceArc = 170;
            var step = go.AddComponent<MovingPlatform2D>();
            step.phase = i % 2 == 0 ? 0 : Mathf.PI;
            Slab("Kirmizi Koklu Tas", new Vector2(0, .17f), 2.2f, go.transform, 5, true);
            steps[i] = step;
        }

        var playerObject = new GameObject("Oyuncu", typeof(Rigidbody2D), typeof(BoxCollider2D), typeof(PlayerController));
        playerObject.transform.position = new Vector3(0, -3.08f, 0);
        var playerBody = playerObject.GetComponent<Rigidbody2D>();
        playerBody.gravityScale = 2;
        playerBody.constraints = RigidbodyConstraints2D.FreezeRotation;
        playerBody.interpolation = RigidbodyInterpolation2D.Interpolate;
        playerBody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        var playerBox = playerObject.GetComponent<BoxCollider2D>();
        playerBox.size = new Vector2(.58f, 1.1f);
        playerBox.offset = new Vector2(0, .25f);
        var player = playerObject.GetComponent<PlayerController>();
        player.moveSpeed = 4.8f;
        player.jumpForce = 8;
        Local("Kare Govde", square, Vector2.zero, new Vector2(.6f, .6f), Hex("AFA18A"), 15, playerObject.transform);
        Local("Yuvarlak Kafa", circle, new Vector2(0, .5f), new Vector2(.4f, .4f), Hex("DECCAC"), 16, playerObject.transform);
        Local("Kirmizi Atki", square, new Vector2(0, .28f), new Vector2(.61f, .09f), Hex("913626"), 17, playerObject.transform);

        var coinObject = new GameObject("Coin", typeof(CircleCollider2D));
        coinObject.tag = "altin";
        coinObject.GetComponent<CircleCollider2D>().radius = .23f;
        coinObject.GetComponent<CircleCollider2D>().isTrigger = true;
        Local("Dis Parilti", circle, Vector2.zero, new Vector2(.7f, .7f), new Color(.8f, .05f, .02f, .13f), 10, coinObject.transform);
        Local("Bakir Cerceve", circle, Vector2.zero, new Vector2(.47f, .47f), Hex("BE8248"), 11, coinObject.transform);
        Local("Coin Yuzu", circle, Vector2.zero, new Vector2(.35f, .35f), Hex("6D432A"), 12, coinObject.transform);
        Local("Coin Kabartmasi", square, Vector2.zero, new Vector2(.055f, .22f), Hex("F0BC77"), 14, coinObject.transform);
        Local("Ust Kabartma", square, new Vector2(0, .09f), new Vector2(.13f, .035f), Hex("F0BC77"), 14, coinObject.transform);
        Local("Alt Kabartma", square, new Vector2(0, -.09f), new Vector2(.13f, .035f), Hex("F0BC77"), 14, coinObject.transform);

        var canvas = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler)).GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvas.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1600, 900);
        scaler.matchWidthOrHeight = .5f;
        Panel("Baslik Paneli", canvas.transform, new Vector2(0, 1), new Vector2(30, -24), new Vector2(385, 90), new Color(.105f, .07f, .065f, .94f));
        Label("Baslik", canvas.transform, "KÖK YANKISI", 32, new Vector2(0, 1), new Vector2(50, -33), new Vector2(350, 48), Hex("E9C39C"));
        Label("Alt Baslik", canvas.transform, "TOPLA • ZIPLA • YÜKSEL", 16, new Vector2(0, 1), new Vector2(52, -82), new Vector2(345, 28), Hex("B79072"));
        Panel("Skor Paneli", canvas.transform, new Vector2(1, 1), new Vector2(-30, -24), new Vector2(260, 72), new Color(.105f, .07f, .065f, .94f));
        player.skorYazisi = Label("Skor", canvas.transform, "COIN  0", 29, new Vector2(1, 1), new Vector2(-48, -38), new Vector2(224, 45), Hex("EEA77A"));
        player.skorYazisi.alignment = TextAlignmentOptions.Right;
        var stage = Label("Asama", canvas.transform, "", 20, new Vector2(.5f, 1), new Vector2(0, -30), new Vector2(300, 40), Hex("D2B69A"));
        stage.alignment = TextAlignmentOptions.Center;
        Panel("Kontrol Paneli", canvas.transform, new Vector2(.5f, 0), new Vector2(0, 15), new Vector2(1250, 80), new Color(.105f, .07f, .065f, .94f));
        var hint = Label("Zamanlama", canvas.transform, "", 22, new Vector2(.5f, 0), new Vector2(0, 54), new Vector2(1200, 34), Hex("E3B491"));
        hint.alignment = TextAlignmentOptions.Center;
        var controls = Label("Kontroller", canvas.transform, "A / D: Yürü     W / BOŞLUK: Zıpla     R: Yeniden başla", 18, new Vector2(.5f, 0), new Vector2(0, 23), new Vector2(1180, 30), Hex("AF9881"));
        controls.alignment = TextAlignmentOptions.Center;
        var sequence = new GameObject("Muhur Sirasi").AddComponent<CoinSequence>();
        sequence.steps = steps;
        sequence.coin = coinObject.transform;
        sequence.stageText = stage;
        sequence.hintText = hint;
        coinObject.transform.position = new Vector3(3, -2.95f, 0);
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/SampleScene.unity");
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene("Assets/Scenes/SampleScene.unity", true) };
        AssetDatabase.SaveAssets();
        File.WriteAllText(Marker, DateTime.Now.ToString("O"));
        EditorApplication.ExecuteMenuItem("Window/General/Game");
        EditorApplication.delayCall += DarkGameVerification.Run;
    }

    static Sprite ImportSprite(string path, bool readable)
    {
        AssetDatabase.ImportAsset(path);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect;
        importer.SetTextureSettings(settings);
        importer.spritePixelsPerUnit = 100;
        importer.maxTextureSize = 4096;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.isReadable = readable;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    static void RootLine(Vector2 a, Vector2 b, float width, Color color)
    {
        var delta = b - a;
        var root = Shape("Derin Kok", square, (a + b) * .5f, new Vector2(delta.magnitude, width), color, 2, null);
        root.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
    }

    static void Slab(string name, Vector2 surface, float width, Transform parent, int order, bool local = false)
    {
        float scale = width / platformSprite.bounds.size.x;
        float topOffset = platformSprite.bounds.size.y * scale * platformTopFraction;
        var go = Shape(name, platformSprite, Vector2.zero, new Vector2(scale, scale), Color.white, order, parent);
        if (local) go.transform.localPosition = new Vector3(surface.x, surface.y - topOffset, 0);
        else go.transform.position = new Vector3(surface.x, surface.y - topOffset, 0);
    }
    static Color Hex(string html) { ColorUtility.TryParseHtmlString("#" + html, out var c); return c; }
    static GameObject Shape(string name, Sprite sprite, Vector2 pos, Vector2 scale, Color color, int order, Transform parent)
    {
        var go = new GameObject(name, typeof(SpriteRenderer));
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
        go.transform.localScale = new Vector3(scale.x, scale.y, 1);
        var renderer = go.GetComponent<SpriteRenderer>();
        renderer.sprite = sprite; renderer.color = color; renderer.sortingOrder = order; renderer.sharedMaterial = material;
        return go;
    }
    static GameObject Local(string name, Sprite sprite, Vector2 pos, Vector2 scale, Color color, int order, Transform parent)
    {
        var go = Shape(name, sprite, Vector2.zero, scale, color, order, parent);
        go.transform.localPosition = pos; return go;
    }
    static void Barrier(string name, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(name, typeof(BoxCollider2D)); go.transform.position = pos; go.GetComponent<BoxCollider2D>().size = size;
    }
    static RectTransform Rect(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false); rect.anchorMin = rect.anchorMax = anchor; rect.pivot = anchor; rect.anchoredPosition = pos; rect.sizeDelta = size; return rect;
    }
    static GameObject Panel(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size, Color color)
    {
        var rect = Rect(name, parent, anchor, pos, size); rect.gameObject.AddComponent<Image>().color = color; return rect.gameObject;
    }
    static TextMeshProUGUI Label(string name, Transform parent, string text, int size, Vector2 anchor, Vector2 pos, Vector2 dimensions, Color color)
    {
        var label = Rect(name, parent, anchor, pos, dimensions).gameObject.AddComponent<TextMeshProUGUI>();
        label.font = font; label.text = text; label.fontSize = size; label.color = color; label.raycastTarget = false; label.textWrappingMode = TextWrappingModes.NoWrap; return label;
    }
}

