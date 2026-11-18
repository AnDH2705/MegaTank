using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MegaTanks.Editor
{
    public static class BossTankAudit
    {
        static StageRewardSaveScope rewardSaves;
        static int phase;
        static double since;
        static EnemyEntity boss;
        static BossTank tank;
        static float startX, hull;
        static string profileBefore, gunBefore;
        static bool errors;
        static int limitingWeapon;
        static Vector3 playerPosition;
        static float mgRange, cannonRange, mortarRange;
        static BossTankProfile overrideTemplate;
        static StageDefinition originalStage, overrideStage;
        static Transform wheel;
        static Quaternion wheelStart;
        public static void RunHeavy()
        {
            HeavyBossSetup.Install();
            overrideTemplate = AssetDatabase.LoadAssetAtPath<BossTankProfile>(HeavyBossSetup.TemplatePath);
            string before = JsonUtility.ToJson(overrideTemplate);
            HeavyBossSetup.Install();
            Require(JsonUtility.ToJson(overrideTemplate) == before, "Repeat installation must preserve heavy boss tuning.");
            var heavy = AssetDatabase.LoadAssetAtPath<TankDefinition>(HeavyTankSetup.HeavyPath);
            Require(overrideTemplate.tank != heavy && overrideTemplate.artworkPrefab == heavy.artworkPrefab
                && overrideTemplate.tank.stats.maxHull == heavy.stats.maxHull && overrideTemplate.tank.stats.advanceSpeed == heavy.stats.advanceSpeed
                && overrideTemplate.tank.ArmorSlots == 2 && overrideTemplate.secondArmor != null, "Heavy boss must copy the heavy player hull with its own two armor definitions.");
            Run();
        }
        static void Require(bool value, string message) { if (!value) throw new Exception(message); }
        public static void Run()
        {
            rewardSaves = new StageRewardSaveScope();
            BossTankSetup.Install();
            EditorSceneManager.OpenScene(MegaTanksSetup.ScenePath);
            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;
            EditorApplication.playModeStateChanged += state =>
            {
                if (state != PlayModeStateChange.EnteredPlayMode) return;
                Application.logMessageReceived += (_, __, type) => { if (type == LogType.Error || type == LogType.Exception) errors = true; };
                phase = 0; since = EditorApplication.timeSinceStartup; EditorApplication.update += Check;
            };
            EditorApplication.isPlaying = true;
        }
        static void Check()
        {
            try
            {
                var game = BattleGame.Instance;
                double elapsed = EditorApplication.timeSinceStartup - since;
                if (phase == 0)
                {
                    if (overrideTemplate != null)
                    {
                        originalStage = game.Stage.Definition;
                        overrideStage = UnityEngine.Object.Instantiate(originalStage);
                        overrideStage.boss = originalStage.boss.Copy(); overrideStage.boss.template = overrideTemplate;
                        game.SelectStage(overrideStage);
                    }
                    TestStageBossConfiguration(game);
                    game.StartBattle(); StageProgressionAudit.DisableWeapons(game); game.SetStance(TankStance.Defensive);
                    int regularEnemies = game.EnemyCount;
                    Require(game.SpawnBoss() == null && game.EnemyCount == regularEnemies, "Direct boss spawning must be rejected outside the special Boss map node.");
                    // Traverse the actual route to its final node, keeping intermediate encounters deterministic.
                    while (game.Stage.Current.EncounterType != StageNodeType.Boss)
                    {
                        Require(UnityEngine.Object.FindObjectsOfType<BossTank>().Length == 0, "Boss appeared before final node.");
                        StageProgressionAudit.ClearWave(game); game.Stage.OnRewardChosen();
                        var next = game.Stage.Route.Get(game.Stage.Current.next[0]);
                        if (next.type != StageNodeType.Boss) next.type = StageNodeType.Battle;
                        game.Rewards.ResetRun();
                        Require(game.Stage.SelectNode(next.id), "Cannot traverse route.");
                    }
                    var enemies = UnityEngine.Object.FindObjectsOfType<EnemyEntity>().Where(e => e.Alive).ToArray();
                    Require(enemies.Length == 1 && enemies[0].Boss != null, "Boss node must spawn one boss tank.");
                    boss = enemies[0]; tank = boss.Boss; startX = boss.transform.position.x;
                    Require(game.SpawnBoss() == boss && game.EnemyCount == 1, "A Boss encounter must not spawn duplicate bosses.");
                    Require(startX > game.TankAimPoint.x && boss.ApproachDirection == -1 && boss.Velocity.x < 0, "Boss must advance from right to left.");
                    Require(tank.Artwork.transform.localScale.x < 0 && tank.Artwork.transform.localScale.y > 0, "Boss artwork must be mirrored horizontally.");
                    Require(tank.Artwork.EnemyOwner == boss && (tank.Artwork.Tracks == null || tank.Artwork.Tracks.EnemyOwner == boss), "Boss animation must follow its own movement.");
                    wheel = tank.Artwork.GetComponentsInChildren<SpriteRenderer>().First(r => r.name.Contains("_wheel_")).transform;
                    wheelStart = wheel.localRotation;
                    var profile = game.BossProfile;
                    Require(profile.tank != game.TankProfile && profile.equipment != game.StartingLoadout && profile.equipment.cannon != game.StartingLoadout.cannon, "Boss assets must be independent.");
                    Require(profile.crew.Length == 5 && profile.crew.All(s => s.member != null && !game.Frontend.crew.members.Contains(s.member)), "Boss must have its own five crew members.");
                    Require(Mathf.Approximately(boss.Health, game.BossSettings.CalculateTankStats().maxHull) && Mathf.Approximately(tank.CannonStats.damage, game.BossSettings.WeaponDamage(profile.equipment.cannon.stats.damage, game.BossSettings.cannon)), "Boss stats must use the stage's equipment and crew progression.");
                    profileBefore = JsonUtility.ToJson(profile); gunBefore = JsonUtility.ToJson(profile.equipment.cannon);
                    Require(!ReferenceEquals(tank.CannonStats, profile.equipment.cannon.stats), "Runtime weapon stats must be copied.");
                    // Keep the firing test alive regardless of saved player equipment.
                    foreach (var gun in new[] { tank.MachineGunStats, tank.CannonStats }) { gun.damage = 1; gun.accuracy = 100; gun.armorPenetration = 100; gun.criticalChance = 0; }
                    tank.MortarStats.damage = 1; tank.MortarStats.accuracy = 100; tank.MortarStats.armorPenetration = 100;
                    phase = 1; since = EditorApplication.timeSinceStartup;
                }
                else if (phase == 1 && elapsed > 1)
                {
                    Require(boss.transform.position.x < startX, "Boss did not move left.");
                    Require(tank.Artwork.Tracks != null ? tank.Artwork.Tracks.TravelledDistance > 0 : Quaternion.Angle(wheelStart, wheel.localRotation) > .1f, "Boss tracks or wheels did not animate.");
                    mgRange = tank.MachineGunStats.range; cannonRange = tank.CannonStats.range; mortarRange = tank.MortarStats.range;
                    playerPosition = game.Artwork.transform.position;
                    tank.enabled = false; limitingWeapon = 0;
                    TestLimitingWeapon(game);
                }
                else if (phase == 10 && elapsed > 1.2)
                {
                    Require(tank.AllWeaponsInRange && boss.Velocity == Vector2.zero, "Boss must stop once every weapon is in range, including the shortest-range weapon.");
                    startX = boss.transform.position.x;
                    phase = 11; since = EditorApplication.timeSinceStartup;
                }
                else if (phase == 11 && elapsed > .4)
                {
                    Require(Mathf.Abs(boss.transform.position.x - startX) < .001f, "An in-range boss must hold position without extra advance.");
                    limitingWeapon++;
                    if (limitingWeapon < 3) TestLimitingWeapon(game);
                    else
                    {
                        // Moving the player away must invalidate the current stopping destination.
                        game.Artwork.transform.position += Vector3.left * 2;
                        Require(!tank.AllWeaponsInRange && boss.Velocity.x < 0, "Boss must resume advancing if the player leaves any weapon's range.");
                        phase = 12; since = EditorApplication.timeSinceStartup;
                    }
                }
                else if (phase == 12 && elapsed > .4)
                {
                    Require(boss.transform.position.x < startX, "Boss failed to follow the player back into range.");
                    game.Artwork.transform.position = playerPosition;
                    tank.MachineGunStats.range = mgRange; tank.CannonStats.range = cannonRange; tank.MortarStats.range = mortarRange;
                    boss.transform.position = new Vector3(game.TankAimPoint.x + 12, boss.transform.position.y, 0);
                    float advance = tank.AdvanceDistanceNeeded;
                    if (advance > 0) boss.transform.position += Vector3.right * boss.ApproachDirection * (advance + .25f);
                    tank.enabled = true;
                    hull = game.Hull;
                    phase = 2; since = EditorApplication.timeSinceStartup;
                }
                else if (phase == 2 && elapsed > 8)
                {
                    Capture(game);
                    Require(tank.MachineGunAmmo < tank.MachineGunStats.reserveCapacity && tank.CannonAmmo < tank.CannonStats.reserveCapacity && tank.MortarAmmo < tank.MortarStats.reserveCapacity, $"All three boss weapons must fire and consume their own ammo. MG {tank.MachineGunAmmo}/{tank.MachineGunStats.reserveCapacity}, cannon {tank.CannonAmmo}/{tank.CannonStats.reserveCapacity}, mortar {tank.MortarAmmo}/{tank.MortarStats.reserveCapacity}. Target {game.TankAimPoint}; muzzles {tank.Artwork.MachineGun.Muzzle.position}, {tank.Artwork.MainGun.Muzzle.position}, {tank.Artwork.Mortar.Muzzle.position}.");
                    Require(game.Hull < hull && boss.Health == game.BossSettings.CalculateTankStats().maxHull, "Boss projectiles must damage player and ignore boss.");
                    Require(UnityEngine.Object.FindObjectsOfType<Projectile2D>().All(p => p.Team == ProjectileTeam.Enemy), "Boss weapons must create enemy projectiles.");
                    Require(JsonUtility.ToJson(game.BossProfile) == profileBefore && JsonUtility.ToJson(game.BossProfile.equipment.cannon) == gunBefore, "Battle must not mutate boss balance assets.");
                    tank.enabled = false;
                    foreach (var shot in UnityEngine.Object.FindObjectsOfType<Projectile2D>()) { shot.gameObject.SetActive(false); UnityEngine.Object.Destroy(shot.gameObject); }
                    hull = game.Hull;
                    // Player equipment may grant evasion; this probe checks team routing, not hit rolls.
                    var attack = new WeaponCombatStats { accuracy = 1000, armorPenetration = 100 };
                    var playerMortar = new GameObject("Player mortar regression probe").AddComponent<Projectile2D>();
                    playerMortar.InitializeMortar(new Vector2(boss.transform.position.x, BattleGame.GroundHeight + .2f), Vector2.down * 100, 10, 12, 6, 8, .45f, game.StartingLoadout.mortar.projectileSprite, attack);
                    var enemyMortar = new GameObject("Boss mortar regression probe").AddComponent<Projectile2D>();
                    enemyMortar.InitializeMortar(new Vector2(game.TankAimPoint.x, BattleGame.GroundHeight + .2f), Vector2.down * 100, 10, 12, 6, 8, .45f, game.BossProfile.equipment.mortar.projectileSprite, attack, ProjectileTeam.Enemy);
                    phase = 3; since = EditorApplication.timeSinceStartup;
                }
                else if (phase == 3 && elapsed > .5)
                {
                    Require(Mathf.Approximately(game.Hull, hull - 10) && Mathf.Approximately(boss.Health, game.BossSettings.CalculateTankStats().maxHull - 10), $"Each mortar team must deal exactly one full hit to its opponent and ignore its own side. Player: {hull} -> {game.Hull}; boss: {game.BossSettings.CalculateTankStats().maxHull} -> {boss.Health}.");
                    game.SetPaused(true); startX = boss.transform.position.x; hull = tank.ShotsFired;
                    Capture(game);
                    phase = 4; since = EditorApplication.timeSinceStartup;
                }
                else if (phase == 4 && elapsed > .5)
                {
                    Require(boss.transform.position.x == startX && tank.ShotsFired == hull, "Paused boss must not move or fire.");
                    game.SetPaused(false);
                    float before = boss.Health;
                    boss.ReceiveAttack(new DamageAttack(10, new WeaponCombatStats { accuracy = 100, armorPenetration = 100 }));
                    Require(boss.Health < before, "Player attacks must damage boss.");
                    boss.Damage(float.MaxValue);
                    Require(game.EnemyCount == 0 && game.Stage.Mode == StageMode.Victory, "Boss death must complete the stage.");
                    Require(game.SpawnBoss() == null && game.EnemyCount == 0, "A completed Boss encounter must not respawn the boss.");
                    Require(!errors, "Runtime errors occurred.");
                    Debug.Log("BOSS_TANK_AUDIT_OK: final-node spawn, independent profiles and crew, mirror, all-three-weapon range stopping/holding/resume, own tracks, all weapons/ammo, team damage, pause, asset isolation and victory.");
                    Finish(0);
                }
                if (elapsed > 40) throw new Exception("Boss audit timed out.");
            }
            catch (Exception e) { Debug.LogException(e); Finish(1); }
        }
        static EnemyEntity EnterBossStage(BattleGame game, StageDefinition stage)
        {
            game.SelectStage(stage); game.StartBattle(); StageProgressionAudit.DisableWeapons(game);
            game.SetStance(TankStance.Defensive);
            while (game.Stage.Current.EncounterType != StageNodeType.Boss)
            {
                StageProgressionAudit.ClearWave(game); game.Stage.OnRewardChosen();
                var next = game.Stage.Route.Get(game.Stage.Current.next[0]);
                if (next.type != StageNodeType.Boss) next.type = StageNodeType.Battle;
                game.Rewards.ResetRun();
                Require(game.Stage.SelectNode(next.id), "Cannot enter configured stage boss.");
            }
            return game.SpawnBoss(); // Returns the existing boss without duplicating it.
        }
        static void LeaveTestStage(BattleGame game)
        {
            game.ReturnHome();
            // Destroy is deferred to frame end; keep previous test encounters out of object searches.
            foreach (var entity in UnityEngine.Object.FindObjectsOfType<EnemyEntity>()) entity.gameObject.SetActive(false);
        }
        static void TestStageBossConfiguration(BattleGame game)
        {
            var original = game.Stage.Definition;
            string originalBefore = JsonUtility.ToJson(original), templateBefore = JsonUtility.ToJson(original.boss.template);
            var low = UnityEngine.Object.Instantiate(original); low.boss = original.boss.Copy();
            low.stageDifficulty = 0; low.rounds = 2;
            low.boss.machineGun.level = 2; low.boss.machineGun.stars = 1;
            low.boss.cannon.level = 4; low.boss.cannon.stars = 3;
            low.boss.mortar.level = 6; low.boss.mortar.stars = 2;
            low.boss.armor.level = 3; low.boss.armor.stars = 2;
            low.boss.crew[0].level = 3; low.boss.crew[0].stars = 1;
            var high = UnityEngine.Object.Instantiate(low); high.boss = low.boss.Copy(); high.stageDifficulty = 999; high.rounds = 9;
            var alternate = UnityEngine.Object.Instantiate(low); alternate.boss = low.boss.Copy();
            var template = UnityEngine.Object.Instantiate(low.boss.template);
            var hull = UnityEngine.Object.Instantiate(template.tank); hull.stats = template.tank.stats.Copy(); hull.stats.maxHull += 123;
            var loadout = UnityEngine.Object.Instantiate(template.equipment);
            var cannon = UnityEngine.Object.Instantiate(loadout.cannon); cannon.stats = loadout.cannon.stats.Copy(); cannon.stats.damage += 7;
            loadout.cannon = cannon; template.equipment = loadout; template.tank = hull; template.id = "AlternateBossAudit";
            alternate.boss.template = template;
            try
            {
                game.ReturnHome();
                var first = EnterBossStage(game, low);
                float hp = first.Health, mg = first.Boss.MachineGunStats.damage, gun = first.Boss.CannonStats.damage, mortar = first.Boss.MortarStats.damage;
                float crew = low.boss.CrewDamage;
                Require(Mathf.Approximately(mg, ProgressionRules.Damage(low.boss.template.equipment.machineGun.stats.damage, 2, 1) + crew)
                    && Mathf.Approximately(gun, ProgressionRules.Damage(low.boss.template.equipment.cannon.stats.damage, 4, 3) + crew)
                    && Mathf.Approximately(mortar, ProgressionRules.Damage(low.boss.template.equipment.mortar.stats.damage, 6, 2) + crew), "Each stage equipment slot must use its own level and stars.");
                float secondHP = low.boss.template.tank.ArmorSlots > 1 && low.boss.template.secondArmor != null
                    ? low.boss.template.secondArmor.HitPoints(low.boss.secondArmor.level, low.boss.secondArmor.stars) : 0;
                Require(Mathf.Approximately(hp, low.boss.template.tank.stats.maxHull + low.boss.template.armor.HitPoints(3, 2) + secondHP), "Stage armor progression must determine boss HP.");
                Require(!ReferenceEquals(first.Boss.Settings.cannon, low.boss.cannon) && !ReferenceEquals(first.Boss.Settings.crew[0], low.boss.crew[0]), "Spawned progression must be copied from the stage.");
                float expectedCrew = 0;
                foreach (var member in low.boss.template.crew)
                    if (member?.member != null) expectedCrew += 0; // Crew has no shared damage pool.
                Require(Mathf.Approximately(crew, expectedCrew), "Crew progression must match template station names.");
                first.Boss.Settings.cannon.level++;
                Require(low.boss.cannon.level == 4 && high.boss.cannon.level == 4, "Runtime stage settings must be isolated.");
                LeaveTestStage(game);
                var second = EnterBossStage(game, high);
                Require(game.BattleDifficulty == 1008 && second.Difficulty == 0 && Mathf.Approximately(second.Health, hp)
                    && Mathf.Approximately(second.Boss.MachineGunStats.damage, mg) && Mathf.Approximately(second.Boss.CannonStats.damage, gun)
                    && Mathf.Approximately(second.Boss.MortarStats.damage, mortar), "Stage difficulty and round count must not affect boss power.");
                LeaveTestStage(game);
                var third = EnterBossStage(game, alternate);
                Require(third.Boss.Profile == template && third.Data.id == "AlternateBossAudit" && Mathf.Approximately(third.Health, hp + 123)
                    && Mathf.Approximately(third.Boss.CannonStats.damage, alternate.boss.WeaponDamage(cannon.stats.damage, alternate.boss.cannon)), "The selected stage must spawn its chosen boss template.");
                LeaveTestStage(game);
                var retry = EnterBossStage(game, low);
                Require(Mathf.Approximately(retry.Health, hp) && Mathf.Approximately(retry.Boss.CannonStats.damage, gun), "Retry must rebuild fresh stats from the stage's template and progression.");
                Require(JsonUtility.ToJson(original) == originalBefore && JsonUtility.ToJson(original.boss.template) == templateBefore, "Tests must not mutate saved stage or template assets.");
                Debug.Log("BOSS_STAGE_CONFIG_OK: per-slot equipment/crew progression, armor HP, difficulty/round independence, alternate template selection, snapshot isolation and retry.");
            }
            finally
            {
                LeaveTestStage(game); game.SelectStage(original);
                foreach (var asset in new UnityEngine.Object[] { low, high, alternate, template, hull, loadout, cannon }) UnityEngine.Object.DestroyImmediate(asset);
            }
        }
        static void TestLimitingWeapon(BattleGame game)
        {
            boss.transform.position = new Vector3(game.TankAimPoint.x + 12, boss.transform.position.y, 0);
            tank.MachineGunStats.range = tank.CannonStats.range = tank.MortarStats.range = 100;
            Transform muzzle = limitingWeapon == 0 ? tank.Artwork.MachineGun.Muzzle
                : limitingWeapon == 1 ? tank.Artwork.MainGun.Muzzle : tank.Artwork.Mortar.Muzzle;
            float shortRange = Vector2.Distance(muzzle.position, game.TankAimPoint) - .15f;
            if (limitingWeapon == 0) tank.MachineGunStats.range = shortRange;
            else if (limitingWeapon == 1) tank.CannonStats.range = shortRange;
            else tank.MortarStats.range = shortRange;
            Require(!tank.AllWeaponsInRange && boss.Velocity.x < 0, "Boss must advance when even one weapon is out of range.");
            phase = 10; since = EditorApplication.timeSinceStartup;
        }
        static void Capture(BattleGame game)
        {
            // Explicit rendering also works when the batch editor has no Game view.
            var camera = game.TankCamera;
            var oldPosition = camera.transform.position;
            var oldTarget = camera.targetTexture;
            var oldActive = RenderTexture.active;
            var target = RenderTexture.GetTemporary(1920, 1080, 24);
            try
            {
                camera.transform.position = new Vector3((game.TankAimPoint.x + boss.transform.position.x) * .5f, -.05f, -10);
                camera.targetTexture = target; camera.Render(); RenderTexture.active = target;
                var image = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
                image.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0); image.Apply();
                string folder = System.IO.Path.Combine(System.IO.Directory.GetParent(Application.dataPath).FullName, "Checks");
                System.IO.Directory.CreateDirectory(folder);
                System.IO.File.WriteAllBytes(System.IO.Path.Combine(folder, overrideTemplate != null ? "heavy-boss-tank.png" : "boss-tank.png"), image.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(image);
            }
            finally { camera.transform.position = oldPosition; camera.targetTexture = oldTarget; RenderTexture.active = oldActive; RenderTexture.ReleaseTemporary(target); }
        }
        static void Finish(int code)
        {
            if (overrideStage != null && BattleGame.Instance != null)
            {
                BattleGame.Instance.ReturnHome(); BattleGame.Instance.SelectStage(originalStage);
                UnityEngine.Object.DestroyImmediate(overrideStage);
            }
            rewardSaves?.Dispose(); EditorApplication.update -= Check;
            EditorSettings.enterPlayModeOptionsEnabled = false; EditorApplication.Exit(code);
        }
    }
}
