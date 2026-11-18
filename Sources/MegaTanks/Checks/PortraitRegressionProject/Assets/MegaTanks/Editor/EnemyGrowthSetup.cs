using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
namespace MegaTanks.Editor
{
    public static class EnemyGrowthSetup
    {
        [MenuItem("MegaTanks/Configure Enemy Growth Profiles")]
        public static void Configure()
        {
            var database=AssetDatabase.LoadAssetAtPath<EnemyDataManager>(EnemyDataSetup.AssetPath);
            var definitions=database.Definitions.ToArray();var profiles=new EnemyStatsProfile[definitions.Length];
            for(int i=0;i<definitions.Length;i++)
            {
                var definition=definitions[i];string path=AssetDatabase.GetAssetPath(definition.artworkPrefab);
                if(string.IsNullOrEmpty(path))throw new InvalidOperationException("Assign an enemy prefab for "+definition.id);
                var root=PrefabUtility.LoadPrefabContents(path);
                try
                {
                    var profile=root.GetComponent<EnemyStatsProfile>();
                    // Existing profiles are the source of truth; re-running setup preserves all tuning.
                    if(profile==null){profile=root.AddComponent<EnemyStatsProfile>();profile.stats=definition.Copy();profile.stats.artworkPrefab=root.GetComponent<EnemyVehicleArtwork>();}
                    PrefabUtility.SaveAsPrefabAsset(root,path);
                }
                finally{PrefabUtility.UnloadPrefabContents(root);}
                profiles[i]=AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponent<EnemyStatsProfile>();
            }
            database.enemyPrefabs=profiles;EditorUtility.SetDirty(database);AssetDatabase.SaveAssets();
            AssetDatabase.ForceReserializeAssets(AssetDatabase.FindAssets("t:StageDefinition").Select(AssetDatabase.GUIDToAssetPath));
            Debug.Log("ENEMY_GROWTH_CONFIGURED: existing base stats preserved; zero-default growth profiles attached to enemy prefabs.");
        }
    }
}
