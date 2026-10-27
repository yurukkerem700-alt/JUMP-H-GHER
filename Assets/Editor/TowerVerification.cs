using System;
using System.IO;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class TowerVerification
{
    static InfiniteTower tower;
    static PlayerController player;
    static CoinSequence coins;
    static Rigidbody2D body;
    static float start, phaseTime;
    static int phase, advances, count;
    static string results;
    static Vector3 coinPosition;

    static TowerVerification()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool("TowerVerify", false)) return;
            Application.runInBackground = true;
            PlayerController.VerificationControl = true; PlayerController.VerificationMovement = 0;
            phase = advances = count = 0; results = ""; start = phaseTime = Time.realtimeSinceStartup;
            EditorApplication.update -= Tick; EditorApplication.update += Tick;
        };
    }
    [MenuItem("Kok Yankisi/Yukari Ilerlemeyi Test Et")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        SessionState.SetBool("TowerVerify", true); EditorApplication.isPlaying = true;
    }
    static void Tick()
    {
        try
        {
            if (!EditorApplication.isPlaying || EditorApplication.isCompiling) return;
            float now = Time.realtimeSinceStartup;
            if (now - start > 50) throw new Exception("Tower verification timed out at " + phase);
            if (phase == 0)
            {
                if (Time.timeSinceLevelLoad < .5f) return;
                tower = UnityEngine.Object.FindFirstObjectByType<InfiniteTower>();
                player = tower.player; coins = tower.coins; body = player.GetComponent<Rigidbody2D>();
                if (coins.SpawnMode == 0 || coins.coin.position.y < -1.65f) throw new Exception("A ground coin was created.");
                coinPosition = coins.coin.position;
                coins.coin.GetComponent<Collider2D>().enabled = false;
                var top = tower.GetPlatform(6);
                body.position = new Vector2(top.transform.position.x + top.CurrentVelocity.x * .27f, top.Top + 1);
                body.linearVelocity = Vector2.zero; Physics2D.SyncTransforms();
                phase = 1; phaseTime = now;
            }
            else if (phase == 1)
            {
                if (tower.HighestGeneratedFloor < 13 || Camera.main.transform.position.y < 1) return;
                if (!tower.GetPlatform(7) || !tower.GetPlatform(13)) throw new Exception("Next section missing.");
                if (Mathf.Abs(tower.GetPlatform(7).Top - tower.GetPlatform(6).Top - 1.15f) > .02f) throw new Exception("Unreachable section handoff gap.");
                if (coins.ActiveIndex != 0 || !coins.coin.gameObject.activeSelf) throw new Exception("Pending coin was replaced on scroll.");
                results += "PASS: landing on last moving platform generates the next section and scrolls the camera; handoff gap is 1.15 units.\n";
                phase = 2; phaseTime = now;
            }
            else if (phase == 2)
            {
                if (now - phaseTime < .12f) return;
                int floor = tower.HighestGeneratedFloor;
                var frontier = tower.GetPlatform(floor);
                if (!frontier) throw new Exception("Frontier was lost.");
                body.position = new Vector2(frontier.transform.position.x, frontier.Top + 1);
                body.linearVelocity = Vector2.zero;
                tower.AdvanceFromFloor(floor);
                advances++; phaseTime = now;
                if (tower.ActivePlatformCount > 42) throw new Exception("Platform pool is growing without bounds.");
                if (coins.ActiveIndex != 0 || !tower.GetPlatform(coins.SpawnFloor)) throw new Exception("Uncollected coin's section was recycled.");
                if (advances < 24) return;
                if (tower.HighestGeneratedFloor < 170) throw new Exception("Tower did not extend far enough.");
                results += "PASS: 24 further section advances, over 170 floors, bounded active platforms and preserved pending coin section.\n";
                player.RestartRun();
                coins.coin.GetComponent<Collider2D>().enabled = true;
                if (tower.HighestGeneratedFloor != 6 || Camera.main.transform.position.y != .3f) throw new Exception("Restart did not reset tower.");
                phase = 3; phaseTime = now;
            }
            else if (phase == 3)
            {
                if (now - phaseTime < .2f) return;
                if (coins.SpawnMode == 0) throw new Exception("Ground spawn reappeared.");
                var platform = coins.steps[coins.TargetStep >= 0 ? coins.TargetStep : 0];
                if (coins.coin.position.y < InfiniteTower.FirstSurface + .6f) throw new Exception("Coin below platform range.");
                body.position = coins.coin.position; body.linearVelocity = Vector2.zero; Physics2D.SyncTransforms();
                phase = 4; phaseTime = now;
            }
            else if (phase == 4)
            {
                if (coins.ActiveIndex <= count) return;
                count++;
                if (player.Score != count || GameObject.FindGameObjectsWithTag("altin").Length != 1) throw new Exception("Single coin progression broken.");
                if (count < 14) { phase = 3; phaseTime = now; return; }
                results += "PASS: 14 real coin collections, all above platforms, one pending coin, no seven-coin cap; restart restores the first section.\n";
                Finish(true);
            }
        }
        catch (Exception e) { results += "FAIL: " + e + "\n"; Finish(false); }
    }
    static void Finish(bool pass)
    {
        File.WriteAllText("Library/TowerVerification.txt", results);
        SessionState.SetBool("TowerVerify", false);
        PlayerController.VerificationControl = false; PlayerController.VerificationMovement = 0;
        EditorApplication.update -= Tick; EditorApplication.isPlaying = false;
        if (pass) Debug.Log("Infinite tower verification passed."); else Debug.LogError(results);
    }
}
