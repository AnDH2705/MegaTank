using System.Collections.Generic;
using UnityEngine;

namespace MegaTanks
{
    public enum TankStance { Defensive, Aggressive }

    [DefaultExecutionOrder(-100)]
    public sealed class BattleGame : MonoBehaviour
    {
        public static BattleGame Instance { get; private set; }
        [Header("Player configuration")]
        [SerializeField] TankDefinition tankDefinition;
        [SerializeField] WeaponLoadout startingLoadout;
        public TankDefinition TankProfile => tankDefinition;
        public WeaponLoadout StartingLoadout => startingLoadout;
        TankStats tankStats = new TankStats();
        public TankStats BaseTankStats => tankStats;
        [Header("Cards and shop")]
        [SerializeField] CardCatalog cardCatalog;
        public CardRewardSystem Rewards { get; private set; }
        [Header("Stage progression")]
        [SerializeField] StageDefinition stageDefinition;
        public StageProgression Stage { get; private set; }
        [Header("Battle backgrounds")]
        [SerializeField] BattleBackgroundCatalog backgroundCatalog;
        public BattleBackgroundController Backgrounds { get; private set; }
        public MortarWeapon Mortar { get; private set; }
        public float TankAdvanceSpeed => tankStats.advanceSpeed;
        public TankStance Stance { get; private set; } = TankStance.Aggressive;
        Vector2 tankVelocity;
        Vector3 tankStartPosition;
        public Vector2 TankVelocity => Running && Stance == TankStance.Aggressive ? tankVelocity : Vector2.zero;
        public bool AllWeaponsInRange => FindTarget(MachineGun.transform.position, EnemyKind.Flyer, MachineGun.MaxRange) != null
            && FindTarget(MainGun.transform.position, EnemyKind.Ground, MainGun.MaxRange) != null
            && FindMortarTarget(Artwork.Mortar.Muzzle.position, Mortar.Stats.range, Mortar.Stats.halfDamageRadius) != null;
        // Enemy cannon art and legacy projectile helpers still share the equipped cannon's visual.
        public Sprite CannonProjectileSprite => MainGun != null ? MainGun.Stats.projectileSprite : startingLoadout.cannon.stats.projectileSprite;
        public float CannonProjectileWidth => MainGun != null ? MainGun.Stats.projectileWidth : startingLoadout.cannon.stats.projectileWidth;
        [Header("Enemy approach")]
        [SerializeField] EnemyDataManager enemyDataManager;
        public EnemyDataManager EnemyDatabase => enemyDataManager;
        public int BattleDifficulty { get; private set; }
        readonly Dictionary<string,EnemyDefinition> battleEnemyStats=new Dictionary<string,EnemyDefinition>();
        public TankHitbox HullHitbox { get; private set; }
        public Vector2 TankAimPoint => HullHitbox.AimPoint;
        [SerializeField, Min(1), Tooltip("Horizontal distance from the tank anchor to the front of each wave.")] float enemySpawnDistance = 90;
        public float EnemySpawnDistance => enemySpawnDistance;
        [SerializeField, Tooltip("Flying enemy lanes stay below the top battle HUD.")] float flyingEnemyHeight=2.1f;
        [SerializeField, Min(0)] float flyingEnemyLaneSpacing=.45f;
        // Enemies spawn ahead of the right-facing tank; Aggressive advances toward them.
        public float EnemySpawnDirection => 1;
        public Camera TankCamera => battleCamera;
        public BattleCameraPan CameraPan { get; private set; }
        [SerializeField, Range(.1f,.4f), Tooltip("Tank spawn position across the battle view at stage start. 0 is the left edge.")]
        float tankStartViewportX=.18f;
        public float TankStartViewportX => tankStartViewportX;
        [Header("Tank artwork")]
        [SerializeField] TankArtwork tankArtworkPrefab;
        [SerializeField, Tooltip("Default tank anchor, including the approved 2-unit downward adjustment.")]
        Vector3 tankSpawnPosition=new Vector3(-7.3f,-4.483333f,0);
        public Vector3 TankSpawnPosition => tankSpawnPosition;
        float tankScale => tankStats.scale;
        public const float ReferenceWidth = 1920;
        public const float ReferenceHeight = 1080;
        public const float GroundHeight = -5.05f;
        // Backdrop placement stays fixed while gameplay entities use the lowered ground lane.
        public const float SceneryGroundHeight = -3.05f;
        public sealed class DamageNumber
        {
            public Vector2 Position { get; internal set; }
            public float Amount { get; internal set; }
            public float Age { get; internal set; }
            public bool IsTank { get; internal set; }
            public string DisplayText => "-" + Mathf.FloorToInt(Amount);
        }
        readonly List<DamageNumber> damageNumbers = new List<DamageNumber>();
        public IReadOnlyList<DamageNumber> DamageNumbers => damageNumbers;
        public const float DamageNumberLifetime = 1.1f;
        public TankArtwork Artwork { get; private set; }
        public AutoTurret MachineGun { get; private set; }
        public AutoTurret MainGun { get; private set; }
        public bool Running => deployed && !paused && Hull > 0 && (Rewards == null || !Rewards.IsOpen)
            && (Stage == null || Stage.Mode == StageMode.Combat);
        float BaseHull => tankStats.maxHull;
        public float Hull { get; private set; } = 100;
        public float MaxHull { get; private set; } = 100;
        public int Score { get; private set; }
        public int Wave { get; private set; }
        public int EnemyCount => enemies.Count;
        public bool Paused => paused;
        public bool Deployed => deployed;
        public PlayerEquipment Equipment { get; private set; }
        public CanvasGameUI UI { get; private set; }
        [SerializeField] FrontendCatalog frontendCatalog;
        public FrontendCatalog Frontend => frontendCatalog;
        readonly List<EnemyEntity> enemies = new List<EnemyEntity>();
        readonly List<FlashEffect> effects = new List<FlashEffect>();
        Transform scenery;
        Camera battleCamera;
        bool deployed, paused, keepHomeBackdrop;
        float lastVibration=-1;
        int best;

