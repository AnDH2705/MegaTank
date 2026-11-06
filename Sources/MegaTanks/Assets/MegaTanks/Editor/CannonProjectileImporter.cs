using UnityEditor;
using UnityEngine;

namespace MegaTanks.Editor
{
    public static class CannonProjectileImporter
    {
        public const string SpritePath = "Assets/MegaTanks/Art/Projectiles/CannonShell.png";

        [MenuItem("MegaTanks/Import Cannon Projectile")]
        public static void Import()
        {
            AssetDatabase.Refresh();
            var importer = (TextureImporter)AssetImporter.GetAtPath(SpritePath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.maxTextureSize = 512;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            var loadout = AssetDatabase.LoadAssetAtPath<WeaponLoadout>(StatsProfileImporter.LoadoutPath);
            if (loadout == null) throw new System.InvalidOperationException("Migrate player stats to profiles first.");
            loadout.cannon.stats.projectileSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
            EditorUtility.SetDirty(loadout.cannon);
            AssetDatabase.SaveAssets();
            Debug.Log("MEGATANKS_CANNON_SPRITE_OK: transparent cannon projectile sprite imported and linked.");
        }
    }
}
