using UnityEngine;

namespace MegaTanks
{
    [CreateAssetMenu(menuName = "MegaTanks/Weapons/Mortar Definition")]
    public sealed class MortarDefinition : WeaponDefinition
    {
        public MortarStats stats = new MortarStats();
        public override EquipmentType Type => EquipmentType.Mortar;
        public Sprite projectileSprite;
    }
}
