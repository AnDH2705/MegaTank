using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace MegaTanks.Editor
{
    public static class EquipmentArtworkImporter
    {
        public const string ArtRoot="Assets/MegaTanks/Art/UI/Equipment/";
        public const string SkinPath="Assets/MegaTanks/Data/UI/EquipmentSkin.asset";
        [MenuItem("MegaTanks/Import Equipment UI")]
        public static void Import()
        {
            AssetDatabase.Refresh();
            var files=new Dictionary<string,string>
            {
                {"background","Background.png"},{"button","Btn.png"},{"back","BtnBAck.png"},{"title","Title.png"},{"panel","Frame.png"},
                {"plus","IconPlus.png"},{"equippedBadge","IconTick(1).png"},{"equippedText","IconTick(2).png"},
                {"itemCard","Untitled-1_0000_Rectangle-4-copy-3.png"},{"slot","Untitled-1_0004_Layer-4.png"},
                {"slotSelected","Untitled-1_0005_Layer-3.png"},{"dropdownArrow","Untitled-1_0010_Triangle-1.png"},
                {"dropdownField","Untitled-1_0011_Rectangle-1-copy-6.png"}
            };
            var skin=AssetDatabase.LoadAssetAtPath<EquipmentUISkin>(SkinPath);
            if(skin==null){skin=ScriptableObject.CreateInstance<EquipmentUISkin>();AssetDatabase.CreateAsset(skin,SkinPath);}
            var fields=new SerializedObject(skin);
            foreach(var pair in files)
            {
                string path=ArtRoot+pair.Value;var importer=(TextureImporter)AssetImporter.GetAtPath(path);
                if(importer==null)throw new InvalidOperationException("Missing equipment sprite: "+path);
                importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.spritePixelsPerUnit=100;
                importer.spriteBorder=pair.Key=="dropdownField"?Vector4.one*12:Vector4.zero;
                importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.npotScale=TextureImporterNPOTScale.None;
                importer.maxTextureSize=2048;importer.textureCompression=TextureImporterCompression.Uncompressed;
                importer.filterMode=FilterMode.Bilinear;importer.wrapMode=TextureWrapMode.Clamp;
                var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);settings.spriteMeshType=SpriteMeshType.FullRect;
                settings.spriteAlignment=(int)SpriteAlignment.Center;settings.spritePivot=Vector2.one*.5f;
                importer.SetTextureSettings(settings);importer.SaveAndReimport();
                fields.FindProperty(pair.Key).objectReferenceValue=AssetDatabase.LoadAssetAtPath<Sprite>(path);
            }
            fields.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(skin);
            EditorSceneManager.OpenScene(MegaTanksSetup.ScenePath);
            var ui=UnityEngine.Object.FindObjectOfType<CanvasGameUI>();var view=new SerializedObject(ui);
            view.FindProperty("equipmentSkin").objectReferenceValue=skin;view.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(ui.gameObject.scene);EditorSceneManager.SaveOpenScenes();AssetDatabase.SaveAssets();
            Debug.Log("MEGATANKS_EQUIPMENT_ART_IMPORTED: 13 supplied sprites, dedicated Canvas skin, garage layout; current loadout, weapon stats and tank pose preserved.");
        }
    }
}
