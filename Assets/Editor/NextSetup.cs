using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class NextSetup
{
    public static void Apply()
    {
        if(EditorApplication.isPlaying)throw new System.Exception("Stop Play Mode first.");
        var tower=Object.FindFirstObjectByType<InfiniteTower>();
        tower.floorLabel.fontSize=24;tower.floorLabel.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,900);
        if(!tower.GetComponent<TowerThemes>())tower.gameObject.AddComponent<TowerThemes>();
        if(!tower.GetComponent<TowerPowers>())tower.gameObject.AddComponent<TowerPowers>();
        var flow=Object.FindFirstObjectByType<GameFlow>();if(!flow)flow=new GameObject("Oyun Akışı").AddComponent<GameFlow>();
        const string path="Assets/CarGameArt/GameIcon.png";
        AssetDatabase.ImportAsset(path);var imp=(TextureImporter)AssetImporter.GetAtPath(path);imp.textureType=TextureImporterType.Sprite;imp.maxTextureSize=1024;imp.mipmapEnabled=false;imp.SaveAndReimport();
        flow.icon=AssetDatabase.LoadAssetAtPath<Sprite>(path);
        PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Unknown,new[]{AssetDatabase.LoadAssetAtPath<Texture2D>(path)});
        PlayerSettings.productName="Kök Yankısı";PlayerSettings.bundleVersion="0.5.0";PlayerSettings.runInBackground=true;
        var world=Object.FindFirstObjectByType<AltitudeWorld>();world.mistSprite=null;
        const string atlasPath="Assets/CarGameArt/ThemeAtlas.png";
        AssetDatabase.ImportAsset(atlasPath);var atlasImp=(TextureImporter)AssetImporter.GetAtPath(atlasPath);atlasImp.isReadable=true;atlasImp.mipmapEnabled=false;atlasImp.maxTextureSize=2048;atlasImp.SaveAndReimport();
        world.themeAtlas=AssetDatabase.LoadAssetAtPath<Texture2D>(atlasPath);
        EditorSceneManager.MarkSceneDirty(tower.gameObject.scene);EditorSceneManager.SaveScene(tower.gameObject.scene);AssetDatabase.SaveAssets();
    }
}
