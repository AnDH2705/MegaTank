using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace MegaTanks.Editor
{
    public static class HomeArtworkImporter
    {
        public const string HomeRoot="Assets/MegaTanks/Art/UI/Home/";
        public const string MortarSpritePath="Assets/MegaTanks/Art/Projectiles/mortar_ammo.png";
        [MenuItem("MegaTanks/Import Home UI And Mortar Shell")]
        public static void Import()
        {
            AssetDatabase.Refresh();
            var files=new Dictionary<string,string>
            {
                {"homeCrew","Tank_0000_Layer-2.png"},{"homeTech","Tank_0001_Layer-1.png"},
                {"homeGarage","Tank_0002_Shape-2.png"},{"homeStageSelector","Tank_0003_Shape-1-copy.png"},
                {"homeMenu","Tank_0004_Rectangle-2-copy-2.png"},{"homeButtonBase","Tank_0005_Rectangle-1-copy-7.png"},
                {"homeYellowPanel","Tank_0006_Rectangle-1.png"}
            };
            var skin=AssetDatabase.LoadAssetAtPath<CanvasUISkin>(LayerLabUISkinImporter.SkinPath);
            if(skin==null)throw new InvalidOperationException("The Canvas skin must be configured before importing home art.");
            var fields=new SerializedObject(skin);
            foreach(var pair in files)
            {
                ConfigureSprite(HomeRoot+pair.Value);
                fields.FindProperty(pair.Key).objectReferenceValue=AssetDatabase.LoadAssetAtPath<Sprite>(HomeRoot+pair.Value);
            }
            fields.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(skin);
            ConfigureSprite(MortarSpritePath);
            var mortar=AssetDatabase.LoadAssetAtPath<MortarDefinition>("Assets/MegaTanks/Data/Weapons/Mortar.asset");
            mortar.projectileSprite=AssetDatabase.LoadAssetAtPath<Sprite>(MortarSpritePath);EditorUtility.SetDirty(mortar);
            AssetDatabase.SaveAssets();
            Debug.Log("MEGATANKS_HOME_ART_IMPORTED: seven supplied home sprites, existing Canvas skin, mortar_ammo projectile; weapon stats and current tank pose preserved.");
        }
        static void ConfigureSprite(string path)
        {
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            if(importer==null)throw new InvalidOperationException("Missing supplied artwork: "+path);
            importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
            importer.spritePixelsPerUnit=100;importer.alphaIsTransparency=true;importer.mipmapEnabled=false;
            importer.npotScale=TextureImporterNPOTScale.None;importer.maxTextureSize=2048;
            importer.textureCompression=TextureImporterCompression.Uncompressed;importer.filterMode=FilterMode.Bilinear;
            importer.wrapMode=TextureWrapMode.Clamp;importer.spriteBorder=Vector4.zero;
            var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);
            settings.spriteMeshType=SpriteMeshType.FullRect;settings.spriteAlignment=(int)SpriteAlignment.Center;
            settings.spritePivot=Vector2.one*.5f;importer.SetTextureSettings(settings);importer.SaveAndReimport();
        }
    }
}
