using UnityEngine;
namespace MegaTanks
{
    [CreateAssetMenu(menuName = "MegaTanks/UI/Frontend Catalog")]
    public sealed class FrontendCatalog : ScriptableObject
    {
        public StageDefinition[] stages;
        public EquipmentDefinition[] inventory;
    }
}
