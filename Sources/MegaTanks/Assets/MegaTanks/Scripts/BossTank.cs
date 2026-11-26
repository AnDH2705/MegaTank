using UnityEngine;

namespace MegaTanks
{
    // Runtime loadout snapshots never read player equipment, progression or PlayerPrefs.
    public sealed class BossTank : MonoBehaviour
    {
        public BossEncounterSettings Settings { get; private set; }
        public BossTankProfile Profile => Settings.template;
        public TankArtwork Artwork { get; private set; }
        public GunStats MachineGunStats { get; private set; }
        public GunStats CannonStats { get; private set; }
        public MortarStats MortarStats { get; private set; }
        public int MachineGunAmmo { get; private set; }
        public int CannonAmmo { get; private set; }
        public int MortarAmmo { get; private set; }
        public int ShotsFired { get; private set; }
        public bool AllWeaponsInRange => InRange(Artwork.MachineGun.Muzzle.position, MachineGunStats.range)
            && InRange(Artwork.MainGun.Muzzle.position, CannonStats.range)
            && InRange(Artwork.Mortar.Muzzle.position, MortarStats.range);
        public float AdvanceDistanceNeeded
        {
            get
            {
                if (AllWeaponsInRange) return 0;
                float distance = Mathf.Max(AdvanceForWeapon(Artwork.MachineGun.Muzzle.position, MachineGunStats.range),
                    AdvanceForWeapon(Artwork.MainGun.Muzzle.position, CannonStats.range),
                    AdvanceForWeapon(Artwork.Mortar.Muzzle.position, MortarStats.range));
                return float.IsInfinity(distance) ? 0 : distance;
            }
        }

        bool InRange(Vector2 origin, float range) => Vector2.Distance(origin, BattleGame.Instance.TankAimPoint) <= range;
        float AdvanceForWeapon(Vector2 origin, float range)
        {
            Vector2 offset = BattleGame.Instance.TankAimPoint - origin;
            offset.x *= owner.ApproachDirection;
            float reach = Mathf.Max(0, range - .05f);
            if (Mathf.Abs(offset.y) > reach) return float.PositiveInfinity;
            float horizontalReach = Mathf.Sqrt(Mathf.Max(0, reach * reach - offset.y * offset.y));
            if (offset.x < -horizontalReach) return float.PositiveInfinity;
            return Mathf.Max(0, offset.x - horizontalReach);
        }
        EnemyEntity owner;
        float mgCooldown, cannonCooldown, mortarCooldown;
        int mgSalvo, cannonSalvo;
        SpriteRenderer[] sprites;
        Color[] colors;
        bool flashing;
        public EnemyEntity Commando { get; private set; }
        EnemyDefinition commandoDefinition;
        float commandoActivationRange;
        bool commandoInitialized,commandoDefeated;

