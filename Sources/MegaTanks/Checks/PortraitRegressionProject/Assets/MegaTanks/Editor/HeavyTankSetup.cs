using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object=UnityEngine.Object;

namespace MegaTanks.Editor
{
    public static class HeavyTankSetup
    {
        public const string ArtRoot="Assets/MegaTanks/Art/HeavyTank/";
        public const string PrefabPath="Assets/MegaTanks/Prefabs/HeavyTank.prefab";
        public const string MediumPath="Assets/MegaTanks/Data/Tanks/PlayerTank.asset";
        public const string HeavyPath="Assets/MegaTanks/Data/Tanks/HeavyTank.asset";
        const float Ppu=180;
        [Serializable] sealed class Part {public string name,file,parent;public float[] position_px,size_px,pivot_canvas_px;public int z_order;}
        [Serializable] sealed class Socket {public float[] position_canvas_px;}
        [Serializable] sealed class Sockets {public Socket cannon_muzzle,machine_gun_muzzle,mortar_muzzle,exhaust;}
        [Serializable] sealed class Manifest {public Part[] layers;public Sockets vfx_sockets;}
        static Vector2 Point(float[] value)=>new Vector2(value[0],-value[1])/Ppu;
        static Vector2 Point(Vector2 value)=>new Vector2(value.x,-value.y)/Ppu;
        static Transform Group(string name,Transform parent,Vector2 pivot,Vector2 parentPivot)
        {
            var result=new GameObject(name).transform;result.SetParent(parent,false);
            result.localPosition=Point(pivot)-Point(parentPivot);return result;
        }
        static Transform PartObject(Part part,Transform parent,Vector2 pivot)
        {
            var result=new GameObject(part.name).transform;result.SetParent(parent,false);
            // Center-pivot sprites also support the supplied body's pivot outside its cropped cell.
            var center=new Vector2(part.position_px[0]+part.size_px[0]*.5f,part.position_px[1]+part.size_px[1]*.5f);
            result.localPosition=Point(center)-Point(pivot);
            var renderer=result.gameObject.AddComponent<SpriteRenderer>();
            renderer.sprite=AssetDatabase.LoadAssetAtPath<Sprite>(ArtRoot+part.file);renderer.sortingOrder=10+part.z_order;
            return result;
        }
        static Transform SocketObject(string name,Transform parent,Socket socket,Vector2 pivot)
        {
            var result=new GameObject(name).transform;result.SetParent(parent,false);result.localPosition=Point(socket.position_canvas_px)-Point(pivot);return result;
        }
        static GunArtwork Gun(Manifest manifest,Transform body,Vector2 bodyPivot,string name,string partName,Socket socket,float recoil)
        {
            var part=manifest.layers.Single(p=>p.name==partName);var pivot=new Vector2(part.pivot_canvas_px[0],part.pivot_canvas_px[1]);
            var mount=Group(name,body,pivot,bodyPivot);var barrel=PartObject(part,mount,pivot);
            var muzzle=SocketObject(name+" muzzle",barrel,socket,new Vector2(part.position_px[0]+part.size_px[0]*.5f,part.position_px[1]+part.size_px[1]*.5f));
            var fallback=new GameObject("Muzzle flash").AddComponent<SpriteClipPlayer>();fallback.transform.SetParent(muzzle,false);
            var catalog=AssetDatabase.LoadAssetAtPath<BattleVfxCatalog>(BattleVfxImporter.CatalogPath);
            fallback.Configure(catalog.muzzle.frames,1/catalog.muzzle.framesPerSecond,80);
            var gun=mount.gameObject.AddComponent<GunArtwork>();gun.Configure(barrel,muzzle,fallback,0,recoil);return gun;
        }
        [MenuItem("MegaTanks/Add Heavy Tank")]
        public static void Install()
        {
            AssetDatabase.Refresh();
            var manifest=JsonUtility.FromJson<Manifest>(File.ReadAllText(ArtRoot+"metadata/parts_manifest.json"));
            foreach(var part in manifest.layers)
            {
                var importer=(TextureImporter)AssetImporter.GetAtPath(ArtRoot+part.file);
                importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
                importer.spritePixelsPerUnit=Ppu;importer.alphaIsTransparency=true;importer.mipmapEnabled=false;
                importer.npotScale=TextureImporterNPOTScale.None;importer.maxTextureSize=2048;
                importer.textureCompression=TextureImporterCompression.Compressed;importer.filterMode=FilterMode.Bilinear;importer.wrapMode=TextureWrapMode.Clamp;
                var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);
                settings.spriteAlignment=0;settings.spritePivot=new Vector2(.5f,.5f);settings.spriteMeshType=SpriteMeshType.FullRect;
                importer.SetTextureSettings(settings);importer.SaveAndReimport();
            }
            var root=new GameObject("Heavy Tank");
            try
            {
                // Align the native 1107px ground baseline with the existing battle spawn/scale.
                var rootPivot=new Vector2(680,971);var bodyPivot=new Vector2(680,922);
                var body=Group("body",root.transform,bodyPivot,rootPivot);
                foreach(var part in manifest.layers.Where(p=>!p.name.Contains("main_cannon_barrel")&&!p.name.Contains("roof_machine_gun")&&!p.name.Contains("mortar")))
                    PartObject(part,part.name.Contains("wheel")||part.name.Contains("chassis")?root.transform:body,
                        part.name.Contains("wheel")||part.name.Contains("chassis")?rootPivot:bodyPivot);
                var mg=Gun(manifest,body,bodyPivot,"roof_gun","08_roof_machine_gun",manifest.vfx_sockets.machine_gun_muzzle,4/Ppu);
                var cannon=Gun(manifest,body,bodyPivot,"cannon","06_main_cannon_barrel",manifest.vfx_sockets.cannon_muzzle,12/Ppu);
                var tube=manifest.layers.Single(p=>p.name=="09_mortar_barrel");var mortarPivot=new Vector2(tube.pivot_canvas_px[0],tube.pivot_canvas_px[1]);
                var mortar=Group("mortar",body,mortarPivot,bodyPivot);var barrel=PartObject(tube,mortar,mortarPivot);
                PartObject(manifest.layers.Single(p=>p.name=="10_mortar_front_support"),mortar,mortarPivot);
                var socket=SocketObject("Mortar muzzle",barrel,manifest.vfx_sockets.mortar_muzzle,new Vector2(tube.position_px[0]+tube.size_px[0]*.5f,tube.position_px[1]+tube.size_px[1]*.5f));
                var mortarArt=mortar.gameObject.AddComponent<MortarArtwork>();mortarArt.Configure(barrel,socket);
                var frames=AssetDatabase.LoadAllAssetsAtPath("Assets/MegaTanks/Art/VFX/sheets/exhaust_smoke_4x2.png").OfType<Sprite>().OrderBy(sprite=>sprite.name).ToArray();
                var smoke=new SpriteClipPlayer[3];
                for(int i=0;i<smoke.Length;i++)
                {
                    smoke[i]=new GameObject("Exhaust puff "+i).AddComponent<SpriteClipPlayer>();smoke[i].transform.SetParent(body,false);
                    smoke[i].transform.localPosition=Point(manifest.vfx_sockets.exhaust.position_canvas_px)-Point(bodyPivot);
                    smoke[i].transform.localScale=Vector3.one*.72f;smoke[i].Configure(frames,.04f,5);
                }
                var art=root.AddComponent<TankArtwork>();art.Configure(mg,cannon,body,smoke);art.BindMortar(mortarArt);
                PrefabUtility.SaveAsPrefabAsset(root,PrefabPath);
            }
            finally{Object.DestroyImmediate(root);}
            var medium=AssetDatabase.LoadAssetAtPath<TankDefinition>(MediumPath);
            medium.id="tank.medium";medium.displayName="Medium Tank";medium.armorSlots=1;medium.artworkPrefab=AssetDatabase.LoadAssetAtPath<TankArtwork>(TankArtImporter.PrefabPath);EditorUtility.SetDirty(medium);
            var heavy=AssetDatabase.LoadAssetAtPath<TankDefinition>(HeavyPath);
            if(heavy==null)
            {
                heavy=ScriptableObject.CreateInstance<TankDefinition>();heavy.stats=medium.stats.Copy();
                heavy.stats.maxHull=medium.stats.maxHull*2;heavy.stats.advanceSpeed=.25f;
                AssetDatabase.CreateAsset(heavy,HeavyPath);
            }
            heavy.id="tank.heavy";heavy.displayName="Heavy Tank";heavy.armorSlots=2;heavy.artworkPrefab=AssetDatabase.LoadAssetAtPath<TankArtwork>(PrefabPath);EditorUtility.SetDirty(heavy);
            var frontend=AssetDatabase.LoadAssetAtPath<FrontendCatalog>(CanvasUISetup.CatalogPath);
            frontend.tanks=new[]{medium,heavy}.Concat(frontend.tanks??Array.Empty<TankDefinition>()).Where(tank=>tank!=null).Distinct().ToArray();EditorUtility.SetDirty(frontend);
            ConfigureGarage();AssetDatabase.SaveAssets();
            Debug.Log("HEAVY_TANK_SETUP_OK: 11 registered layers, independent aiming/recoil/muzzles, 200 base HP/0.25 speed defaults, Medium/Heavy profiles, garage selector and tank-specific slot counts.");
        }
        static void Place(RectTransform rect,float x,float y,float width,float height)
        {rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);rect.anchoredPosition=new Vector2(x,-y);rect.sizeDelta=new Vector2(width,height);}
        static void Bind(UIView view,string key,RectTransform target)
        {
            var serialized=new SerializedObject(view);var bindings=serialized.FindProperty("bindings");
            for(int i=0;i<bindings.arraySize;i++)if(bindings.GetArrayElementAtIndex(i).FindPropertyRelative("key").stringValue==key)return;
            bindings.InsertArrayElementAtIndex(bindings.arraySize);var item=bindings.GetArrayElementAtIndex(bindings.arraySize-1);
            item.FindPropertyRelative("key").stringValue=key;item.FindPropertyRelative("target").objectReferenceValue=target;serialized.ApplyModifiedPropertiesWithoutUndo();
        }
        static void ConfigureGarage()
        {
            const string path="Assets/MegaTanks/Prefabs/UI/Equipment.prefab";var root=PrefabUtility.LoadPrefabContents(path);
            try
            {
                var view=root.GetComponent<UIView>();var existing=root.transform.Find("Tank Selection");
                var button=existing!=null?existing.gameObject:Object.Instantiate(view.Get<Button>("Inventory Sort").gameObject,root.transform,false);
                button.name="Tank Selection";Place((RectTransform)button.transform,390,24,260,72);
                var text=button.transform.Find("Label").GetComponent<Text>();Place(text.rectTransform,8,3,244,66);
                text.text="MEDIUM TANK  ›\nCHANGE TANK";text.fontSize=21;text.alignment=TextAnchor.MiddleCenter;text.color=Color.white;text.lineSpacing=1;
                var arrow=button.transform.Find("Dropdown Arrow");if(arrow!=null)arrow.gameObject.SetActive(false);
                var title=view.Get<Text>("Popup Title");Place(title.rectTransform,190,0,190,108);title.fontSize=36;title.text="GARAGE";
                Place(view.Get<RectTransform>("Tank Avatar"),784,190,352,440);
                var stats=root.transform.Find("Tank Stats");
                var caption=stats!=null?stats.GetComponent<Text>():Object.Instantiate(view.Get<Text>("Popup Title"),root.transform,false);
                caption.name="Tank Stats";Place(caption.rectTransform,744,120,432,58);
                caption.fontSize=21;caption.alignment=TextAnchor.MiddleCenter;caption.color=new Color(1,.77f,.34f);caption.raycastTarget=false;
                caption.text="BASE HP 100   •   SPEED 0.5\n1 ARMOR   •   2 ACCESSORIES";
                foreach(var shadow in caption.GetComponents<Shadow>())shadow.enabled=false;
                Bind(view,"Tank Selection",(RectTransform)button.transform);Bind(view,"Tank Selection/Label",text.rectTransform);Bind(view,"Tank Stats",caption.rectTransform);
                // Keep authored aliases: progression star bindings do not use their object paths.
                foreach(string slot in new[]{"Mortar","Accessory 2","Accessory 3","MG","Cannon","Armor"})
                    Bind(view,"Slot "+slot+"/Stars",(RectTransform)root.transform.Find("Slot "+slot+"/Equipped Item Icon Stars"));
                Bind(view,"Detail Stars",(RectTransform)root.transform.Find("Detail Item Icon Stars"));
                PrefabUtility.SaveAsPrefabAsset(root,path);
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
        }
    }
}
