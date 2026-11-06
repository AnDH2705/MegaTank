using System;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MegaTanks.Editor
{
    public static class StatsProfileImporter
    {
        public const string TankPath = "Assets/MegaTanks/Data/Tanks/PlayerTank.asset";
        public const string WeaponRoot = "Assets/MegaTanks/Data/Weapons/";
        public const string LoadoutPath = WeaponRoot + "StartingLoadout.asset";
        [MenuItem("MegaTanks/Migrate Player Stats To Profiles")]
        public static void Migrate()
        {
            // Read the old serialized values before saving the scene with the new component schema.
            string legacy = File.ReadAllText(MegaTanksSetup.ScenePath);
            string mortarText = Regex.Match(legacy, @"(?m)^  mortar:\r?\n((?:    .*\r?\n)+)").Groups[1].Value;
            float Read(string name, float fallback, string text = null, int indent = 2)
            {
                var match = Regex.Match(text ?? legacy, "(?m)^" + new string(' ', indent) + Regex.Escape(name) + @": ([-+\d.eE]+)");
                return match.Success ? float.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture) : fallback;
            }
            var spriteGuid = Regex.Match(legacy, @"(?m)^  cannonProjectileSprite:.*guid: ([a-f0-9]+)").Groups[1].Value;
            Sprite shell = AssetDatabase.LoadAssetAtPath<Sprite>(string.IsNullOrEmpty(spriteGuid) ? CannonProjectileImporter.SpritePath : AssetDatabase.GUIDToAssetPath(spriteGuid));
            var tank = Asset<TankDefinition>(TankPath, asset => asset.stats = new TankStats
                { maxHull = 100, advanceSpeed = Read("tankAdvanceSpeed", .5f), scale = Read("tankScale", 1.2f) });
            GunStats Gun(string prefix, bool heavy) => new GunStats
            {
                preferredTarget = heavy ? EnemyKind.Ground : EnemyKind.Flyer,
                damage = Read(prefix + "Damage", heavy ? 45 : 6), range = Read(prefix + "MaxRange", heavy ? 50 : 35),
                salvoSize = (int)Read(prefix + "SalvoSize", heavy ? 1 : 3), shotDelay = Read(prefix + "ShotDelay", .1f),
                reloadTime = Read(prefix + "ReloadTime", heavy ? 4 : 1.25f), gravity = Read(prefix + "ProjectileGravity", heavy ? 3 : .35f),
                reserveCapacity = (int)Read(prefix + "AmmoCapacity", heavy ? 50 : 150), projectileSpeed = 18,
                heavyProjectile = heavy, projectileSprite = heavy ? shell : null,
                projectileWidth = heavy ? Read("cannonProjectileWidth", 1.5f) : .65f
            };
            var mg = Asset<GunDefinition>(WeaponRoot + "MachineGun.asset", asset => { asset.displayName = "MG / AA"; asset.stats = Gun("mg", false); });
            var cannon = Asset<GunDefinition>(WeaponRoot + "Cannon.asset", asset => { asset.displayName = "MAIN GUN"; asset.stats = Gun("mainGun", true); });
            var mortar = Asset<MortarDefinition>(WeaponRoot + "Mortar.asset", asset =>
            {
                asset.displayName = "MORTAR"; asset.projectileSprite = shell;
                var deviation = Regex.Match(mortarText, @"deviation: \{x: ([-+\d.eE]+), y: ([-+\d.eE]+)\}");
                asset.stats = new MortarStats
                {
                    damage = Read("damage", 60, mortarText, 4), reloadTime = Read("reloadTime", 6, mortarText, 4),
                    range = Read("range", 80, mortarText, 4), gravity = Read("gravity", 12, mortarText, 4),
                    launchAngle = Read("launchAngle", 70, mortarText, 4), reserveCapacity = (int)Read("reserveCapacity", 20, mortarText, 4),
                    fullDamageRadius = Read("fullDamageRadius", 2.5f, mortarText, 4), halfDamageRadius = Read("halfDamageRadius", 5, mortarText, 4),
                    shellWidth = Read("shellWidth", .45f, mortarText, 4),
                    deviation = deviation.Success ? new Vector2(float.Parse(deviation.Groups[1].Value, CultureInfo.InvariantCulture), float.Parse(deviation.Groups[2].Value, CultureInfo.InvariantCulture)) : new Vector2(1, 3)
                };
            });
            var loadout = Asset<WeaponLoadout>(LoadoutPath, asset => { asset.machineGun = mg; asset.cannon = cannon; asset.mortar = mortar; });
            AssetDatabase.SaveAssets();
            var scene = EditorSceneManager.OpenScene(MegaTanksSetup.ScenePath);
            var serialized = new SerializedObject(UnityEngine.Object.FindObjectOfType<BattleGame>());
            serialized.FindProperty("tankDefinition").objectReferenceValue = tank;
            serialized.FindProperty("startingLoadout").objectReferenceValue = loadout;
            serialized.ApplyModifiedPropertiesWithoutUndo(); EditorSceneManager.SaveScene(scene);
            Debug.Log("MEGATANKS_STATS_MIGRATED: current scene tuning preserved in tank + three independent weapon definitions and starting loadout; existing profiles left untouched.");
        }
        static T Asset<T>(string path, Action<T> initialize) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            Directory.CreateDirectory(Path.GetDirectoryName(path)); AssetDatabase.Refresh();
            asset = ScriptableObject.CreateInstance<T>(); initialize(asset); AssetDatabase.CreateAsset(asset, path); return asset;
        }
    }
}
