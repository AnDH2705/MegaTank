using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MegaTanks.Editor
{
    public static class BossTankSetup
    {
        public const string Folder = "Assets/MegaTanks/Data/Boss";
        public const string ProfilePath = Folder + "/BossTank.asset";
        static T Copy<T>(string source, string name) where T : Object
        {
            string target = Folder + "/" + name + ".asset";
            if (AssetDatabase.LoadAssetAtPath<T>(target) == null && !AssetDatabase.CopyAsset(source, target))
                throw new System.InvalidOperationException("Could not copy " + source);
            return AssetDatabase.LoadAssetAtPath<T>(target);
        }
        [MenuItem("MegaTanks/Boss/Select Boss Template")]
        public static void SelectProfile() { Selection.activeObject = AssetDatabase.LoadAssetAtPath<BossTankProfile>(ProfilePath); EditorGUIUtility.PingObject(Selection.activeObject); }

        [MenuItem("MegaTanks/Boss/Install Boss Tank")]
        public static void Install()
        {
            Directory.CreateDirectory(Folder); AssetDatabase.Refresh();
            var profile = AssetDatabase.LoadAssetAtPath<BossTankProfile>(ProfilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<BossTankProfile>();
                profile.tank = Copy<TankDefinition>("Assets/MegaTanks/Data/Tanks/PlayerTank.asset", "BossHull");
                profile.tank.stats.maxHull = 300; EditorUtility.SetDirty(profile.tank);
                profile.artworkPrefab = AssetDatabase.LoadAssetAtPath<TankArtwork>("Assets/MegaTanks/Prefabs/TankCutaway.prefab");
                var loadout = new BossEquipmentSlots();
                loadout.machineGun = Copy<GunDefinition>("Assets/MegaTanks/Data/Weapons/MachineGun.asset", "BossMachineGun");
                loadout.cannon = Copy<GunDefinition>("Assets/MegaTanks/Data/Weapons/Cannon.asset", "BossCannon");
                loadout.mortar = Copy<MortarDefinition>("Assets/MegaTanks/Data/Weapons/Mortar.asset", "BossMortar");
                // Starter tuning; future installs preserve all designer edits.
                loadout.machineGun.id = "boss-machine-gun"; loadout.machineGun.stats.damage = 3;
                loadout.cannon.id = "boss-cannon"; loadout.cannon.stats.damage = 12;
                loadout.mortar.id = "boss-mortar"; loadout.mortar.stats.damage = 20;
                foreach (var weapon in new WeaponDefinition[] { loadout.machineGun, loadout.cannon, loadout.mortar }) EditorUtility.SetDirty(weapon);
                profile.equipment = loadout;
                profile.armor = Copy<ArmorDefinition>("Assets/MegaTanks/Data/Armor/BasicArmor.asset", "BossArmor");
                profile.armor.id = "boss-armor"; EditorUtility.SetDirty(profile.armor);
                var catalog = AssetDatabase.LoadAssetAtPath<CrewCatalog>(CrewSetup.CatalogPath);
                profile.crew = new BossCrewStation[catalog.stations.Length];
                for (int i = 0; i < profile.crew.Length; i++)
                {
                    var member = Copy<CrewDefinition>(AssetDatabase.GetAssetPath(catalog.members[i]), "BossCrew" + i);
                    member.id = "boss-crew-" + i; member.displayName = "Boss " + catalog.members[i].displayName;
                    member.baseDamage = 0; EditorUtility.SetDirty(member);
                    profile.crew[i] = new BossCrewStation { station = catalog.stations[i], member = member };
                }
                AssetDatabase.CreateAsset(profile, ProfilePath);
            }
            SharedBossSlotsSetup.Configure(profile);
            // Existing stages keep their chosen templates and progression on repeat installation.
            foreach (string guid in AssetDatabase.FindAssets("t:StageDefinition"))
            {
                var stage = AssetDatabase.LoadAssetAtPath<StageDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                if (stage.boss == null) stage.boss = new BossEncounterSettings();
                if (stage.boss.template == null) { stage.boss.template = profile; EditorUtility.SetDirty(stage); }
            }
            EditorUtility.SetDirty(profile); // Remove legacy template progression/difficulty fields when saving.
            var scene = EditorSceneManager.OpenScene(MegaTanksSetup.ScenePath);
            EditorSceneManager.SaveScene(scene); // Remove the old scene-wide boss reference.
            AssetDatabase.SaveAssets();
            Debug.Log("BOSS_TANK_INSTALLED: reusable template and stage-owned boss progression, independent of difficulty.");
        }
    }
}
