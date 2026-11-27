using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace MegaTanks.Editor
{
    public static class MissilePackSetup
    {
        public const string ArtRoot="Assets/MegaTanks/Art/Missiles/";
        public const string ATPath="Assets/MegaTanks/Data/Weapons/MissilePackAT.asset";
        public const string AAPath="Assets/MegaTanks/Data/Weapons/MissilePackAA.asset";
        static Sprite Import(string name,bool launcher)
        {
            string path=ArtRoot+name+".png";AssetDatabase.ImportAsset(path);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            if(importer==null)throw new InvalidOperationException("Missing missile artwork: "+path);
            importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
            importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.npotScale=TextureImporterNPOTScale.None;
            importer.spritePixelsPerUnit=100;importer.maxTextureSize=1024;importer.textureCompression=TextureImporterCompression.Uncompressed;
            importer.filterMode=FilterMode.Bilinear;importer.wrapMode=TextureWrapMode.Clamp;
            var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);
            settings.spriteAlignment=(int)SpriteAlignment.Custom;settings.spritePivot=launcher?new Vector2(.45f,.3f):Vector2.one*.5f;
            settings.spriteMeshType=SpriteMeshType.FullRect;importer.SetTextureSettings(settings);importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
        static GunArtwork Launcher(string name,Sprite sprite)
        {
            string path="Assets/MegaTanks/Prefabs/Weapons/"+name+".prefab";
            var existing=AssetDatabase.LoadAssetAtPath<GunArtwork>(path);
            if(existing!=null)
            {
                var contents=PrefabUtility.LoadPrefabContents(path);
                try
                {
                    var tube=contents.transform.Find("Launcher");var socket=contents.transform.Find("Launch socket");
                    tube.localRotation=Quaternion.Euler(0,0,90);socket.localPosition=new Vector3(-.22f,1.03f,0);
                    contents.GetComponent<GunArtwork>().Configure(tube,socket,socket.GetComponentInChildren<SpriteClipPlayer>(),90,.025f);
                    PrefabUtility.SaveAsPrefabAsset(contents,path);
                }
                finally{PrefabUtility.UnloadPrefabContents(contents);}
                return AssetDatabase.LoadAssetAtPath<GunArtwork>(path);
            }
            var root=new GameObject(name);var gun=root.AddComponent<GunArtwork>();
            var barrel=new GameObject("Launcher").transform;barrel.SetParent(root.transform,false);
            var visual=barrel.gameObject.AddComponent<SpriteRenderer>();visual.sprite=sprite;visual.sortingOrder=35;
            barrel.localScale=Vector3.one*(2.1f/sprite.bounds.size.x);
            barrel.localRotation=Quaternion.Euler(0,0,90);
            var muzzle=new GameObject("Launch socket").transform;muzzle.SetParent(root.transform,false);muzzle.localPosition=new Vector3(-.22f,1.03f,0);
            var flash=new GameObject("Launch flash").AddComponent<SpriteClipPlayer>();flash.transform.SetParent(muzzle,false);flash.Configure(Array.Empty<Sprite>(),.04f,75);
            gun.Configure(barrel,muzzle,flash,90,.025f);
            var prefab=PrefabUtility.SaveAsPrefabAsset(root,path);UnityEngine.Object.DestroyImmediate(root);return prefab.GetComponent<GunArtwork>();
        }
        static GunDefinition Create(string path,string title,string id,EnemyKind kind,Sprite art,Sprite missile)
        {
            var item=AssetDatabase.LoadAssetAtPath<GunDefinition>(path);
            if(item==null)
            {
                item=ScriptableObject.CreateInstance<GunDefinition>();item.displayName=title;item.id=id;
                item.stats=new GunStats {homingMissile=true,heavyProjectile=true,preferredTarget=kind,damage=kind==EnemyKind.Ground?24:16,
                    accuracy=100,armorPenetration=kind==EnemyKind.Ground?25:0,range=kind==EnemyKind.Ground?12:14,salvoSize=2,shotDelay=.3f,
                    reloadTime=6,reloadSpeed=100,projectileSpeed=12,gravity=0,reserveCapacity=60,projectileWidth=.7f,missileTurnRate=240,missileLifetime=5};
                AssetDatabase.CreateAsset(item,path);
            }
            if(item.icon==null)item.icon=art;if(item.stats.projectileSprite==null)item.stats.projectileSprite=missile;
            item.artworkPrefab=Launcher(Path.GetFileNameWithoutExtension(path),art);
            EditorUtility.SetDirty(item);return item;
        }
        [MenuItem("MegaTanks/Configure Missile Packs")]
        public static void Install()
        {
            Directory.CreateDirectory("Assets/MegaTanks/Prefabs/Weapons");AssetDatabase.Refresh();
            var missile=Import("HomingMissile",false);
            var at=Create(ATPath,"AT Missile Pack","weapon.missile-pack.at",EnemyKind.Ground,Import("MissilePackAT",true),missile);
            var aa=Create(AAPath,"AA Missile Pack","weapon.missile-pack.aa",EnemyKind.Flyer,Import("MissilePackAA",true),missile);
            var catalog=AssetDatabase.LoadAssetAtPath<FrontendCatalog>(CanvasUISetup.CatalogPath);
            catalog.inventory=(catalog.inventory??Array.Empty<EquipmentDefinition>()).Concat(new EquipmentDefinition[]{at,aa}).Distinct().ToArray();
            EditorUtility.SetDirty(catalog);AssetDatabase.SaveAssets();Debug.Log("MISSILE_PACK_SETUP_OK: restored AT/AA definitions, prefabs, sprites and garage entries.");
        }
    }
}
