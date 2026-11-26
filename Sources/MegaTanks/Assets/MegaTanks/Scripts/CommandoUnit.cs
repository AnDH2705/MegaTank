using UnityEngine;
namespace MegaTanks
{
    // An independent allied unit. Its integrated weapon never inherits tank equipment or crew bonuses.
    public sealed class CommandoUnit : MonoBehaviour
    {
        BattleGame game;
        CrewDefinition member;
        GameObject visual;
        BoxCollider2D hitbox;
        SpriteRenderer portrait;
        CommandoArtwork artwork;
        CommandoArtwork artworkPrefab;
        EnemyHealthBarUI healthBar;
        public EnemyHealthBarUI HealthBar=>healthBar;
        public CommandoArtwork Artwork=>artwork;
        TankStats defense;
        GunStats weapon;
        float cooldown,activationRange;
        float healthRegeneration, regenerationTime;
        bool initialized,defeated;
        enum CrewMotion { Aboard, JumpOut, Fighting, Returning, JumpIn }
        CrewMotion motion;
        Vector3 jumpStart;
        float jumpTime;
        const float JumpDuration = .55f;
        const float SeatedSpriteSize = 1.15f;
        const int CrewSortingOrder = 35;
        const int LowerDeckCrewSortingOrder = 43;
        public CrewRole Role { get; private set; }
        public bool Active=>visual!=null&&visual.activeSelf&&hitbox.enabled;
        public bool IsUnconscious { get; private set; }
        public float Health { get; private set; }
        public float MaxHealth=>defense?.maxHull??0;
        public GunStats Weapon=>weapon;
        public Vector2 Velocity { get; private set; }
        public int ShotsFired { get; private set; }
        public int MeleeAttacks { get; private set; }
        public Vector2 AimPoint=>transform.position;
        public void Configure(BattleGame owner, CrewRole role = CrewRole.Commando)
        {
            game=owner;Role=role;
            visual=new GameObject("Crew artwork");visual.transform.SetParent(transform,false);
            var portraitObject = new GameObject("Crew sprite");
            portraitObject.transform.SetParent(visual.transform, false);
            portrait=portraitObject.AddComponent<SpriteRenderer>();
            hitbox=gameObject.AddComponent<BoxCollider2D>();hitbox.size=new Vector2(.65f,.9f);hitbox.isTrigger=true;
            ResetForRun();
        }
        void Stow(){artwork?.CancelAttack();visual.SetActive(false);hitbox.enabled=false;if(healthBar!=null)healthBar.gameObject.SetActive(false);Velocity=Vector2.zero;}
        public void ResetForRun()
        {
            initialized=defeated=false;Health=0;cooldown=0;ShotsFired=MeleeAttacks=0;
            defense = null; weapon = null;
            IsUnconscious = false;
            regenerationTime = 0;
            motion = CrewMotion.Aboard;
            if(visual!=null)Stow();
        }
        void Update()=>Tick(Time.deltaTime);
        void LateUpdate()
        {
            // Match the hull after the tank's suspension animation has updated.
            if (motion == CrewMotion.Aboard && visual != null && visual.activeSelf && !Active)
                transform.position = SeatPoint();
        }

