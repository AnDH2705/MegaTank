using UnityEngine;

namespace MegaTanks
{
    public enum EnemyKind { Ground, Flyer }

    public sealed class EnemyEntity : MonoBehaviour
    {
        public EnemyKind Kind { get; private set; }
        public int WaveNumber { get; private set; }
        public int Difficulty { get; private set; }
        public EnemyDefinition Data { get; private set; }
        public EnemyWeapon Weapon { get; private set; }
        public EnemyVehicleArtwork Artwork { get; private set; }
        public BossTank Boss { get; private set; }
        public BossTank CommandoOwner { get; set; }
        public CommandoArtwork CommandoArtwork { get; private set; }
        Vector2 commandoVelocity;
        float corpseTime;
        public int ScrapsReward => Data.baseScraps;
        public float Health { get; private set; }
        public float Speed { get; private set; }
        public bool Alive => Health > 0;
        public bool InWeaponRange => Boss != null ? Boss.AllWeaponsInRange : Vector2.Distance(transform.position, BattleGame.Instance.TankAimPoint) <= Data.weapon.range + .001f;
        public float ApproachDirection { get; private set; }
        public Vector2 Velocity => CommandoOwner!=null ? (Alive&&BattleGame.Instance.Running?commandoVelocity:Vector2.zero) : Alive && BattleGame.Instance.Running && (StoppingX-transform.position.x)*ApproachDirection > .0001f ? Vector2.right * (ApproachDirection*Speed) : Vector2.zero;
        public float AdvanceInsideRange { get; private set; }
        float stoppingX;
        public float StoppingX
        {
            get => Boss != null ? transform.position.x + ApproachDirection * Boss.AdvanceDistanceNeeded : stoppingX;
            private set => stoppingX = value;
        }
        float maxHealth;
        SpriteRenderer body;
        EnemyHealthBarUI healthBar;
        Color color;
        float hitUntil;

        public void Initialize(EnemyKind kind, int wave)
        {
            Initialize(BattleGame.Instance.EnemyDatabase.Get(kind).AtDifficulty(BattleGame.Instance.BattleDifficulty), wave, BattleGame.Instance.BattleDifficulty);
        }

        public void Initialize(EnemyDefinition definition, int wave, int difficulty=0)
        {
            definition=definition.Copy();Data = definition;
            Kind = definition.kind;
            WaveNumber = wave;
            Difficulty = difficulty;
            gameObject.name = definition.id;
            ApproachDirection = transform.position.x < BattleGame.Instance.TankAimPoint.x ? 1 : -1;
            maxHealth = definition.baseHealth;
            Health = maxHealth;
            Speed = definition.baseSpeed;
            if(!Alive)return;
            color = definition.color;
            float healthY = .51f, artworkWidth=1.4f;
            Boss = GetComponent<BossTank>();
            if (Boss != null)
            {
                var bounds = Boss.BuildArtwork();
                var collider = gameObject.AddComponent<BoxCollider2D>();
                collider.offset = bounds.center; collider.size = bounds.size; collider.isTrigger = true;
                healthY = bounds.max.y; artworkWidth = bounds.size.x;
            }
            else if(definition.commandoPrefab!=null)
            {
                CommandoArtwork=Instantiate(definition.commandoPrefab,transform,false);CommandoArtwork.ResetVisual();CommandoArtwork.Face(ApproachDirection);
                var collider=gameObject.AddComponent<BoxCollider2D>();collider.size=new Vector2(.65f,.9f);collider.isTrigger=true;
                healthY=.9f;artworkWidth=.9f;
            }
            else if (definition.artworkPrefab != null)
            {
                Artwork = Instantiate(definition.artworkPrefab, transform);
                Artwork.Attach(this, definition.ScaledArtworkWidth);
                var bounds = Artwork.OwnerLocalBounds;
                var collider = gameObject.AddComponent<BoxCollider2D>();
                collider.offset = bounds.center; collider.size = bounds.size; collider.isTrigger = true;
                healthY = bounds.max.y;
                artworkWidth = bounds.size.x;
            }
            else
            {
                body = ShapeFactory.Create(Kind.ToString(), transform, Vector2.zero, Vector2.one * .8f, color, Kind == EnemyKind.Ground, 10);
                if (Kind == EnemyKind.Flyer) body.transform.localRotation = Quaternion.Euler(0, 0, 45);
                ShapeFactory.Create("Core", transform, Vector2.zero, Vector2.one * .22f, new Color(.15f, .1f, .18f), true, 11);
                var collider = gameObject.AddComponent<CircleCollider2D>(); collider.radius = .46f; collider.isTrigger = true;
            }
            float minimumAdvance = Mathf.Max(0, Mathf.Min(definition.advanceInsideRange.x, definition.advanceInsideRange.y));
            float maximumAdvance = Mathf.Max(minimumAdvance, Mathf.Max(definition.advanceInsideRange.x, definition.advanceInsideRange.y));
            AdvanceInsideRange = Random.Range(minimumAdvance, maximumAdvance);
            Vector2 target = BattleGame.Instance.TankAimPoint;
            float height = transform.position.y - target.y;
            float rangeEdgeX = target.x - ApproachDirection*Mathf.Sqrt(Mathf.Max(0, definition.weapon.range * definition.weapon.range - height * height));
            // Choose once per enemy so the destination stays stable while the tank idles.
            StoppingX = ApproachDirection>0 ? Mathf.Min(target.x,rangeEdgeX+AdvanceInsideRange) : Mathf.Max(target.x,rangeEdgeX-AdvanceInsideRange);
            var healthPrefab=BattleGame.Instance.EnemyDatabase.healthBarPrefab;
            if(healthPrefab==null)throw new System.InvalidOperationException("Assign EnemyHealthBar.prefab on the Enemy Data Manager.");
            healthBar=Instantiate(healthPrefab,transform,false);
            healthBar.Configure(healthY,artworkWidth,color,BattleGame.Instance.TankCamera);
            if (Boss == null)
            {
                Weapon = gameObject.AddComponent<EnemyWeapon>();
                Weapon.Configure(this, definition.weapon);
            }
        }

