using System;
using UnityEngine;

namespace MegaTanks
{
    [Serializable]
    public sealed class BossEquipmentSlots
    {
        public GunDefinition machineGun;
        public GunDefinition cannon;
        public MortarDefinition mortar;
        public void Validate()
        {
            if(machineGun==null||cannon==null||mortar==null)
                throw new InvalidOperationException("Assign the boss's machine gun, cannon and mortar slots.");
            if(machineGun.Type!=EquipmentType.MachineGun||cannon.Type!=EquipmentType.Cannon)
                throw new InvalidOperationException("Assign a machine gun to the MG slot and a cannon to the Cannon slot.");
        }
    }
    [Serializable]
    public sealed class BossCrewStation
    {
        public string station;
        public CrewDefinition member;
    }

    [CreateAssetMenu(menuName = "MegaTanks/Boss Tank Template")]
    public sealed class BossTankProfile : ScriptableObject
    {
        public string id = "BossTank";
        [HideInInspector] public int sharedSlotVersion;
        public TankDefinition tank;
        public TankArtwork artworkPrefab;
        public BossEquipmentSlots equipment = new BossEquipmentSlots();
        public ArmorDefinition armor;
        [Tooltip("Optional second armor, used by hulls with two armor slots.")]
        public ArmorDefinition secondArmor;
        public AccessoryDefinition accessory;
        public AccessoryDefinition secondAccessory;
        public BossCrewStation[] crew = Array.Empty<BossCrewStation>();
        [Header("Rewards and presentation")]
        [Min(0)] public int score = 1000;
        [Min(0)] public int scraps = 150;
        public Color healthBarColor = new Color(.85f, .2f, .2f);
    }
}
