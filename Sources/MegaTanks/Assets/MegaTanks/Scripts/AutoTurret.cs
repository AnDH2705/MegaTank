using UnityEngine;

namespace MegaTanks
{
    public sealed class AutoTurret : MonoBehaviour
    {
        string displayName = "MG / AA";
        GunStats stats = new GunStats();
        float initialDamageBonus;
        public GunDefinition Definition { get; private set; }
        public GunStats Stats => stats;
        EnemyKind preferredTarget => stats.preferredTarget;
        float reloadTime => stats.reloadTime;
        int salvoSize => stats.salvoSize;
        float shotDelay => stats.shotDelay;
        float range => stats.range;
        float projectileSpeed => stats.projectileSpeed;
        float projectileGravity => stats.gravity;
        public float MaxRange => range;
        public float ProjectileGravity => projectileGravity;
        [SerializeField] GunArtwork artwork;
        public GunArtwork Artwork => artwork;
        public string DisplayName => displayName;
        public float DamagePerShot => new DamageAttack(stats.damage,stats).WeaponDamage;
        public float ReloadTime => reloadTime;
        public int SalvoSize => salvoSize;
        public float ShotDelay => shotDelay;
        public int ShotsFired { get; private set; }
        public int Ammo { get; private set; }
        public int ReserveCapacity { get; private set; }
        public event System.Action<float> ShotFired;
        public EnemyEntity Target { get; private set; }
        public float CooldownFraction => Mathf.Clamp01(cooldown / (shotsRemaining > 0 ? shotDelay : reloadTime));
        float cooldown;
        int shotsRemaining;
        Color shotColor;

        public void BindArtwork(GunArtwork visual) { artwork = visual; artwork.ResetVisual(); }

        public void Configure(GunDefinition definition)
        {
            Definition = definition;
            displayName = definition.displayName;
            stats = definition.stats.Copy();
            initialDamageBonus = stats.damageBonus;
            ReserveCapacity = Mathf.Max(0, stats.reserveCapacity);
            shotColor = stats.heavyProjectile ? new Color(1, .72f, .28f) : new Color(.34f, .91f, 1);
            ResetWeapon();
        }

        public void Configure(string label, EnemyKind priority, float reload, float hitDamage, int shotsPerSalvo, float delayBetweenShots, float maximumRange, float gravity)
        {
            displayName = label;
            stats = stats.Copy();
            stats.preferredTarget = priority;
            stats.reloadTime = Mathf.Max(.02f, reload);
            stats.damage = hitDamage;
            initialDamageBonus = stats.damageBonus;
            stats.salvoSize = Mathf.Max(1, shotsPerSalvo);
            stats.shotDelay = Mathf.Max(.01f, delayBetweenShots);
            stats.range = Mathf.Max(1, maximumRange);
            stats.gravity = Mathf.Max(0, gravity);
            if (Definition == null) stats.heavyProjectile = priority == EnemyKind.Ground;
            shotColor = stats.heavyProjectile ? new Color(1, .72f, .28f) : new Color(.34f, .91f, 1);
            ResetWeapon();
        }

        public void ResetWeapon()
        {
            cooldown = 0; shotsRemaining = 0; ShotsFired = 0; Target = null;
            Ammo = ReserveCapacity;
            stats.damageBonus = initialDamageBonus;
            if (artwork != null) artwork.ResetVisual();
        }

        public void ConfigureAmmunition(int capacity) { stats.reserveCapacity = ReserveCapacity = Mathf.Max(0, capacity); Ammo = ReserveCapacity; }
        public bool TryConsumeRound() { if (Ammo <= 0) return false; Ammo--; return true; }
        public void Resupply(float fraction) { Ammo = Mathf.Min(ReserveCapacity, Ammo + Mathf.CeilToInt(ReserveCapacity * Mathf.Max(0, fraction))); }
        public void IncreaseDamage(float fraction) { stats.damageBonus += 100*Mathf.Max(0,fraction); }

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
            if (cooldown > 0 || !TryConsumeRound()) return;
            // Preserve a partial salvo while reacquiring; reload starts after its final shot.
            if (shotsRemaining == 0) shotsRemaining = salvoSize;
            shotsRemaining--;
            cooldown = shotsRemaining > 0 ? shotDelay : reloadTime;
            ShotsFired++;
            Vector2 direction = artwork.FireDirection;
            Vector2 origin = (Vector2)artwork.Muzzle.position + direction * .04f;
            var shell = new GameObject(displayName + " shell").AddComponent<Projectile2D>();
            shell.Initialize(origin, direction, projectileSpeed, stats.damage, shotColor, stats.heavyProjectile, range, projectileGravity,
                ProjectileTeam.Player, stats.projectileSprite, stats.projectileWidth, false, stats);
            artwork.Fire();
            ShotFired?.Invoke(Time.time);
        }
    }
}