        void Update()
        {
            if (!BattleGame.Instance.Running) return;
            if(!Alive)
            {
                if(CommandoArtwork!=null){CommandoArtwork.Advance(Time.deltaTime);corpseTime-=Time.deltaTime;if(corpseTime<=0)Destroy(gameObject);}
                return;
            }
            if(CommandoOwner!=null)
            {
                var game=BattleGame.Instance;Vector2 target=game.TankAimPoint;
                if(game.Commando!=null&&game.Commando.Active&&Vector2.Distance(transform.position,game.Commando.AimPoint)<Vector2.Distance(transform.position,target))target=game.Commando.AimPoint;
                float direction=target.x-transform.position.x;ApproachDirection=direction<0?-1:1;commandoVelocity=Vector2.zero;
                if(Vector2.Distance(transform.position,target)>Data.weapon.range)
                {
                    Vector3 position=transform.position;position.x=Mathf.MoveTowards(position.x,target.x,Speed*Time.deltaTime);
                    if(Time.deltaTime>0)commandoVelocity=Vector2.right*((position.x-transform.position.x)/Time.deltaTime);transform.position=position;
                }
            }
            else if ((StoppingX-transform.position.x)*ApproachDirection > 0)
            {
                Vector3 position = transform.position;
                position.x = Mathf.MoveTowards(position.x,StoppingX,Speed*Time.deltaTime);
                transform.position = position;
            }
            if (Boss != null) Boss.SetHitFlash(Time.time < hitUntil);
            else if (Artwork != null) Artwork.SetHitFlash(Time.time < hitUntil);
            else if(CommandoArtwork!=null){CommandoArtwork.Face(ApproachDirection);CommandoArtwork.Locomotion(Velocity.sqrMagnitude>.00001f);CommandoArtwork.Advance(Time.deltaTime);}
            else body.color = Time.time < hitUntil ? Color.white : color;
        }

        public DamageResult ReceiveAttack(DamageAttack attack,float areaMultiplier=1)
        {
            if(!Alive)return new DamageResult(false,false,0,attack.DamageType);
            var result=CombatDamage.Resolve(attack,Data,areaMultiplier);
            Damage(result.Damage);return result;
        }

        // Direct HP loss remains available for scripted effects and debug helpers.
        public void Damage(float damage)
        {
            if (!Alive || damage <= 0) return;
            Vector2 numberPosition = transform.position;
            numberPosition.y += Artwork != null ? Artwork.OwnerLocalBounds.max.y + .3f : .8f;
            BattleGame.Instance.ShowDamageNumber(numberPosition, Mathf.Min(Health, damage));
            Health = Mathf.Max(0, Health - damage);
            hitUntil = Time.time + .07f;
            healthBar.SetHealth(Health / maxHealth);
            if (Alive) return;
            GetComponent<Collider2D>().enabled = false;
            Boss?.RecallCommando();
            if(CommandoOwner!=null)CommandoOwner.OnCommandoDefeated(this);
            BattleGame.Instance.RemoveEnemy(this, true);
            if(CommandoArtwork!=null){healthBar.gameObject.SetActive(false);corpseTime=CommandoArtwork.Die();return;}
            var game=BattleGame.Instance;
            if(game.Vfx.Catalog!=null)
            {
                Vector2 origin=transform.position;
                if(Kind==EnemyKind.Ground)origin.y=BattleGame.GroundHeight;
                game.Vfx.Blast(origin,false,Kind==EnemyKind.Ground,Boss!=null?1.5f:1);
            }
            else game.Flash(transform.position, color, .7f);
            Destroy(gameObject);
        }
    }
}
