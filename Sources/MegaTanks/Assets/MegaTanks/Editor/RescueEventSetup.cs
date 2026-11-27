using System;
using System.IO;
using UnityEditor;
using UnityEngine;
namespace MegaTanks.Editor
{
    [InitializeOnLoad] public static class RescueEventSetup
    {
        static RescueEventSetup(){EditorApplication.update+=Pending;}
        static void Pending()
        {
            const string flag="Checks/rescue-apply.pending";
            if(!File.Exists(flag)||EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorApplication.isUpdating||!OriginalPortSetup.AssembliesCurrent())return;
            File.Delete(flag);try{Apply();}catch(Exception e){Debug.LogException(e);File.WriteAllText("Checks/rescue-apply.error",e.ToString());}
        }
        [MenuItem("MegaTanks/Rescue/Import original cage")]
        public static void Apply()
        {
            const string dest="Assets/MegaTanks/Art/Props/Prisoner_Cage/prisoner_cage.png";
            string source=Path.GetFullPath(Path.Combine(Application.dataPath,"../../../Art/Asset/Props/Prisoner_Cage/prisoner_cage.png"));
            Directory.CreateDirectory(Path.GetDirectoryName(dest));File.Copy(source,dest,true);AssetDatabase.ImportAsset(dest,ImportAssetOptions.ForceUpdate);
            var importer=(TextureImporter)AssetImporter.GetAtPath(dest);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.spritePixelsPerUnit=100;importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.npotScale=TextureImporterNPOTScale.None;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.filterMode=FilterMode.Bilinear;importer.maxTextureSize=2048;
            var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);settings.spriteMeshType=SpriteMeshType.FullRect;importer.SetTextureSettings(settings);importer.SaveAndReimport();
            const string path="Assets/MegaTanks/Resources/RescueEventArt.asset";var art=AssetDatabase.LoadAssetAtPath<RescueEventArt>(path);
            if(art==null){art=ScriptableObject.CreateInstance<RescueEventArt>();AssetDatabase.CreateAsset(art,path);}
            art.cage=AssetDatabase.LoadAssetAtPath<Sprite>(dest);
            art.crewPortrait=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/MegaTanks/Art/UI/CrewHtml/illustrations/cat_driver.png");
            if(art.crewPortrait==null)throw new InvalidOperationException("Missing original Art/Asset/UI/Crew/illustrations/cat_driver.png import.");
            EditorUtility.SetDirty(art);AssetDatabase.SaveAssets();
            File.WriteAllText("Checks/rescue-ready.flag",DateTime.UtcNow.ToString("O"));Debug.Log("RESCUE_ART_READY: original cage PNG; stage-scoped companions, post-boss Common recruitment.");
        }
    }
}
