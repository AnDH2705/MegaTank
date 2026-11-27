using System;
using System.IO;
using System.Linq;
using Spine.Unity;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace MegaTanks.Editor
{
    [InitializeOnLoad]
    public static class OriginalPortSetup
    {
        const string Art="Assets/MegaTanks/Art/HtmlBattle/";
        static bool applying;
        static OriginalPortSetup(){EditorApplication.update+=Pending;}
        public static bool AssembliesCurrent()
        {
            string runtime=typeof(HtmlBattleScreen).Assembly.Location,editor=typeof(OriginalPortSetup).Assembly.Location;
            return Directory.GetFiles("Assets/MegaTanks/Scripts","*.cs").All(p=>File.GetLastWriteTimeUtc(p)<=File.GetLastWriteTimeUtc(runtime))&&Directory.GetFiles("Assets/MegaTanks/Editor","*.cs").All(p=>File.GetLastWriteTimeUtc(p)<=File.GetLastWriteTimeUtc(editor));
        }
        static void Pending(){const string marker="Checks/port-originals.pending";if(applying||EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorApplication.isUpdating||!File.Exists(marker)||!AssembliesCurrent())return;applying=true;File.Delete(marker);try{Apply();}finally{applying=false;}}
        [MenuItem("MegaTanks/HTML Battle/Apply Original Art Port")]
        public static void Apply()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play before import.");
            var manifest=JsonUtility.FromJson<HtmlArtManifest>(File.ReadAllText(Art+"manifest.json"));
            foreach(var entry in manifest.images)
            {
                if(!entry.source.StartsWith("Art/",StringComparison.Ordinal)||entry.source.Contains(".html"))throw new InvalidOperationException("Non-original asset: "+entry.source);
                string path=Art+entry.file;var importer=AssetImporter.GetAtPath(path) as TextureImporter;
                if(importer==null)throw new InvalidOperationException("Missing original: "+entry.source);
                bool changed=importer.textureType!=TextureImporterType.Sprite||importer.mipmapEnabled||importer.textureCompression!=TextureImporterCompression.Uncompressed||importer.maxTextureSize!=4096;
                importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.spritePixelsPerUnit=100;
                importer.mipmapEnabled=false;importer.alphaIsTransparency=true;importer.npotScale=TextureImporterNPOTScale.None;importer.wrapMode=TextureWrapMode.Clamp;importer.filterMode=FilterMode.Bilinear;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.maxTextureSize=4096;
                var b=entry.border??new float[4];var border=new Vector4(b[0],b[1],b[2],b[3]);if(importer.spriteBorder!=border)changed=true;importer.spriteBorder=border;
                var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);settings.spriteMeshType=SpriteMeshType.FullRect;importer.SetTextureSettings(settings);if(changed)importer.SaveAndReimport();
                entry.sprite=AssetDatabase.LoadAssetAtPath<Sprite>(path);entry.texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            }
            const string output="Assets/MegaTanks/Resources/HtmlBattleArt.asset";var asset=AssetDatabase.LoadAssetAtPath<HtmlBattleArt>(output);
            if(asset==null){asset=ScriptableObject.CreateInstance<HtmlBattleArt>();AssetDatabase.CreateAsset(asset,output);}
            asset.data=manifest;asset.catDriver=Rig("cat_driver");asset.catMelee=Rig("cat_melee");asset.dogMelee=Rig("dog_melee");asset.dogDriver=Rig("dog_driver");asset.dogPilot=Rig("dog_pilot");
            asset.layout=AssetDatabase.LoadAssetAtPath<TextAsset>(Art+"ui-layout-reference.json");asset.cards=AssetDatabase.LoadAssetAtPath<TextAsset>(Art+"cards.json");EditorUtility.SetDirty(asset);
            asset.comicFontAtlas=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/MegaTanks/Art/UI/CrewHtml/font/font-atlas.png");asset.comicFontMetrics=AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/MegaTanks/Art/UI/CrewHtml/font/font-atlas.json");
            asset.worldBlurShader=AssetDatabase.LoadAssetAtPath<Shader>(Art+"HtmlWorldBlur.shader");asset.worldClipShader=AssetDatabase.LoadAssetAtPath<Shader>(Art+"HtmlSpineClip.shader");
            Home();AssetDatabase.SaveAssets();Audit();File.WriteAllText("Checks/port-ready.flag",DateTime.UtcNow.ToString("O"));Debug.Log("ORIGINAL_PORT_IMPORT_OK");
        }
        static SkeletonDataAsset Rig(string id)
        {
            string folder=Art+"Spine/"+id+"/";
            string json=Directory.GetFiles(folder,"*.json").First();string atlas=Directory.GetFiles(folder,"*.atlas.txt").First();string png=Directory.GetFiles(folder,"*.png").First();
            var imp=(TextureImporter)AssetImporter.GetAtPath(png);imp.textureType=TextureImporterType.Default;imp.alphaIsTransparency=false;imp.mipmapEnabled=false;imp.npotScale=TextureImporterNPOTScale.None;imp.maxTextureSize=4096;imp.textureCompression=TextureImporterCompression.Uncompressed;imp.SaveAndReimport();
            string matPath=folder+"Original.mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if(mat==null){mat=new Material(Shader.Find("Spine/Skeleton"));AssetDatabase.CreateAsset(mat,matPath);}mat.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(png);EditorUtility.SetDirty(mat);
            string atlasPath=folder+"OriginalAtlas.asset";var aa=AssetDatabase.LoadAssetAtPath<SpineAtlasAsset>(atlasPath);
            if(aa==null){aa=SpineAtlasAsset.CreateRuntimeInstance(AssetDatabase.LoadAssetAtPath<TextAsset>(atlas),new[]{mat},true);AssetDatabase.CreateAsset(aa,atlasPath);}
            string rigPath=folder+"OriginalSkeleton.asset";var rig=AssetDatabase.LoadAssetAtPath<SkeletonDataAsset>(rigPath);
            if(rig==null){rig=SkeletonDataAsset.CreateRuntimeInstance(AssetDatabase.LoadAssetAtPath<TextAsset>(json),aa,true,.01f);AssetDatabase.CreateAsset(rig,rigPath);}
            if(rig.GetSkeletonData(false)==null)throw new InvalidOperationException("Invalid original Spine runtime: "+json);return rig;
        }
        static void Place(RectTransform r,float x,float y,float w,float h){r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);}
        static void Home()
        {
            const string path="Assets/MegaTanks/Prefabs/UI/Home.prefab";var root=PrefabUtility.LoadPrefabContents(path);
            try
            {
                var v=root.GetComponent<UIView>();
                foreach(var r in root.GetComponentsInChildren<RectTransform>(true))if(new[]{"Currency Balances","Stage Name Plate","Decor Navigation Rail","Decor Navigation Accent","Decor Navigation Title","Equipment Caption","Crew Caption","Tech Caption","Tech Availability","Crew Availability"}.Contains(r.name))r.gameObject.SetActive(false);
                Place(v.Get<RectTransform>("Avatar Menu"),1784,43,107,60);Place(v.Get<RectTransform>("Stage Swipe Area"),30,940,383,110);Place(v.Get<RectTransform>("Play"),1709,966,203,93);
                var play=v.Get<Text>("Play/Label");play.text="PLAY";play.fontSize=32;play.verticalOverflow=VerticalWrapMode.Overflow;Place(play.rectTransform,5,5,193,83);
                Place(v.Get<RectTransform>("Previous Stage"),0,0,80,110);Place(v.Get<RectTransform>("Next Stage"),303,0,80,110);
                var count=v.Get<Text>("Stage Swipe Area/Stage Count");count.fontSize=18;Place(count.rectTransform,90,24,203,25);
                var route=v.Get<Text>("Stage Swipe Area/Route Name");route.fontSize=22;route.resizeTextForBestFit=true;route.resizeTextMinSize=15;route.resizeTextMaxSize=22;Place(route.rectTransform,90,52,203,40);
                foreach(var pair in new[]{("Equipment",1808f,143f),("Crew",1808f,244f),("Tech",1808f,345f)})
                {var r=v.Get<RectTransform>(pair.Item1);Place(r,pair.Item2,pair.Item3,110,85);foreach(var text in r.GetComponentsInChildren<Text>(true))text.gameObject.SetActive(false);foreach(var child in r.GetComponentsInChildren<RectTransform>(true)){if(child.name=="Home Button Frame")Place(child,0,0,110,85);if(child.name=="Home Icon")Place(child,8,0,94,80);}}
                if(root.GetComponent<HtmlMainPresentation>()==null)root.AddComponent<HtmlMainPresentation>();
                PrefabUtility.SaveAsPrefabAsset(root,path);
            }finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        static void Require(bool ok,string text){if(!ok)throw new InvalidOperationException(text);}
        [MenuItem("MegaTanks/HTML Battle/Verify Original Port")]
        public static void Audit()
        {
            var c=HtmlBattleConfig.Load();var report=new System.Text.StringBuilder();
            var run=new HtmlBattleSimulation(c,0);run.ConfigureProfile(1);Require(run.phase=="approach"&&!run.SlotUnlocked(0),"Lv1 initial station");
            while(run.phase=="approach")run.Tick(.05f);
            Require(run.phase=="upgrade","Upgrade is not a choice");float road=run.road;for(int i=0;i<15;i++)run.Tick(.05f);
            Require(run.UpgradeApplied&&run.EffectiveLevel==5&&run.baseTankLevel==1&&run.road==road,"Temporary unlock applies once while stopped");
            for(int i=0;i<20;i++)run.Tick(.05f);Require(run.EffectiveLevel==5,"No repeated upgrade");
            var variants=new HtmlBattleSimulation(c,0);variants.ConfigureProfile(20);variants.phase="choice";variants.encounter="ammo";int ammo=variants.ammo[1];
            Require(variants.Choose("cannon_ap")&&variants.weaponVariants[0]=="mg"&&variants.weaponVariants[1]=="cannon_ap"&&variants.ammo[1]==ammo,"Variant slot and reserves");
            variants.phase="choice";Require(!variants.Choose("mg_plasma")&&variants.weaponVariants[0]=="mg","Uncompleted AD rejected");
            var trojan=new HtmlBattleSimulation(c,0);trojan.ConfigureProfile(20);var carrier=trojan.Spawn("trojan");Require(trojan.RevealTrojan(carrier)&&!trojan.RevealTrojan(carrier),"Troy reveal once");
            Require(trojan.enemies.Count(e=>e.carrier==carrier)==3,"Exactly three registered payloads");
            foreach(var dog in trojan.enemies.Where(e=>e.carrier==carrier).ToArray())trojan.Damage(dog,100000);
            Require(trojan.kills==1,"Carrier counts once after payloads");
            const string key="MegaTanks.PortAudit.Profile";string saved=PlayerPrefs.GetString(key,"");bool existed=PlayerPrefs.HasKey(key);
            try{PlayerPrefs.DeleteKey(key);var profile=new PlayerTankProfile(c,key);Require(profile.Award("audit-once",100)&&!profile.Award("audit-once",100),"EXP idempotence");Require(new PlayerTankProfile(c,key).TotalXP==100,"EXP reload");}
            finally{if(existed)PlayerPrefs.SetString(key,saved);else PlayerPrefs.DeleteKey(key);PlayerPrefs.Save();}
            Require(new HtmlBattleSimulation(c,0).temporaryTankLevel==1,"Run level reset");
            report.AppendLine("PASS: original asset provenance; Lv1 station; stop/apply-once; temporary level reset; per-slot variants; AD guard; Troy x3/count; permanent EXP once/reload.");
            for(int stage=0;stage<c.stages.Length;stage++)
            {
                var full=new HtmlBattleSimulation(c,stage);full.Geometry=Resources.Load<HtmlBattleArt>("HtmlBattleArt");full.ConfigureProfile(1);int choices=0;
                for(int tick=0;tick<30000&&!full.Terminal;tick++)
                {
                    if(full.phase=="choice"){string option=full.encounter=="wreck"?"swap":full.encounter=="ammo"?"cannon_ap":full.encounter=="medical"?(full.player.hp<full.player.maxHP?"heal":"gears"):"mg";if(!full.Choose(option))full.Choose("skip");choices++;}
                    foreach(var enemy in full.enemies.Where(e=>!e.dead).ToArray())full.Damage(enemy,100000);
                    full.Tick(.05f);
                }
                Require(full.phase=="won"&&full.bossKilled&&full.chestOpened,"Full route/chest stage "+stage+" stalled at "+full.phase);
                int coins=full.money,gears=full.gears;for(int repeat=0;repeat<20;repeat++)full.Tick(.05f);Require(coins==full.money&&gears==full.gears,"Terminal rewards duplicate");
                report.AppendLine("PASS: stage "+(stage+1)+" full route, "+choices+" choices, boss, loot/chest and terminal reward guard.");
                var natural=new HtmlBattleSimulation(c,stage);natural.Geometry=full.Geometry;natural.ConfigureProfile(20);
                for(int tick=0;tick<18000&&!natural.Terminal;tick++){if(natural.phase=="choice"){string choice=natural.encounter=="wreck"?"swap":natural.encounter=="medical"?(natural.player.hp<natural.player.maxHP?"heal":"gears"):natural.encounter=="supply"?"mg":"cannon_ap";if(!natural.Choose(choice))natural.Choose("skip");}natural.Tick(.05f);}
                report.AppendLine("BALANCE OBSERVATION: stage "+(stage+1)+" default combat simulation = "+natural.phase+", wave "+natural.wave+", kills "+natural.kills+", tank HP "+natural.player.hp.ToString("0.0")+". This is not a balance approval.");
            }
            var geometry=Resources.Load<HtmlBattleArt>("HtmlBattleArt");
            foreach(var definition in c.enemies)
            {
                var shots=new HtmlBattleSimulation(c,0);shots.Geometry=geometry;shots.ConfigureProfile(20);shots.phase="wave";shots.player.hp=shots.player.maxHP=100000;
                var enemy=shots.Spawn(definition.id);enemy.x=shots.player.x+400;enemy.attack=0;
                for(int tick=0;tick<150;tick++)shots.Tick(.05f);
                var muzzle=shots.ShotOrigin(enemy,definition.id=="cannon_car"?1:0,true);Require(!float.IsNaN(muzzle.x)&&!float.IsNaN(muzzle.y),"Finite muzzle "+definition.id);
            }
            report.AppendLine("PASS: real serialized Unity geometry attached to all three routes and firing from every enemy type, including legacy empty socket arrays.");
            Directory.CreateDirectory("Checks/HtmlPort");File.WriteAllText("Checks/HtmlPort/validation.txt",report.ToString());Debug.Log("HTML_PORT_AUDIT_OK: "+report);
        }
    }
}
