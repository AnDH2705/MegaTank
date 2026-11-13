using UnityEngine;

namespace MegaTanks
{
    [System.Serializable]
    public sealed class MortarStats : WeaponCombatStats
    {
        [Min(1), InspectorName("Base Damage")] public float damage = 60;
        [Min(.1f)] public float reloadTime = 6;
        [Min(1)] public float range = 80;
        [Range(46, 85)] public float launchAngle = 70;
        [Min(.1f)] public float gravity = 12;
        [Min(0), InspectorName("Max Ammo")] public int reserveCapacity = 20;
        public Vector2 deviation = new Vector2(1, 3);
        [Min(0)] public float fullDamageRadius = 2.5f;
        [Min(0)] public float halfDamageRadius = 5;
        [Min(.05f)] public float shellWidth = .45f;
        public MortarStats Copy() => (MortarStats)MemberwiseClone();
    }

    public sealed class MortarWeapon : MonoBehaviour
    {
        MortarStats stats;
        MortarArtwork artwork;
        float cooldown;
        public MortarStats Stats => stats;
        public MortarDefinition Definition { get; private set; }
        Sprite projectileSprite;
        public int Ammo { get; private set; }
        public int ShotsFired { get; private set; }
        public Vector2 LastIntendedTarget { get; private set; }
        public Vector2 LastAimPoint { get; private set; }
        public Vector2 LastLaunchVelocity { get; private set; }
        public void Configure(MortarArtwork visual, MortarDefinition definition)
        { artwork = visual; Definition = definition; stats = definition.stats.Copy(); projectileSprite = definition.projectileSprite; ResetWeapon(); }
        public void ResetWeapon() { Ammo = Mathf.Max(0, stats.reserveCapacity); ShotsFired = 0; cooldown = 0; artwork.ResetVisual(); }
        public void Resupply(float fraction) { Ammo = Mathf.Min(stats.reserveCapacity, Ammo + Mathf.CeilToInt(stats.reserveCapacity * Mathf.Max(0, fraction))); }

        public static float SampleDeviation(Vector2 limits)
        {
            float low = Mathf.Max(0, Mathf.Min(limits.x, limits.y));
            float high = Mathf.Max(low, Mathf.Max(limits.x, limits.y));
            return Random.Range(low, high) * (Random.value < .5f ? -1 : 1);
        }

        // Choose launch speed for a fixed steep angle rather than allowing the ordinary low-arc solver.
        public static bool TryHighArc(Vector2 origin, Vector2 destination, float angle, float gravity,
            out Vector2 velocity, out float flightTime)
        {
            velocity = Vector2.zero; flightTime = 0;
            if (gravity <= 0 || angle <= 45 || angle >= 90) return false;
            float dx = destination.x - origin.x;
            if (Mathf.Abs(dx) < .01f) return false;
            float horizontal = Mathf.Abs(dx), dy = destination.y - origin.y;
            float radians = angle * Mathf.Deg2Rad;
            float denominator = 2 * Mathf.Cos(radians) * Mathf.Cos(radians) * (horizontal * Mathf.Tan(radians) - dy);
            if (denominator <= 0) return false;
            float speed = Mathf.Sqrt(gravity * horizontal * horizontal / denominator);
            velocity = new Vector2(Mathf.Sign(dx) * speed * Mathf.Cos(radians), speed * Mathf.Sin(radians));
            flightTime = horizontal / Mathf.Abs(velocity.x);
            return true;
        }

        void Update()
        {
            var game = BattleGame.Instance;
            if (!game.Running) return;
            cooldown = Mathf.Max(0, cooldown - Time.deltaTime);
            if (cooldown > 0 || Ammo <= 0) return;
            Vector2 origin = artwork.Muzzle.position;
            var target = game.FindMortarTarget(origin, stats.range, stats.halfDamageRadius);
            if (target == null) return;
            float angle = Mathf.Clamp(stats.launchAngle, 46, 85), gravity = Mathf.Max(.1f, stats.gravity);
            Vector2 aim = target.transform.position;
            // Lead the formation's motion before sampling scatter once for this shot.
            for (int i = 0; i < 8; i++)
            {
                if (!TryHighArc(origin, aim, angle, gravity, out _, out float time)) return;
                float predictedX = target.transform.position.x;
                if (target.Velocity.x < 0) predictedX = Mathf.Max(target.StoppingX, predictedX + target.Velocity.x * time);
                else if (target.Velocity.x > 0) predictedX = Mathf.Min(target.StoppingX, predictedX + target.Velocity.x * time);
                aim = new Vector2(predictedX, target.Kind == EnemyKind.Ground ? BattleGame.GroundHeight : target.transform.position.y);
            }
            LastIntendedTarget = aim;
            aim.x += SampleDeviation(stats.deviation);
            if (!TryHighArc(origin, aim, angle, gravity, out Vector2 launch, out _)) return;
            LastAimPoint = aim; LastLaunchVelocity = launch;
            Ammo--; ShotsFired++; cooldown = Mathf.Max(.1f, stats.reloadTime);
            var shell = new GameObject("Mortar shell").AddComponent<Projectile2D>();
            shell.InitializeMortar(origin, launch, stats.damage, gravity, stats.fullDamageRadius, stats.halfDamageRadius, stats.shellWidth, projectileSprite, stats);
            artwork.Fire();
            game.Flash(origin, new Color(1, .8f, .3f), .35f);
        }
    }
}
