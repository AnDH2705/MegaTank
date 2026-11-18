using System;
using UnityEngine;

namespace MegaTanks
{
    [Serializable]
    public sealed class BossCrewStation
    {
        public string station;
        public CrewDefinition member;
    }

    [CreateAssetMenu(menuName = "MegaTanks/Boss Tank Template")]
    public sealed class BossTankProfile : ScriptableObject
    {
        public string id = "BossTank";
        public TankDefinition tank;
        public TankArtwork artworkPrefab;
        public WeaponLoadout equipment;
        public ArmorDefinition armor;
        [Tooltip("Optional second armor, used by hulls with two armor slots.")]
        public ArmorDefinition secondArmor;
        public BossCrewStation[] crew = Array.Empty<BossCrewStation>();
        [Header("Rewards and presentation")]
        [Min(0)] public int score = 1000;
        [Min(0)] public int scraps = 150;
        public Color healthBarColor = new Color(.85f, .2f, .2f);
    }
}
