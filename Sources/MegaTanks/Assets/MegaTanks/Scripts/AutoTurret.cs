using UnityEngine;
using UnityEngine.Serialization;

namespace MegaTanks
{
    public sealed class AutoTurret : MonoBehaviour
    {
        [SerializeField] string displayName = "MG / AA";
        [SerializeField] EnemyKind preferredTarget = EnemyKind.Flyer;
        [FormerlySerializedAs("fireInterval")]
        [SerializeField, Min(.02f), Tooltip("Time from the final shot of a salvo to the next salvo.")] float reloadTime = 1.25f;
        [SerializeField, Min(1), Tooltip("Number of shots fired in each salvo.")] int salvoSize = 3;
        [SerializeField, Min(.01f), Tooltip("Time between shots within the same salvo.")] float shotDelay = .1f;
        [SerializeField, Min(1)] float damage = 6;
        [SerializeField, Min(1)] float range = 50;
        public float MaxRange => range;
        [SerializeField, Min(1)] float projectileSpeed = 18;
        [SerializeField, Min(0), Tooltip("Downward acceleration in world units per second squared. Zero gives a straight shot.")] float projectileGravity = .35f;
        public float ProjectileGravity => projectileGravity;
        [SerializeField] GunArtwork artwork;
        public GunArtwork Artwork => artwork;
        public string DisplayName => displayName;
        public float DamagePerShot => damage;
        public float ReloadTime => reloadTime;
        public int SalvoSize => salvoSize;
        public float ShotDelay => shotDelay;
        public int ShotsFired { get; private set; }
        public event System.Action<float> ShotFired;
        public EnemyEntity Target { get; private set; }
        public float CooldownFraction => Mathf.Clamp01(cooldown / (shotsRemaining > 0 ? shotDelay : reloadTime));
        float cooldown;
        int shotsRemaining;
        Color shotColor;

        public void BindArtwork(GunArtwork visual) { artwork = visual; artwork.ResetVisual(); }

        public void Configure(string label, EnemyKind priority, float reload, float hitDamage, int shotsPerSalvo, float delayBetweenShots, float maximumRange, float gravity)
        {
            displayName = label;
            preferredTarget = priority;
            reloadTime = Mathf.Max(.02f, reload);
            damage = hitDamage;
            salvoSize = Mathf.Max(1, shotsPerSalvo);
            shotDelay = Mathf.Max(.01f, delayBetweenShots);
            range = Mathf.Max(1, maximumRange);
            projectileGravity = Mathf.Max(0, gravity);
            shotColor = priority == EnemyKind.Ground ? new Color(1, .72f, .28f) : new Color(.34f, .91f, 1);
            ResetWeapon();
        }

        public void ResetWeapon()
        {
            cooldown = 0; shotsRemaining = 0; ShotsFired = 0; Target = null;
            if (artwork != null) artwork.ResetVisual();
        }

        void Update()
        {
            BattleGame game = BattleGame.Instance;
            if (!game.Running) return;
            cooldown = Mathf.Max(0, cooldown - Time.deltaTime);
            Target = game.FindTarget(transform.position, preferredTarget, range);
            if (Target == null) return;
            // Compensate for gravity and enemy movement from the rotating artwork's actual muzzle.
            for (int i = 0; i < 5; i++)
            {
                Vector2 muzzle = (Vector2)artwork.Muzzle.position + artwork.FireDirection * .04f;
                if (!Ballistics2D.TryIntercept(muzzle, Target.transform.position, Target.Velocity,
                    projectileSpeed, projectileGravity, out Vector2 launchVelocity)) return;
                artwork.Aim(launchVelocity);
            }
            if (cooldown > 0) return;
            // Preserve a partial salvo while reacquiring; reload starts after its final shot.
            if (shotsRemaining == 0) shotsRemaining = salvoSize;
            shotsRemaining--;
            cooldown = shotsRemaining > 0 ? shotDelay : reloadTime;
            ShotsFired++;
            Vector2 direction = artwork.FireDirection;
            Vector2 origin = (Vector2)artwork.Muzzle.position + direction * .04f;
            var shell = new GameObject(displayName + " shell").AddComponent<Projectile2D>();
            shell.Initialize(origin, direction, projectileSpeed, damage, shotColor, preferredTarget == EnemyKind.Ground, range, projectileGravity);
            artwork.Fire();
            ShotFired?.Invoke(Time.time);
        }
    }
}
