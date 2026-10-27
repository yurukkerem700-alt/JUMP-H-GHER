using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.EventSystems;

[InitializeOnLoad]
public static class DeviceVerification
{
    static float started;
    static DeviceVerification()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool("DeviceVerify", false)) return;
            started = Time.realtimeSinceStartup;
            Application.runInBackground = true;
            EditorApplication.update -= Tick; EditorApplication.update += Tick;
        };
    }
    [MenuItem("Cihazlar/Giris ve Ekranlari Test Et")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        SessionState.SetBool("DeviceVerify", true); EditorApplication.isPlaying = true;
    }
    static void Tick()
    {
        if (!EditorApplication.isPlaying || Time.realtimeSinceStartup - started < .7f) return;
        string report = "";
        try
        {
            PlayerController.VerificationControl = false;
            var layout = UnityEngine.Object.FindFirstObjectByType<GameDeviceLayout>();
            var player = UnityEngine.Object.FindFirstObjectByType<PlayerController>();
            var sequence = UnityEngine.Object.FindFirstObjectByType<CoinSequence>();
            layout.SetTouchPreview(true);
            var buttons = UnityEngine.Object.FindObjectsByType<TouchControlButton>(FindObjectsSortMode.None);
            var left = Array.Find(buttons, b => b.action == TouchControlButton.Action.Left);
            var right = Array.Find(buttons, b => b.action == TouchControlButton.Action.Right);
            var jump = Array.Find(buttons, b => b.action == TouchControlButton.Action.Jump);
            if (!left || !right || !jump) throw new Exception("Touch controls missing.");
            var a = new PointerEventData(EventSystem.current) { pointerId = 31 };
            var b = new PointerEventData(EventSystem.current) { pointerId = 32 };
            left.OnPointerDown(a); if (GameInput.Horizontal != -1) throw new Exception("Touch left failed.");
            right.OnPointerDown(b); if (GameInput.Horizontal != 0) throw new Exception("Simultaneous input balance failed.");
            left.OnPointerUp(a); if (GameInput.Horizontal != 1) throw new Exception("Independent touch release failed.");
            right.OnPointerUp(b); if (GameInput.Horizontal != 0) throw new Exception("Touch release stuck.");
            report += "PASS: left/right multitouch state, simultaneous contacts and independent release.\n";
            player.RestartRun();
            player.GroundPlatform(out bool grounded);
            if (!grounded) throw new Exception("Player is not grounded for touch jump test.");
            jump.OnPointerDown(a);
            typeof(PlayerController).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(player, null);
            if (player.GetComponent<Rigidbody2D>().linearVelocity.y < 7.9f) throw new Exception("Touch jump did not reach player physics.");
            if (GameInput.ConsumeJump()) throw new Exception("Held jump repeats without a new press.");
            jump.OnPointerUp(a);
            GameInput.Clear();
            if (GameInput.Horizontal != 0) throw new Exception("Input clear failed.");
            report += "PASS: touch jump reaches Rigidbody2D; no automatic repeated jump; input clear works.\n";
            foreach (var size in new[] { new Vector2Int(720, 1280), new Vector2Int(1280, 720), new Vector2Int(1536, 2048), new Vector2Int(2560, 1080) })
            {
                var safe = new Rect(0, size.y * .025f, size.x, size.y * .95f);
                layout.ApplyLayout(size.x, size.y, safe);
                sequence.ResetSequence();
                if (Mathf.Abs(sequence.coin.position.x) > GameDeviceLayout.HorizontalLimit - .45f) throw new Exception("Coin outside narrow-screen playable bounds.");
                foreach (var platform in sequence.steps)
                    if (platform.amplitude + 1.1f > GameDeviceLayout.HorizontalLimit + .01f) throw new Exception("Platform outside screen.");
                report += "PASS: layout profile " + size.x + "x" + size.y + ", safe area, platform range and ground coin bounds.\n";
            }
            layout.SetTouchPreview(false); player.RestartRun();
            report += "BUILD MODULES: Windows=" + BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64)
                + ", Android=" + BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android)
                + ", iOS=" + BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.iOS, BuildTarget.iOS) + ".\n";
            report += "Physical Android/iOS device testing and mobile builds have not been performed.\n";
        }
        catch (Exception e) { report += "FAIL: " + e + "\n"; }
        File.WriteAllText("Library/DeviceVerification.txt", report);
        GameInput.Clear(); SessionState.SetBool("DeviceVerify", false);
        EditorApplication.update -= Tick; EditorApplication.isPlaying = false;
        if (report.Contains("FAIL:")) Debug.LogError(report); else Debug.Log("Device input and layout verification passed.");
    }
}
