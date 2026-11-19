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
        float cooldown,activationRange,deathTime;
        bool initialized,defeated;
        public bool Active=>visual!=null&&visual.activeSelf&&hitbox.enabled;
        public float Health { get; private set; }
        public float MaxHealth=>defense?.maxHull??0;
        public GunStats Weapon=>weapon;
        public Vector2 Velocity { get; private set; }
        public int ShotsFired { get; private set; }
        public int MeleeAttacks { get; private set; }
        public Vector2 AimPoint=>transform.position;
        public void Configure(BattleGame owner)
        {
            game=owner;
            visual=new GameObject("Commando portrait and integrated weapon");visual.transform.SetParent(transform,false);
            portrait=visual.AddComponent<SpriteRenderer>();portrait.sortingOrder=35;
            hitbox=gameObject.AddComponent<BoxCollider2D>();hitbox.size=new Vector2(.65f,.9f);hitbox.isTrigger=true;
            ResetForRun();
        }
        void Stow(){artwork?.CancelAttack();visual.SetActive(false);hitbox.enabled=false;if(healthBar!=null)healthBar.gameObject.SetActive(false);Velocity=Vector2.zero;}
        public void ResetForRun()
        {
            initialized=defeated=false;Health=0;cooldown=0;ShotsFired=MeleeAttacks=0;
            if(visual!=null)Stow();
        }
        void Update()=>Tick(Time.deltaTime);
        public void Tick(float deltaTime)
        {
            if(game==null)return;
            if(!game.Deployed||game.Hull<=0){Stow();return;}
            if(!game.Running){Velocity=Vector2.zero;return;}
            if(!initialized)
            {
                initialized=true;member=game.Crew.Get(CrewRole.Commando);
                if(member==null){defeated=true;return;}
                var state=game.Progression.Get(member);int level=state.Level;defense=member.CommandoHull(level,state.Stars);weapon=member.CommandoWeapon(level,state.Stars);
                activationRange=member.stats.activationRange.AtLevel(level,state.Stars);Health=defense.maxHull;
                defeated=Health<=0;
            }
            if(defeated)
            {
                if(visual.activeSelf){artwork?.Advance(deltaTime);deathTime-=deltaTime;if(deathTime<=0)Stow();}
                return;
            }
            var nearby=activationRange>0?game.FindTarget(game.TankAimPoint,EnemyKind.Ground,activationRange):null;
            if(nearby==null){Stow();return;}
            if(!Active)
            {
                float front=game.HullHitbox.GetComponent<Collider2D>().bounds.max.x;
                transform.position=new Vector3(front+.6f,BattleGame.GroundHeight+.55f,0);
                if(artwork!=null&&artworkPrefab!=member.commandoPrefab){artwork.gameObject.SetActive(false);Destroy(artwork.gameObject);artwork=null;}
                visual.transform.localScale=Vector3.one;
                if(artwork==null&&member.commandoPrefab!=null){artwork=Instantiate(member.commandoPrefab,visual.transform,false);artworkPrefab=member.commandoPrefab;}
                portrait.enabled=artwork==null;
                if(artwork!=null)artwork.ResetVisual();
                else
                {
                    portrait.sprite=member.portrait;portrait.color=Color.white;
                    if(portrait.sprite!=null)visual.transform.localScale=Vector3.one*(.85f/Mathf.Max(portrait.sprite.bounds.size.x,portrait.sprite.bounds.size.y));
                }
                visual.SetActive(true);hitbox.enabled=true;
                if(healthBar==null)healthBar=Instantiate(game.EnemyDatabase.healthBarPrefab,transform,false);
                healthBar.ConfigureCommando(.9f,new Color(.34f,.91f,1),game.TankCamera);
                healthBar.SetHealth(MaxHealth>0?Health/MaxHealth:0);healthBar.gameObject.SetActive(true);
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
                if(healthBar!=null)healthBar.gameObject.SetActive(false);
                defeated=true;hitbox.enabled=false;Velocity=Vector2.zero;
                deathTime=artwork!=null?artwork.Die():0;
                if(deathTime<=0)Stow();
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
