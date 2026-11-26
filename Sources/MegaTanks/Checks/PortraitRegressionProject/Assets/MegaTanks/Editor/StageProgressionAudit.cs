using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace MegaTanks.Editor
{
    public static class StageProgressionAudit
    {
        static readonly MethodInfo AdvanceMethod = typeof(StageProgression).GetMethod("Advance", BindingFlags.Instance | BindingFlags.NonPublic);
        static readonly PropertyInfo CampTypeProperty = typeof(StageProgression).GetProperty(nameof(StageProgression.CampType));
        static StageRewardSaveScope rewardSaves;
        static double deadline;
        static bool errors, captureCamp;
        static int shops, battleStartScraps;
        static float battleCameraOffset, departureX, departureTime;

        public static void CaptureCamp() { captureCamp = true; Run(); }

        public static void Run()
        {
            rewardSaves = new StageRewardSaveScope();
            EditorSceneManager.OpenScene(MegaTanksSetup.ScenePath);
            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;
            EditorApplication.playModeStateChanged += state =>
            {
                if (state != PlayModeStateChange.EnteredPlayMode) return;
                try
                {
                    Application.logMessageReceived += (_, __, type) => { if (type == LogType.Error || type == LogType.Exception) errors = true; };
                    var game = BattleGame.Instance;
                    var definition = UnityEngine.Object.Instantiate(game.Stage.Definition);
                    definition.rounds = 7;
                    definition.hardBattleWeight = 0;
                    definition.waveBreakSeconds = .1f;
                    definition.backgroundId = "RuinedCity";
                    game.SelectStage(definition);
                    Require(game.Backgrounds.CurrentPreset.id == definition.backgroundId, "Selecting the stage must apply its background on Home.");
                    game.StartBattle();
                    game.SetStance(TankStance.Defensive);
                    DisableWeapons(game);
                    game.Stage.Route.rounds[1][0].type = StageNodeType.HardBattle;
                    Require(game.Stage.Route.rounds.All(round => round.Count == 1)
                        && game.Stage.CurrentRoundNumber == 1 && game.Wave == 1, "The stage must begin as a single battle sequence.");
                    deadline = EditorApplication.timeSinceStartup + 90;
                    EditorApplication.update += Check;
                }
                catch (Exception exception) { Debug.LogException(exception); Finish(1); }
            };
            EditorApplication.isPlaying = true;
        }

        public static void DisableWeapons(BattleGame game) { game.MachineGun.enabled = game.MainGun.enabled = game.Mortar.enabled = false; }

        public static void ClearWave(BattleGame game, bool resolveCamp = true)
        {
            EnemyEntity[] targets;
            do
            {
                targets = UnityEngine.Object.FindObjectsOfType<EnemyEntity>().Where(enemy => enemy.Alive).ToArray();
                foreach (var enemy in targets) enemy.Damage(enemy.Health);
            } while (game.EnemyCount > 0 && targets.Length > 0);
            if (resolveCamp) OpenCampRewards(game);
        }

        public static void OpenCampRewards(BattleGame game, TentType type = TentType.Upgrade)
        {
            if (game.Stage.Mode != StageMode.ApproachingEvent) return;
            if (game.Stage.CampType != TentType.Vendor) CampTypeProperty.SetValue(game.Stage, type);
            AdvanceStage(game, float.MaxValue);
        }

        public static void EnterNextBattle(BattleGame game) => EnterType(game, StageNodeType.Battle);

        public static void EnterType(BattleGame game, StageNodeType type)
        {
            var next = game.Stage.Route.Get(game.Stage.Current.next[0]);
            if (next.type != StageNodeType.Boss) next.type = type;
            OpenCampRewards(game);
            if (game.Rewards.Mode == IntermissionMode.Reward) game.Rewards.ChooseReward(0);
            if (game.Rewards.Mode == IntermissionMode.Shop) game.Rewards.LeaveShop();
            Require(game.Stage.Mode == StageMode.Advancing, "The resolved camp must start travel.");
            AdvanceStage(game, 1f);
            Require(game.Stage.Current == next, "Travel must reach the next battle.");
            DisableWeapons(game);
        }

        static void AdvanceStage(BattleGame game, float seconds) => AdvanceMethod.Invoke(game.Stage, new object[] { seconds });
        static void Require(bool value, string message) { if (!value) throw new Exception(message); }

        static float CameraOffset(BattleGame game) => game.TankCamera.transform.position.x - game.Artwork.transform.position.x;

        static void Check()
        {
            try
            {
                var game = BattleGame.Instance;
                var stage = game.Stage;
                Require(EditorApplication.timeSinceStartup < deadline, "Stage audit timed out at battle " + stage.CurrentRoundNumber + " in " + stage.Mode);
                Require(game.Backgrounds.CurrentPreset.id == stage.Definition.backgroundId, "The stage background must persist through battles, waves and tents.");
                Require(stage.Mode != StageMode.Map, "The battle sequence must advance without a node map.");
                if (stage.Mode == StageMode.Combat)
                {
                    if (departureTime > 0)
                    {
                        Require(Time.time - departureTime >= .99f && Mathf.Abs(game.Artwork.transform.position.x - departureX - game.TankAdvanceSpeed) < .01f,
                            "A resolved camp must be followed by one second of travel at normal speed.");
                        Require(Mathf.Abs(CameraOffset(game) - battleCameraOffset) < .001f, "The next battle must preserve the pan offset.");
                        departureTime = 0;
                    }
                    if (stage.EncounterWave == 1) battleStartScraps = game.Rewards.Scraps;
                    if (stage.CurrentRoundNumber == 3) game.CameraPan.PanByPixels(-game.TankCamera.pixelWidth * .5f);
                    battleCameraOffset = CameraOffset(game);
                    game.DamageHull(1);
                    game.MachineGun.TryConsumeRound();
                    ClearWave(game, resolveCamp: false);
                    if (stage.EncounterWave < stage.EncounterWaves)
                    {
                        Require(stage.Mode == StageMode.WaveBreak && !game.Rewards.IsOpen, "Intermediate hard waves must skip camp.");
                        return;
                    }
                    if (stage.Current.EncounterType == StageNodeType.Boss) return;

                    Require(stage.Mode == StageMode.ApproachingEvent && !game.Rewards.IsOpen, "A cleared encounter must approach a tent before opening its event.");
                    int round = stage.CurrentRoundNumber;
                    bool vendor = round == 5 || round == 6;
                    Require((stage.CampType == TentType.Vendor) == vendor, "Vendor stops must occur after five battles and immediately before the boss.");
                    if (!vendor) CampTypeProperty.SetValue(stage, (TentType)((round - 1) % 3));
                    CheckApproach(game);
                }
                else if (stage.Mode == StageMode.Event || stage.Mode == StageMode.Reward || stage.Mode == StageMode.Shop)
                {
                    Require(Mathf.Abs(CameraOffset(game) - battleCameraOffset) < .001f, "Camp arrival must preserve the battle framing.");
                    CheckOffers(game);
                }
                else if (stage.Mode == StageMode.Victory)
                {
                    Require(game.StageRewards.LastReceipt != null && game.Wave == 9 && shops == 2
                        && !game.Rewards.IsOpen && GameObject.Find("Field Camp Tent") == null && !errors,
                        "The final boss must lead directly to victory with the stage receipt.");
                    if (captureCamp)
                    {
                        var layout = UnityEngine.Object.FindObjectOfType<UICanvasLayout>();
                        if (!layout.View(UIScreen.Victory).gameObject.activeInHierarchy) return;
                        ItemProgressionAudit.Capture(game, "StageVictory");
                    }
                    Debug.Log("MEGATANKS_STAGE_OK: automatic tents, panning, service cards, vendor schedule, hard waves, and direct victory.");
                    Finish(0);
                }
            }
            catch (Exception exception) { Debug.LogException(exception); Finish(1); }
        }

        static void CheckApproach(BattleGame game)
        {
            float edge = game.TankCamera.ViewportToWorldPoint(new Vector3(1, .5f, 0)).x;
            float start = game.Artwork.transform.position.x;
            AdvanceStage(game, .1f);
            var tent = GameObject.Find("Field Camp Tent");
            Require(tent != null && tent.GetComponent<SpriteRenderer>().bounds.min.x > edge, "The tent must spawn beyond the battle viewport.");
            Require(game.Stage.Mode == StageMode.ApproachingEvent
                && Mathf.Abs(game.Artwork.transform.position.x - start - .3f * game.TankAdvanceSpeed) < .001f,
                "The tank must drive to the tent at triple speed with event choices closed.");
            Require(Mathf.Abs(CameraOffset(game) - battleCameraOffset) < .001f, "The approach must preserve the battle framing.");
            AdvanceStage(game, float.MaxValue);
            float front = game.Artwork.GetComponentsInChildren<SpriteRenderer>()
                .Where(renderer => renderer.enabled && renderer.sprite != null && renderer.color.a > 0).Max(renderer => renderer.bounds.max.x);
            float gap = tent.GetComponent<SpriteRenderer>().bounds.min.x - front;
            Require(gap > 0 && gap < .75f, "The tank must stop just short of the tent.");
            var icon = tent.transform.Find("Camp Road Sign/Camp Sign Icon").GetComponent<SpriteRenderer>();
            var skin = game.UI.RouteSkin;
            var expected = game.Stage.CampType switch
            {
                TentType.Repair => skin.repair,
                TentType.Resupply => skin.supply,
                TentType.Vendor => skin.shop,
                _ => skin.backArrow
            };
            Require(icon.sprite == expected && icon.transform.position.x < tent.transform.position.x,
                "The matching sign must stand before the tent.");
        }

        static void CheckOffers(BattleGame game)
        {
            var offers = game.Rewards.Offers;
            var layout = UnityEngine.Object.FindObjectOfType<UICanvasLayout>();
            if (game.Stage.Mode == StageMode.Shop)
            {
                var shop = layout.View(UIScreen.Shop);
                if (!shop.gameObject.activeSelf) return;
                Require(offers.Length == 8 && offers.Select(offer => offer.card.id).Distinct().Count() == 8, "A vendor must open the eight-card shop.");
                if (captureCamp && shops == 0) ItemProgressionAudit.Capture(game, "StageVendor");
                shops++;
                ItemProgressionAudit.Press(shop.Get<Button>("Leave Shop"));
            }
            else if (game.Stage.Mode == StageMode.Event)
            {
                var panel = game.UI.GetComponentsInChildren<UIView>().FirstOrDefault(view => view.name == "Camp Event");
                if (panel == null) return;
                bool repair = game.Stage.CampType == TentType.Repair;
                int balance = game.Rewards.Scraps;
                int drops = balance - battleStartScraps;
                Require(offers.Length == 2 && offers[0].card.rarity == CardRarity.Normal
                    && offers[0].card.effect == (repair ? CardEffect.Repair : CardEffect.Resupply), "A service tent must offer its common service card.");
                var scavenge = offers[1].card;
                Require(scavenge.rarity == CardRarity.Rare && scavenge.effect == CardEffect.Scraps && scavenge.art != null
                    && scavenge.scraps * 100L >= drops * 75L && scavenge.scraps * 100L <= drops * 110L,
                    "Scavenge must award 75–110% of encounter drops and show the framed rare card.");
                Require(panel.Get<Image>("Card 1/Artwork").sprite == scavenge.art, "The displayed Scavenge artwork must come from its card.");
                if (captureCamp) ItemProgressionAudit.Capture(game, repair ? "StageRepair" : "StageResupply");
                int ammo = game.MachineGun.Ammo;
                float hull = game.Hull;
                ItemProgressionAudit.Press(panel.Get<Button>(repair ? "Card 0/Action" : "Card 1/Action"));
                Require(game.MachineGun.Ammo == ammo
                    && (repair ? game.Hull > hull && game.Rewards.Scraps == balance : game.Hull == hull && game.Rewards.Scraps == balance + scavenge.scraps),
                    "Only the chosen service or scrap reward may apply.");
            }
            else
            {
                var rewards = layout.View(UIScreen.Rewards);
                if (!rewards.gameObject.activeSelf) return;
                Require(offers.Length == 3 && offers.Select(offer => offer.card.id).Distinct().Count() == 3
                    && offers.All(offer => offer.card.effect != CardEffect.Repair && offer.card.effect != CardEffect.Resupply
                        && offer.card.effect != CardEffect.Scraps), "Upgrade tents must offer three distinct upgrades.");
                if (captureCamp && game.Stage.CurrentRoundNumber == 1) ItemProgressionAudit.Capture(game, "StageUpgrade");
                ItemProgressionAudit.Press(rewards.Get<Button>("Card 1/Action"));
            }
            Require(game.Stage.Mode == StageMode.Advancing && !game.Rewards.IsOpen && !game.Rewards.ChooseReward(0),
                "A camp resolution must close the choices and start travel exactly once.");
            departureTime = Time.time;
            departureX = game.Artwork.transform.position.x;
        }

        static void Finish(int code)
        {
            EditorApplication.update -= Check;
            rewardSaves?.Dispose();
            EditorSettings.enterPlayModeOptionsEnabled = false;
            EditorApplication.Exit(code);
        }
    }
}