        public void Tick(float deltaTime)
        {
            if(game==null)return;
            if(!game.Deployed||game.Hull<=0){Stow();return;}
            if(!game.Running)
            {
                Velocity=Vector2.zero;
                if (initialized && !defeated && !game.Paused && game.EnemyCount == 0)
                {
                    bool alreadyAboard = motion == CrewMotion.Aboard && !Active;
                    if (motion != CrewMotion.Aboard || Active) Stow();
                    motion = CrewMotion.Aboard;
                    if (alreadyAboard && game.Stage.Mode == StageMode.WaveBreak)
                        TickAboard(deltaTime);
                    else FollowSeat(0);
                }
                return;
            }
            if(!initialized)
            {
                InitializeCrew();
            }
            if (defeated) return;
            if (IsUnconscious)
            {
                TickAboard(deltaTime);
                return;
            }
            if (Role != CrewRole.Commando)
            {
                TickSupportingCrew(deltaTime);
                return;
            }
            var nearby=activationRange>0?game.FindTarget(game.TankAimPoint,EnemyKind.Ground,activationRange):null;
            if(nearby==null)
            {
                if (Active)
                {
                    Stow();
                    FollowSeat(0);
                }
                else TickAboard(deltaTime);
                return;
            }
            if(!Active)
            {
                regenerationTime = 0;
                transform.position = ExitPoint();
                ShowVisual();
                SetStationArtwork(false);
                artwork?.SetPhysicsReference(null);
                artwork?.ResetVisual();
                SetTargetable(true);
            }
            cooldown=Mathf.Max(0,cooldown-deltaTime);
            if(weapon.range<=0){TickMelee(deltaTime);return;}
            var target=weapon.range>0?game.FindTarget(AimPoint,EnemyKind.Ground,weapon.range):null;
            Velocity=Vector2.zero;
            if(target==null)
            {
                float previous=transform.position.x;
                var position=transform.position;position.x=Mathf.MoveTowards(position.x,nearby.transform.position.x,defense.advanceSpeed*deltaTime);
                transform.position=position;
                if(deltaTime>0)Velocity=Vector2.right*((position.x-previous)/deltaTime);
                target=weapon.range>0?game.FindTarget(AimPoint,EnemyKind.Ground,weapon.range):null;
            }
            if(artwork!=null)
            {
                artwork.Face((target!=null?target:nearby).transform.position.x-transform.position.x);
                artwork.Locomotion(Velocity.sqrMagnitude>.00001f);artwork.Advance(deltaTime);
            }
            if(target==null||cooldown>0||weapon.projectileSpeed<=0||weapon.reloadTime<=0)return;
            if(!Ballistics2D.TryIntercept(AimPoint,target.transform.position,target.Velocity,weapon.projectileSpeed,0,out var launch))return;
            portrait.flipX=launch.x<0;artwork?.Attack();
            var shell=new GameObject("Commando round").AddComponent<Projectile2D>();
            shell.Initialize(AimPoint,launch,weapon.projectileSpeed,weapon.damage,new Color(.4f,1,.6f),false,weapon.range,0,ProjectileTeam.Player,inheritCannonArt:false,combatStats:weapon);
            cooldown=weapon.EffectiveReloadTime;ShotsFired++;
        }

        void InitializeCrew()
        {
            initialized = true;
            member = game.Crew.Get(Role);
            var source = Role == CrewRole.Commando ? member : game.CommandoStatsSource();
            if (member == null || source == null) { defeated = true; return; }

            var state = game.Progression.Get(source);
            defense = source.CommandoHull(state.Level, state.Stars);
            weapon = source.CommandoWeapon(state.Level, state.Stars);
            activationRange = source.stats.activationRange.AtLevel(state.Level, state.Stars);
            healthRegeneration = source.stats.healthRegeneration.AtLevel(state.Level, state.Stars);
            if (Role != CrewRole.Commando)
            {
                defense.maxHull *= .5f;
                healthRegeneration *= .5f;
                defense.evasion *= .5f;
                defense.armor *= .5f;
                defense.criticalChanceReduction *= .5f;
                defense.criticalDamageReduction *= .5f;
                weapon.damage *= .5f;
                weapon.damageBonus *= .5f;
                weapon.accuracy *= .5f;
                weapon.reloadSpeed *= .5f;
                weapon.criticalChance *= .5f;
                weapon.criticalDamage *= .5f;
                weapon.armorPenetration *= .5f;
                weapon.range *= .5f;
                weapon.reloadTime *= .5f;
                weapon.projectileSpeed *= .5f;
            }
            Health = defense.maxHull;
            defeated = Health <= 0;
        }

        void ShowVisual()
        {
            if (visual.activeSelf) return;
            if (artwork != null && artworkPrefab != member.commandoPrefab)
            {
                artwork.gameObject.SetActive(false);
                Destroy(artwork.gameObject);
                artwork = null;
            }
            if (artwork == null && member.commandoPrefab != null)
            {
                artwork = Instantiate(member.commandoPrefab, visual.transform, false);
                artworkPrefab = member.commandoPrefab;
            }
            artwork?.SetCombatMaterial(member.combatMaterial);
            SetStationArtwork(false);
            visual.SetActive(true);
            artwork?.ResetVisual();
        }

