using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

namespace CatTanks.Editor
{
    public static class StudioSliceRepair
    {
        [MenuItem("CatTanks/UI/Repair LayerLab 9-Slice Borders")]
        public static void Repair()
        {
            var repaired=new List<string>();
            string root="Assets/CatTanks/Layer Lab/GUI Pro-SuperCasual/ResourcesData/Sprites/Components";
            foreach(string guid in AssetDatabase.FindAssets("t:Texture2D",new[]{root}))
            {
                string path=AssetDatabase.GUIDToAssetPath(guid);
                if(path.Contains("/Icon"))continue;
                var importer=AssetImporter.GetAtPath(path) as TextureImporter;
                if(!importer||importer.textureType!=TextureImporterType.Sprite||importer.spriteBorder.sqrMagnitude==0)continue;
                importer.GetSourceTextureWidthAndHeight(out int width,out int height);
                var before=importer.spriteBorder;var border=before;
                var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);
                float horizontal=Mathf.Max(0,width-2),vertical=Mathf.Max(0,height-2);
                if(border.x+border.z>horizontal){float f=horizontal/(border.x+border.z);border.x=Mathf.Floor(border.x*f);border.z=Mathf.Floor(border.z*f);}
                if(border.y+border.w>vertical){float f=vertical/(border.y+border.w);border.y=Mathf.Floor(border.y*f);border.w=Mathf.Floor(border.w*f);}
                if(border!=before||settings.spriteMeshType!=SpriteMeshType.FullRect)
                {
                    importer.spriteBorder=border;settings.spriteBorder=border;settings.spriteMeshType=SpriteMeshType.FullRect;importer.SetTextureSettings(settings);importer.SaveAndReimport();repaired.Add(path);
                }
            }
            foreach(string guid in AssetDatabase.FindAssets("t:Texture2D",new[]{"Assets/CatTanks/Resources/Art"}))
            {
                var importer=AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(guid)) as TextureImporter;
                if(importer&&importer.textureType!=TextureImporterType.Sprite){importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.SaveAndReimport();}
            }
            File.WriteAllText("Logs/nine-slice-repair.json",JsonConvert.SerializeObject(new{count=repaired.Count,paths=repaired},Formatting.Indented));
            Debug.Log("NINE_SLICE_REPAIRED: "+repaired.Count+" component textures");
        }
    }
}
