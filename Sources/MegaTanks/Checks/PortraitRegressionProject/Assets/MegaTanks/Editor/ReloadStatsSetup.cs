using System.Linq;
using UnityEditor;
using UnityEngine;

namespace MegaTanks.Editor
{
    public static class ReloadStatsSetup
    {
        [MenuItem("MegaTanks/Configure Weapon Reload Stats")]
        public static void Install()
        {
            // New fields initialize to 100; serialize that default without changing existing base times or tuned speeds.
            var types=new[]{"t:GunDefinition","t:MortarDefinition","t:EnemyDataManager"};int count=0;
            foreach(var guid in types.SelectMany(type=>AssetDatabase.FindAssets(type,new[]{"Assets/MegaTanks"})).Distinct())
            {
                var asset=AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GUIDToAssetPath(guid));
                EditorUtility.SetDirty(asset);count++;
            }
            foreach(var guid in AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/MegaTanks/Prefabs/Enemies"}))
            {
                string path=AssetDatabase.GUIDToAssetPath(guid);var asset=AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if(asset.GetComponent<EnemyStatsProfile>()==null)continue;
                var root=PrefabUtility.LoadPrefabContents(path);
                try{PrefabUtility.SaveAsPrefabAsset(root,path);count++;}
                finally{PrefabUtility.UnloadPrefabContents(root);}
            }
            AssetDatabase.SaveAssets();
            Debug.Log("RELOAD_STATS_SETUP_OK: Base Reload Time labels, default Reload Speed 100%, preserved base values and "+count+" weapon/database/prefab assets saved.");
        }
    }
}
