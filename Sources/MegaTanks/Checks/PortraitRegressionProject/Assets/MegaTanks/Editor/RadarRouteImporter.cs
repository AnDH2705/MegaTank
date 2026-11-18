using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace MegaTanks.Editor
{
    public static class RadarRouteImporter
    {
        public const string ArtRoot="Assets/MegaTanks/Art/UI/RadarRoute/";
        public const string SkinPath="Assets/MegaTanks/Data/UI/RadarRouteSkin.asset";
        [Serializable] sealed class Manifest{public Entry[] sprites;}
        [Serializable] sealed class Entry{public string file;public int[] border_left_bottom_right_top;}
        [MenuItem("MegaTanks/Import Radar Route UI")]
        public static void Import()
        {
            AssetDatabase.Refresh();
            var borders=new Dictionary<string,Vector4>();
            foreach(var entry in JsonUtility.FromJson<Manifest>(File.ReadAllText(ArtRoot+"sprite_manifest.json")).sprites)
            {
                var b=entry.border_left_bottom_right_top;
                if(b!=null&&b.Length==4)borders[entry.file]=new Vector4(b[0],b[1],b[2],b[3]);
            }
            var files=new Dictionary<string,string>
            {
                {"background","Background/scene_background_1920x1080.png"},
                {"header","Sprites/Frames/header_full_width_gray.png"},{"stagePlate","Sprites/Frames/header_stage_plate.png"},
                {"backPlate","Sprites/Frames/header_back_plate.png"},{"legend","Sprites/Frames/legend_panel.png"},
                {"nodeNormal","Sprites/Frames/node_normal.png"},{"nodeAvailable","Sprites/Frames/node_available.png"},
                {"nodeCurrent","Sprites/Frames/node_current.png"},{"nodeLocked","Sprites/Frames/node_locked.png"},
                {"battle","Sprites/Icons/01_battle.png"},{"hardBattle","Sprites/Icons/02_hard_battle.png"},
                {"supply","Sprites/Icons/03_supply.png"},{"unknown","Sprites/Icons/04_unknown.png"},{"repair","Sprites/Icons/05_heal.png"},
                {"backArrow","Sprites/Controls/back_arrow.png"},{"playerTank","Sprites/Controls/player_tank_marker.png"}
            };
            var skin=AssetDatabase.LoadAssetAtPath<RouteUISkin>(SkinPath);
            if(skin==null){skin=ScriptableObject.CreateInstance<RouteUISkin>();AssetDatabase.CreateAsset(skin,SkinPath);}
            var data=new SerializedObject(skin);
            foreach(var pair in files)
            {
                string path=ArtRoot+pair.Value;var importer=(TextureImporter)AssetImporter.GetAtPath(path);
                if(importer==null)throw new InvalidOperationException("Missing route artwork: "+path);
                importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.spritePixelsPerUnit=100;
                importer.spriteBorder=borders.TryGetValue(pair.Value,out var border)?border:Vector4.zero;
                importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.npotScale=TextureImporterNPOTScale.None;
                importer.maxTextureSize=2048;importer.textureCompression=TextureImporterCompression.Uncompressed;
                importer.filterMode=FilterMode.Bilinear;importer.wrapMode=TextureWrapMode.Clamp;
                var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);
                settings.spriteMeshType=SpriteMeshType.FullRect;settings.spriteAlignment=(int)SpriteAlignment.Center;settings.spritePivot=Vector2.one*.5f;
                importer.SetTextureSettings(settings);importer.SaveAndReimport();
                data.FindProperty(pair.Key).objectReferenceValue=AssetDatabase.LoadAssetAtPath<Sprite>(path);
            }
            data.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(skin);
            EditorSceneManager.OpenScene(MegaTanksSetup.ScenePath);
            var ui=UnityEngine.Object.FindObjectOfType<CanvasGameUI>();var view=new SerializedObject(ui);
            view.FindProperty("routeSkin").objectReferenceValue=skin;view.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(ui.gameObject.scene);EditorSceneManager.SaveOpenScenes();AssetDatabase.SaveAssets();
            Debug.Log("MEGATANKS_RADAR_ROUTE_IMPORTED: 16 supplied sprites, source slice borders, dedicated route skin, scene binding; procedural route logic preserved.");
        }
    }
}
