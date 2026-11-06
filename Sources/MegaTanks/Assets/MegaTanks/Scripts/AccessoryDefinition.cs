using UnityEngine;
namespace MegaTanks
{
    [CreateAssetMenu(menuName = "MegaTanks/Equipment/Accessory")]
    public sealed class AccessoryDefinition : EquipmentDefinition
    {
        public override EquipmentType Type => EquipmentType.Accessory;
    }
}
