using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;

namespace MegaTanks.Editor
{
    [InitializeOnLoad]
    public static class EquipmentHtmlSetup
    {
        const string Root="Assets/MegaTanks/",Data=Root+"Data/",Prefab=Root+"Prefabs/UI/Equipment.prefab";
        [Serializable] sealed class Layer { public string name,file,parent;public float[] position_px,size_px,pivot_canvas_px;public int z_order; }
        [Serializable] sealed class Placement { public Layer[] replacement_layers,layers; }
        static EquipmentHtmlSetup(){EditorApplication.update+=Pending;}
        static void Pending()
        {
            const string file="Checks/equipment-html-apply.pending";
            if(!File.Exists(file)||EditorApplication.isPlaying||EditorApplication.isCompiling||EditorApplication.isUpdating||!OriginalPortSetup.AssembliesCurrent())return;
            File.Delete(file);try{Apply();}catch(Exception e){Debug.LogException(e);File.WriteAllText("Checks/equipment-html-apply.error",e.ToString());}
        }
        static T Asset<T>(string path,Func<T> create) where T:Object
        {
            var result=AssetDatabase.LoadAssetAtPath<T>(path);if(result!=null)return result;
            Directory.CreateDirectory(Path.GetDirectoryName(path));result=create();AssetDatabase.CreateAsset(result,path);return result;
        }
        [MenuItem("MegaTanks/Equipment/Apply approved Crew layout")]
        public static void Apply()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play before applying the Equipment prefab.");
            var art=Resources.Load<HtmlBattleArt>("HtmlBattleArt");var config=HtmlBattleConfig.Load();
            var frontend=AssetDatabase.LoadAssetAtPath<FrontendCatalog>(Data+"UI/Frontend.asset");
            var normal=AssetDatabase.LoadAssetAtPath<TankDefinition>(Data+"Tanks/PlayerTank.asset");
            var heavy=AssetDatabase.LoadAssetAtPath<TankDefinition>(Data+"Tanks/HeavyTank.asset");
            var speed=Asset<TankDefinition>(Data+"Tanks/SpeedTank.asset",()=>Object.Instantiate(normal));
            normal.htmlSkin="base";heavy.htmlSkin="heavy";speed.htmlSkin="speed";speed.id="tank.speed";speed.displayName="Speed Tank";
            speed.stats.maxHull=300;speed.stats.advanceSpeed=.625f;
            var cannon=AssetDatabase.LoadAssetAtPath<GunDefinition>(Data+"Weapons/Cannon.asset");
            var mg=AssetDatabase.LoadAssetAtPath<GunDefinition>(Data+"Weapons/MachineGun.asset");
            var mortar=AssetDatabase.LoadAssetAtPath<MortarDefinition>(Data+"Weapons/Mortar.asset");
            var ap=Asset<GunDefinition>(Data+"Weapons/ArmorPiercingCannon.asset",()=>Object.Instantiate(cannon));
            ap.id="html.cannon_ap";ap.displayName="AP CANNON";ap.icon=art.Image("card_cannon_ap").sprite;ap.stats.damage=cannon.stats.damage*config.piercingMultiplier;
            var plasma=Asset<GunDefinition>(Data+"Weapons/PlasmaMG.asset",()=>Object.Instantiate(mg));
            plasma.id="html.mg_plasma";plasma.displayName="PLASMA MG";plasma.icon=art.Image("card_mg_plasma").sprite;plasma.stats.damage=mg.stats.damage*config.plasmaMultiplier;
            var standard=Asset<WheelsDefinition>(Data+"Wheels/StandardWheels.asset",()=>ScriptableObject.CreateInstance<WheelsDefinition>());
            standard.id="wheels.standard";standard.skin="standard";standard.displayName="STANDARD";standard.icon=art.Image("tank_base_02_wheel_01").sprite;
            var offroad=Asset<WheelsDefinition>(Data+"Wheels/OffroadWheels.asset",()=>ScriptableObject.CreateInstance<WheelsDefinition>());
            offroad.id="wheels.offroad";offroad.skin="offroad";offroad.displayName="OFFROAD";
            var catalog=Asset<EquipmentHtmlCatalog>(Root+"Resources/EquipmentHtmlCatalog.asset",()=>ScriptableObject.CreateInstance<EquipmentHtmlCatalog>());
            catalog.hulls=new[]{normal,speed,heavy};catalog.wheels=new[]{standard,offroad};
            catalog.weapons=new[]{
                new EquipmentHtmlWeapon{definition=mg,slot=0,title="MACHINE GUN",variant="mg",artKey="tank_base_08_roof_machine_gun"},
                new EquipmentHtmlWeapon{definition=plasma,slot=0,title="PLASMA MG",variant="mg_plasma",artKey="card_mg_plasma"},
                new EquipmentHtmlWeapon{definition=cannon,slot=1,title="CANNON",variant="cannon",artKey="tank_base_06_main_cannon_barrel"},
                new EquipmentHtmlWeapon{definition=ap,slot=1,title="AP CANNON",variant="cannon_ap",artKey="card_cannon_ap"},
                new EquipmentHtmlWeapon{definition=mortar,slot=2,title="MORTAR",variant="mortar",artKey="tank_base_09_mortar_barrel"}};
            ImportWheels(catalog,art);
            offroad.icon=catalog.wheelImages.First(i=>i.key=="offroad_base_02_offroad_wheel_rear").sprite;
            frontend.tanks=(frontend.tanks??Array.Empty<TankDefinition>()).Concat(new[]{speed}).Distinct().ToArray();
            frontend.inventory=(frontend.inventory??Array.Empty<EquipmentDefinition>()).Concat(new EquipmentDefinition[]{ap,plasma,standard,offroad}).Distinct().ToArray();
            foreach(var asset in new Object[]{normal,heavy,speed,ap,plasma,standard,offroad,frontend,catalog})EditorUtility.SetDirty(asset);
            var crew=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"Prefabs/UI/Crew.prefab").GetComponent<CrewHtmlScreen>();
            var root=PrefabUtility.LoadPrefabContents(Prefab);
            try
            {
                var old=root.transform.Find("Equipment Design 770x436");if(old!=null)Object.DestroyImmediate(old.gameObject);
                foreach(Transform child in root.transform)child.gameObject.SetActive(false);
                var wrapper=Object.Instantiate(crew.transform.Find("Crew Design 770x436").gameObject,root.transform,false);wrapper.name="Equipment Design 770x436";wrapper.SetActive(true);
                var screen=root.GetComponent<EquipmentHtmlScreen>()??root.AddComponent<EquipmentHtmlScreen>();screen.template=crew;screen.art=art;screen.catalog=catalog;screen.BuildLayout();
                var serialized=new SerializedObject(root.GetComponent<UIView>());var bindings=serialized.FindProperty("bindings");
                for(int i=0;i<bindings.arraySize;i++)if(bindings.GetArrayElementAtIndex(i).FindPropertyRelative("key").stringValue=="Close Popup")bindings.GetArrayElementAtIndex(i).FindPropertyRelative("target").objectReferenceValue=wrapper.transform.Find("Back");
                serialized.ApplyModifiedPropertiesWithoutUndo();PrefabUtility.SaveAsPrefabAsset(root,Prefab);
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
            AssetDatabase.SaveAssets();File.WriteAllText("Checks/equipment-html-ready.flag",DateTime.UtcNow.ToString("O"));
            Debug.Log("EQUIPMENT_HTML_READY: approved Crew layout, WHEELS, original Offroad native fits, coins for crew/gears for equipment.");
        }
        static void ImportWheels(EquipmentHtmlCatalog catalog,HtmlBattleArt art)
        {
            string artRoot=Path.GetFullPath(Path.Combine(Application.dataPath,"../../../Art"));
            string pack=Path.Combine(artRoot,"Asset/Tanks/Changeable_parts/Tank_Modules_Upgrade_Offroad_v1");
            string[] skins={"base","speed","heavy"},folders={"00_Tank_Goc","01_Tank_Do_Toc_Do","02_Tank_Xanh_Chong_Chiu"};
            string[] sources={"Tank_1_optimizedPart/Tank_Mortar_Frame_v4","Tank_2_speed","Tank_3_HP"};
            var fits=new List<HtmlWheelFit>();var images=new List<HtmlArtImage>();
            for(int i=0;i<3;i++)
            {
                string fitPath=Path.Combine(pack,"native_fits",folders[i]);var placement=JsonUtility.FromJson<Placement>(File.ReadAllText(Path.Combine(fitPath,"placement_manifest.json")));
                var original=JsonUtility.FromJson<Placement>(File.ReadAllText(Path.Combine(artRoot,"Asset/Tanks",sources[i],"metadata/parts_manifest.json"))).layers.First(p=>p.name=="01_track_chassis");
                var target=art.Profile(skins[i]).parts.First(p=>p.name==original.name);float z=target.size[0]/original.size_px[0];
                var parts=new List<HtmlArtPart>();
                foreach(var layer in placement.replacement_layers.Take(3))
                {
                    string key="offroad_"+skins[i]+"_"+layer.name,dest=Root+"Art/Equipment/Offroad/"+skins[i]+"/"+Path.GetFileName(layer.file);
                    Directory.CreateDirectory(Path.GetDirectoryName(dest));File.Copy(Path.Combine(fitPath,layer.file),dest,true);AssetDatabase.ImportAsset(dest,ImportAssetOptions.ForceUpdate);
                    var importer=(TextureImporter)AssetImporter.GetAtPath(dest);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.spritePixelsPerUnit=100;importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.npotScale=TextureImporterNPOTScale.None;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.filterMode=FilterMode.Bilinear;
                    var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);settings.spriteMeshType=SpriteMeshType.FullRect;importer.SetTextureSettings(settings);importer.SaveAndReimport();
                    images.Add(new HtmlArtImage{key=key,source=Path.GetRelativePath(artRoot,Path.Combine(fitPath,layer.file)).Replace('\\','/'),sprite=AssetDatabase.LoadAssetAtPath<Sprite>(dest)});
                    parts.Add(new HtmlArtPart{key=key,name=layer.name,parent=null,order=layer.z_order,
                        position=new[]{target.position[0]+(layer.position_px[0]-original.position_px[0])*z,target.position[1]+(layer.position_px[1]-original.position_px[1])*z},size=new[]{layer.size_px[0]*z,layer.size_px[1]*z},
                        pivot=new[]{target.position[0]+(layer.pivot_canvas_px[0]-original.position_px[0])*z,target.position[1]+(layer.pivot_canvas_px[1]-original.position_px[1])*z}});
                }
                fits.Add(new HtmlWheelFit{hull=skins[i],parts=parts.ToArray()});
            }
            catalog.offroadFits=fits.ToArray();catalog.wheelImages=images.ToArray();
        }
    }
}
