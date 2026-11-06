using System;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MegaTanks.Editor
{
    public static class CardDataImporter
    {
        public const string CatalogPath = "Assets/MegaTanks/Data/Cards/CardCatalog.asset";
        public const string AtlasPath = "Assets/MegaTanks/Art/Cards/CardAtlas.png";
        const string SnapshotPath = "Assets/MegaTanks/Data/Cards/SheetSnapshot.json";
        [Serializable] sealed class SourceCard { public string id, name, description, rarity, value; public int shopValue; }
        [Serializable] sealed class Snapshot { public string sourceUrl; public SourceCard[] cards; }

        [MenuItem("MegaTanks/Import Card Catalog")]
        public static void Import()
        {
            AssetDatabase.Refresh();
            var snapshot = JsonUtility.FromJson<Snapshot>(File.ReadAllText(SnapshotPath));
            if (snapshot.cards.Length != 18) throw new InvalidOperationException("Expected 18 spreadsheet cards for this atlas.");
            var textureImporter = (TextureImporter)AssetImporter.GetAtPath(AtlasPath);
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(AtlasPath);
            int width = texture.width, height = texture.height;
            textureImporter.textureType = TextureImporterType.Sprite;
            textureImporter.spriteImportMode = SpriteImportMode.Multiple;
            textureImporter.mipmapEnabled = false;
            textureImporter.npotScale = TextureImporterNPOTScale.None;
            textureImporter.maxTextureSize = 2048;
            textureImporter.wrapMode = TextureWrapMode.Clamp;
            textureImporter.filterMode = FilterMode.Bilinear;
            textureImporter.textureCompression = TextureImporterCompression.CompressedHQ;
            textureImporter.spritesheet = snapshot.cards.Select((card, index) =>
            {
                int column = index % 6, row = index / 6;
                int left = Mathf.RoundToInt(column * width / 6f), right = Mathf.RoundToInt((column + 1) * width / 6f);
                int top = Mathf.RoundToInt(row * height / 3f), bottom = Mathf.RoundToInt((row + 1) * height / 3f);
                return new SpriteMetaData { name = "Card_" + card.id.PadLeft(2, '0'), rect = new Rect(left, height - bottom, right - left, bottom - top), alignment = 0, pivot = Vector2.one * .5f };
            }).ToArray();
            textureImporter.SaveAndReimport();
            var sprites = AssetDatabase.LoadAllAssetsAtPath(AtlasPath).OfType<Sprite>().ToDictionary(sprite => sprite.name);
            var catalog = AssetDatabase.LoadAssetAtPath<CardCatalog>(CatalogPath);
            if (catalog == null) { catalog = ScriptableObject.CreateInstance<CardCatalog>(); AssetDatabase.CreateAsset(catalog, CatalogPath); }
            catalog.sourceUrl = snapshot.sourceUrl;
            catalog.cards = snapshot.cards.Select(card => new CardDefinition
            {
                id = card.id, title = card.name, description = card.description,
                rarity = (CardRarity)Enum.Parse(typeof(CardRarity), card.rarity),
                effect = ParseEffect(card.name), fraction = float.Parse(card.value.TrimEnd('%'), CultureInfo.InvariantCulture) / 100,
                price = card.shopValue, art = sprites["Card_" + card.id.PadLeft(2, '0')]
            }).ToArray();
            catalog.Validate();
            EditorUtility.SetDirty(catalog);
            var scene = EditorSceneManager.OpenScene(MegaTanksSetup.ScenePath);
            var game = UnityEngine.Object.FindObjectOfType<BattleGame>();
            var serialized = new SerializedObject(game);
            serialized.FindProperty("cardCatalog").objectReferenceValue = catalog;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("MEGATANKS_CARD_IMPORT_OK: 18 spreadsheet definitions, exact values/prices, rarity pools and generated art linked.");
        }

        static CardEffect ParseEffect(string title)
        {
            switch (title)
            {
                case "Cannon damage": return CardEffect.CannonDamage;
                case "MG damage": return CardEffect.MachineGunDamage;
                case "Tank HP": return CardEffect.MaxHull;
                case "Resupply order": return CardEffect.Resupply;
                case "Repair order": return CardEffect.Repair;
                default: throw new InvalidOperationException("Unknown spreadsheet effect: " + title);
            }
        }
    }
}
