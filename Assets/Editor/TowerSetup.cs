using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using TMPro;

[InitializeOnLoad]
public static class TowerSetup
{
    static TowerSetup() { EditorApplication.delayCall += Requested; }
    static void Requested()
    {
        if (!File.Exists("Library/TowerSetup.request")) return;
        if (EditorApplication.isCompiling || EditorApplication.isUpdating) { EditorApplication.delayCall += Requested; return; }
        if (EditorApplication.isPlayingOrWillChangePlaymode) { EditorApplication.isPlaying = false; EditorApplication.delayCall += Requested; return; }
        File.Delete("Library/TowerSetup.request");
        Apply();
    }
    [MenuItem("Kok Yankisi/Yukari Ilerlemeyi Kur")]
    public static void Apply()
    {
        var tower = Object.FindFirstObjectByType<InfiniteTower>();
        if (!tower) tower = new GameObject("Sonsuz Kule").AddComponent<InfiniteTower>();
        tower.player = Object.FindFirstObjectByType<PlayerController>();
        tower.coins = Object.FindFirstObjectByType<CoinSequence>();
        tower.floorLabel = GameObject.Find("Asama").GetComponent<TextMeshProUGUI>();
        var rect = tower.floorLabel.rectTransform;
        rect.anchoredPosition = new Vector2(0, -122);
        tower.floorLabel.text = "KAT 0";
        EditorSceneManager.MarkSceneDirty(tower.gameObject.scene);
        EditorSceneManager.SaveScene(tower.gameObject.scene);
        AssetDatabase.SaveAssets();
        EditorApplication.delayCall += TowerVerification.Run;
    }
}
