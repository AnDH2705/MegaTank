using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace MegaTanks.Editor
{
    public static class CombatStatsSetup
    {
        [MenuItem("MegaTanks/Persist Combat Stat Properties")]
        public static void ImportAndValidate()
        {
            var types=new[]{"TankDefinition","GunDefinition","MortarDefinition","EnemyDataManager"};
            var paths=types.SelectMany(type=>AssetDatabase.FindAssets("t:"+type,new[]{"Assets/MegaTanks/Data"}))
                .Select(AssetDatabase.GUIDToAssetPath).Distinct().ToArray();
            var before=paths.ToDictionary(path=>path,path=>JsonUtility.ToJson(AssetDatabase.LoadMainAssetAtPath(path)));
            // Reserialize adds missing fields using their zero defaults; no stat values are assigned here.
            AssetDatabase.ForceReserializeAssets(paths,ForceReserializeAssetsOptions.ReserializeAssets);
            foreach(var path in paths)
                if(JsonUtility.ToJson(AssetDatabase.LoadMainAssetAtPath(path))!=before[path])
                    throw new InvalidOperationException("Existing stat values changed while persisting properties: "+path);
            CombatPropertiesAudit.Validate();
            Debug.Log("MEGATANKS_COMBAT_STATS_PERSISTED: "+paths.Length+" profiles; existing values preserved, new fields serialized with zero defaults, Normal damage type is zero.");
        }
    }
}
