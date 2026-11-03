using UnityEditor;
using UnityEditor.SceneManagement;
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
            var scene = EditorSceneManager.OpenScene(MegaTanksSetup.ScenePath);
            var game = Object.FindObjectOfType<BattleGame>();
            var serialized = new SerializedObject(game);
            serialized.FindProperty("cannonProjectileSprite").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("MEGATANKS_CANNON_SPRITE_OK: transparent cannon projectile sprite imported and linked.");
        }
    }
}
