using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace MegaTanks.Editor
{
    public static class TanketteSetup
    {
        public const string EnemyId="Tankette";
        public const string PrefabPath="Assets/MegaTanks/Prefabs/Enemies/TanketteVehicle.prefab";
        public const string ArtRoot="Assets/MegaTanks/Art/EnemyVehicles/tankette/";
        const float Ppu=600;
        static Vector2 Point(float x,float y)=>new Vector2(x-768,512-y)/Ppu;
        static Sprite Import(string file,Vector2 pivot)
        {
            string path=ArtRoot+file;AssetDatabase.ImportAsset(path);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            bool hull=file.StartsWith("Body",StringComparison.Ordinal);
            importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=hull?SpriteImportMode.Multiple:SpriteImportMode.Single;
            importer.spritePixelsPerUnit=Ppu;importer.alphaIsTransparency=true;importer.mipmapEnabled=false;
            importer.npotScale=TextureImporterNPOTScale.None;importer.maxTextureSize=2048;
            importer.textureCompression=TextureImporterCompression.Uncompressed;importer.filterMode=FilterMode.Bilinear;importer.wrapMode=TextureWrapMode.Clamp;
            var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);
            settings.spriteAlignment=(int)SpriteAlignment.Custom;settings.spritePivot=pivot;settings.spriteMeshType=SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
            // Metadata crop trims transparent padding without editing the generated PNG.
            // Pivot keeps every part in the original 1536x1024 assembly coordinates.
            if(hull)importer.spritesheet=new[]{new SpriteMetaData{name="TanketteBody",rect=new Rect(99,2,1330,1008),alignment=(int)SpriteAlignment.Custom,pivot=new Vector2(669f/1330,510f/1008)}};
            importer.SaveAndReimport();return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().Single();
        }
        [MenuItem("MegaTanks/Configure Tankette Enemy")]
        public static void Install()
        {
            AssetDatabase.Refresh();var body=Import("BodyCannon.png",Vector2.one*.5f);var wheel=Import("RoadWheel.png",new Vector2(199f/401,1-205f/399));
            var database=AssetDatabase.LoadAssetAtPath<EnemyDataManager>(EnemyDataSetup.AssetPath);
            var car=database.Get("GroundEnemy1");var profile=AssetDatabase.LoadAssetAtPath<EnemyStatsProfile>(PrefabPath);
            if(profile==null)
            {
                var root=new GameObject("Tankette Vehicle Art");
                try
                {
                    var renderer=new GameObject("Tankette hull, turret and track belt").AddComponent<SpriteRenderer>();renderer.transform.SetParent(root.transform,false);renderer.sprite=body;renderer.sortingOrder=10;
                    float[] centers={335,620,905,1190};var wheels=new Transform[centers.Length];
                    for(int i=0;i<centers.Length;i++)
                    {
                        var road=new GameObject("Tankette road wheel "+(i+1)).AddComponent<SpriteRenderer>();road.transform.SetParent(root.transform,false);
                        road.transform.localPosition=Point(centers[i],845);road.sprite=wheel;road.sortingOrder=11;
                        road.transform.localScale=Vector3.one*((232/Ppu)/wheel.bounds.size.x);wheels[i]=road.transform;
                    }
                    var muzzle=new GameObject("Tankette illustrated gun muzzle").transform;muzzle.SetParent(root.transform,false);muzzle.localPosition=Point(1285,460);
                    var artwork=root.AddComponent<EnemyVehicleArtwork>();artwork.Configure(muzzle,wheels,null,Array.Empty<Sprite>(),true);
                    var stats=car.Copy();stats.id=EnemyId;stats.kind=EnemyKind.Ground;stats.artworkPrefab=artwork;
                    stats.armor=3;stats.armorGrow=0;stats.baseSpeed=Mathf.Max(.1f,car.baseSpeed-.1f);
                    stats.score=car.score+25;stats.baseScraps=car.baseScraps+5;
                    stats.weapon.type=EnemyWeaponType.Cannon;stats.weapon.damage=14;stats.weapon.salvoSize=1;stats.weapon.reloadTime=3;
                    root.AddComponent<EnemyStatsProfile>().stats=stats;
                    PrefabUtility.SaveAsPrefabAsset(root,PrefabPath);
                }
                finally{UnityEngine.Object.DestroyImmediate(root);}
                profile=AssetDatabase.LoadAssetAtPath<EnemyStatsProfile>(PrefabPath);
            }
            else
            {
                var root=PrefabUtility.LoadPrefabContents(PrefabPath);
                try
                {
                    root.transform.Find("Tankette hull, turret and track belt").GetComponent<SpriteRenderer>().sprite=body;
                    root.transform.Find("Tankette illustrated gun muzzle").localPosition=Point(1285,460);
                    foreach(var renderer in root.GetComponentsInChildren<SpriteRenderer>().Where(r=>r.name.StartsWith("Tankette road wheel")))renderer.sprite=wheel;
                    PrefabUtility.SaveAsPrefabAsset(root,PrefabPath);
                }
                finally{PrefabUtility.UnloadPrefabContents(root);}
                profile=AssetDatabase.LoadAssetAtPath<EnemyStatsProfile>(PrefabPath);
            }
            // Append without changing the default GroundEnemy1 or resetting tuned profiles.
            database.enemyPrefabs=(database.enemyPrefabs??Array.Empty<EnemyStatsProfile>()).Concat(new[]{profile}).Distinct().ToArray();
            EditorUtility.SetDirty(database);int stages=0;
            foreach(string guid in AssetDatabase.FindAssets("t:StageDefinition",new[]{"Assets/MegaTanks/Data/Stages"}))
            {
                var stage=AssetDatabase.LoadAssetAtPath<StageDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                var roster=stage.spawnableEnemies??Array.Empty<EnemyStatsProfile>();
                if(!roster.Any(p=>p!=null&&p.stats.id=="GroundEnemy1")||roster.Contains(profile))continue;
                stage.spawnableEnemies=roster.Concat(new[]{profile}).ToArray();EditorUtility.SetDirty(stage);stages++;
            }
            AssetDatabase.SaveAssets();Debug.Log("TANKETTE_SETUP_OK: original car size/style, four rotating reused wheels, 3 armor, separate prefab stats and "+stages+" stage rosters updated.");
        }
    }
}
