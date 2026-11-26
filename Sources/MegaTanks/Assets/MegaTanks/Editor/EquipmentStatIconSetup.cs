using System;
using UnityEditor;
using UnityEngine;

namespace MegaTanks.Editor
{
    public static class EquipmentStatIconSetup
    {
        const string Folder="Assets/MegaTanks/Art/UI/Stat_Icons/";
        public static readonly string[] Ids={"atk","accuracy","hp","defense","move_speed","crit_rate","penetration","crit_damage","range","ammo","reload_time","reload_speed","evasion"};
        [MenuItem("MegaTanks/Equipment/Apply Stat Icons")]
        public static void Apply()
        {
            AssetDatabase.Refresh();var sprites=new Sprite[Ids.Length];
            for(int i=0;i<Ids.Length;i++)
            {
                string path=Folder+Ids[i]+".png";var importer=AssetImporter.GetAtPath(path) as TextureImporter;
                if(importer==null)throw new InvalidOperationException("Missing equipment stat icon: "+path);
                importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.spritePixelsPerUnit=100;importer.maxTextureSize=512;importer.mipmapEnabled=false;importer.alphaIsTransparency=true;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.filterMode=FilterMode.Bilinear;
                var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);settings.spriteMeshType=SpriteMeshType.FullRect;importer.SetTextureSettings(settings);importer.SaveAndReimport();sprites[i]=AssetDatabase.LoadAssetAtPath<Sprite>(path);
            }
            const string prefab="Assets/MegaTanks/Prefabs/UI/Equipment.prefab";var root=PrefabUtility.LoadPrefabContents(prefab);
            try
            {
                var art=root.GetComponent<LoadoutArtPresentation>();var rows=art.statsText.GetComponent<EquipmentStatIcons>()??art.statsText.gameObject.AddComponent<EquipmentStatIcons>();
                rows.sourceStyle=art.statsText;rows.iconIds=Ids;rows.icons=sprites;art.equipmentStats=rows;art.statsText.enabled=false;
                PrefabUtility.SaveAsPrefabAsset(root,prefab);
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
            AssetDatabase.SaveAssets();Debug.Log("EQUIPMENT_STAT_ICONS_APPLIED");
        }
        public static void ApplyAndCheck(){Apply();LoadoutArtworkAudit.Run();}
    }
}
