using UnityEngine;

namespace MegaTanks
{
    public enum EquipmentType { MachineGun, Cannon, Mortar, Armor, Accessory }
    public abstract class EquipmentDefinition : ScriptableObject
    {
        public string displayName;
        public Sprite icon;
        public abstract EquipmentType Type { get; }
    }
}
