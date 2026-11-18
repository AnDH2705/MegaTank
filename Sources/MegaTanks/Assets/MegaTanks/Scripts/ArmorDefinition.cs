using UnityEngine;
namespace MegaTanks
{
    [CreateAssetMenu(menuName = "MegaTanks/Equipment/Armor")]
    public sealed class ArmorDefinition : EquipmentDefinition
    {
        [Tooltip("Permanent save identity. Preserve across renames; clear on a duplicated asset.")]
        public string id;
        [Min(0)] public float hp = 100;
        [Min(0)] public float armor = 10;
        [Min(0)] public float speed;
        [Min(0), Tooltip("Evasion percentage points added to the tank.")] public float evasion;
        [Min(0)] public float hpPerLevel = 10;
        public override EquipmentType Type => EquipmentType.Armor;
        public float HitPoints(int level, int stars) => ProgressionRules.Scale(hp, hpPerLevel, level, stars);
        public void ApplyTo(TankStats tank, float hitPoints)
        {
            tank.maxHull += hitPoints;
            tank.armor += armor;
            tank.advanceSpeed += speed;
            tank.evasion += evasion;
        }
        void OnValidate() { if (string.IsNullOrWhiteSpace(id)) id = System.Guid.NewGuid().ToString("N"); }
    }
}
