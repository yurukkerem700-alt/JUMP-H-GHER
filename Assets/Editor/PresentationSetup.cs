using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class PresentationSetup
{
    const string Art="Assets/CarGameArt/";
    static PresentationSetup(){EditorApplication.delayCall+=Requested;}
    static void Requested()
    {
        if(!File.Exists("Library/PresentationSetup.request"))return;
        if(EditorApplication.isCompiling||EditorApplication.isUpdating){EditorApplication.delayCall+=Requested;return;}
        if(EditorApplication.isPlayingOrWillChangePlaymode){EditorApplication.isPlaying=false;EditorApplication.delayCall+=Requested;return;}
        File.Delete("Library/PresentationSetup.request"); Apply();
    }
    [MenuItem("Kok Yankisi/Gokyuzu ve Gezgin Tasarimini Kur")]
    public static void Apply()
    {
        var sky=Import(Art+"CloudSky.png",false);
        var birds=Import(Art+"MutantRavenSheet.png",true);
        var hero=Import(Art+"RootWandererSheet.png",true);
        var renderer=GameObject.Find("Mezarlik").GetComponent<SpriteRenderer>();
        renderer.transform.position=new Vector3(0,.3f,0);
        renderer.transform.localScale=new Vector3(21.7f/renderer.sprite.bounds.size.x,12.2f/renderer.sprite.bounds.size.y,1);
        var world=Object.FindFirstObjectByType<AltitudeWorld>();
        if(!world)world=new GameObject("Yukseklik Dunyasi").AddComponent<AltitudeWorld>();
        var material=AssetDatabase.LoadAssetAtPath<Material>(Art+"AltitudeSky.mat");
        if(!material){material=new Material(Shader.Find("KokYankisi/AltitudeSky"));AssetDatabase.CreateAsset(material,Art+"AltitudeSky.mat");}
        world.skySprite=AssetDatabase.LoadAssetAtPath<Sprite>(Art+"CloudSky.png");
        world.skyMaterial=material;world.birdSheet=birds;
        world.birdMaterial=AssetDatabase.LoadAssetAtPath<Material>(Art+"FlatSprites.mat");
        world.mistSprite=null;
        var player=Object.FindFirstObjectByType<PlayerController>();
        var visual=player.GetComponent<PlayerVisual2D>();if(!visual)visual=player.gameObject.AddComponent<PlayerVisual2D>();
        visual.animationSheet=hero;visual.spriteMaterial=world.birdMaterial;
        EditorSceneManager.MarkSceneDirty(player.gameObject.scene);EditorSceneManager.SaveScene(player.gameObject.scene);AssetDatabase.SaveAssets();
        EditorApplication.delayCall+=PresentationVerification.Run;
    }
    static Texture2D Import(string path,bool readable)
    {
        AssetDatabase.ImportAsset(path);
        var imp=(TextureImporter)AssetImporter.GetAtPath(path);imp.textureType=TextureImporterType.Sprite;imp.spriteImportMode=SpriteImportMode.Single;
        var set=new TextureImporterSettings();imp.ReadTextureSettings(set);set.spriteMeshType=SpriteMeshType.FullRect;imp.SetTextureSettings(set);
        imp.isReadable=readable;imp.alphaIsTransparency=true;imp.mipmapEnabled=false;imp.maxTextureSize=2048;imp.textureCompression=TextureImporterCompression.Uncompressed;imp.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }
}