        void SetStationArtwork(bool aboard)
        {
            bool useEquippedSprite = aboard && !IsUnconscious && member.equippedSprite != null;
            artwork?.SetVisible(!useEquippedSprite);
            portrait.enabled = useEquippedSprite || artwork == null;
            if (!portrait.enabled) return;

            portrait.sprite = useEquippedSprite ? member.equippedSprite : member.portrait;
            portrait.flipX = false;
            Vector2 size = portrait.sprite != null ? portrait.sprite.bounds.size : Vector2.one;
            portrait.transform.localScale = Vector3.one
                * ((useEquippedSprite ? SeatedSpriteSize : .85f) / Mathf.Max(size.x, size.y));
            Vector3 seatOffset = Vector3.zero;
            if (useEquippedSprite)
            {
                switch (Role)
                {
                    case CrewRole.Driver: seatOffset = new Vector3(0, -.46f, 0); break;
                    case CrewRole.Commando: seatOffset = new Vector3(-.13f, -.83f, 0); break;
                }
            }
            portrait.transform.localPosition = seatOffset;
            // Draw lower-deck crew above the interior fittings and beneath the hull frame.
            portrait.sortingOrder = seatOffset == Vector3.zero ? CrewSortingOrder : LowerDeckCrewSortingOrder;
        }

        void SetTargetable(bool targetable)
        {
            hitbox.enabled = targetable;
            SetHealthBarVisible(targetable);
        }

        void SetHealthBarVisible(bool visible)
        {
            if (visible && healthBar == null)
            {
                healthBar = Instantiate(game.EnemyDatabase.healthBarPrefab, transform, false);
                healthBar.ConfigureCommando(.9f, new Color(.34f, .91f, 1), game.TankCamera);
            }
            if (healthBar == null) return;
            if (visible) healthBar.SetHealth(Health / MaxHealth);
            if (healthBar.gameObject.activeSelf != visible) healthBar.gameObject.SetActive(visible);
        }

        Vector3 SeatPoint()
        {
            Vector2 seat;
            switch (Role)
            {
                case CrewRole.Gunner: seat = new Vector2(.60f, .49f); break;
                case CrewRole.SubGunner: seat = new Vector2(.53f, .90f); break;
                case CrewRole.WeaponSpecialist: seat = new Vector2(.22f, .44f); break;
                case CrewRole.Driver: seat = new Vector2(.34f, .16f); break;
                default: seat = new Vector2(.85f, .26f); break;
            }
            return game.HullHitbox.AnchorPoint(seat);
        }

        Vector3 ExitPoint()
        {
            float front = game.HullHitbox.AnchorPoint(new Vector2(1f, .5f)).x;
            float spacing = Role == CrewRole.Commando ? .6f : .9f + .19f * (int)Role;
            return new Vector3(front + spacing, BattleGame.GroundHeight + .55f, 0);
        }

        void FollowSeat(float deltaTime)
        {
            transform.position = SeatPoint();
            ShowVisual();
            SetStationArtwork(true);
            // Seated animation physics follow motion relative to the hull.
            artwork?.SetPhysicsReference(game.HullHitbox.transform);
            artwork?.Face(1);
            if (!IsUnconscious) artwork?.Sit();
            artwork?.Advance(deltaTime);
            SetHealthBarVisible(healthRegeneration > 0 && Health < MaxHealth);
        }

        void TickAboard(float deltaTime)
        {
            if (Health < MaxHealth && healthRegeneration > 0)
            {
                regenerationTime += deltaTime;
                int seconds = Mathf.FloorToInt(regenerationTime);
                regenerationTime -= seconds;
                Health = Mathf.Min(MaxHealth, Health + healthRegeneration * seconds);
                if (Health >= MaxHealth)
                {
                    regenerationTime = 0;
                    if (IsUnconscious)
                    {
                        IsUnconscious = false;
                        artwork?.ResetVisual();
                    }
                }
            }
            FollowSeat(deltaTime);
        }

        void BeginJump(CrewMotion direction)
        {
            motion = direction;
            jumpStart = transform.position;
            jumpTime = 0;
            Velocity = Vector2.zero;
            SetStationArtwork(false);
            artwork?.SetPhysicsReference(null);
            artwork?.CancelAttack();
            artwork?.Locomotion(true);
            SetTargetable(false);
        }

