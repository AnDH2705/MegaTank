using System;
using System.IO;
using System.Linq;
using Spine.Unity;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;
namespace MegaTanks.Editor
{
    public static class BossCommandoArtImporter
    {
        public const string Folder="Assets/MegaTanks/Art/Commando/Enemy/";
        public const string PrefabPath="Assets/MegaTanks/Prefabs/EnemyBossCommando.prefab";
        [MenuItem("MegaTanks/Crew/Import Enemy Commando")]
        public static void Install()
        {
            AssetDatabase.Refresh();
            var textureImporter=(TextureImporter)AssetImporter.GetAtPath(Folder+"dog-enemy-v2.png");
            textureImporter.textureType=TextureImporterType.Default;textureImporter.alphaIsTransparency=false;
            textureImporter.mipmapEnabled=false;textureImporter.npotScale=TextureImporterNPOTScale.None;
            textureImporter.textureCompression=TextureImporterCompression.Uncompressed;textureImporter.maxTextureSize=2048;
            textureImporter.wrapMode=TextureWrapMode.Clamp;textureImporter.SaveAndReimport();
            var skeleton=AssetDatabase.LoadAssetAtPath<SkeletonDataAsset>(Folder+"dog-enemy-v2_SkeletonData.asset");
            if(skeleton==null)throw new InvalidOperationException("Spine did not import the enemy SkeletonDataAsset.");
            foreach(var atlas in skeleton.atlasAssets)
                foreach(var material in atlas.Materials)
                {
                    // The supplied atlas is premultiplied alpha (pma:true).
                    material.shader=Shader.Find("Spine/Skeleton");material.DisableKeyword("_STRAIGHT_ALPHA_INPUT");material.SetFloat("_StraightAlphaInput",0);EditorUtility.SetDirty(material);
                }
            skeleton.scale=.01f;skeleton.Clear();EditorUtility.SetDirty(skeleton);
            var data=skeleton.GetSkeletonData(false);
            if(data==null)throw new InvalidOperationException("Enemy Spine 4.2 skeleton failed to load.");
            foreach(string name in new[]{"idle","walk","melee_attack","death"})
                if(data.FindAnimation(name)==null)throw new InvalidOperationException("Missing enemy animation: "+name);
            var root=new GameObject("Enemy Commando");
            try
            {
                var visual=SkeletonAnimation.NewSkeletonAnimationGameObject(skeleton);visual.name="Enemy Spine";visual.transform.SetParent(root.transform,false);
                var artwork=root.AddComponent<CommandoArtwork>();artwork.Configure(visual,"melee_attack");
                var renderer=visual.GetComponent<MeshRenderer>();renderer.sortingOrder=35;
                var bounds=renderer.bounds;
                if(bounds.size.y<=.001f)throw new InvalidOperationException("Enemy mesh has no visible bounds.");
                float scale=1.35f/bounds.size.y;visual.transform.localScale=Vector3.one*scale;
                visual.transform.localPosition=new Vector3(-bounds.center.x*scale,-.55f-bounds.min.y*scale,0);
                PrefabUtility.SaveAsPrefabAsset(root,PrefabPath);
            }
            finally{Object.DestroyImmediate(root);}
            var prefab=AssetDatabase.LoadAssetAtPath<CommandoArtwork>(PrefabPath);
            foreach(string guid in AssetDatabase.FindAssets("t:CrewDefinition"))
            {
                var member=AssetDatabase.LoadAssetAtPath<CrewDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                if(member.role!=CrewRole.Commando||!AssetDatabase.GetAssetPath(member).StartsWith("Assets/MegaTanks/Data/Boss/",StringComparison.Ordinal))continue;
                member.commandoPrefab=prefab;EditorUtility.SetDirty(member);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("BOSS_COMMANDO_ENEMY_IMPORTED: original Spine 4.2.43 atlas/texture/skeleton; idle, walk, melee_attack, death; grounded 1.35-unit prefab assigned to commando crew; no stat values changed.");
        }
        public static void ValidateAndCapture()
        {
            var tuning=AssetDatabase.FindAssets("t:CrewDefinition").Select(guid=>AssetDatabase.LoadAssetAtPath<CrewDefinition>(AssetDatabase.GUIDToAssetPath(guid))).ToDictionary(member=>member,member=>JsonUtility.ToJson(member.stats));
            Install();
            var prefab=AssetDatabase.LoadAssetAtPath<CommandoArtwork>(PrefabPath);
            var scene=UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
            var root=(GameObject)PrefabUtility.InstantiatePrefab(prefab.gameObject,scene);
            var cameraObject=new GameObject("Enemy Preview Camera");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(cameraObject,scene);
            var camera=cameraObject.AddComponent<Camera>();camera.scene=scene;camera.orthographic=true;camera.orthographicSize=1.15f;
            camera.transform.position=new Vector3(0,.12f,-10);camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.09f,.13f,.18f);
            var target=RenderTexture.GetTemporary(720,720,24);var previous=RenderTexture.active;
            try
            {
                camera.targetTexture=target;Directory.CreateDirectory("Checks/BossCommandoEnemy");var art=root.GetComponent<CommandoArtwork>();
                foreach(string state in new[]{"idle","walk","melee_attack","death"})
                {
                    art.ResetVisual();art.Animation.AnimationState.SetAnimation(0,state,state=="idle"||state=="walk");art.Advance(state=="death"?.5f:.25f);
                    camera.Render();RenderTexture.active=target;
                    var image=new Texture2D(720,720,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,720,720),0,0);image.Apply();
                    File.WriteAllBytes("Checks/BossCommandoEnemy/"+state+".png",image.EncodeToPNG());Object.DestroyImmediate(image);
                }
                foreach(var pair in tuning)
                    if(JsonUtility.ToJson(pair.Key.stats)!=pair.Value)throw new Exception("Import changed authored crew stats: "+pair.Key.name);
                Debug.Log("BOSS_COMMANDO_ENEMY_ART_PASS: original imported skeleton renders all four animations; commando prefab assigned; all authored crew stats and gains preserved.");
            }
            finally
            {
                camera.targetTexture=null;RenderTexture.active=previous;RenderTexture.ReleaseTemporary(target);
                UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);
            }
            EditorApplication.Exit(0);
        }
    }
}