        readonly Color navy = new Color(.035f, .065f, .105f);

        void Awake()
        {
            Instance = this;
            Application.targetFrameRate = 60;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            best = PlayerPrefs.GetInt("MegaTanks.Best2D", 0);
            Physics2D.queriesHitTriggers = true;
            BuildScene();
            Equipment = new PlayerEquipment(startingLoadout, frontendCatalog);
            RefreshEquipmentArtwork();
            UI = CanvasGameUI.Create(this);
        }

        public void RefreshEquipmentArtwork()
        {
            Artwork.EquipMachineGunVisual(Equipment.MachineGun);
            MachineGun.BindArtwork(Artwork.MachineGun);
        }

        void BuildScene()
        {
            if (tankDefinition == null || startingLoadout == null) throw new System.InvalidOperationException("Player profiles are missing. Run MegaTanks > Migrate Player Stats To Profiles.");
            startingLoadout.Validate();
            tankStats = tankDefinition.stats.Copy();
            Hull = MaxHull = BaseHull;
            battleCamera = new GameObject("2D Battle Camera").AddComponent<Camera>();
            battleCamera.tag = "MainCamera";
            battleCamera.orthographic = true;
            battleCamera.transform.position = new Vector3(0, 0, -10);
            battleCamera.clearFlags = CameraClearFlags.SolidColor;
            battleCamera.backgroundColor = navy;
            scenery = new GameObject("2D Tank and Battlefield").transform;
            if (tankArtworkPrefab == null) throw new System.InvalidOperationException("Tank artwork is missing. Run MegaTanks > Import Tank Artwork.");
            Artwork = Instantiate(tankArtworkPrefab, scenery);
            Artwork.transform.localScale = Vector3.one * tankScale;
            Artwork.transform.localPosition = tankSpawnPosition;
            tankStartPosition = Artwork.transform.position;
            CameraPan=battleCamera.gameObject.AddComponent<BattleCameraPan>();
            CameraPan.Configure(this,tankStartPosition.x,tankStartPosition.x+EnemySpawnDirection*enemySpawnDistance);
            foreach (var renderer in Artwork.GetComponentsInChildren<SpriteRenderer>())
            {
                if (renderer.name != "32_front_hull_frame") continue;
                var hull = new GameObject("Tank Hull Hitbox");
                hull.transform.SetParent(renderer.transform.parent, false);
                hull.transform.position = renderer.bounds.center;
                Vector3 scale = hull.transform.lossyScale;
                HullHitbox = hull.AddComponent<TankHitbox>();
                HullHitbox.Configure(new Vector2(renderer.bounds.size.x / scale.x, renderer.bounds.size.y / scale.y));
                break;
            }
            if (HullHitbox == null || enemyDataManager == null) throw new System.InvalidOperationException("Tank hitbox or enemy data manager is missing.");
            MachineGun = CreateTurret(startingLoadout.machineGun, Artwork.MachineGun);
            MainGun = CreateTurret(startingLoadout.cannon, Artwork.MainGun);
            if (Artwork.Mortar == null) throw new System.InvalidOperationException("Mortar artwork is missing. Run MegaTanks > Import Mortar Artwork.");
            Mortar = Artwork.Mortar.gameObject.AddComponent<MortarWeapon>();
            Mortar.Configure(Artwork.Mortar, Equipment != null ? Equipment.Mortar : startingLoadout.mortar);
            Rewards = gameObject.AddComponent<CardRewardSystem>();
            Rewards.Configure(cardCatalog);
            if (stageDefinition == null) throw new System.InvalidOperationException("Stage definition is missing.");
            Stage = gameObject.AddComponent<StageProgression>();
            Stage.Configure(this, stageDefinition, cardCatalog);
            FitCamera();
            Backgrounds = gameObject.AddComponent<BattleBackgroundController>();
            Backgrounds.Configure(backgroundCatalog, battleCamera);
            Backgrounds.SelectRandom();
        }

