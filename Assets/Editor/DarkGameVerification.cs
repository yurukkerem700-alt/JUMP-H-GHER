using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class DarkGameVerification
{
    static PlayerController player;
    static CoinSequence sequence;
    static Rigidbody2D body;
    static MethodInfo jump;
    static int phase, count;
    static float started, phaseTime, carryOffset;
    static Vector3 previousCoin;
    static string results;
    static bool groundSeen, attachedSeen, airSeen;

    static DarkGameVerification()
    {
        EditorApplication.playModeStateChanged += s =>
        {
            if (s != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool("EchoVerify", false)) return;
            phase = count = 0; results = ""; started = phaseTime = Time.realtimeSinceStartup;
            groundSeen = attachedSeen = airSeen = false;
            Application.runInBackground = true;
            PlayerController.VerificationControl = true;
            PlayerController.VerificationMovement = 0;
            EditorApplication.update -= Tick; EditorApplication.update += Tick;
        };
    }

    [MenuItem("Karanlik Gecit/Oyunu Test Et")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        SessionState.SetBool("EchoVerify", true);
        EditorApplication.isPlaying = true;
    }

    static void Tick()
    {
        try
        {
            if (!EditorApplication.isPlaying || EditorApplication.isCompiling) return;
            float now = Time.realtimeSinceStartup;
            if (now - started > 50) throw new Exception("Test timeout at phase " + phase);
            if (phase == 0)
            {
                if (Time.timeSinceLevelLoad < .5f) return;
                player = UnityEngine.Object.FindFirstObjectByType<PlayerController>();
                sequence = UnityEngine.Object.FindFirstObjectByType<CoinSequence>();
                body = player.GetComponent<Rigidbody2D>();
                jump = typeof(PlayerController).GetMethod("ZiplamayiDene", BindingFlags.Instance | BindingFlags.NonPublic);
                if (GameObject.Find("Merkez Isareti")) throw new Exception("Center guide still exists.");
                if (sequence.winPanel || player.Score != 0 || sequence.ActiveIndex != 0) throw new Exception("Old completion state remains.");
                if (GameObject.FindGameObjectsWithTag("altin").Length != 1) throw new Exception("Expected one active coin.");
                previousCoin = sequence.coin.position;
                phase = 1; phaseTime = now;
            }
            else if (phase == 1)
            {
                if (now - phaseTime < .6f) return;
                if (sequence.ActiveIndex != 0 || Mathf.Abs(sequence.coin.position.x - previousCoin.x) > .01f) throw new Exception("Coin moved to a new spawn without collection.");
                results += "PASS: no center guide or victory state; one coin waits for collection.\n";
                sequence.coin.GetComponent<Collider2D>().enabled = false;
                var target = sequence.steps[0];
                body.position = new Vector2(target.transform.position.x + target.CurrentVelocity.x * .27f, target.Top + 1);
                body.linearVelocity = Vector2.zero; Physics2D.SyncTransforms();
                phase = 2; phaseTime = now;
            }
            else if (phase == 2)
            {
                var support = player.GroundPlatform(out bool grounded);
                if (!grounded || support != sequence.steps[0]) return;
                carryOffset = body.position.x - support.transform.position.x;
                phase = 3; phaseTime = now;
            }
            else if (phase == 3)
            {
                if (now - phaseTime < .35f) return;
                var support = player.GroundPlatform(out bool grounded);
                if (!grounded || support != sequence.steps[0] || Mathf.Abs(body.position.x - support.transform.position.x - carryOffset) > .25f) throw new Exception("Moving platform carry failed.");
                if (Mathf.Abs(support.transform.position.x) < .8f) return;
                jump.Invoke(player, null);
                if (body.linearVelocity.y < 7.9f) throw new Exception("Jump away from center was blocked.");
                phase = 4; phaseTime = now;
            }
            else if (phase == 4)
            {
                if (now - phaseTime < .18f) return;
                float velocity = body.linearVelocity.y;
                jump.Invoke(player, null);
                if (Mathf.Abs(body.linearVelocity.y - velocity) > .01f) throw new Exception("Air jump was allowed.");
                results += "PASS: moving platform carry and jumping away from center; air jump stays blocked.\n";
                sequence.coin.GetComponent<Collider2D>().enabled = true;
                player.RestartRun();
                count = 0; phase = 5; phaseTime = now;
            }
            else if (phase == 5)
            {
                if (now - phaseTime < .2f) return;
                if (GameObject.FindGameObjectsWithTag("altin").Length != 1 || !sequence.coin.gameObject.activeSelf) throw new Exception("Coin population changed.");
                var pos = sequence.coin.position;
                if (sequence.SpawnMode == 0) { groundSeen = true; if (pos.y > -2.8f || Mathf.Abs(pos.x) > 7.4f) throw new Exception("Invalid ground spawn."); }
                else if (sequence.SpawnMode == 1) { attachedSeen = true; if (Mathf.Abs(pos.x - sequence.steps[sequence.TargetStep].transform.position.x) > .7f) throw new Exception("Moving coin does not follow platform."); }
                else { airSeen = true; if (pos.y < -1.8f || pos.y > 5.5f || Mathf.Abs(pos.x) > 2.7f) throw new Exception("Invalid air spawn."); }
                body.position = pos;
                body.linearVelocity = Vector2.zero; Physics2D.SyncTransforms();
                phase = 6; phaseTime = now;
            }
            else if (phase == 6)
            {
                if (sequence.ActiveIndex <= count) return;
                count++;
                if (sequence.ActiveIndex != count || player.Score != count) throw new Exception("Score increment or spawn sequence mismatch.");
                if (count < 20) { phase = 5; phaseTime = now; return; }
                if (!groundSeen || !attachedSeen || !airSeen || !sequence.coin.gameObject.activeSelf || player.skorYazisi.text.Contains("/")) throw new Exception("Endless or spawn diversity check failed.");
                results += "PASS: 20 real trigger collections; ground, moving-platform and static-air spawns; no seven-coin limit.\n";
                player.RestartRun();
                if (player.Score != 0 || sequence.ActiveIndex != 0 || !sequence.coin.gameObject.activeSelf) throw new Exception("Restart failed.");
                results += "PASS: reset starts a new unrestricted coin run.\n";
                Finish(true);
            }
        }
        catch (Exception e) { results += "FAIL: " + e + "\n"; Finish(false); }
    }

    static void Finish(bool pass)
    {
        File.WriteAllText("Library/EchoGameVerification.txt", results);
        SessionState.SetBool("EchoVerify", false);
        PlayerController.VerificationControl = false; PlayerController.VerificationMovement = 0;
        EditorApplication.update -= Tick; EditorApplication.isPlaying = false;
        if (pass) Debug.Log("Kok Yankisi verification passed."); else Debug.LogError(results);
    }
}
