using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace MegaTanks.Editor
{
    public static class SharedBossSlotsSetup
    {
        // Only migrate missing or old boss-only references; preserve subsequent drag/drop choices.
        static bool Legacy(UnityEngine.Object item) => item==null||AssetDatabase.GetAssetPath(item).StartsWith(BossTankSetup.Folder+"/",StringComparison.Ordinal);
        public static void Configure(BossTankProfile profile)
        {
            if(profile.sharedSlotVersion>=1)return;
            var defaults=AssetDatabase.LoadAssetAtPath<WeaponLoadout>("Assets/MegaTanks/Data/Weapons/StartingLoadout.asset");
            var catalog=AssetDatabase.LoadAssetAtPath<CrewCatalog>(CrewSetup.CatalogPath);
            if(profile.equipment==null)profile.equipment=new BossEquipmentSlots();
            if(Legacy(profile.equipment.machineGun))profile.equipment.machineGun=defaults.machineGun;
            if(Legacy(profile.equipment.cannon))profile.equipment.cannon=defaults.cannon;
            if(Legacy(profile.equipment.mortar))profile.equipment.mortar=defaults.mortar;
            if(Legacy(profile.armor))profile.armor=AssetDatabase.LoadAssetAtPath<ArmorDefinition>("Assets/MegaTanks/Data/Armor/BasicArmor.asset");
            if(profile.tank.ArmorSlots>1&&Legacy(profile.secondArmor))
                profile.secondArmor=AssetDatabase.LoadAssetAtPath<ArmorDefinition>("Assets/MegaTanks/Data/Armor/LightArmor.asset");
            if(profile.crew==null||profile.crew.Length!=5)profile.crew=new BossCrewStation[5];
            for(int i=0;i<5;i++)
            {
                var role=(CrewRole)i;
                if(profile.crew[i]==null)profile.crew[i]=new BossCrewStation();
                profile.crew[i].station=CrewDefinition.RoleLabel(role);
                if(Legacy(profile.crew[i].member))profile.crew[i].member=catalog.members.FirstOrDefault(member=>member!=null&&member.role==role);
            }
            profile.sharedSlotVersion=1;EditorUtility.SetDirty(profile);
        }
        [MenuItem("MegaTanks/Boss/Migrate Shared Equipment Slots")]
        public static void Run()
        {
            foreach(var guid in AssetDatabase.FindAssets("t:BossTankProfile"))
                Configure(AssetDatabase.LoadAssetAtPath<BossTankProfile>(AssetDatabase.GUIDToAssetPath(guid)));
            AssetDatabase.SaveAssets();
            Debug.Log("BOSS_SHARED_SLOTS_OK: inline weapon slots, shared player weapons/armor/crew and stage-owned progression.");
        }
    }
}
