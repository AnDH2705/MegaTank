using System;
using UnityEngine;

namespace MegaTanks
{
    [Serializable]
    public sealed class GunStats : WeaponCombatStats
    {
        public EnemyKind preferredTarget = EnemyKind.Flyer;
        [Min(1), InspectorName("Base Damage")] public float damage = 6;
        [Min(1)] public float range = 35;
        [Min(1)] public int salvoSize = 3;
        [Min(.01f)] public float shotDelay = .1f;
        [Min(.02f), Tooltip("Reload starts after the final salvo shot.")] public float reloadTime = 1.25f;
        [Min(1)] public float projectileSpeed = 18;
        [Min(0)] public float gravity = .35f;
        [Min(0), InspectorName("Max Ammo")] public int reserveCapacity = 150;
        public bool heavyProjectile;
        public Sprite projectileSprite;
        [Min(.01f)] public float projectileWidth = .65f;
        public GunStats Copy() => (GunStats)MemberwiseClone();
    }
    [CreateAssetMenu(menuName = "MegaTanks/Weapons/Gun Definition")]
    public sealed class GunDefinition : WeaponDefinition
    {
        public GunStats stats = new GunStats();
        [Tooltip("Optional replacement artwork for this weapon's tank mount.")]
        public GunArtwork artworkPrefab;
        public override EquipmentType Type => stats.heavyProjectile ? EquipmentType.Cannon : EquipmentType.MachineGun;
    }
}
