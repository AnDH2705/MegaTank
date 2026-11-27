using System;
using UnityEngine;

namespace MegaTanks
{
    [Serializable]
    public sealed class TankStats : UnitDefenseStats
    {
        [Min(1), InspectorName("HP")] public float maxHull = 100;
        [Min(0), InspectorName("Speed")] public float advanceSpeed = .5f;
        [Min(.1f)] public float scale = .75f;
        public TankStats Copy() => (TankStats)MemberwiseClone();
    }
    [CreateAssetMenu(menuName = "MegaTanks/Tanks/Tank Definition")]
    public sealed class TankDefinition : ScriptableObject
    {
        public string id;
        public string htmlSkin;
        public string displayName = "Medium Tank";
        public TankArtwork artworkPrefab;
        [Range(1,2)] public int armorSlots = 1;
        public int ArmorSlots => Mathf.Clamp(armorSlots,1,2);
        public int AccessorySlots => 3-ArmorSlots;
        public TankStats stats = new TankStats();
    }
}
