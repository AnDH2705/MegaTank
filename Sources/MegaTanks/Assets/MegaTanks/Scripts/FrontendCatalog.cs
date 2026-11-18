using UnityEngine;
namespace MegaTanks
{
    [CreateAssetMenu(menuName = "MegaTanks/UI/Frontend Catalog")]
    public sealed class FrontendCatalog : ScriptableObject
    {
        public StageDefinition[] stages;
        public TankDefinition[] tanks;
        public EquipmentDefinition[] inventory;
        public CrewCatalog crew;
    }
}
