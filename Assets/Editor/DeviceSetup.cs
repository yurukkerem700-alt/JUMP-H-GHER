using System;
using System.IO;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

[InitializeOnLoad]
public static class DeviceSetup
{
    const string Marker = "Library/DeviceSetup.v1.done";
    static TMP_FontAsset font;
    static DeviceSetup() { EditorApplication.delayCall += AutoSetup; }
    static void AutoSetup()
    {
        if (File.Exists(Marker)) return;
        if (EditorApplication.isCompiling || EditorApplication.isUpdating) { EditorApplication.delayCall += AutoSetup; return; }
        if (EditorApplication.isPlayingOrWillChangePlaymode) { EditorApplication.isPlaying = false; EditorApplication.delayCall += AutoSetup; return; }
        try { Build(); }
        catch (Exception e) { File.WriteAllText("Library/DeviceSetup.error.txt", e.ToString()); Debug.LogException(e); }
    }

    [MenuItem("Cihazlar/Dokunmatik Arayuzu Kur")]
    public static void Build()
    {
        var canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();
        if (UnityEngine.Object.FindFirstObjectByType<GameDeviceLayout>()) return;
        font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/CarGameArt/GameFont.asset");
        font.TryAddCharacters("SOLSAĞZIPLAYENİDEN<>");
        if (!canvas.GetComponent<GraphicRaycaster>()) canvas.gameObject.AddComponent<GraphicRaycaster>();
        if (!UnityEngine.Object.FindFirstObjectByType<EventSystem>()) new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        var safe = Rect("Cihaz Guvenli Alani", canvas.transform, Vector2.zero, Vector2.zero, Vector2.zero);
        safe.anchorMin = Vector2.zero; safe.anchorMax = Vector2.one; safe.offsetMin = safe.offsetMax = Vector2.zero;
        var titlePanel = GameObject.Find("Baslik Paneli").GetComponent<RectTransform>();
        var scorePanel = GameObject.Find("Skor Paneli").GetComponent<RectTransform>();
        titlePanel.SetParent(safe, false); titlePanel.anchoredPosition = new Vector2(20, -20);
        scorePanel.SetParent(safe, false); scorePanel.anchoredPosition = new Vector2(-20, -20);
        ReparentLabel("Baslik", titlePanel, new Vector2(18, -7), new Vector2(-18, -47), 20, 32);
        ReparentLabel("Alt Baslik", titlePanel, new Vector2(18, -53), new Vector2(-18, -80), 11, 16);
        ReparentLabel("Skor", scorePanel, new Vector2(14, -10), new Vector2(-14, -61), 18, 29);
        var touch = Rect("Dokunmatik Kontroller", safe, Vector2.zero, Vector2.zero, Vector2.zero);
        touch.anchorMin = Vector2.zero; touch.anchorMax = Vector2.one; touch.offsetMin = touch.offsetMax = Vector2.zero;
        TouchButton("Sola", "<", touch, new Vector2(0, 0), new Vector2(25, 22), new Vector2(120, 120), TouchControlButton.Action.Left);
        TouchButton("Saga", ">", touch, new Vector2(0, 0), new Vector2(160, 22), new Vector2(120, 120), TouchControlButton.Action.Right);
        TouchButton("Zipla", "ZIPLA", touch, new Vector2(1, 0), new Vector2(-25, 22), new Vector2(170, 120), TouchControlButton.Action.Jump);
        var restart = Rect("Dokunmatik Yeniden", touch, new Vector2(1, 1), new Vector2(-20, -110), new Vector2(170, 48));
        restart.gameObject.AddComponent<Image>().color = new Color(.15f, .085f, .065f, .9f);
        var restartButton = restart.gameObject.AddComponent<Button>();
        UnityEventTools.AddPersistentListener(restartButton.onClick, UnityEngine.Object.FindFirstObjectByType<PlayerController>().RestartRun);
        Label("Yeniden", "YENİDEN", restart, 19);
        var layout = new GameObject("Cihaz Uyarlamasi").AddComponent<GameDeviceLayout>();
        layout.touchControls = touch.gameObject; layout.safeArea = safe;
        layout.titlePanel = titlePanel; layout.scorePanel = scorePanel;
        layout.desktopHelp = GameObject.Find("Kontrol Paneli");
        layout.desktopControlsText = GameObject.Find("Kontroller");
        layout.desktopHintText = GameObject.Find("Zamanlama");
        layout.scaler = canvas.GetComponent<CanvasScaler>();
        layout.desktopControlsText.GetComponent<TextMeshProUGUI>().text = "A / D: Yürü   W / BOŞLUK: Zıpla   R: Yeniden   T: Dokunmatik";
        touch.gameObject.SetActive(false);
        EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
        EditorSceneManager.SaveScene(canvas.gameObject.scene);
        AssetDatabase.SaveAssets();
        File.WriteAllText(Marker, DateTime.Now.ToString("O"));
        EditorApplication.delayCall += DeviceVerification.Run;
    }

    static void ReparentLabel(string name, Transform parent, Vector2 topLeft, Vector2 bottomRight, int minSize, int maxSize)
    {
        var label = GameObject.Find(name).GetComponent<TextMeshProUGUI>();
        var rect = label.rectTransform; rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0, 1); rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(topLeft.x, bottomRight.y); rect.offsetMax = new Vector2(bottomRight.x, topLeft.y);
        label.enableAutoSizing = true; label.fontSizeMin = minSize; label.fontSizeMax = maxSize;
    }
    static RectTransform Rect(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false); rect.anchorMin = rect.anchorMax = anchor; rect.pivot = anchor; rect.anchoredPosition = pos; rect.sizeDelta = size; return rect;
    }
    static void TouchButton(string name, string text, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size, TouchControlButton.Action action)
    {
        var rect = Rect(name, parent, anchor, pos, size);
        rect.gameObject.AddComponent<Image>().color = new Color(.15f, .085f, .065f, .88f);
        rect.gameObject.AddComponent<TouchControlButton>().action = action;
        Label(name + " Yazi", text, rect, action == TouchControlButton.Action.Jump ? 26 : 48);
    }
    static void Label(string name, string text, Transform parent, int size)
    {
        var rect = Rect(name, parent, Vector2.zero, Vector2.zero, Vector2.zero);
        rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
        var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
        label.font = font; label.text = text; label.fontSize = size; label.alignment = TextAlignmentOptions.Center;
        label.color = new Color(.95f, .78f, .59f); label.raycastTarget = false;
    }
}
