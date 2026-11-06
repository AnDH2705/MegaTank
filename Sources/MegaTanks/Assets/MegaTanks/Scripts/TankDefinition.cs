using System;
using UnityEngine;

namespace MegaTanks
{
    [Serializable]
    public sealed class TankStats
    {
        [Min(1)] public float maxHull = 100;
        [Min(0)] public float advanceSpeed = .5f;
        [Min(.1f)] public float scale = 1.2f;
        public TankStats Copy() => (TankStats)MemberwiseClone();
    }
    [CreateAssetMenu(menuName = "MegaTanks/Tanks/Tank Definition")]
    public sealed class TankDefinition : ScriptableObject
    {
        public TankStats stats = new TankStats();
    }
}
