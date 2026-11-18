using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace MegaTanks.Editor
{
    public static class AccessorySetup
    {
        public const string AutoLoaderPath="Assets/MegaTanks/Data/Accessories/AutoLoader.asset";
        public const string IconPath="Assets/MegaTanks/Art/Accessories/AutoLoader.png";
        public const string TargetingPath="Assets/MegaTanks/Data/Accessories/TargetingComputer.asset";
        public const string CamoPath="Assets/MegaTanks/Data/Accessories/CamoPaint.asset";
        static AccessoryDefinition Create(string path,string id,string title,string iconPath,AccessoryBonusType type)
        {
            AssetDatabase.ImportAsset(iconPath);
            var importer=(TextureImporter)AssetImporter.GetAtPath(iconPath);
            importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
            importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.maxTextureSize=512;
            importer.textureCompression=TextureImporterCompression.Uncompressed;importer.filterMode=FilterMode.Bilinear;importer.wrapMode=TextureWrapMode.Clamp;
            importer.SaveAndReimport();
            var item=AssetDatabase.LoadAssetAtPath<AccessoryDefinition>(path);
            if(item==null)
            {
                item=ScriptableObject.CreateInstance<AccessoryDefinition>();item.id=id;item.displayName=title;item.bonusType=type;
                item.baseBonus=10;item.bonusPerLevel=.5f;AssetDatabase.CreateAsset(item,path);
            }
            if(item.icon==null)item.icon=AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);
            EditorUtility.SetDirty(item);return item;
        }
        [MenuItem("MegaTanks/Configure Accessories")]
        public static void Install()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(AutoLoaderPath));AssetDatabase.Refresh();
            var loader=Create(AutoLoaderPath,"accessory.autoloader","Auto Loader",IconPath,AccessoryBonusType.ReloadSpeed);
            var targeting=Create(TargetingPath,"accessory.targeting-computer","Targeting Computer","Assets/MegaTanks/Art/Accessories/TargetingComputer.png",AccessoryBonusType.Accuracy);
            var camo=Create(CamoPath,"accessory.camo-paint","Camo Paint","Assets/MegaTanks/Art/Accessories/CamoPaint.png",AccessoryBonusType.Evasion);
            var catalog=AssetDatabase.LoadAssetAtPath<FrontendCatalog>(CanvasUISetup.CatalogPath);
            catalog.inventory=(catalog.inventory??Array.Empty<EquipmentDefinition>()).Concat(new EquipmentDefinition[]{loader,targeting,camo}).Distinct().ToArray();
            EditorUtility.SetDirty(catalog);AssetDatabase.SaveAssets();
            Debug.Log("ACCESSORY_SETUP_OK: Auto Loader, Targeting Computer and Camo Paint, generated alpha icons, +10% at level 1/+0.5pp per level and garage inventory.");
        }
    }
}
