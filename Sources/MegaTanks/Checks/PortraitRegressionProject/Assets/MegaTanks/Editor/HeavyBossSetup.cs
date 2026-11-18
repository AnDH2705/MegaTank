using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MegaTanks.Editor
{
    public static class HeavyBossSetup
    {
        public const string Folder = BossTankSetup.Folder + "/Heavy";
        public const string TemplatePath = Folder + "/HeavyBossTank.asset";
        static T Copy<T>(Object source, string name, Action<T> configure = null) where T : Object
        {
            string path = Folder + "/" + name + ".asset";
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            if (source == null || !AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(source), path))
                throw new InvalidOperationException("Cannot create heavy boss asset: " + path);
            asset = AssetDatabase.LoadAssetAtPath<T>(path);
            configure?.Invoke(asset); EditorUtility.SetDirty(asset);
            return asset;
        }
        [MenuItem("MegaTanks/Boss/Select Heavy Boss Template")]
        public static void SelectTemplate()
        {
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<BossTankProfile>(TemplatePath);
            EditorGUIUtility.PingObject(Selection.activeObject);
        }
        [MenuItem("MegaTanks/Boss/Create Heavy Boss Template")]
        public static void Install()
        {
            var heavy = AssetDatabase.LoadAssetAtPath<TankDefinition>(HeavyTankSetup.HeavyPath);
            var source = AssetDatabase.LoadAssetAtPath<BossTankProfile>(BossTankSetup.ProfilePath);
            if (heavy == null || heavy.artworkPrefab == null || source == null)
                throw new InvalidOperationException("Install the heavy player tank and first boss template before creating the heavy boss.");
            Directory.CreateDirectory(Folder); AssetDatabase.Refresh();
            if (AssetDatabase.LoadAssetAtPath<BossTankProfile>(TemplatePath) == null)
            {
                var profile = ScriptableObject.CreateInstance<BossTankProfile>();
                profile.id = "HeavyBossTank";
                profile.tank = Copy<TankDefinition>(heavy, "HeavyBossHull", hull => { hull.id = "boss.heavy.hull"; hull.displayName = "Heavy Boss Hull"; });
                profile.artworkPrefab = heavy.artworkPrefab;
                profile.equipment = Copy<WeaponLoadout>(source.equipment, "HeavyBossEquipment");
                profile.equipment.machineGun = Copy<GunDefinition>(source.equipment.machineGun, "HeavyBossMachineGun", gun => { gun.id = "boss.heavy.machine-gun"; gun.displayName = "Heavy Boss MG"; });
                profile.equipment.cannon = Copy<GunDefinition>(source.equipment.cannon, "HeavyBossCannon", gun => { gun.id = "boss.heavy.cannon"; gun.displayName = "Heavy Boss Cannon"; });
                profile.equipment.mortar = Copy<MortarDefinition>(source.equipment.mortar, "HeavyBossMortar", gun => { gun.id = "boss.heavy.mortar"; gun.displayName = "Heavy Boss Mortar"; });
                EditorUtility.SetDirty(profile.equipment);
                profile.armor = Copy<ArmorDefinition>(source.armor, "HeavyBossArmor", armor => { armor.id = "boss.heavy.armor"; armor.displayName = "Heavy Boss Armor"; });
                profile.secondArmor = Copy<ArmorDefinition>(source.armor, "HeavyBossSecondArmor", armor => { armor.id = "boss.heavy.second-armor"; armor.displayName = "Heavy Boss Second Armor"; });
                profile.crew = new BossCrewStation[source.crew.Length];
                for (int i = 0; i < source.crew.Length; i++)
                {
                    int station = i;
                    var member = Copy<CrewDefinition>(source.crew[i].member, "HeavyBossCrew" + i,
                        crew => { crew.id = "boss.heavy.crew." + station; crew.displayName = "Heavy Boss " + source.crew[station].member.displayName.Replace("Boss ", ""); });
                    profile.crew[i] = new BossCrewStation { station = source.crew[i].station, member = member };
                }
                profile.score = source.score; profile.scraps = source.scraps; profile.healthBarColor = source.healthBarColor;
                AssetDatabase.CreateAsset(profile, TemplatePath);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("HEAVY_BOSS_TEMPLATE_OK: heavy hull/artwork, independent weapons, two armor definitions and five crew; select it on a Stage Definition.");
        }
    }
}
