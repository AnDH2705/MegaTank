using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace MegaTanks.Editor
{
    public static class TrojanScrapCatAudit
    {
        public static void Run()
        {
            EditorSceneManager.OpenScene(MegaTanksSetup.ScenePath);
            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            EditorApplication.isPlaying = true;
        }

        static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredPlayMode) return;
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            EditorApplication.delayCall += Check;
        }

        static void Check()
        {
            try
            {
                using var saves = new StageRewardSaveScope();
                var game = BattleGame.Instance;
                game.StartBattle();
                var opening = Object.FindObjectsOfType<EnemyEntity>().Where(enemy => enemy.Alive).ToArray();
                Require(opening.Count(enemy => enemy.Kind == EnemyKind.Ground) == 3 &&
                    opening.Count(enemy => enemy.Kind == EnemyKind.Flyer) == 2 &&
                    opening.All(enemy => enemy.Data.id != TrojanScrapCat.EnemyId),
                    "Stage 1 must start with three regular ground vehicles and two flyers.");
                foreach (var enemy in opening) enemy.gameObject.SetActive(false);
                game.ReturnHome();
                var stage = game.Frontend.stages.Single(definition => definition.stageNumber == 2);
                game.SelectStage(stage);
                CheckRelease(game, false);
                game.ReturnHome();
                CheckRelease(game, true);
                game.ReturnHome();
                Debug.Log("TROJAN_SCRAP_CAT_PASS: mixed ground formation, three animated occupants, both release triggers, melee, and wave completion.");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
                return;
            }
            EditorApplication.Exit(0);
        }

        static void CheckRelease(BattleGame game, bool destroyCarrier)
        {
            game.StartBattle();
            game.SetStance(TankStance.Defensive);
            StageProgressionAudit.DisableWeapons(game);
            var ground = Object.FindObjectsOfType<EnemyEntity>()
                .Where(enemy => enemy.Alive && enemy.Kind == EnemyKind.Ground)
                .OrderBy(enemy => enemy.transform.position.x * game.EnemySpawnDirection)
                .ToArray();
            var carrier = ground.Single(enemy => enemy.Data.id == TrojanScrapCat.EnemyId);
            var regularIds=game.Stage.Definition.spawnableEnemies.Where(p=>p!=null&&p.stats.kind==EnemyKind.Ground&&p.stats.id!=TrojanScrapCat.EnemyId).Select(p=>p.stats.id).ToArray();
            Require(ground.Length == 3 && ground.Where(enemy => enemy != carrier).All(enemy => regularIds.Contains(enemy.Data.id)),
                "The opening formation must mix one carrier with two regular ground vehicles.");
            Require(ground[0] != carrier && ground[ground.Length - 1] != carrier, "The carrier must spawn between ground vehicles.");
            foreach (var enemy in Object.FindObjectsOfType<EnemyEntity>())
            {
                if (enemy == carrier) continue;
                game.RemoveEnemy(enemy, false);
                enemy.gameObject.SetActive(false);
                Object.Destroy(enemy.gameObject);
            }

            var body = carrier.GetComponent<Collider2D>();
            var hull = game.HullHitbox.GetComponent<Collider2D>();
            var art = carrier.GetComponentInChildren<TrojanScrapCat>();
            var occupants = art.GetComponentsInChildren<CommandoArtwork>();
            var mask = art.GetComponentInChildren<SpriteMask>();
            Require(game.EnemyCount == 1 && carrier.Weapon == null, "The closed carrier must be the only active enemy and have no weapon.");
            Require(art.GetComponentsInChildren<SpriteRenderer>().All(renderer => renderer.sprite != null), "All carrier layers must have sprites.");
            Require(occupants.Length == 3 && occupants.All(occupant =>
                occupant.Animation.maskInteraction == SpriteMaskInteraction.VisibleInsideMask &&
                occupant.GetComponentInChildren<Collider2D>() == null), "Three masked dogs must ride inside without their own hitboxes.");
            Require(Mathf.Abs(body.bounds.min.y - BattleGame.GroundHeight) < .01f, "Carrier must be grounded.");
            float startX = carrier.transform.position.x;
            carrier.Tick(.5f);
            Require((carrier.transform.position.x - startX) * carrier.ApproachDirection > 0 && game.EnemyCount == 1,
                "Carrier must approach without releasing at long range.");
            Require(occupants.All(occupant => occupant.CurrentAnimation == "walk") &&
                occupants.Select(occupant => occupant.Animation.AnimationState.GetCurrent(0).TrackTime).Distinct().Count() == 3,
                "Dogs must walk in staggered strides while the carrier moves.");
            if (!destroyCarrier && SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null)
                CheckVisibleFeet(carrier, mask, occupants);

            if (destroyCarrier)
                carrier.Damage(carrier.Health);
            else
            {
                float x = hull.bounds.center.x - carrier.ApproachDirection * (hull.bounds.extents.x + body.bounds.extents.x + 2.9f);
                carrier.transform.position = new Vector3(x, carrier.transform.position.y, 0);
                carrier.Tick(0);
            }

            carrier.Damage(1000);
            carrier.Tick(1);
            var dogs = Object.FindObjectsOfType<EnemyEntity>().Where(enemy => enemy.Alive && enemy.Data.id == "TrojanMeleeDog").ToArray();
            Require(!carrier.Alive && !body.enabled && art.transform.parent == null, "The carrier must leave a cosmetic wreck and no hitbox.");
            Require(game.EnemyCount == 3 && dogs.Length == 3 && game.Stage.Mode == StageMode.Combat,
                "One release must register exactly three dogs before the wave can clear.");
            Require(dogs.Select(dog => dog.transform.position.x).Distinct().Count() == 3, "Dogs must have distinct spawn positions.");
            Require(!mask.enabled && dogs.All(dog => dog.Weapon.IsMelee && occupants.Contains(dog.CommandoArtwork) &&
                dog.CommandoArtwork.Animation.maskInteraction == SpriteMaskInteraction.None &&
                dog.WaveNumber == game.Wave && dog.Difficulty == game.BattleDifficulty),
                "The same three dogs must leave the carrier and join the wave as unmasked melee enemies.");

            if (!destroyCarrier)
            {
                var dog = dogs.OrderBy(d => Mathf.Abs(d.transform.position.x - game.TankAimPoint.x)).First();
                for (int step = 0; step < 80 && dog.Weapon.MeleeAttacks == 0; step++)
                {
                    dog.Tick(.05f);
                    dog.Weapon.Tick(.05f);
                }
                Require(dog.Weapon.MeleeAttacks == 1 && dog.Weapon.ShotsFired == 0, "A released dog must reach the tank and start a melee attack.");
            }

            for (int i = 0; i < 2; i++) dogs[i].Damage(dogs[i].Health);
            Require(game.EnemyCount == 1 && game.Stage.Mode == StageMode.Combat, "The last dog must keep the wave active.");
            dogs[2].Damage(dogs[2].Health);
            Require(game.EnemyCount == 0 && game.Stage.Mode == StageMode.ApproachingEvent, "Defeating the final dog must clear the wave and start travel to camp.");
        }

        static void CheckVisibleFeet(EnemyEntity carrier, SpriteMask mask, CommandoArtwork[] occupants)
        {
            var view = new GameObject("Scrap Cat audit camera").AddComponent<Camera>();
            var target = RenderTexture.GetTemporary(400, 360, 24);
            var image = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
            var previous = RenderTexture.active;
            view.enabled = false;
            view.transform.position = carrier.transform.position + carrier.Artwork.OwnerLocalBounds.center + Vector3.back * 10;
            view.orthographic = true;
            view.orthographicSize = 2;
            view.aspect = (float)target.width / target.height;
            view.clearFlags = CameraClearFlags.SolidColor;
            view.targetTexture = target;
            var renderers = occupants.Select(occupant => occupant.Animation.GetComponent<MeshRenderer>()).ToArray();
            try
            {
                var visible = Render();
                foreach (var renderer in renderers) renderer.enabled = false;
                var empty = Render();
                var min = view.WorldToScreenPoint(mask.bounds.min);
                var max = view.WorldToScreenPoint(mask.bounds.max);
                int changed = 0;
                for (int y = 0; y < target.height; y++)
                for (int x = 0; x < target.width; x++)
                {
                    int index = y * target.width + x;
                    if (visible[index].Equals(empty[index])) continue;
                    changed++;
                    Require(x >= min.x - 1 && x <= max.x + 1 && y >= min.y - 1 && y <= max.y + 1,
                        "Only dog pixels inside the belly opening may be visible.");
                }
                Require(changed > 20, "Dog legs must be visible through the belly opening.");
                Debug.Log("TROJAN_SCRAP_CAT_FEET_PASS: rendered dog pixels are visible and contained in the belly opening.");
            }
            finally
            {
                foreach (var renderer in renderers) renderer.enabled = true;
                RenderTexture.active = previous;
                view.targetTexture = null;
                RenderTexture.ReleaseTemporary(target);
                Object.Destroy(image);
                Object.Destroy(view.gameObject);
            }

            Color32[] Render()
            {
                view.Render();
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
                return image.GetPixels32();
            }
        }

        static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