        public void TickCommando()
        {
            var game=BattleGame.Instance;
            if(game==null||!game.Running||owner==null||!owner.Alive)return;
            if(!commandoInitialized)
            {
                commandoInitialized=true;var member=Settings.CrewMember(CrewRole.Commando);
                if(member==null||member.commandoPrefab==null)return;
                var progression=Settings.CrewLevel(CrewDefinition.RoleLabel(CrewRole.Commando));
                var hull=member.CommandoHull(progression.level,progression.stars);var gun=member.CommandoWeapon(progression.level,progression.stars);
                commandoActivationRange=member.stats.activationRange.AtLevel(progression.level,progression.stars);
                commandoDefinition=new EnemyDefinition {
                    id=Profile.id+" Commando",kind=EnemyKind.Ground,baseHealth=hull.maxHull,baseSpeed=hull.advanceSpeed,
                    armor=hull.armor,evasion=hull.evasion,criticalChanceReduction=hull.criticalChanceReduction,criticalDamageReduction=hull.criticalDamageReduction,
                    score=0,baseScraps=0,advanceInsideRange=Vector2.zero,commandoPrefab=member.commandoPrefab,
                    weapon=new EnemyWeaponStats { damage=gun.damage,damageBonus=gun.damageBonus,accuracy=gun.accuracy,
                        armorPenetration=gun.armorPenetration,criticalChance=gun.criticalChance,criticalDamage=gun.criticalDamage,
                        reloadSpeed=gun.reloadSpeed,reloadTime=gun.reloadTime,range=gun.range,projectileSpeed=gun.projectileSpeed,gravity=0,salvoSize=1 }
                };
            }
            if(commandoDefinition==null||commandoDefinition.baseHealth<=0||commandoActivationRange<=0||commandoDefeated)return;
            bool activate=Vector2.Distance(transform.position,game.TankAimPoint)<=commandoActivationRange;
            if(game.Commando!=null&&game.Commando.Active)activate|=Vector2.Distance(transform.position,game.Commando.AimPoint)<=commandoActivationRange;
            if(!activate){if(Commando!=null&&Commando.gameObject.activeSelf)game.SetBossCommandoDeployed(Commando,false);return;}
            if(Commando!=null&&Commando.gameObject.activeSelf)return;
            var bounds=owner.GetComponent<Collider2D>().bounds;
            float direction=game.TankAimPoint.x<transform.position.x?-1:1;
            var position=new Vector2(direction<0?bounds.min.x-.6f:bounds.max.x+.6f,BattleGame.GroundHeight+.55f);
            if(Commando==null)Commando=game.SpawnBossCommando(this,commandoDefinition,position);
            else {Commando.transform.position=position;game.SetBossCommandoDeployed(Commando,true);}
        }
        public void OnCommandoDefeated(EnemyEntity unit){if(unit==Commando)commandoDefeated=true;}
        public void RecallCommando()
        {
            if(Commando==null)return;
            if(BattleGame.Instance!=null)BattleGame.Instance.SetBossCommandoDeployed(Commando,false);
            Destroy(Commando.gameObject);Commando=null;
        }
        void OnDestroy()=>RecallCommando();

        public void Configure(BossEncounterSettings settings) { Settings = settings.Copy(); owner = GetComponent<EnemyEntity>(); }
        public Bounds BuildArtwork()
        {
            var tank = Settings.CalculateTankStats();
            Artwork = Instantiate(Profile.artworkPrefab, transform, false);
            Artwork.name = "Mirrored Boss Tank";
            Artwork.transform.localScale = new Vector3(-tank.scale, tank.scale, tank.scale);
            Artwork.EnemyOwner = owner;
            Artwork.EquipMachineGunVisual(Profile.equipment.machineGun);
            if (Artwork.Tracks != null) Artwork.Tracks.EnemyOwner = owner;
            Artwork.ResetVisual();
            // Fit the complete tank, including the Spine tracks, onto the enemy ground lane.
            Bounds bounds = new Bounds(); bool first = true;
            foreach (var renderer in Artwork.GetComponentsInChildren<Renderer>())
            {
                if (!renderer.enabled || !renderer.gameObject.activeInHierarchy) continue;
                if (first) { bounds = renderer.bounds; first = false; } else bounds.Encapsulate(renderer.bounds);
            }
            Artwork.transform.position += Vector3.up * (BattleGame.GroundHeight - bounds.min.y);
            bounds.center += Vector3.up * (BattleGame.GroundHeight - bounds.min.y);
            Vector3 artPosition = Artwork.transform.position;
            transform.position = bounds.center;
            Artwork.transform.position = artPosition;
            sprites = Artwork.GetComponentsInChildren<SpriteRenderer>();
            colors = new Color[sprites.Length];
            for (int i = 0; i < sprites.Length; i++) colors[i] = sprites[i].color;
            MachineGunStats = Profile.equipment.machineGun.stats.Copy();
            CannonStats = Profile.equipment.cannon.stats.Copy();
            MortarStats = Profile.equipment.mortar.stats.Copy();
            MachineGunStats.damage = Settings.WeaponDamage(MachineGunStats.damage, Settings.machineGun);
            CannonStats.damage = Settings.WeaponDamage(CannonStats.damage, Settings.cannon);
            MortarStats.damage = Settings.WeaponDamage(MortarStats.damage, Settings.mortar);
            Settings.ApplyCrew(CrewRole.SubGunner,MachineGunStats);
            Settings.ApplyCrew(CrewRole.Gunner,CannonStats);
            Settings.ApplyCrew(CrewRole.WeaponSpecialist,MortarStats);
            Settings.ApplyAccessories(MachineGunStats);
            Settings.ApplyAccessories(CannonStats);
            Settings.ApplyAccessories(MortarStats);
            MachineGunAmmo = MachineGunStats.reserveCapacity; CannonAmmo = CannonStats.reserveCapacity; MortarAmmo = MortarStats.reserveCapacity;
            return new Bounds(transform.InverseTransformPoint(bounds.center), bounds.size);
        }

