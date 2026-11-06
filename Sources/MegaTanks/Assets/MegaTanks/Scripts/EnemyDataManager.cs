using System;
using UnityEngine;

namespace MegaTanks
{
    public enum EnemyWeaponType { MachineGun, Cannon }

    [Serializable]
    public sealed class EnemyWeaponStats
    {
        public EnemyWeaponType type;
        [Min(1)] public float damage = 2;
        [Min(1)] public float range = 30;
        [Min(1)] public int salvoSize = 3;
        [Min(.01f)] public float shotDelay = .1f;
        [Min(.02f)] public float reloadTime = 1.8f;
        [Min(1)] public float projectileSpeed = 22;
        [Min(0)] public float gravity = .35f;
    }

    [Serializable]
    public sealed class EnemyDefinition
    {
        public string id;
        public EnemyKind kind;
        [Min(1)] public float baseHealth = 26;
        [Min(0)] public float healthPerWave = 4;
        [Min(.01f)] public float baseSpeed = .72f;
        [Min(0)] public float speedPerWave = .035f;
        [Min(0)] public float maxSpeedBonus = .5f;
        [Tooltip("Random extra horizontal travel after entering weapon range, in world units.")] public Vector2 advanceInsideRange = new Vector2(1, 5);
        [Min(0)] public int score = 75;
        [Min(0)] public int baseScraps = 20;
        [Min(0)] public int scrapsPerWave = 2;
        public Color color = new Color(1, .63f, .25f);
        public EnemyVehicleArtwork artworkPrefab;
        [Min(.1f)] public float artworkWidth = 2.2f;
        [Min(.1f), Tooltip("Size multiplier for this enemy's artwork and fitted hitbox.")] public float scale = 1.25f;
        public float ScaledArtworkWidth => artworkWidth * Mathf.Max(.1f, scale);
        public EnemyWeaponStats weapon = new EnemyWeaponStats();
    }

    [CreateAssetMenu(menuName = "MegaTanks/Enemy Data Manager")]
    public sealed class EnemyDataManager : ScriptableObject
    {
        [SerializeField] EnemyDefinition[] enemies =
        {
            new EnemyDefinition { id = "FlyingEnemy1", kind = EnemyKind.Flyer },
            new EnemyDefinition
            {
                id = "GroundEnemy1", kind = EnemyKind.Ground, baseHealth = 70, healthPerWave = 10,
                baseSpeed = .42f, score = 100, baseScraps = 30, scrapsPerWave = 3, color = new Color(.96f, .37f, .4f),
                weapon = new EnemyWeaponStats { type = EnemyWeaponType.Cannon, damage = 8, range = 40,
                    salvoSize = 1, shotDelay = .1f, reloadTime = 4, projectileSpeed = 18, gravity = 3 }
            }
        };

        public EnemyDefinition Get(EnemyKind kind)
        {
            foreach (var enemy in enemies) if (enemy != null && enemy.kind == kind) return enemy;
            throw new InvalidOperationException("No enemy definition for " + kind);
        }

        public EnemyDefinition Get(string id)
        {
            foreach (var enemy in enemies) if (enemy != null && enemy.id == id) return enemy;
            throw new InvalidOperationException("No enemy definition named " + id);
        }
    }
}
