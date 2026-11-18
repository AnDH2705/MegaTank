using UnityEngine;

namespace MegaTanks
{
    // Weapon identity is independent of the tank that equips it.
    public abstract class WeaponDefinition : EquipmentDefinition
    {
        public string id;
        void OnValidate() { if (string.IsNullOrWhiteSpace(id)) id = System.Guid.NewGuid().ToString("N"); }
    }
}
