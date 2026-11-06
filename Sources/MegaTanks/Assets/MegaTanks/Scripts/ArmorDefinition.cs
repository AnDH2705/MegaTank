using UnityEngine;
namespace MegaTanks
{
    [CreateAssetMenu(menuName = "MegaTanks/Equipment/Armor")]
    public sealed class ArmorDefinition : EquipmentDefinition
    {
        public override EquipmentType Type => EquipmentType.Armor;
    }
}