        AutoTurret CreateTurret(GunDefinition definition, GunArtwork visual)
        {
            var turret = visual.gameObject.AddComponent<AutoTurret>();
            turret.Configure(definition);
            turret.BindArtwork(visual);
            return turret;
        }

        void FitCamera()
        {
            // Home and combat share the same view; Canvas controls overlay the live scene.
            Rect safe = Screen.safeArea;
            float height=Screen.height-safe.yMin;
            battleCamera.rect = new Rect(safe.xMin / Screen.width, safe.yMin / Screen.height, safe.width / Screen.width, height / Screen.height);
            float aspect = safe.width / Mathf.Max(1, height);
            battleCamera.orthographicSize = Mathf.Max(5.8f, 4.6f / aspect);
            CameraPan.UpdateFraming(2*battleCamera.orthographicSize*aspect);
            battleCamera.transform.position = new Vector3(CameraPan.X, -.05f, -10);
        }

        public void StartBattle()
        {
            keepHomeBackdrop = !deployed;
            Time.timeScale = 1;
            foreach (EnemyEntity enemy in enemies) if (enemy != null) Destroy(enemy.gameObject);
            enemies.Clear();
            foreach (Projectile2D projectile in FindObjectsOfType<Projectile2D>()) Destroy(projectile.gameObject);
            foreach (FlashEffect effect in effects) if (effect != null) Destroy(effect.gameObject);
            effects.Clear();
            damageNumbers.Clear();
            startingLoadout.Validate();
            tankStats = tankDefinition.stats.Copy();
            Artwork.transform.localScale = Vector3.one * tankScale;
            tankStartPosition = tankSpawnPosition;
            RefreshEquipmentArtwork();
            MachineGun.Configure(Equipment != null ? Equipment.MachineGun : startingLoadout.machineGun);
            MainGun.Configure(Equipment != null ? Equipment.Cannon : startingLoadout.cannon);
            Mortar.Configure(Artwork.Mortar, Equipment != null ? Equipment.Mortar : startingLoadout.mortar);
            Artwork.ResetVisual();
            Artwork.transform.position = tankStartPosition;
            Stance = TankStance.Aggressive;
            tankVelocity = Vector2.zero;
            CameraPan.ResetToSpawn(tankStartPosition.x,tankStartPosition.x+EnemySpawnDirection*enemySpawnDistance);
            FitCamera();
            MaxHull = BaseHull;
            Hull = MaxHull;
            Rewards.ResetRun();
            Score = Wave = 0;
            paused = false;
            deployed = true;
            Stage.BeginRun();
        }

        public EnemyEntity SpawnEnemy(EnemyKind kind, Vector2 position)
        {
            return SpawnEnemy(enemyDataManager.Get(kind).id, position);
        }

