using UnityEngine;
namespace MegaTanks
{
    [CreateAssetMenu(menuName="MegaTanks/Equipment/Wheels")]
    public sealed class WheelsDefinition : EquipmentDefinition
    {
        public string id;
        public string skin="base";
        public override EquipmentType Type=>EquipmentType.Wheels;
    }
}
