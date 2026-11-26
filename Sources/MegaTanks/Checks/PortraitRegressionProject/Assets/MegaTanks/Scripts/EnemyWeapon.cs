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
        public int MeleeAttacks { get; private set; }
        public bool IsMelee => owner != null && (owner.Data.melee || (owner.CommandoOwner != null && stats.range <= 0));
        public int Ammo { get; private set; }
        public int MaxAmmo => Mathf.Max(0,stats.reserveCapacity);
        public void Resupply(float fraction) { Ammo=Mathf.Min(MaxAmmo,Ammo+Mathf.CeilToInt(MaxAmmo*Mathf.Max(0,fraction))); }
        public EnemyWeaponStats Stats => stats;
        Vector2 MuzzlePosition => owner.CommandoArtwork!=null ? (Vector2)transform.position : owner.Artwork != null ? (Vector2)owner.Artwork.Muzzle.position : (Vector2)transform.position + (Vector2)barrel.right * .85f;

        public void Configure(EnemyEntity enemy, EnemyWeaponStats data)
        {
            owner = enemy;
            stats = data.Copy();Ammo=MaxAmmo;ShotsFired=MeleeAttacks=remainingShots=0;cooldown=0;
            barrel = new GameObject("Enemy weapon").transform;
            barrel.SetParent(transform, false);
            barrel.localRotation = Quaternion.Euler(0, 0, 180);
            bool heavy = stats.type == EnemyWeaponType.Cannon;
            if (owner.Artwork == null&&owner.CommandoArtwork==null)
                ShapeFactory.Create("Barrel", barrel, new Vector2(.45f, 0), new Vector2(.7f, heavy ? .2f : .1f), new Color(.8f, .85f, .88f), false, 14);
        }

        void Update()=>Tick(Time.deltaTime);
        public void Tick(float deltaTime)
        {
            var game = BattleGame.Instance;
            if (!game.Running || !owner.Alive) return;
            cooldown = Mathf.Max(0, cooldown - deltaTime);
            if(IsMelee){TickMelee(game);return;}
            var commando=game.Commando;
            bool attackCommando=commando!=null&&commando.Active&&Vector2.Distance(MuzzlePosition,commando.AimPoint)<=stats.range&&Vector2.Distance(MuzzlePosition,commando.AimPoint)<Vector2.Distance(MuzzlePosition,game.TankAimPoint);
            Vector2 aim=attackCommando?commando.AimPoint:game.TankAimPoint;
            Vector2 targetVelocity=attackCommando?commando.Velocity:game.TankVelocity;
            if(owner.CommandoOwner!=null&&(stats.range<=0||stats.reloadTime<=0||stats.projectileSpeed<=0))return;
            if ((!attackCommando&&!owner.InWeaponRange) || (Ammo<=0&&owner.CommandoOwner==null)) return;
            for (int i = 0; i < 4; i++)
            {
                Vector2 origin = MuzzlePosition;
                if (!Ballistics2D.TryIntercept(origin, aim, targetVelocity, stats.projectileSpeed,
                    stats.gravity, out Vector2 velocity)) return;
                barrel.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg);
            }
            if (cooldown > 0) return;
            if (remainingShots == 0) remainingShots = Mathf.Max(1, stats.salvoSize);
            remainingShots--;
            if(owner.CommandoOwner==null)Ammo--;
            cooldown = remainingShots > 0 ? Mathf.Max(.01f, stats.shotDelay) : stats.EffectiveReloadTime;
            bool heavy = stats.type == EnemyWeaponType.Cannon;
            Vector2 muzzle = MuzzlePosition;
            var shell = new GameObject(owner.Data.id + " projectile").AddComponent<Projectile2D>();
            shell.Initialize(muzzle, barrel.right, stats.projectileSpeed, stats.damage,
                heavy ? new Color(1, .45f, .2f) : new Color(1, .3f, .4f), heavy, stats.range, stats.gravity, ProjectileTeam.Enemy, combatStats:stats);
            ShotsFired++;
            if(owner.CommandoArtwork!=null){owner.CommandoArtwork.Face(aim.x-transform.position.x);owner.CommandoArtwork.Attack();}
            if(game.Vfx.Muzzle(owner.Artwork!=null?owner.Artwork.Muzzle:barrel,barrel.right,heavy)==null)
                game.Flash(muzzle, new Color(1, .55f, .3f), heavy ? .3f : .15f);
        }
        void TickMelee(BattleGame game)
        {
            var body=owner.GetComponent<Collider2D>();var target=CommandoMelee.PlayerTarget(game,body);
            if(target==null||cooldown>0||(owner.CommandoArtwork!=null&&owner.CommandoArtwork.IsAttacking)||!CommandoMelee.InReach(body,target))return;
            var attack=new DamageAttack(stats.damage,stats);MeleeAttacks++;cooldown=Mathf.Max(.02f,stats.EffectiveReloadTime);
            System.Action impact=()=>{
                if(!game.Running||owner==null||!owner.Alive||!owner.gameObject.activeInHierarchy||!CommandoMelee.InReach(body,target))return;
                var crew=target.GetComponent<CommandoUnit>();
                if(crew!=null){if(crew.Active)crew.ReceiveAttack(attack);}
                else if(target.GetComponent<TankHitbox>()!=null)game.ReceiveAttack(attack);
            };
            if(owner.CommandoArtwork!=null){owner.CommandoArtwork.Face(target.bounds.center.x-transform.position.x);owner.CommandoArtwork.Attack(impact);}
            else impact();
        }
    }
}
