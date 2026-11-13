using System;
using UnityEngine;

namespace MegaTanks
{
    public enum DamageType { Normal = 0, Fire = 1 }

    [Serializable]
    public class WeaponCombatStats
    {
        [Min(0), Tooltip("Percentage bonus to base damage: (1 + bonus / 100) * base damage.")]
        public float damageBonus = 0;
        [Min(0), Tooltip("Uncapped hit percentage before target evasion: accuracy - evasion. Only the final roll chance is clamped.")]
        public float accuracy = 0;
        [Min(0), Tooltip("Uncapped percentage of target armor bypassed: armor * (1 - penetration / 100).")]
        public float armorPenetration = 0;
        [Min(0), Tooltip("Uncapped critical percentage before reductions. Only the final roll chance is clamped.")]
        public float criticalChance = 0;
        [Min(0), Tooltip("Percentage damage increase when a critical hit occurs.")]
        public float criticalDamage = 0;
        public DamageType damageType = DamageType.Normal;
    }
}