        public EnemyEntity SpawnEnemy(string definitionId, Vector2 position)
        {
            var entity = new GameObject(definitionId).AddComponent<EnemyEntity>();
            entity.transform.position = position;
            if(!battleEnemyStats.TryGetValue(definitionId,out var stats))stats=enemyDataManager.Get(definitionId).AtDifficulty(BattleDifficulty);
            entity.Initialize(stats, Wave, BattleDifficulty);
            if(entity.Alive)enemies.Add(entity);else Destroy(entity.gameObject);
            return entity;
        }

        void SpawnWave()
        {
            Wave++;
            int ground = Mathf.Min(3 + Wave, 12);
            int flyers = Mathf.Min(1 + Wave, 8);
            float spawnX = Artwork.transform.position.x + EnemySpawnDirection*enemySpawnDistance;
            CameraPan.SetEnemySpawn(spawnX);
            float groundSpacing = Mathf.Max(1.7f, enemyDataManager.Get(EnemyKind.Ground).ScaledArtworkWidth + .3f);
            float flyerSpacing = Mathf.Max(2, enemyDataManager.Get(EnemyKind.Flyer).ScaledArtworkWidth + .4f);
            for (int i = 0; i < ground; i++)
                SpawnEnemy(EnemyKind.Ground, new Vector2(spawnX + EnemySpawnDirection*i * groundSpacing, -4.25f + i % 2 * .25f));
            for (int i = 0; i < flyers; i++)
                SpawnEnemy(EnemyKind.Flyer, new Vector2(spawnX + EnemySpawnDirection*i * flyerSpacing, flyingEnemyHeight + i % 2 * flyingEnemyLaneSpacing));
            if(enemies.Count==0)Stage.OnWaveCleared();
        }

        public void ContinueAfterRewards()
        {
            if (Hull <= 0 || enemies.Count > 0) return;
            Stage.OnRewardChosen();
        }

        public void ClearEncounterTransient()
        {
            foreach (var shot in FindObjectsOfType<Projectile2D>()) { shot.gameObject.SetActive(false); Destroy(shot.gameObject); }
            foreach (var effect in effects) if (effect != null) Destroy(effect.gameObject);
            effects.Clear();
            damageNumbers.Clear();
        }

        public void ReturnHome()
        {
            deployed = false; paused = false; Time.timeScale = 1;
            ClearEncounterTransient(); Rewards.ResetRun(); Stage.EndRun();
            foreach(var enemy in enemies) if(enemy!=null) Destroy(enemy.gameObject);
            enemies.Clear();
            tankVelocity = Vector2.zero;
            Artwork.transform.position=tankStartPosition;Artwork.ResetVisual();
            CameraPan.ResetToSpawn(tankStartPosition.x,tankStartPosition.x+EnemySpawnDirection*enemySpawnDistance);FitCamera();
            Backgrounds.SelectRandom();
        }
        public void SelectStage(StageDefinition definition)
        {
            if (deployed || definition == null) return;
            stageDefinition = definition; Stage.Configure(this, definition, cardCatalog);
        }

        public void BeginStageWave()
        {
            ClearEncounterTransient();
            if(Stage.EncounterWave==1)
            {
                BattleDifficulty=Stage.Difficulty;
                battleEnemyStats.Clear();
                foreach(var definition in enemyDataManager.Definitions)battleEnemyStats.Add(definition.id,definition.AtDifficulty(BattleDifficulty));
            }
            if (Stage.EncounterWave == 1 && !(Wave==0 && keepHomeBackdrop)) Backgrounds.SelectRandom();
            SpawnWave();
        }

        public void ApplyCardEffect(CardDefinition card)
        {
            switch (card.effect)
            {
                case CardEffect.CannonDamage: MainGun.IncreaseDamage(card.fraction); break;
                case CardEffect.MachineGunDamage: MachineGun.IncreaseDamage(card.fraction); break;
                case CardEffect.Resupply: MachineGun.Resupply(card.fraction); MainGun.Resupply(card.fraction); Mortar.Resupply(card.fraction); break;
                case CardEffect.Repair: Hull = Mathf.Min(MaxHull, Hull + Mathf.Ceil(MaxHull * card.fraction)); break;
                case CardEffect.MaxHull:
                    float previous = MaxHull;
                    MaxHull += BaseHull * Mathf.Max(0, card.fraction);
                    Hull += MaxHull - previous;
                    break;
            }
        }

