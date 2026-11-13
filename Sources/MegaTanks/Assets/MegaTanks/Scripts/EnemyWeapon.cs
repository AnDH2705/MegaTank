using UnityEngine;

namespace MegaTanks
{
    public sealed class EnemyWeapon : MonoBehaviour
    {
        EnemyEntity owner;
        EnemyWeaponStats stats;
        Transform barrel;
        float cooldown;
        int remainingShots;
        public int ShotsFired { get; private set; }
        public int Ammo { get; private set; }
        public int MaxAmmo => Mathf.Max(0,stats.reserveCapacity);
        public void Resupply(float fraction) { Ammo=Mathf.Min(MaxAmmo,Ammo+Mathf.CeilToInt(MaxAmmo*Mathf.Max(0,fraction))); }
        public EnemyWeaponStats Stats => stats;
        Vector2 MuzzlePosition => owner.Artwork != null ? (Vector2)owner.Artwork.Muzzle.position : (Vector2)transform.position + (Vector2)barrel.right * .85f;

        public void Configure(EnemyEntity enemy, EnemyWeaponStats data)
        {
            owner = enemy;
            stats = data.Copy();Ammo=MaxAmmo;ShotsFired=remainingShots=0;cooldown=0;
            barrel = new GameObject("Enemy weapon").transform;
            barrel.SetParent(transform, false);
            barrel.localRotation = Quaternion.Euler(0, 0, 180);
            bool heavy = stats.type == EnemyWeaponType.Cannon;
            if (owner.Artwork == null)
                ShapeFactory.Create("Barrel", barrel, new Vector2(.45f, 0), new Vector2(.7f, heavy ? .2f : .1f), new Color(.8f, .85f, .88f), false, 14);
        }

        void Update()
        {
            var game = BattleGame.Instance;
            if (!game.Running || !owner.Alive) return;
            cooldown = Mathf.Max(0, cooldown - Time.deltaTime);
            if (!owner.InWeaponRange || Ammo<=0) return;
            for (int i = 0; i < 4; i++)
            {
                Vector2 origin = MuzzlePosition;
                if (!Ballistics2D.TryIntercept(origin, game.TankAimPoint, game.TankVelocity, stats.projectileSpeed,
                    stats.gravity, out Vector2 velocity)) return;
                barrel.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg);
            }
            if (cooldown > 0) return;
            if (remainingShots == 0) remainingShots = Mathf.Max(1, stats.salvoSize);
            remainingShots--;
            Ammo--;
            cooldown = remainingShots > 0 ? Mathf.Max(.01f, stats.shotDelay) : Mathf.Max(.02f, stats.reloadTime);
            bool heavy = stats.type == EnemyWeaponType.Cannon;
            Vector2 muzzle = MuzzlePosition;
            var shell = new GameObject(owner.Data.id + " projectile").AddComponent<Projectile2D>();
            shell.Initialize(muzzle, barrel.right, stats.projectileSpeed, stats.damage,
                heavy ? new Color(1, .45f, .2f) : new Color(1, .3f, .4f), heavy, stats.range, stats.gravity, ProjectileTeam.Enemy, combatStats:stats);
            ShotsFired++;
            game.Flash(muzzle, new Color(1, .55f, .3f), heavy ? .3f : .15f);
        }
    }
}
