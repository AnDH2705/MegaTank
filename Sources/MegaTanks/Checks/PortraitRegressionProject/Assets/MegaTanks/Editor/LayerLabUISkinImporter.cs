using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace MegaTanks.Editor
{
    public static class LayerLabUISkinImporter
    {
        public const string SkinPath="Assets/MegaTanks/Data/UI/LayerLabSkin.asset";
        public const string ArtPath="Assets/MegaTanks/Art/UI/LayerLab/";
        [MenuItem("MegaTanks/Import Layer Lab UI Skin")]
        public static void Import()
        {
            AssetDatabase.Refresh();
            var sprites=new Dictionary<string,string>
            {
                {"buttonBlue","Button01_l_Blue"},{"buttonDark","Button01_l_DarkGray"},{"buttonDisabled","Button01_l_Gray"},
                {"buttonMint","Button01_l_Mint"},{"buttonRed","Button01_l_Red"},{"buttonSky","Button01_l_Sky"},{"buttonYellow","Button01_l_Yellow"},
                {"panel","Popup02~09_Topber_White_Bg"},{"panelHeader","Popup02~09_Topber_White_BgTop"},{"roundFrame","BasicFrame_Round17"},{"itemFrame","ItemFrame03_Single_Navy"},
                {"check","Icon_Check01"},{"close","Icon_Close01"},{"menu","Icon_Menu_Hamburger"},{"pause","Icon_Pause"},
                {"previous","Icon_Arrow_Prev1"},{"next","Icon_Arrow_Next1"},{"music","Icon_Music"},{"sound","PictoIcon_Sound"},{"settings","Icon_Setting"},
                {"sliderBackground","Slider_Handle_Bg"},{"sliderFill","Slider_Handle_Fill_White1"},{"sliderHandle","Slider_Handle_Handle"},
                {"barBackground","Slider_Play01_Bg"},{"barFill","Slider_Play01_Fill"},
                {"switchBackground","Switch_White_Bg"},{"switchFill","Switch_White_Fill1"},{"switchHandle","Switch_White_Handle"}
            };
            foreach(var file in Directory.GetFiles(ArtPath))
            {
                if(!file.EndsWith(".png",StringComparison.OrdinalIgnoreCase))continue;
                var importer=(TextureImporter)AssetImporter.GetAtPath(file);var border=importer.spriteBorder;
                importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
                importer.spritePixelsPerUnit=100;importer.spriteBorder=border;importer.alphaIsTransparency=true;
                importer.mipmapEnabled=false;importer.npotScale=TextureImporterNPOTScale.None;importer.maxTextureSize=512;
                importer.textureCompression=TextureImporterCompression.Uncompressed;importer.filterMode=FilterMode.Bilinear;importer.wrapMode=TextureWrapMode.Clamp;
                var textureSettings=new TextureImporterSettings();importer.ReadTextureSettings(textureSettings);textureSettings.spriteMeshType=SpriteMeshType.FullRect;importer.SetTextureSettings(textureSettings);
                importer.SaveAndReimport();
            }
            var skin=AssetDatabase.LoadAssetAtPath<CanvasUISkin>(SkinPath);
            if(skin==null){skin=ScriptableObject.CreateInstance<CanvasUISkin>();AssetDatabase.CreateAsset(skin,SkinPath);}
            var data=new SerializedObject(skin);
            foreach(var pair in sprites)
            {
                string extension=pair.Value=="PictoIcon_Sound"?".Png":".png";
                var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(ArtPath+pair.Value+extension);
                if(sprite==null)throw new InvalidOperationException("Missing Layer Lab sprite: "+pair.Value);
                data.FindProperty(pair.Key).objectReferenceValue=sprite;
            }
            data.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(skin);
            EditorSceneManager.OpenScene(MegaTanksSetup.ScenePath);
            var ui=UnityEngine.Object.FindObjectOfType<CanvasGameUI>();
            if(ui==null)throw new InvalidOperationException("Configure Canvas UI before assigning its skin.");
            var view=new SerializedObject(ui);view.FindProperty("skin").objectReferenceValue=skin;view.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(ui.gameObject.scene);EditorSceneManager.SaveOpenScenes();AssetDatabase.SaveAssets();
            Debug.Log("MEGATANKS_LAYERLAB_SKIN_IMPORTED: 28 selected source sprites, preserved slice borders, Canvas skin asset and scene reference; no demo scenes, scripts, fonts or TMP dependency.");
        }
    }
}
