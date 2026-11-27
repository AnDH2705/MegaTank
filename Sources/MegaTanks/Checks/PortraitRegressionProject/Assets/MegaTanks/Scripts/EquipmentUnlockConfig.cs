using System;
using UnityEngine;

namespace MegaTanks
{
    [Serializable]
    public sealed class EquipmentUnlockConfig
    {
        public int version=1;
        public int machineGun=5,cannon=1,mortar=10,wheels=15,tankHull=20;
        public int driver=1,melee=10;
        public int[] EquipmentLevels=>new[]{machineGun,cannon,mortar,tankHull,wheels};
        public int[] CrewAndWeaponLevels=>new[]{machineGun,cannon,mortar,driver,melee};
        public int LevelForPart(int part)
        {
            switch(part)
            {
                case 0:return machineGun;
                case 1:return cannon;
                case 2:return mortar;
                case 3:return tankHull;
                case 4:return wheels;
                default:throw new ArgumentOutOfRangeException(nameof(part));
            }
        }
        public static EquipmentUnlockConfig Load()
        {
            var file=Resources.Load<TextAsset>("EquipmentUnlockConfig");
            if(file==null)throw new InvalidOperationException("Missing EquipmentUnlockConfig.json");
            var config=JsonUtility.FromJson<EquipmentUnlockConfig>(file.text);config.Validate();return config;
        }
        public void Validate()
        {
            if(version!=1||machineGun<1||cannon<1||mortar<1||wheels<1||tankHull<1||driver<1||melee<1)
                throw new InvalidOperationException("EquipmentUnlockConfig.json requires version 1 and positive Tank unlock levels.");
        }
    }
}
