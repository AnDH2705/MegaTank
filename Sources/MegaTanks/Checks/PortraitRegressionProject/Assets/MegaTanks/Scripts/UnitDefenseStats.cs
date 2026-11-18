using System;
using UnityEngine;

namespace MegaTanks
{
    [Serializable]
    public class UnitDefenseStats
    {
        [Min(0)] public float armor = 0;
        [Min(0), Tooltip("Uncapped evasion percentage, subtracted from attacker accuracy before clamping the final roll chance.")]
        public float evasion = 0;
        [Min(0), Tooltip("Reduction to an incoming attack's critical chance, in percentage points.")]
        public float criticalChanceReduction = 0;
        [Min(0), Tooltip("Percentage points subtracted from an incoming attack's critical damage bonus.")]
        public float criticalDamageReduction = 0;
    }
}
