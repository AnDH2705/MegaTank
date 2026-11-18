using UnityEngine;

namespace MegaTanks
{
    public enum BattleVfxKind { GroundFire, Explosion, Tracer, Muzzle, Smoke, Impact }

    [System.Serializable]
    public sealed class BattleVfxClip
    {
        public Sprite[] frames;
        [Min(1)] public float framesPerSecond = 24;
        public bool loop;
    }

    [CreateAssetMenu(menuName = "MegaTanks/Battle VFX")]
    public sealed class BattleVfxCatalog : ScriptableObject
    {
        public BattleVfxClip groundFire, explosion, tracer, muzzle, smoke, impact;
        public GameObject muzzlePrefab, hitPrefab, explosionSmallPrefab, explosionBigPrefab, fireBoltPrefab;
        [Min(.1f)] public float machineGunMuzzleSize = .8f;
        [Min(.1f)] public float cannonMuzzleSize = 1.4f;
        [Min(.1f)] public float mortarMuzzleSize = 1;
        [Min(.1f)] public float tracerSize = 1.1f;
        [Min(.1f)] public float smokeSize = 1.5f;
        [Min(.1f)] public float impactSize = .7f;
        [Min(.1f)] public float cannonExplosionSize = 1.5f;
        [Min(.1f)] public float mortarExplosionSize = 3;
        [Min(.1f)] public float vehicleExplosionSize = 2.5f;
        [Min(.1f)] public float groundFireHeight = 1.2f;
        [Min(.1f)] public float groundFireSeconds = 2.5f;
        [Min(8)] public int maxEffects = 128;

        public BattleVfxClip Get(BattleVfxKind kind)
        {
            switch(kind)
            {
                case BattleVfxKind.GroundFire: return groundFire;
                case BattleVfxKind.Explosion: return explosion;
                case BattleVfxKind.Tracer: return tracer;
                case BattleVfxKind.Muzzle: return muzzle;
                case BattleVfxKind.Smoke: return smoke;
                default: return impact;
            }
        }
    }
}
