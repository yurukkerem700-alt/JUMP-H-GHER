using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Assertions;

public static class CostumeRigVerification
{
    [MenuItem("Dogrulama/Yeni Kostum Iskeletlerini Kontrol Et")]
    public static void Run()
    {
        var scene=EditorSceneManager.NewPreviewScene();
        var shader=Shader.Find("Sprites/Default");
        var mat=new Material(shader);
        var created=new GameObject[4];
        try
        {
            Assert.AreEqual(10,Costumes.All.Length);
            for(int i=0;i<4;i++)
            {
                Assert.AreEqual(i,CostumeRig2D.Index(CostumeRig2D.Ids[i]));
                var go=new GameObject(CostumeRig2D.Ids[i]); created[i]=go;
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go,scene);
                var body=go.AddComponent<Rigidbody2D>(); body.bodyType=RigidbodyType2D.Kinematic;
                var collider=go.AddComponent<BoxCollider2D>();
                var player=go.AddComponent<PlayerController>(); player.enabled=false;
                var flags=BindingFlags.Instance|BindingFlags.NonPublic;
                typeof(PlayerController).GetField("rb",flags).SetValue(player,body);
                typeof(PlayerController).GetField("oyuncuCollider",flags).SetValue(player,collider);
                var rig=go.AddComponent<CostumeRig2D>(); rig.Init(player,mat,i);
                Assert.IsTrue(go.GetComponentsInChildren<Transform>().Length>20);
                Assert.AreEqual(1,go.GetComponentsInChildren<Collider2D>().Length);
                var update=typeof(CostumeRig2D).GetMethod("LateUpdate",flags);
                body.linearVelocity=new Vector2(2,4); update.Invoke(rig,null);
                var leg=go.transform.Find("Iskelet/Sag Kalca");
                var ascent=leg.localRotation;
                body.linearVelocity=new Vector2(2,-4); update.Invoke(rig,null);
                Assert.IsTrue(Quaternion.Angle(ascent,leg.localRotation)>10);
                var preview=CostumeRig2D.Preview(i);
                Assert.IsTrue(System.Array.Exists(preview.texture.GetPixels(),pixel=>pixel.a>.5f));
                UnityEngine.Object.DestroyImmediate(preview.texture); UnityEngine.Object.DestroyImmediate(preview);
                Debug.Log("COSTUME PASS: "+CostumeRig2D.Ids[i]+"; rig hierarchy, jump/fall poses, collider and preview");
            }
            bool hadSelection=PlayerPrefs.HasKey("Costume");
            string saved=PlayerPrefs.GetString("Costume",Costumes.Default);
            var effectObject=new GameObject("Effect Switch Verification");
            try
            {
                effectObject.AddComponent<Rigidbody2D>().bodyType=RigidbodyType2D.Kinematic;
                var effects=effectObject.AddComponent<CostumeEffects>();
                typeof(CostumeEffects).GetMethod("Start",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(effects,null);
                foreach(var entry in Costumes.All)
                {
                    Costumes.SelectedId=entry.Id;
                    Assert.AreEqual(18,effectObject.GetComponentsInChildren<SpriteRenderer>().Length);
                }
                Debug.Log("COSTUME EFFECT SWITCH PASSED: 10/10; fixed 18-particle pool");
            }
            finally
            {
                Costumes.SelectedId=saved;
                if(!hadSelection) { PlayerPrefs.DeleteKey("Costume"); PlayerPrefs.Save(); }
                UnityEngine.Object.DestroyImmediate(effectObject);
            }
            Debug.Log("COSTUME VERIFICATION PASSED: 4/4");
        }
        finally
        {
            foreach(var go in created) if(go) UnityEngine.Object.DestroyImmediate(go);
            UnityEngine.Object.DestroyImmediate(mat);
            EditorSceneManager.ClosePreviewScene(scene);
        }
    }
}
