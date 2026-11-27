using UnityEditor;
using UnityEngine;
namespace MegaTanks.Editor
{
    public static class CombatHudSetup
    {
        [MenuItem("MegaTanks/UI/Rebuild Combat HUD")]
        public static void Apply()
        {
            const string path="Assets/MegaTanks/Prefabs/UI/BattleHUD.prefab";
            var root=PrefabUtility.LoadPrefabContents(path);
            try
            {
                var art=root.GetComponent<CombatHudPresentation>()??root.AddComponent<CombatHudPresentation>();
                art.cannonAmmo=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/MegaTanks/Art/Projectiles/CannonShell.png");
                art.mgAmmo=art.cannonAmmo;
                art.mortarAmmo=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/MegaTanks/Art/Projectiles/mortar_ammo.png");
                art.partsIcon=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/MegaTanks/Art/Currencies/Parts.png");
                PrefabUtility.SaveAsPrefabAsset(root,path);
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
            AssetDatabase.SaveAssets();Debug.Log("COMBAT_HUD_SETUP_PASS");
        }
    }
}
