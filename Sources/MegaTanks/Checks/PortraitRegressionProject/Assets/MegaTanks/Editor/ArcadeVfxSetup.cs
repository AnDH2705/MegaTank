using System;
using MegaTankVFX;
using UnityEditor;
using UnityEngine;

namespace MegaTanks.Editor
{
    public static class ArcadeVfxSetup
    {
        public const string Prefabs = "Assets/MegaTankVFX/Generated/Prefabs/";
        [MenuItem("MegaTanks/Use Arcade VFX")]
        public static void Apply()
        {
            if(!EditorApplication.ExecuteMenuItem("MegaTank VFX/1. Build prefabs"))
                throw new InvalidOperationException("MegaTank VFX Build prefabs menu is missing.");
            var catalog=AssetDatabase.LoadAssetAtPath<BattleVfxCatalog>(BattleVfxImporter.CatalogPath);
            if(catalog==null)throw new InvalidOperationException("Battle VFX catalog is missing.");
            // Migrate existing generated FireBolt assets without recreating artist edits.
            string path=Prefabs+"FireBolt.prefab";
            var root=PrefabUtility.LoadPrefabContents(path);
            try
            {
                foreach(var motion in root.GetComponentsInChildren<FireBoltDemoMotion>(true))UnityEngine.Object.DestroyImmediate(motion);
                PrefabUtility.SaveAsPrefabAsset(root,path);
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
            foreach(string prefabPath in System.IO.Directory.GetFiles(Prefabs,"*.prefab"))
            {
                var effect=PrefabUtility.LoadPrefabContents(prefabPath);
                try
                {
                    // Unity requires x/y/z velocity curves to use the same mode.
                    foreach(var ps in effect.GetComponentsInChildren<ParticleSystem>(true))
                    {
                        var velocity=ps.velocityOverLifetime;
                        if(velocity.enabled&&velocity.x.mode==ParticleSystemCurveMode.TwoConstants&&velocity.y.mode==ParticleSystemCurveMode.TwoConstants)
                            velocity.z=new ParticleSystem.MinMaxCurve(0f,0f);
                    }
                    PrefabUtility.SaveAsPrefabAsset(effect,prefabPath);
                }
                finally{PrefabUtility.UnloadPrefabContents(effect);}
            }
            catalog.muzzlePrefab=Load("MuzzleFlash");catalog.hitPrefab=Load("HitSpark");
            catalog.explosionSmallPrefab=Load("ExplosionSmall");catalog.explosionBigPrefab=Load("ExplosionBig");
            catalog.fireBoltPrefab=Load("FireBolt");
            EditorUtility.SetDirty(catalog);AssetDatabase.SaveAssets();
            Debug.Log("ARCADE_VFX_SETUP_OK: menu built eight prefabs; battle catalog bound; FireBolt has no demo motion.");
        }
        static GameObject Load(string name)
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs+name+".prefab");
            if(!prefab)throw new InvalidOperationException("Missing arcade prefab: "+name);
            return prefab;
        }
    }
}
