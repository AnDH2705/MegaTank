using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MegaTanks.Editor
{
    public static class StatsProfileAudit
    {
        static bool errors;
        public static void Run()
        {
            EditorSceneManager.OpenScene(MegaTanksSetup.ScenePath);
            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;
            EditorApplication.playModeStateChanged += state =>
            {
                if (state != PlayModeStateChange.EnteredPlayMode) return;
                errors = false;
                Application.logMessageReceived += (_, __, type) => { if (type == LogType.Error || type == LogType.Exception) errors = true; };
                EditorApplication.delayCall += Check;
            };
            EditorApplication.isPlaying = true;
        }
        static void Require(bool value, string message) { if (!value) throw new Exception(message); }
        static void Check()
        {
            try
            {
                var game = BattleGame.Instance;
                game.StartBattle(); StageProgressionAudit.DisableWeapons(game);
                var tank = game.TankProfile; var loadout = game.StartingLoadout;
                string tankBefore = JsonUtility.ToJson(tank), mgBefore = JsonUtility.ToJson(loadout.machineGun),
                    cannonBefore = JsonUtility.ToJson(loadout.cannon), mortarBefore = JsonUtility.ToJson(loadout.mortar), loadoutBefore = JsonUtility.ToJson(loadout);
                Require(game.MachineGun.Definition == loadout.machineGun && game.MainGun.Definition == loadout.cannon && game.Mortar.Definition == loadout.mortar,
                    "The starting loadout must configure each weapon from its own definition.");
                Require(!ReferenceEquals(game.BaseTankStats, tank.stats) && !ReferenceEquals(game.MachineGun.Stats, loadout.machineGun.stats)
                    && !ReferenceEquals(game.MainGun.Stats, loadout.cannon.stats) && !ReferenceEquals(game.Mortar.Stats, loadout.mortar.stats),
                    "All mutable runtime stats must be independent of definition assets.");
                Require(JsonUtility.ToJson(game.MachineGun.Stats) == JsonUtility.ToJson(loadout.machineGun.stats)
                    && JsonUtility.ToJson(game.MainGun.Stats) == JsonUtility.ToJson(loadout.cannon.stats)
                    && JsonUtility.ToJson(game.Mortar.Stats) == JsonUtility.ToJson(loadout.mortar.stats), "Initial runtime values must match the configured profiles exactly.");
                Require(game.Hull == tank.stats.maxHull && game.TankAdvanceSpeed == tank.stats.advanceSpeed
                    && game.Artwork.transform.localScale == Vector3.one * tank.stats.scale, "Tank-only settings must come from the tank definition.");
                var serialized = new SerializedObject(game);
                foreach (string oldField in new[] { "mgDamage", "mainGunDamage", "mgAmmoCapacity", "mainGunAmmoCapacity", "mortar", "tankScale", "tankAdvanceSpeed", "cannonProjectileSprite" })
                    Require(serialized.FindProperty(oldField) == null, "BattleGame must no longer own duplicated stats: " + oldField);
                game.MachineGun.IncreaseDamage(.1f); game.MachineGun.IncreaseDamage(.1f);
                Require(Mathf.Approximately(game.MachineGun.DamagePerShot, loadout.machineGun.stats.damage * 1.2f)
                    && game.MainGun.DamagePerShot == loadout.cannon.stats.damage, "Additive upgrades must remain local to the selected weapon.");
                var catalog = AssetDatabase.LoadAssetAtPath<CardCatalog>(CardDataImporter.CatalogPath);
                var hpCard = catalog.cards.Single(card => card.id == "9"); game.ApplyCardEffect(hpCard); game.ApplyCardEffect(hpCard);
                Require(Mathf.Approximately(game.MaxHull, tank.stats.maxHull * 1.2f), "HP cards must stack against configured base hull.");
                game.MachineGun.ConfigureAmmunition(2); game.MachineGun.TryConsumeRound(); game.MachineGun.Stats.range += 3;
                game.Mortar.Stats.damage += 5; game.Mortar.Stats.range += 10;
                Require(JsonUtility.ToJson(tank) == tankBefore && JsonUtility.ToJson(loadout.machineGun) == mgBefore
                    && JsonUtility.ToJson(loadout.cannon) == cannonBefore && JsonUtility.ToJson(loadout.mortar) == mortarBefore,
                    "Runtime buffs, ammunition and stat changes must not modify saved definitions.");
                var alternate = UnityEngine.Object.Instantiate(loadout.machineGun);
                alternate.stats = loadout.machineGun.stats.Copy(); alternate.displayName = "Alternate audit gun";
                alternate.stats.damage = 17; alternate.stats.reserveCapacity = 7; alternate.stats.range = 21;
                game.MachineGun.Configure(alternate);
                Require(game.MachineGun.DamagePerShot == 17 && game.MachineGun.Ammo == 7 && game.MachineGun.MaxRange == 21
                    && JsonUtility.ToJson(loadout) == loadoutBefore && JsonUtility.ToJson(tank) == tankBefore,
                    "A different weapon definition must configure the weapon without changing the tank or starting loadout.");
                game.StartBattle(); StageProgressionAudit.DisableWeapons(game);
                Require(game.MachineGun.Definition == loadout.machineGun && game.MachineGun.DamagePerShot == loadout.machineGun.stats.damage
                    && game.MachineGun.Ammo == loadout.machineGun.stats.reserveCapacity && game.MachineGun.MaxRange == loadout.machineGun.stats.range
                    && game.Mortar.Stats.damage == loadout.mortar.stats.damage && game.Mortar.Stats.range == loadout.mortar.stats.range
                    && game.Mortar.Ammo == loadout.mortar.stats.reserveCapacity && game.MaxHull == tank.stats.maxHull,
                    "Restart must restore the configured tank and starting weapon profiles.");
                UnityEngine.Object.Destroy(alternate);
                Require(!errors, "Runtime errors occurred.");
                Debug.Log("MEGATANKS_STATS_PROFILES_OK: separate tank/weapons/loadout assets, exact runtime copies, additive local buffs, no asset mutation, alternate weapon configuration and restart to starting profiles.");
                Finish(0);
            }
            catch (Exception exception) { Debug.LogException(exception); Finish(1); }
        }
        static void Finish(int code) { EditorSettings.enterPlayModeOptionsEnabled = false; EditorApplication.Exit(code); }
    }
}
