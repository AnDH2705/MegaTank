using UnityEngine;
namespace MegaTanks
{
    [CreateAssetMenu(menuName="MegaTanks/Crew/Crew Catalog")]
    public sealed class CrewCatalog : ScriptableObject
    {
        public CrewDefinition[] members = new CrewDefinition[0];
        public string[] stations={"Gunner","Driver","Sub gunner","Weapon specialist","Commando"};
    }
}
