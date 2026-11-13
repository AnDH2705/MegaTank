using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace MegaTanks.Editor
{
    public static class GatlingGunImporter
    {
        public const string ImagePath="Assets/MegaTanks/Art/Weapons/Gatling/Gatling.png";
        public const string DefinitionPath="Assets/MegaTanks/Data/Weapons/GatlingGun.asset";
        public const string PrefabPath="Assets/MegaTanks/Prefabs/Weapons/GatlingGun.prefab";

        [MenuItem("MegaTanks/Import Gatling Gun")]
        public static void Import()
        {
            AssetDatabase.Refresh();
            var image=new Texture2D(2,2);
            image.LoadImage(File.ReadAllBytes(ImagePath));
            float width=image.width,height=image.height;
            Object.DestroyImmediate(image);
            var importer=(TextureImporter)AssetImporter.GetAtPath(ImagePath);
            importer.textureType=TextureImporterType.Sprite;
            importer.spriteImportMode=SpriteImportMode.Single;
            importer.spritePixelsPerUnit=width/3f;
            var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);
            settings.spriteAlignment=(int)SpriteAlignment.Custom;
            settings.spritePivot=new Vector2(.286f,.107f);settings.spriteMeshType=SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
            importer.alphaIsTransparency=true;importer.mipmapEnabled=false;
            importer.filterMode=FilterMode.Bilinear;importer.wrapMode=TextureWrapMode.Clamp;
            importer.textureCompression=TextureImporterCompression.Uncompressed;importer.maxTextureSize=2048;
            importer.SaveAndReimport();
            var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(ImagePath);
            var tank=AssetDatabase.LoadAssetAtPath<TankArtwork>(TankArtImporter.PrefabPath);
            var root=new GameObject("Gatling Gun");
            Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath));
            try
            {
                var gun=root.AddComponent<GunArtwork>();
                var barrel=new GameObject("Gatling artwork").transform;barrel.SetParent(root.transform,false);
                barrel.localPosition=Vector3.down*.2f;
                var renderer=barrel.gameObject.AddComponent<SpriteRenderer>();renderer.sprite=sprite;renderer.sortingOrder=65;
                var muzzle=new GameObject("Gatling muzzle").transform;muzzle.SetParent(barrel,false);
                // Pixel coordinates locate the forward barrel mouths relative to the mounting pivot.
                muzzle.localPosition=new Vector3((.91f-.286f)*3f,.335f*height/(width/3f),0);
                var originalFlash=tank.MachineGun.Muzzle.GetComponentInChildren<SpriteClipPlayer>(true);
                var flash=Object.Instantiate(originalFlash,muzzle,false);flash.name="Gatling muzzle flash";
                flash.transform.localPosition=Vector3.zero;flash.Stop();
                gun.Configure(barrel,muzzle,flash,0,.022222223f);
                PrefabUtility.SaveAsPrefabAsset(root,PrefabPath);
            }
            finally { Object.DestroyImmediate(root); }
            var definition=AssetDatabase.LoadAssetAtPath<GunDefinition>(DefinitionPath);
            if(definition==null)
            {
                definition=ScriptableObject.CreateInstance<GunDefinition>();
                definition.stats=AssetDatabase.LoadAssetAtPath<GunDefinition>("Assets/MegaTanks/Data/Weapons/MachineGun.asset").stats.Copy();
                AssetDatabase.CreateAsset(definition,DefinitionPath);
            }
            definition.displayName="Gatling Gun";definition.icon=sprite;
            definition.artworkPrefab=AssetDatabase.LoadAssetAtPath<GunArtwork>(PrefabPath);
            var catalog=AssetDatabase.LoadAssetAtPath<FrontendCatalog>(CanvasUISetup.CatalogPath);
            if(!catalog.inventory.Contains(definition))catalog.inventory=catalog.inventory.Concat(new EquipmentDefinition[]{definition}).ToArray();
            EditorUtility.SetDirty(definition);EditorUtility.SetDirty(catalog);AssetDatabase.SaveAssets();
            Debug.Log("MEGATANKS_GATLING_IMPORTED: independent MG-slot weapon, current MG stats copied, supplied sprite, roof mount, forward muzzle and inventory icon.");
        }
    }
}