        void TickSupportingCrew(float deltaTime)
        {
            bool engage = game.HasGroundThreat(game.TankAimPoint, activationRange) && game.CanDeployCrew(Role);
            if (motion == CrewMotion.Aboard)
            {
                if (engage)
                {
                    FollowSeat(0);
                    regenerationTime = 0;
                    BeginJump(CrewMotion.JumpOut);
                }
                else TickAboard(deltaTime);
                return;
            }

            if (motion == CrewMotion.JumpOut || motion == CrewMotion.JumpIn)
            {
                bool boarding = motion == CrewMotion.JumpIn;
                var destination = boarding ? SeatPoint() : ExitPoint();
                jumpTime += deltaTime;
                float progress = Mathf.Clamp01(jumpTime / JumpDuration);
                transform.position = Vector3.Lerp(jumpStart, destination, progress)
                    + Vector3.up * (2f * progress * (1f - progress));
                artwork?.Face(destination.x - jumpStart.x);
                artwork?.Advance(deltaTime);
                if (progress >= 1f)
                {
                    if (boarding)
                    {
                        motion = CrewMotion.Aboard;
                        FollowSeat(0);
                    }
                    else if (engage)
                    {
                        motion = CrewMotion.Fighting;
                        SetTargetable(true);
                    }
                    else BeginJump(CrewMotion.JumpIn);
                }
                return;
            }

            if (engage)
            {
                motion = CrewMotion.Fighting;
                cooldown = Mathf.Max(0, cooldown - deltaTime);
                TickMelee(deltaTime);
                return;
            }

            if (motion == CrewMotion.Fighting)
            {
                artwork?.ResetVisual();
                motion = CrewMotion.Returning;
            }
            var exit = ExitPoint();
            float previous = transform.position.x;
            transform.position = Vector3.MoveTowards(transform.position, exit, defense.advanceSpeed * deltaTime);
            Velocity = deltaTime > 0 ? Vector2.right * ((transform.position.x - previous) / deltaTime) : Vector2.zero;
            artwork?.Face(exit.x - previous);
            artwork?.Locomotion(Velocity.sqrMagnitude > .00001f);
            artwork?.Advance(deltaTime);
            if (transform.position == exit) BeginJump(CrewMotion.JumpIn);
        }

        void TickMelee(float deltaTime)
        {
            var target=game.FindMeleeTarget(hitbox,game.TankAimPoint,activationRange);
            var body=target!=null?target.GetComponent<Collider2D>():null;
            bool attacking=artwork!=null&&artwork.IsAttacking;
            Velocity=body!=null&&!attacking?CommandoMelee.Approach(hitbox,body,defense.advanceSpeed,deltaTime):Vector2.zero;
            if(artwork!=null)
            {
                if(body!=null&&!attacking)artwork.Face(body.bounds.center.x-transform.position.x);
                artwork.Locomotion(Velocity.sqrMagnitude>.00001f);artwork.Advance(deltaTime);
            }
            if(target==null||cooldown>0||(artwork!=null&&artwork.IsAttacking)||!CommandoMelee.InReach(hitbox,body))return;
            var attack=new DamageAttack(weapon.damage,weapon);MeleeAttacks++;cooldown=Mathf.Max(.02f,weapon.EffectiveReloadTime);
            System.Action impact=()=>{
                if(game.Running&&Active&&target!=null&&target.Alive&&CommandoMelee.InReach(hitbox,body))target.ReceiveAttack(attack);
            };
            if(artwork!=null)artwork.Attack(impact);else impact();
        }
        public DamageResult ReceiveAttack(DamageAttack attack,float areaMultiplier=1)
        {
            if(!Active||Health<=0)return new DamageResult(false,false,0,attack.DamageType);
            var result=CombatDamage.Resolve(attack,defense,areaMultiplier);Health=Mathf.Max(0,Health-result.Damage);
            if(!result.Hit)game.ShowDamageNumber(AimPoint+Vector2.up*.8f,0,true,true);
            if(healthBar!=null)healthBar.SetHealth(MaxHealth>0?Health/MaxHealth:0);
            if(Health<=0)
            {
                SetTargetable(false);
                Velocity = Vector2.zero;
                motion = CrewMotion.Aboard;
                IsUnconscious = true;
                regenerationTime = 0;
                FollowSeat(0);
                artwork?.Die();
            }
            return result;
        }
        public void ReceiveBlast(Vector2 position,DamageAttack attack,float fullRadius,float halfRadius)
        {
            if(!Active)return;
            float distance=Vector2.Distance(position,hitbox.ClosestPoint(position));
            if(distance<=halfRadius)ReceiveAttack(attack,distance<=fullRadius?1:.5f);
        }
    }
}
