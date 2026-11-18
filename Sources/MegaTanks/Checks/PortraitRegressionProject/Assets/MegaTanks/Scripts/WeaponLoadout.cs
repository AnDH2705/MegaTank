using UnityEngine;

namespace MegaTanks
{
    [CreateAssetMenu(menuName = "MegaTanks/Weapons/Weapon Loadout")]
    public sealed class WeaponLoadout : ScriptableObject
    {
        public GunDefinition machineGun;
        public GunDefinition cannon;
        public MortarDefinition mortar;
        public void Validate()
        {
            if (machineGun == null || cannon == null || mortar == null)
                throw new System.InvalidOperationException("The starting loadout needs MG, cannon and mortar definitions.");
        }
    }
}