        public EnemyEntity FindTarget(Vector2 position, EnemyKind priority, float range)
        {
            EnemyEntity preferred = null, fallback = null;
            float preferredDistance = range * range, fallbackDistance = range * range;
            foreach (EnemyEntity enemy in enemies)
            {
                if (enemy == null || !enemy.Alive) continue;
                float distance = ((Vector2)enemy.transform.position - position).sqrMagnitude;
                if (enemy.Kind == priority && distance <= preferredDistance) { preferred = enemy; preferredDistance = distance; }
                if (distance <= fallbackDistance) { fallback = enemy; fallbackDistance = distance; }
            }
            return preferred != null ? preferred : fallback;
        }

        public void RemoveEnemy(EnemyEntity enemy, bool killed)
        {
            if (!enemies.Remove(enemy)) return;
            if (killed)
            {
                Score += enemy.Data.score;
                Rewards.AddScraps(enemy.ScrapsReward);
            }
            if (enemies.Count == 0)
            {
                if (killed && deployed && Hull > 0) Stage.OnWaveCleared();
            }
        }

        public EnemyEntity FindMortarTarget(Vector2 origin, float range, float blastRadius)
        {
            EnemyEntity best = null;
            int bestCount = -1;
            float nearest = float.PositiveInfinity;
            foreach (var candidate in enemies)
            {
                if (candidate == null || !candidate.Alive) continue;
                float distance = ((Vector2)candidate.transform.position - origin).sqrMagnitude;
                if (distance > range * range) continue;
                bool groundPriority = candidate.Kind == EnemyKind.Ground;
                if (best != null && best.Kind == EnemyKind.Ground && !groundPriority) continue;
                int count = 0;
                foreach (var other in enemies)
                    if (other != null && other.Alive && ((Vector2)(other.transform.position - candidate.transform.position)).sqrMagnitude <= blastRadius * blastRadius) count++;
                bool preferGround = best != null && best.Kind != EnemyKind.Ground && groundPriority;
                if (best == null || preferGround || count > bestCount || (count == bestCount && distance < nearest))
                { best = candidate; bestCount = count; nearest = distance; }
            }
            return best;
        }

        public static float MortarDamageAtDistance(float distance, float damage, float fullRadius, float halfRadius)
        {
            if (distance <= Mathf.Max(0, fullRadius)) return damage;
            return distance <= Mathf.Max(fullRadius, halfRadius) ? damage * .5f : 0;
        }

        public void DetonateMortar(Vector2 position, float damage, float fullRadius, float halfRadius)
            => DetonateMortar(position,new DamageAttack(damage,Mortar.Stats),fullRadius,halfRadius);

        public void DetonateMortar(Vector2 position, DamageAttack attack, float fullRadius, float halfRadius)
        {
            // Snapshot before applying damage: kills can remove enemies and open the wave reward screen.
            foreach (var enemy in enemies.ToArray())
            {
                if (enemy == null || !enemy.Alive) continue;
                float multiplier = MortarDamageAtDistance(Vector2.Distance(position, enemy.transform.position), 1, fullRadius, halfRadius);
                if (multiplier > 0) enemy.ReceiveAttack(attack,multiplier);
            }
            Flash(position, new Color(1, .55f, .15f, .12f), Mathf.Max(fullRadius, halfRadius) * 2);
            Flash(position, new Color(1, .8f, .25f, .35f), fullRadius * 2);
        }

        public DamageResult ReceiveAttack(DamageAttack attack)
        {
            if(!Running)return new DamageResult(false,false,0,attack.DamageType);
            var result=CombatDamage.Resolve(attack,tankStats);DamageHull(result.Damage);return result;
        }

        // Direct HP loss remains available for scripted effects and debug helpers.
        public void DamageHull(float damage)
        {
            if (!Running || damage <= 0) return;
            ShowDamageNumber(TankAimPoint + Vector2.up * 1.2f, Mathf.Min(Hull, damage), true);
            Hull = Mathf.Max(0, Hull - damage);
            if(Application.platform==RuntimePlatform.Android && GameAudioSettings.Vibration && Time.unscaledTime-lastVibration>.25f)
            { Handheld.Vibrate();lastVibration=Time.unscaledTime; }
            if (Hull > 0) return;
            best = Mathf.Max(best, Score);
            PlayerPrefs.SetInt("MegaTanks.Best2D", best);
            PlayerPrefs.Save();
        }

