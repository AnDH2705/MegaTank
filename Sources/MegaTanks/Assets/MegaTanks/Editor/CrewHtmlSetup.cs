using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object=UnityEngine.Object;

namespace MegaTanks.Editor
{
    [InitializeOnLoad]
    public static class CrewHtmlSetup
    {
        static CrewHtmlSetup()
        {
            EditorApplication.delayCall+=()=>{
                const string marker="Checks/crew-apply.pending";
                if(!EditorApplication.isPlaying&&File.Exists(marker)){File.Delete(marker);Apply();}
            };
        }
        const string Art="Assets/MegaTanks/Art/UI/CrewHtml/";
        const string Path="Assets/MegaTanks/Prefabs/UI/Crew.prefab";
        [Serializable] sealed class SkillData { public string id,role,name,purpose,icon_file; }
        [Serializable] sealed class SkillList { public SkillData[] skills; }
        static Texture2D fontAtlas;static TextAsset fontMetrics;
        static Sprite S(string file)=>AssetDatabase.LoadAssetAtPath<Sprite>(Art+file+".png");
        static RectTransform Rect(Transform parent,string name,float x,float y,float w,float h)
        {
            var r=new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer)).GetComponent<RectTransform>();r.SetParent(parent,false);
            r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);return r;
        }
        static Image Image(Transform parent,string name,Sprite sprite,float x,float y,float w,float h)
        { var i=Rect(parent,name,x,y,w,h).gameObject.AddComponent<Image>();i.sprite=sprite;i.raycastTarget=false;return i; }
        static CrewBitmapText Text(Transform parent,string name,string value,float x,float y,float w,float h,float size=16,bool center=false)
        { var t=Rect(parent,name,x,y,w,h).gameObject.AddComponent<CrewBitmapText>();t.atlas=fontAtlas;t.metrics=fontMetrics;t.fontSize=size;t.centered=center;t.Value=value;t.raycastTarget=false;return t; }
        static Button Button(Transform p,string name,Sprite sprite,float x,float y,float w,float h)
        {
            var image=Image(p,name,sprite,x,y,w,h);image.raycastTarget=true;
            var b=image.gameObject.AddComponent<Button>();b.targetGraphic=image;var c=b.colors;c.disabledColor=new Color(.65f,.65f,.65f,1);b.colors=c;return b;
        }
        [MenuItem("MegaTanks/Crew/Apply HTML Layout %#&c")]
        public static void Apply()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play before changing the Crew prefab.");
            foreach(var guid in AssetDatabase.FindAssets("t:Texture2D",new[]{Art.TrimEnd('/')}))
            {
                var importer=(TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(guid));
                importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
                importer.spritePixelsPerUnit=400;importer.alphaIsTransparency=true;importer.mipmapEnabled=false;
                importer.npotScale=TextureImporterNPOTScale.None;importer.wrapMode=TextureWrapMode.Clamp;importer.filterMode=FilterMode.Bilinear;
                importer.textureCompression=TextureImporterCompression.Uncompressed;importer.maxTextureSize=4096;
                var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);settings.spriteMeshType=SpriteMeshType.FullRect;importer.SetTextureSettings(settings);importer.SaveAndReimport();
            }
            fontAtlas=AssetDatabase.LoadAssetAtPath<Texture2D>(Art+"font/font-atlas.png");fontMetrics=AssetDatabase.LoadAssetAtPath<TextAsset>(Art+"font/font-atlas.json");
            if(fontAtlas==null||fontMetrics==null||S("illustrations/garage")==null)throw new InvalidOperationException("Crew art must be copied from Art/Asset/UI/Crew first.");
            string matPath=Art+"LockedSkill.mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if(mat==null){mat=new Material(Shader.Find("MegaTanks/UI/Crew Grayscale"));AssetDatabase.CreateAsset(mat,matPath);}
            var root=PrefabUtility.LoadPrefabContents(Path);
            try
            {
                foreach(var child in root.transform.Cast<Transform>().ToArray())Object.DestroyImmediate(child.gameObject);
                var ui=root.GetComponent<CrewHtmlScreen>()??root.AddComponent<CrewHtmlScreen>();
                var wrapper=Rect(root.transform,"Crew Design 770x436",0,0,770,436);wrapper.localScale=new Vector3(1920f/770,1080f/436,1);
                var bg=Image(wrapper,"Garage",S("illustrations/garage"),0,0,770,436);bg.raycastTarget=true;
                Image(wrapper,"Tank",S("illustrations/tank"),84,96,259,267);
                Image(wrapper,"Detail Panel",S("layers/panel_detail"),306,0,333,436);
                Image(wrapper,"Roster Panel",S("layers/panel_roster"),523,0,247,436);
                Image(wrapper,"Stats Panel",S("layers/panel_stats"),342,136,200,115);
                Image(wrapper,"Name Ribbon",S("decor/ribbon_name"),425,62,122,24);
                ui.back=Button(wrapper,"Back",S("decor/back"),0,1,76,47);
                ui.normalPortrait=S("layers/portrait_driver_roster_normal");ui.meleePortrait=S("layers/portrait_melee_roster");ui.emptyPortrait=S("layers/portrait_empty");
                ui.yellowButton=S("layers/button_yellow");ui.grayButton=S("layers/button_disabled");ui.normalPlate=S("slices/slot_label_normal");ui.selectedPlate=S("slices/slot_label_selected");ui.lockedMaterial=mat;
                CrewRole[] roles={CrewRole.SubGunner,CrewRole.WeaponSpecialist,CrewRole.Gunner,CrewRole.Driver,CrewRole.Commando};
                Vector2[] pos={new Vector2(155,84),new Vector2(104,161),new Vector2(202,161),new Vector2(104,245),new Vector2(190,245)};
                ui.stations=new CrewHtmlScreen.Station[5];
                for(int i=0;i<5;i++)
                {
                    var b=Button(wrapper,"Station "+roles[i],ui.emptyPortrait,pos[i].x,pos[i].y,65,65);
                    var outline=Image(b.transform,"Selection",S("decor/selection_outline"),-3,-3,71,71);
                    var plate=Image(b.transform,"Role Plate",ui.normalPlate,-3,60,71,11);plate.type=UnityEngine.UI.Image.Type.Simple;
                    var label=Text(b.transform,"Role",CrewHtmlScreen.RoleKey(roles[i]).ToUpperInvariant(),-3,60,71,11,9,true);
                    ui.stations[i]=new CrewHtmlScreen.Station{role=roles[i],button=b,portrait=b.image,outline=outline,plate=plate,label=label};
                }
                ui.hero=Image(wrapper,"Hero Portrait",ui.normalPortrait,355,63,68,65);
                ui.roleLabel=Text(wrapper,"Role Title","Tank Cannon",363,13,185,25,20,true);
                ui.nameLabel=Text(wrapper,"Crew Name","Milo",431,64,88,19,16);ui.nameLabel.color=Color.black;
                ui.levelLabel=Text(wrapper,"Crew Level","LV.1",429,87,92,18,16);
                ui.stars=Rect(wrapper,"Crew Stars",425,102,90,17).gameObject.AddComponent<UIStarStrip>();ui.stars.raycastTarget=false;
                string[] icons={"atk","hp","accuracy","defense","move_speed","crit_rate"};
                ui.stats=new CrewBitmapText[6];
                for(int i=0;i<6;i++)
                {
                    int col=i%2,row=i/2;float x=356+col*94,y=143+row*36;
                    Image(wrapper,"Stat Icon "+icons[i],S("layers/icon_"+icons[i]),x,y,23,23);
                    ui.stats[i]=Text(wrapper,"Stat "+icons[i],"0",x+30,y+4,57,17,14);
                }
                var data=JsonUtility.FromJson<SkillList>(AssetDatabase.LoadAssetAtPath<TextAsset>(Art+"skills.json").text);
                ui.skills=data.skills.Select(s=>new CrewHtmlScreen.Skill{role=s.role,name=s.name,purpose=s.purpose,icon=AssetDatabase.LoadAssetAtPath<Sprite>(Art+s.icon_file.Replace("../Skill_Icons/","skills/"))}).ToArray();
                ui.skillIcons=new Image[3];ui.skillNames=new CrewBitmapText[3];
                for(int i=0;i<3;i++)
                {
                    var b=Button(wrapper,"Skill "+i,S("layers/skill_frame"),341+i*67,262,48,48);
                    ui.skillIcons[i]=Image(b.transform,"Icon",S("layers/icon_skill_"+(i+1)),4,4,40,40);
                    ui.skillNames[i]=Text(wrapper,"Skill Name "+i,new[]{"Steady\nAim","Quick\nReload","Heavy\nShells"}[i],341+i*67,312,50,25,12,true);
                }
                ui.levelUp=Button(wrapper,"Level Up",ui.yellowButton,328,347,121,37);
                ui.levelUpLabel=Text(ui.levelUp.transform,"Label","LEVEL UP!",8,3,105,29,17,true);ui.levelUpLabel.color=Color.black;
                ui.ascend=Button(wrapper,"Ascend",ui.grayButton,449,347,72,37);
                ui.ascendProgress=Image(ui.ascend.transform,"Progress",ui.yellowButton,0,0,72,37);ui.ascendProgress.type=UnityEngine.UI.Image.Type.Filled;ui.ascendProgress.fillMethod=UnityEngine.UI.Image.FillMethod.Horizontal;
                ui.fragmentIcon=Image(ui.ascend.transform,"Fragment Icon",S("decor/hero_fragment"),4,10,15,15);
                ui.ascendLabel=Text(ui.ascend.transform,"Label","0/20",20,5,48,27,14,true);
                ui.equip=Button(wrapper,"Equip",S("layers/button_green"),327,390,187,37);
                ui.equipLabel=Text(ui.equip.transform,"Label","Equip",10,4,167,27,19,true);ui.equipLabel.color=Color.black;
                ui.feedback=Text(wrapper,"Cost and Feedback","LEVEL UP: 100 FOOD",332,335,205,11,7.5f,true);
                Text(wrapper,"Roster Title","Select your crew:",575,12,187,29,19,true);
                var viewport=Image(wrapper,"Roster",null,536,59,234,366);viewport.color=Color.clear;viewport.raycastTarget=true;
                viewport.gameObject.AddComponent<RectMask2D>();var content=Rect(viewport.transform,"Content",0,0,234,366);
                ui.rosterScroll=viewport.gameObject.AddComponent<ScrollRect>();ui.rosterScroll.viewport=viewport.rectTransform;ui.rosterScroll.content=content;
                ui.rosterScroll.horizontal=false;ui.rosterScroll.vertical=true;ui.rosterScroll.movementType=ScrollRect.MovementType.Clamped;
                var catalog=AssetDatabase.LoadAssetAtPath<CrewCatalog>(CrewSetup.CatalogPath);
                int count=Math.Max(15,catalog.members.GroupBy(m=>m.role).Max(g=>g.Count()));content.sizeDelta=new Vector2(234,Math.Max(366,Mathf.Ceil(count/3f)*73));
                ui.candidates=new CrewHtmlScreen.Candidate[count];
                for(int i=0;i<count;i++)
                {
                    int row=i/3,col=i%3;float x=Mathf.Max(0,28+col*66-row*7),y=3+row*73;
                    var b=Button(content,"Candidate "+i,ui.emptyPortrait,x,y,65,65);
                    var selected=Image(b.transform,"Selected Outline",S("decor/roster_selected_outline"),-2,-2,69,69);
                    var tick=Image(b.transform,"Equipped Tick",S("decor/check_equipped"),48,48,16,16);
                    var name=Text(b.transform,"Name","",0,65,65,8,6,true);
                    ui.candidates[i]=new CrewHtmlScreen.Candidate{button=b,portrait=b.image,selected=selected,equipped=tick,name=name};
                }
                var tip=Image(wrapper,"Skill Tooltip",null,330,211,220,124);tip.color=new Color(.04f,.055f,.075f,.98f);tip.raycastTarget=true;
                var close=tip.gameObject.AddComponent<Button>();close.onClick.AddListener(()=>tip.gameObject.SetActive(false));
                var tt=Rect(tip.transform,"Text",8,7,204,110).gameObject.AddComponent<Text>();tt.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");tt.fontSize=11;tt.color=Color.white;tt.raycastTarget=false;
                ui.tooltip=tip.gameObject;ui.tooltipText=tt;tip.gameObject.SetActive(false);
                // Shared currency bindings remain available to the existing Canvas controller.
                var balances=Rect(root.transform,"Currency Balances",0,0,0,0);balances.gameObject.AddComponent<Text>();
                foreach(var name in new[]{"Parts","PetFood","Gems"})Rect(Rect(balances,name,0,0,0,0),"Amount",0,0,0,0).gameObject.AddComponent<Text>();
                balances.gameObject.SetActive(false);
                var view=root.GetComponent<UIView>();view.CaptureBindings();
                var serialized=new SerializedObject(view);var bindings=serialized.FindProperty("bindings");
                int alias=bindings.arraySize;bindings.arraySize++;
                bindings.GetArrayElementAtIndex(alias).FindPropertyRelative("key").stringValue="Close Popup";
                bindings.GetArrayElementAtIndex(alias).FindPropertyRelative("target").objectReferenceValue=ui.back.transform;
                serialized.ApplyModifiedPropertiesWithoutUndo();PrefabUtility.SaveAsPrefabAsset(root,Path);
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
            AssetDatabase.SaveAssets();Debug.Log("CREW_HTML_LAYOUT_OK: native Crew screen uses Art/Asset/UI/Crew; preview and Equip are separate.");
        }
    }
}


