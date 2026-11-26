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
        public const string SelectedTankSaveKey="MegaTanks.SelectedTank";
        public WeaponLoadout StartingLoadout => startingLoadout;
        public BossEncounterSettings BossSettings => (Stage != null ? Stage.Definition : stageDefinition)?.boss;
        public BossTankProfile BossProfile => BossSettings?.template;
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
        [Header("Battle effects")]
        [SerializeField] BattleVfxCatalog battleVfxCatalog;
        public BattleVfx Vfx { get; private set; }
        public BattleBackgroundController Backgrounds { get; private set; }
        public MortarWeapon Mortar { get; private set; }
        public float TankAdvanceSpeed => tankStats.advanceSpeed;
        public float ReloadSpeedBonus=>Equipment!=null?Equipment.ReloadSpeedBonus(Progression):0;
        public float AccuracyBonus=>Equipment!=null?Equipment.AccessoryBonus(AccessoryBonusType.Accuracy,Progression):0;
        public float EvasionBonus=>Equipment!=null?Equipment.AccessoryBonus(AccessoryBonusType.Evasion,Progression):0;
        public TankStance Stance { get; private set; } = TankStance.Aggressive;
        Vector2 tankVelocity;
        Vector3 tankStartPosition;
        public Vector2 TankVelocity => Stage?.Mode == StageMode.ApproachingEvent || Stage?.Mode == StageMode.Advancing
            || (Running && Stance == TankStance.Aggressive) ? tankVelocity : Vector2.zero;
        internal float TankTravelThisFrame { get; private set; }
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
        readonly List<EnemyDefinition> groundWaveEnemies = new List<EnemyDefinition>();
        readonly List<EnemyDefinition> flyingWaveEnemies = new List<EnemyDefinition>();
        EnemyDefinition waveCarrier;
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
            public bool IsMiss { get; internal set; }
            public string DisplayText => IsMiss ? "Miss" : "-" + Mathf.FloorToInt(Amount);
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
        public float StartingMaxHull => BaseHull;
        public float Hull { get; private set; } = 100;
        public float MaxHull { get; private set; } = 100;
        public int Score { get; private set; }
        public int EnemiesDefeated { get; private set; }
        public int RunId { get; private set; }
        public int Wave { get; private set; }
        public int EnemyCount => enemies.Count;
        public bool Paused => paused;
        public bool Deployed => deployed;
        public PlayerEquipment Equipment { get; private set; }
        public PlayerCrew Crew { get; private set; }
        public CommandoUnit Commando { get; private set; }
        public IReadOnlyList<CommandoUnit> AlliedCrew => alliedCrew;
        readonly List<CommandoUnit> alliedCrew = new List<CommandoUnit>();
        public PlayerWallet Wallet { get; private set; }
        public PlayerProgression Progression { get; private set; }
        public StageCompletionRewards StageRewards { get; private set; }
        public CanvasGameUI UI { get; private set; }
        [SerializeField] FrontendCatalog frontendCatalog;
        public FrontendCatalog Frontend => frontendCatalog;
        readonly List<EnemyEntity> enemies = new List<EnemyEntity>();
        readonly List<FlashEffect> effects = new List<FlashEffect>();
        Transform scenery;
        Camera battleCamera;
        bool deployed, paused;
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
            string savedTank=PlayerPrefs.GetString(SelectedTankSaveKey,"");
            if(frontendCatalog!=null&&frontendCatalog.tanks!=null)
                foreach(var tank in frontendCatalog.tanks)
                    if(tank!=null&&tank.id==savedTank){tankDefinition=tank;break;}
            BuildScene();
            Equipment = new PlayerEquipment(startingLoadout, frontendCatalog);
            Equipment.ConfigureTank(tankDefinition);
            Crew = new PlayerCrew(frontendCatalog!=null?frontendCatalog.crew:null);
            RefreshEquipmentArtwork();
            Wallet = new PlayerWallet();
            Progression = new PlayerProgression(frontendCatalog, Wallet);
            PlaytestResources.ApplyOnce(frontendCatalog, Wallet, Progression);
            Crew.Changed+=RefreshTankLoadoutStats;
            Commando = CreateCrewUnit(CrewRole.Commando);
            CreateCrewUnit(CrewRole.Gunner);
            CreateCrewUnit(CrewRole.SubGunner);
            CreateCrewUnit(CrewRole.WeaponSpecialist);
            CreateCrewUnit(CrewRole.Driver);
            RefreshTankLoadoutStats();
            StageRewards = new StageCompletionRewards(Wallet);
            UI = CanvasGameUI.Create(this);
        }

        CommandoUnit CreateCrewUnit(CrewRole role)
        {
            var unit = new GameObject("Crew " + role).AddComponent<CommandoUnit>();
            unit.transform.SetParent(transform, false);
            unit.Configure(this, role);
            alliedCrew.Add(unit);
            return unit;
        }

        internal CrewDefinition CommandoStatsSource()
        {
            var equipped = Crew.Get(CrewRole.Commando);
            if (equipped != null) return equipped;
            var members = Frontend?.crew?.members;
            return members == null ? null : System.Array.Find(members, member => member != null && member.role == CrewRole.Commando);
        }

        internal bool CanDeployCrew(CrewRole role)
        {
            switch (role)
            {
                case CrewRole.Gunner: return MainGun.Ammo <= 0;
                case CrewRole.SubGunner: return MachineGun.Ammo <= 0;
                case CrewRole.WeaponSpecialist: return Mortar.Ammo <= 0;
                case CrewRole.Driver: return StoppedInBossWeaponRange();
                default: return true;
            }
        }

        bool StoppedInBossWeaponRange()
        {
            if (Stage.Current?.type != StageNodeType.Boss || TankVelocity.sqrMagnitude > .0001f) return false;
            foreach (var enemy in enemies)
            {
                if (enemy == null || !enemy.Alive || enemy.Boss == null || !enemy.gameObject.activeInHierarchy) continue;
                Vector2 target = enemy.transform.position;
                return InWeaponRange(target, MainGun.transform.position, MainGun.MaxRange)
                    && InWeaponRange(target, MachineGun.transform.position, MachineGun.MaxRange)
                    && InWeaponRange(target, Artwork.Mortar.Muzzle.position, Mortar.Stats.range);
            }
            return false;
        }

        static bool InWeaponRange(Vector2 target, Vector2 origin, float range)
            => range > 0 && (target - origin).sqrMagnitude <= range * range;

        public TankStats CalculateTankStats(ArmorDefinition armor,ArmorDefinition secondArmor=null)
        {
            var result = tankDefinition.stats.Copy();
            if (armor != null) armor.ApplyTo(result, Progression != null ? Progression.ArmorHP(armor) : armor.hp);
            if(secondArmor!=null&&tankDefinition.ArmorSlots>1&&secondArmor!=armor)
                secondArmor.ApplyTo(result,Progression!=null?Progression.ArmorHP(secondArmor):secondArmor.hp);
            var driver=Crew?.Get(CrewRole.Driver);
            if(driver!=null)driver.ApplyHull(result,Progression!=null?Progression.Get(driver).Level:1,Progression!=null?Progression.Get(driver).Stars:0);
            return result;
        }
        TankStats EquippedTankStats()
        {
            var result=CalculateTankStats(Equipment?.Armor,Equipment!=null&&Equipment.ArmorSlotCount>1?Equipment.SecondArmor:null);
            result.evasion+=EvasionBonus;return result;
        }
        public void RefreshTankLoadoutStats()
        {
            if (deployed) return;
            tankStats = EquippedTankStats();
            Hull = MaxHull = BaseHull;
            if(Equipment!=null&&MachineGun!=null&&MainGun!=null&&Mortar!=null)ConfigurePlayerWeapons();
        }
        public void RefreshEquipmentArtwork()
        {
            RefreshTankLoadoutStats();
            Artwork.EquipMachineGunVisual(Equipment.MachineGun);
            MachineGun.BindArtwork(Artwork.MachineGun);
        }

        void BuildScene()
        {
            if (tankDefinition == null || startingLoadout == null) throw new System.InvalidOperationException("Player profiles are missing. Run MegaTanks > Migrate Player Stats To Profiles.");
            startingLoadout.Validate();
            tankStats = EquippedTankStats();
            Hull = MaxHull = BaseHull;
            battleCamera = new GameObject("2D Battle Camera").AddComponent<Camera>();
            battleCamera.tag = "MainCamera";
            battleCamera.orthographic = true;
            battleCamera.transform.position = new Vector3(0, 0, -10);
            battleCamera.clearFlags = CameraClearFlags.SolidColor;
            battleCamera.backgroundColor = navy;
            Vfx=new GameObject("Battle VFX").AddComponent<BattleVfx>();
            Vfx.transform.SetParent(transform,false);
            Vfx.Configure(this,battleVfxCatalog!=null?battleVfxCatalog:Resources.Load<BattleVfxCatalog>("BattleVfx"));
            scenery = new GameObject("2D Tank and Battlefield").transform;
            BuildPlayerTank();
            CameraPan=battleCamera.gameObject.AddComponent<BattleCameraPan>();
            CameraPan.Configure(this,tankStartPosition.x,tankStartPosition.x+EnemySpawnDirection*enemySpawnDistance);
            Rewards = gameObject.AddComponent<CardRewardSystem>();
            Rewards.Configure(cardCatalog);
            if (stageDefinition == null) throw new System.InvalidOperationException("Stage definition is missing.");
            Stage = gameObject.AddComponent<StageProgression>();
            Stage.Configure(this, stageDefinition, cardCatalog);
            FitCamera();
            Backgrounds = gameObject.AddComponent<BattleBackgroundController>();
            Backgrounds.Configure(backgroundCatalog, battleCamera);
            Backgrounds.SelectStagePreset(stageDefinition);
            Backgrounds.SelectRandom();
        }

        void BuildPlayerTank()
        {
            var prefab=tankDefinition.artworkPrefab!=null?tankDefinition.artworkPrefab:tankArtworkPrefab;
            if(prefab==null)throw new System.InvalidOperationException("Selected tank artwork is missing.");
            Artwork=Instantiate(prefab,scenery);
            Artwork.transform.localScale=Vector3.one*tankScale;
            Artwork.transform.localPosition=tankSpawnPosition;
            tankStartPosition=Artwork.transform.position;
            HullHitbox=null;
            foreach (var renderer in Artwork.GetComponentsInChildren<SpriteRenderer>())
            {
                if (!renderer.name.EndsWith("_front_hull_frame")) continue;
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
            Mortar.Configure(Artwork.Mortar, Equipment != null ? Equipment.Mortar : startingLoadout.mortar, Progression, crewMember:Crew?.Get(CrewRole.WeaponSpecialist));
        }

        public bool SelectTank(TankDefinition tank)
        {
            if(deployed||tank==null||frontendCatalog?.tanks==null||System.Array.IndexOf(frontendCatalog.tanks,tank)<0)return false;
            if(tank==tankDefinition)return true;
            Vfx.Clear();
            Artwork.gameObject.SetActive(false);Destroy(Artwork.gameObject);
            tankDefinition=tank;Equipment.ConfigureTank(tank);RefreshTankLoadoutStats();
            BuildPlayerTank();RefreshEquipmentArtwork();
            tankVelocity=Vector2.zero;
            CameraPan.ResetToSpawn(tankStartPosition.x,tankStartPosition.x+EnemySpawnDirection*enemySpawnDistance);FitCamera();
            PlayerPrefs.SetString(SelectedTankSaveKey,tank.id);PlayerPrefs.Save();return true;
        }

        AutoTurret CreateTurret(GunDefinition definition, GunArtwork visual)
        {
            var turret = visual.gameObject.AddComponent<AutoTurret>();
            turret.Configure(definition);
            turret.BindArtwork(visual);
            return turret;
        }
        public CrewDefinition WeaponCrew(EquipmentType type)=>Crew?.Get(type==EquipmentType.Cannon?CrewRole.Gunner:type==EquipmentType.MachineGun?CrewRole.SubGunner:CrewRole.WeaponSpecialist);
        public WeaponCombatStats PreviewWeaponStats(WeaponDefinition weapon)
        {
            WeaponCombatStats result=weapon is GunDefinition gun?gun.stats.Copy():(WeaponCombatStats)((MortarDefinition)weapon).stats.Copy();
            var member=WeaponCrew(weapon.Type);if(member!=null)member.ApplyWeapon(result,Progression.Get(member).Level,Progression.Get(member).Stars);
            return result;
        }
        void ConfigurePlayerWeapons()
        {

            MachineGun.Configure(Equipment!=null?Equipment.MachineGun:startingLoadout.machineGun,Progression,0,ReloadSpeedBonus,AccuracyBonus,Crew?.Get(CrewRole.SubGunner));
            MainGun.Configure(Equipment!=null?Equipment.Cannon:startingLoadout.cannon,Progression,0,ReloadSpeedBonus,AccuracyBonus,Crew?.Get(CrewRole.Gunner));
            Mortar.Configure(Artwork.Mortar,Equipment!=null?Equipment.Mortar:startingLoadout.mortar,Progression,0,ReloadSpeedBonus,AccuracyBonus,Crew?.Get(CrewRole.WeaponSpecialist));
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
            Vfx.Clear();
            RunId++;
            foreach (var unit in alliedCrew) unit.ResetForRun();
            Time.timeScale = 1;
            foreach (EnemyEntity enemy in enemies) if (enemy != null) Destroy(enemy.gameObject);
            enemies.Clear();
            foreach (Projectile2D projectile in FindObjectsOfType<Projectile2D>()) Destroy(projectile.gameObject);
            foreach (FlashEffect effect in effects) if (effect != null) Destroy(effect.gameObject);
            effects.Clear();
            damageNumbers.Clear();
            startingLoadout.Validate();
            tankStats = EquippedTankStats();
            Artwork.transform.localScale = Vector3.one * tankScale;
            tankStartPosition = tankSpawnPosition;
            RefreshEquipmentArtwork();
            ConfigurePlayerWeapons();
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
            EnemiesDefeated = 0;
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
            if(!battleEnemyStats.TryGetValue(definitionId,out var stats))stats=enemyDataManager.Get(definitionId).AtDifficulty(BattleDifficulty);
            return SpawnEnemy(stats, position, BattleDifficulty);
        }

        internal EnemyEntity SpawnEnemy(EnemyDefinition stats, Vector2 position, int difficulty, CommandoArtwork existingCommando = null)
        {
            var entity = new GameObject(stats.id).AddComponent<EnemyEntity>();
            entity.transform.position = position;
            entity.Initialize(stats, Wave, difficulty, existingCommando);
            if(entity.Alive)enemies.Add(entity);else Destroy(entity.gameObject);
            return entity;
        }

        public EnemyEntity SpawnBossCommando(BossTank owner,EnemyDefinition definition,Vector2 position)
        {
            var entity=new GameObject(definition.id).AddComponent<EnemyEntity>();entity.CommandoOwner=owner;
            entity.transform.position=position;entity.Initialize(definition,Wave);
            if(entity.Alive)enemies.Add(entity);else Destroy(entity.gameObject);
            return entity;
        }
        public void SetBossCommandoDeployed(EnemyEntity entity,bool active)
        {
            if(entity==null)return;
            if(active){if(entity.Alive&&!enemies.Contains(entity))enemies.Add(entity);entity.gameObject.SetActive(true);}
            else {RemoveEnemy(entity,false);entity.gameObject.SetActive(false);}
        }
        void SpawnWave()
        {
            Wave++;
            if (Stage.Current != null && Stage.Current.type == StageNodeType.Boss)
            {
                SpawnBoss();
                return;
            }
            int growth = Wave / 3;
            int ground = groundWaveEnemies.Count > 0 || waveCarrier != null ? Mathf.Min(3 + growth, 5) : 0;
            int flyers = flyingWaveEnemies.Count > 0 ? Mathf.Min(2 + growth, 5) : 0;
            float spawnX = Artwork.transform.position.x + EnemySpawnDirection * enemySpawnDistance;
            CameraPan.SetEnemySpawn(spawnX);

            int carrierSlot = waveCarrier != null && groundWaveEnemies.Count > 0 ? Random.Range(1, ground - 1) : -1;
            float groundOffset = 0;
            float previousWidth = 0;
            for (int i = 0; i < ground; i++)
            {
                var unit = i == carrierSlot || groundWaveEnemies.Count == 0 ? waveCarrier : PickWaveEnemy(groundWaveEnemies);
                float width = unit.ScaledArtworkWidth;
                if (i > 0)
                    groundOffset += Mathf.Max(1.7f, (previousWidth + width) * .5f + .3f);
                SpawnEnemy(unit, new Vector2(spawnX + EnemySpawnDirection * groundOffset, -4.25f + i % 2 * .25f), BattleDifficulty);
                previousWidth = width;
            }

            float flyerOffset = 0;
            previousWidth = 0;
            for (int i = 0; i < flyers; i++)
            {
                var unit = PickWaveEnemy(flyingWaveEnemies);
                float width = unit.ScaledArtworkWidth;
                if (i > 0)
                    flyerOffset += Mathf.Max(2, (previousWidth + width) * .5f + .4f);
                SpawnEnemy(unit, new Vector2(spawnX + EnemySpawnDirection * flyerOffset,
                    flyingEnemyHeight + i % 2 * flyingEnemyLaneSpacing), BattleDifficulty);
                previousWidth = width;
            }
            if (enemies.Count == 0) Stage.OnWaveCleared();
        }

        static EnemyDefinition PickWaveEnemy(List<EnemyDefinition> roster) =>
            roster[roster.Count == 1 ? 0 : Random.Range(0, roster.Count)];

        void PrepareWaveRoster()
        {
            groundWaveEnemies.Clear();
            flyingWaveEnemies.Clear();
            waveCarrier = null;
            foreach (var profile in Stage.Definition.spawnableEnemies ?? System.Array.Empty<EnemyStatsProfile>())
            {
                if (profile == null || string.IsNullOrEmpty(profile.stats?.id)) continue;
                string id = profile.stats.id;
                if (!battleEnemyStats.TryGetValue(id, out var stats))
                {
                    stats = profile.stats.AtDifficulty(BattleDifficulty);
                    battleEnemyStats.Add(id, stats);
                }
                if (stats.kind == EnemyKind.Flyer)
                {
                    if (!flyingWaveEnemies.Contains(stats)) flyingWaveEnemies.Add(stats);
                }
                else if (id == TrojanScrapCat.EnemyId)
                    waveCarrier = stats;
                else if (!groundWaveEnemies.Contains(stats))
                    groundWaveEnemies.Add(stats);
            }
        }

        public EnemyEntity SpawnBoss()
        {
            // Only the authored Boss map node can own this encounter, including direct API calls.
            if (!Running || Stage == null || Stage.Mode != StageMode.Combat || Stage.Current == null
                || Stage.Current.type != StageNodeType.Boss || !Stage.Current.entered || Stage.Current.completed) return null;
            foreach (var enemy in enemies)
                if (enemy != null && enemy.Alive && enemy.Boss != null) return enemy;
            var settings = BossSettings?.Copy();
            if (settings == null) throw new System.InvalidOperationException("Configure the boss on the Stage Definition.");
            settings.Validate();
            float spawnX = Artwork.transform.position.x + enemySpawnDistance;
            CameraPan.SetEnemySpawn(spawnX);
            var entity = new GameObject(settings.template.id).AddComponent<EnemyEntity>();
            entity.transform.position = new Vector3(spawnX, GroundHeight, 0);
            entity.gameObject.AddComponent<BossTank>().Configure(settings);
            entity.Initialize(settings.CreateEnemy(), Wave);
            enemies.Add(entity);
            return entity;
        }

        public void ContinueAfterRewards()
        {
            if (Hull <= 0 || enemies.Count > 0) return;
            Stage.OnRewardChosen();
        }

        public void ClearEncounterTransient()
        {
            Vfx.Clear();
            foreach (var shot in FindObjectsOfType<Projectile2D>()) { shot.gameObject.SetActive(false); Destroy(shot.gameObject); }
            foreach (var effect in effects) if (effect != null) Destroy(effect.gameObject);
            effects.Clear();
            damageNumbers.Clear();
        }

        public void ReturnHome()
        {
            deployed = false; paused = false; Time.timeScale = 1;
            foreach (var unit in alliedCrew) unit.ResetForRun();
            ClearEncounterTransient(); Rewards.ResetRun(); Stage.EndRun();
            foreach(var enemy in enemies) if(enemy!=null) Destroy(enemy.gameObject);
            enemies.Clear();
            tankVelocity = Vector2.zero;
            Artwork.transform.position=tankStartPosition;Artwork.ResetVisual();
            CameraPan.ResetToSpawn(tankStartPosition.x,tankStartPosition.x+EnemySpawnDirection*enemySpawnDistance);FitCamera();
        }
        public void SelectStage(StageDefinition definition)
        {
            if (deployed || definition == null) return;
            stageDefinition = definition; Stage.Configure(this, definition, cardCatalog);
            Backgrounds.SelectStagePreset(definition);
        }

        public void BeginStageWave()
        {
            ClearEncounterTransient();
            if(Stage.EncounterWave==1)
            {
                Rewards.BeginBattle();
                BattleDifficulty=Stage.Difficulty;
                battleEnemyStats.Clear();
                foreach(var definition in enemyDataManager.Definitions)battleEnemyStats.Add(definition.id,definition.AtDifficulty(BattleDifficulty));
                PrepareWaveRoster();
            }
            SpawnWave();
        }

        public void ApplyCardEffect(CardDefinition card)
        {
            switch (card.effect)
            {
                case CardEffect.Scraps: Rewards.AddScraps(card.scraps); break;
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

        internal bool HasGroundThreat(Vector2 position, float range)
        {
            if (range <= 0) return false;
            foreach (var enemy in enemies)
                if (enemy != null && enemy.Alive && enemy.Kind == EnemyKind.Ground && enemy.gameObject.activeInHierarchy
                    && ((Vector2)enemy.transform.position - position).sqrMagnitude <= range * range)
                    return true;
            return false;
        }

        public EnemyEntity FindMeleeTarget(Collider2D attacker,Vector2 activationOrigin,float activationRange)
        {
            EnemyEntity target=null;float distance=float.PositiveInfinity;
            foreach(var enemy in enemies)
            {
                if(enemy==null||!enemy.Alive||!enemy.gameObject.activeInHierarchy||Vector2.Distance(activationOrigin,enemy.transform.position)>activationRange)continue;
                var body=enemy.GetComponent<Collider2D>();if(!CommandoMelee.VerticallyReachable(attacker,body))continue;
                float gap=CommandoMelee.DistanceSquared(attacker,body);if(gap<distance){distance=gap;target=enemy;}
            }
            return target;
        }
        public void RemoveEnemy(EnemyEntity enemy, bool killed)
        {
            if (!enemies.Remove(enemy)) return;
            if (killed)
            {
                EnemiesDefeated++;
                Score += enemy.Data.score;
                Rewards.AddBattleScraps(enemy.ScrapsReward);
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
            if(Vfx.Catalog!=null)Vfx.Blast(position,true,position.y<=GroundHeight+.3f);
            else
            {
                Flash(position, new Color(1, .55f, .15f, .12f), Mathf.Max(fullRadius, halfRadius) * 2);
                Flash(position, new Color(1, .8f, .25f, .35f), fullRadius * 2);
            }
        }

        public DamageResult ReceiveAttack(DamageAttack attack, float areaMultiplier = 1)
        {
            if(!Running)return new DamageResult(false,false,0,attack.DamageType);
            var result=CombatDamage.Resolve(attack,tankStats,areaMultiplier);
            if(!result.Hit)ShowDamageNumber(TankAimPoint + Vector2.up * 1.2f,0,true,true);
            DamageHull(result.Damage);return result;
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
            Vfx.Blast(new Vector2(Artwork.transform.position.x,GroundHeight),false,true,1.5f);
            best = Mathf.Max(best, Score);
            PlayerPrefs.SetInt("MegaTanks.Best2D", best);
            PlayerPrefs.Save();
        }

        public void SetPaused(bool value)
        {
            if (!deployed || Hull <= 0 || Rewards.IsOpen || (Stage.Mode != StageMode.Combat && Stage.Mode != StageMode.WaveBreak)) return;
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
            MoveTankForward(step, Time.deltaTime);
        }

        const float CampViewportClearance = .25f;
        const float CampStopClearance = .35f;
        SpriteRenderer campTent, campSign, campSignIcon;
        float campApproachStartX, campStopX;

        void ShowCamp()
        {
            if (campTent == null)
            {
                campTent = CreateCampSprite("Field Camp Tent", scenery, Resources.Load<Sprite>("Events/CampTent"), -10);
                campTent.transform.localScale = Vector3.one * .7f;
                campSign = CreateCampSprite("Camp Road Sign", campTent.transform, Resources.Load<Sprite>("Events/CampRoadSign"), 100);
                campSign.transform.localScale = Vector3.one * (2.65f / (campSign.sprite.bounds.size.x * campTent.transform.lossyScale.x));
                campSignIcon = CreateCampSprite("Camp Sign Icon", campSign.transform, null, 101);
            }

            campApproachStartX = Artwork.transform.position.x;
            float tankFront = campApproachStartX;
            foreach (var renderer in Artwork.GetComponentsInChildren<SpriteRenderer>())
                if (renderer.enabled && renderer.sprite != null && renderer.color.a > 0)
                    tankFront = Mathf.Max(tankFront, renderer.bounds.max.x);
            float viewportRight = battleCamera.ViewportToWorldPoint(new Vector3(1, .5f, 0)).x;
            float tentLeft = Mathf.Max(viewportRight + CampViewportClearance, tankFront + CampStopClearance);
            campStopX = campApproachStartX + tentLeft - tankFront - CampStopClearance;
            Vector3 tentSize = campTent.sprite.bounds.size * campTent.transform.lossyScale.x;
            float tentX = tentLeft + tentSize.x * .5f;
            campTent.transform.position = new Vector3(tentX, GroundHeight + tentSize.y * .4f - .2f, 0);
            PositionCampSign(tentX);
            FadeCamp(0);
            campTent.gameObject.SetActive(true);
        }

        static SpriteRenderer CreateCampSprite(string name, Transform parent, Sprite sprite, int order)
        {
            var renderer = new GameObject(name).AddComponent<SpriteRenderer>();
            renderer.transform.SetParent(parent, false);
            renderer.sprite = sprite;
            renderer.sortingOrder = order;
            return renderer;
        }

        void PositionCampSign(float tentX)
        {
            var skin = UI.RouteSkin;
            campSignIcon.sprite = Stage.CampType switch
            {
                TentType.Repair => skin.repair,
                TentType.Resupply => skin.supply,
                TentType.Vendor => skin.IconFor(StageNodeType.Shop),
                _ => skin.backArrow
            };
            campSignIcon.transform.localRotation = Quaternion.Euler(0, 0, Stage.CampType == TentType.Upgrade ? -90f : 0f);
            Vector2 iconSize = campSignIcon.sprite.bounds.size;
            float iconScale = .62f / (Mathf.Max(iconSize.x, iconSize.y) * campSign.transform.lossyScale.x);
            campSignIcon.transform.localScale = Vector3.one * iconScale;
            Vector2 signSize = campSign.sprite.bounds.size;
            campSignIcon.transform.localPosition = new Vector3(-.14f * signSize.x, .155f * signSize.y, 0);
            float signHeight = signSize.y * campSign.transform.lossyScale.y;
            campSign.transform.position = new Vector3(tentX - 5.4f, GroundHeight + signHeight * .44f - .2f, 0);
        }

        internal bool ApproachCamp(float seconds)
        {
            if (campTent == null || !campTent.gameObject.activeSelf) ShowCamp();
            // A tank configured with zero combat speed must still reach the event.
            float speed = TankAdvanceSpeed > 0 ? 2f * TankAdvanceSpeed : 1f;
            float distance = Mathf.Max(0, campStopX - Artwork.transform.position.x);
            float timeToStop = distance / speed;
            bool arrived = seconds >= timeToStop;
            MoveTankForward(arrived ? distance : speed * seconds, Mathf.Min(seconds, timeToStop));
            float progress = arrived ? 1f : Mathf.InverseLerp(campApproachStartX, campStopX, Artwork.transform.position.x);
            FadeCamp(Mathf.Clamp01(progress * 5f));
            return arrived;
        }

        internal void FadeCamp(float opacity)
        {
            if (campTent == null || campTent.color.a == opacity) return;
            campTent.color = campSign.color = campSignIcon.color = new Color(1, 1, 1, opacity);
        }

        internal void HideCamp()
        {
            if (campTent != null) campTent.gameObject.SetActive(false);
        }

        internal void AdvanceBetweenBattles(float seconds) => MoveTankForward(Mathf.Max(0, TankAdvanceSpeed) * seconds, seconds);

        void MoveTankForward(float distance, float seconds)
        {
            float step = EnemySpawnDirection * distance;
            Artwork.transform.position += Vector3.right * step;
            TankTravelThisFrame += step;
            CameraPan.FollowTank(step);
            tankVelocity = seconds > 0 ? Vector2.right * (step / seconds) : Vector2.zero;
        }

        void Update()
        {
            TankTravelThisFrame = 0;
            if (Stage.Mode == StageMode.ApproachingEvent || Stage.Mode == StageMode.Advancing) Stage.Advance(Time.deltaTime);
            else AdvanceTank();
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
        public void ShowDamageNumber(Vector2 position, float amount, bool isTank = false, bool isMiss = false)
        {
            if (amount <= 0 && !isMiss) return;
            if (damageNumbers.Count >= 128) damageNumbers.RemoveAt(0);
            damageNumbers.Add(new DamageNumber { Position = position, Amount = amount, IsTank = isTank, IsMiss = isMiss });
        }

        void OnApplicationPause(bool value) { if (value && Running) SetPaused(true); }
        void OnDestroy() { StageRewards?.Dispose(); Time.timeScale = 1; if (Instance == this) Instance = null; ShapeFactory.Release(); }

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