        public void SetPaused(bool value)
        {
            if (!deployed || Hull <= 0 || Rewards.IsOpen || Stage.Mode != StageMode.Combat) return;
            paused = value;
            Time.timeScale = value ? 0 : 1;
        }

        public void SetStance(TankStance stance)
        {
            Stance = stance;
            if (stance == TankStance.Defensive) tankVelocity = Vector2.zero;
        }

        float AdvanceNeededForWeapon(Vector2 origin, float range)
        {
            float minimum = float.PositiveInfinity;
            float reach = Mathf.Max(0, range - .05f);
            foreach (var enemy in enemies)
            {
                if (enemy == null || !enemy.Alive) continue;
                Vector2 offset = (Vector2)enemy.transform.position - origin;
                offset.x *= EnemySpawnDirection;
                if (Mathf.Abs(offset.y) > reach) continue;
                float horizontalReach = Mathf.Sqrt(Mathf.Max(0, reach * reach - offset.y * offset.y));
                if (offset.x < -horizontalReach) continue;
                minimum = Mathf.Min(minimum, Mathf.Max(0, offset.x - horizontalReach));
            }
            return minimum;
        }

        void AdvanceTank()
        {
            tankVelocity = Vector2.zero;
            if (!Running || Stance != TankStance.Aggressive || enemies.Count == 0 || AllWeaponsInRange) return;
            float distance = Mathf.Max(AdvanceNeededForWeapon(MachineGun.transform.position, MachineGun.MaxRange),
                AdvanceNeededForWeapon(MainGun.transform.position, MainGun.MaxRange),
                AdvanceNeededForWeapon(Artwork.Mortar.Muzzle.position, Mortar.Stats.range));
            if (float.IsInfinity(distance) || Time.deltaTime <= 0) return;
            float step = Mathf.Min(distance, Mathf.Max(0, TankAdvanceSpeed) * Time.deltaTime);
            Artwork.transform.position += Vector3.right * (EnemySpawnDirection*step);
            tankVelocity = Vector2.right * (EnemySpawnDirection*step / Time.deltaTime);
        }

        void Update()
        {
            AdvanceTank();
            FitCamera();
            if (!paused && !Rewards.IsOpen)
                for (int i = damageNumbers.Count - 1; i >= 0; i--)
                {
                    var number = damageNumbers[i];
                    number.Age += Time.deltaTime;
                    number.Position += Vector2.up * (1.4f * Time.deltaTime);
                    if (number.Age >= DamageNumberLifetime) damageNumbers.RemoveAt(i);
                }
        }

        public void Flash(Vector2 position, Color color, float size)
        {
            var effect = ShapeFactory.Create("Impact", scenery, position, Vector2.one * size, color, true, 85).gameObject.AddComponent<FlashEffect>();
            effects.Add(effect);
        }

        public void ForgetEffect(FlashEffect effect) { effects.Remove(effect); }
        public void ShowDamageNumber(Vector2 position, float amount, bool isTank = false)
        {
            if (amount <= 0) return;
            if (damageNumbers.Count >= 128) damageNumbers.RemoveAt(0);
            damageNumbers.Add(new DamageNumber { Position = position, Amount = amount, IsTank = isTank });
        }

        void OnApplicationPause(bool value) { if (value && Running) SetPaused(true); }
        void OnDestroy() { Time.timeScale = 1; if (Instance == this) Instance = null; ShapeFactory.Release(); }

    }

    public sealed class FlashEffect : MonoBehaviour
    {
        float lifetime = .16f;
        float initialAlpha;
        void Awake() { initialAlpha = GetComponent<SpriteRenderer>().color.a; }
        void Update()
        {
            if (BattleGame.Instance == null || !BattleGame.Instance.Running) return;
            lifetime -= Time.deltaTime;
            transform.localScale *= 1 + Time.deltaTime * 5;
            var sprite = GetComponent<SpriteRenderer>();
            Color color = sprite.color;
            color.a = initialAlpha * Mathf.Clamp01(lifetime / .16f);
            sprite.color = color;
            if (lifetime <= 0)
            {
                BattleGame.Instance.ForgetEffect(this);
                Destroy(gameObject);
            }
        }
    }
}