        public void SetHitFlash(bool value)
        {
            if (flashing == value) return;
            flashing = value;
            for (int i = 0; i < sprites.Length; i++) sprites[i].color = value ? new Color(1, .45f, .45f) : colors[i];
        }

        bool FireGun(GunStats stats, GunArtwork gun, ref float cooldown, ref int salvo)
        {
            cooldown = Mathf.Max(0, cooldown - Time.deltaTime);
            var game = BattleGame.Instance;
            Vector2 origin = gun.Muzzle.position;
            if (Vector2.Distance(origin, game.TankAimPoint) > stats.range) return false;
            Vector2 velocity = Vector2.zero;
            for (int i = 0; i < 4; i++)
            {
                origin = gun.Muzzle.position;
                if (!Ballistics2D.TryIntercept(origin, game.TankAimPoint, game.TankVelocity, stats.projectileSpeed, stats.gravity, out velocity)) return false;
                gun.Aim(velocity);
            }
            if (cooldown > 0) return false;
            if (salvo == 0) salvo = Mathf.Max(1, stats.salvoSize);
            salvo--;
            cooldown = salvo > 0 ? Mathf.Max(.01f, stats.shotDelay) : stats.EffectiveReloadTime;
            var shot = new GameObject("Boss gun projectile").AddComponent<Projectile2D>();
            shot.Initialize(gun.Muzzle.position, velocity, stats.projectileSpeed, stats.damage, Profile.healthBarColor,
                stats.heavyProjectile, stats.range, stats.gravity, ProjectileTeam.Enemy, stats.projectileSprite, stats.projectileWidth, false, stats);
            gun.Fire(stats.heavyProjectile); ShotsFired++;
            return true;
        }

        void Update()
        {
            var game = BattleGame.Instance;
            if (game == null || !game.Running || !owner.Alive) return;
            TickCommando();
            if (MachineGunAmmo > 0 && FireGun(MachineGunStats, Artwork.MachineGun, ref mgCooldown, ref mgSalvo)) MachineGunAmmo--;
            if (CannonAmmo > 0 && FireGun(CannonStats, Artwork.MainGun, ref cannonCooldown, ref cannonSalvo)) CannonAmmo--;
            mortarCooldown = Mathf.Max(0, mortarCooldown - Time.deltaTime);
            if (MortarAmmo <= 0 || mortarCooldown > 0) return;
            Vector2 origin = Artwork.Mortar.Muzzle.position;
            if (Vector2.Distance(origin, game.TankAimPoint) > MortarStats.range) return;
            Vector2 aim = game.TankAimPoint;
            for (int i = 0; i < 8; i++)
            {
                if (!MortarWeapon.TryHighArc(origin, aim, MortarStats.launchAngle, MortarStats.gravity, out _, out float time)) return;
                aim = game.TankAimPoint + game.TankVelocity * time;
            }
            aim.y = BattleGame.GroundHeight;
            aim.x += MortarWeapon.SampleDeviation(MortarStats.deviation);
            if (!MortarWeapon.TryHighArc(origin, aim, MortarStats.launchAngle, MortarStats.gravity, out Vector2 launch, out _)) return;
            var shell = new GameObject("Boss mortar shell").AddComponent<Projectile2D>();
            shell.InitializeMortar(origin, launch, MortarStats.damage, MortarStats.gravity, MortarStats.fullDamageRadius,
                MortarStats.halfDamageRadius, MortarStats.shellWidth, Profile.equipment.mortar.projectileSprite, MortarStats, ProjectileTeam.Enemy);
            Artwork.Mortar.Fire(); MortarAmmo--; ShotsFired++; mortarCooldown = MortarStats.EffectiveReloadTime;
        }
    }
}
