using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object=UnityEngine.Object;

namespace MegaTanks.Editor
{
    // Applies native Canvas geometry without recapturing stable UIView binding IDs.
    public static class LoadoutArtworkSetup
    {
        const string Art="Assets/MegaTanks/Art/UI/Crew/",UI="Assets/MegaTanks/Prefabs/UI/";
        static Font font;
        [Serializable] sealed class Glyph{public int code,advance;public int[] baseline,box;}
        [Serializable] sealed class Metrics{public int size;public Glyph[] glyphs;}
        static Sprite S(string path)=>AssetDatabase.LoadAssetAtPath<Sprite>(Art+path+".png");
        static void Place(RectTransform r,float x,float y,float w,float h){r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);r.localScale=Vector3.one;}
        static void Move(Component c,Transform parent,float x,float y,float w,float h){c.transform.SetParent(parent,false);Place((RectTransform)c.transform,x,y,w,h);c.gameObject.SetActive(true);}
        static Image Image(Transform p,string name,string sprite,float x,float y,float w,float h,bool sliced=false)
        {
            var image=new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(Image)).GetComponent<Image>();image.transform.SetParent(p,false);image.sprite=S(sprite);image.raycastTarget=false;image.type=sliced?UnityEngine.UI.Image.Type.Sliced:UnityEngine.UI.Image.Type.Simple;Place(image.rectTransform,x,y,w,h);return image;
        }
        static void Type(Text t,int size,Color color)
        {
            t.font=font;t.fontSize=size;t.fontStyle=FontStyle.Normal;t.color=color;t.alignment=TextAnchor.MiddleCenter;t.raycastTarget=false;t.resizeTextForBestFit=false;t.horizontalOverflow=HorizontalWrapMode.Wrap;t.verticalOverflow=VerticalWrapMode.Truncate;t.lineSpacing=1;
            foreach(var shadow in t.GetComponents<Shadow>())Object.DestroyImmediate(shadow);
        }
        static Text Label(Transform p,string name,string value,float x,float y,float w,float h,int size)
        {var t=new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(Text)).GetComponent<Text>();t.transform.SetParent(p,false);Place(t.rectTransform,x,y,w,h);Type(t,size,Color.white);t.text=value;return t;}
        static void Bind(UIView view,string key,RectTransform target)
        {
            var so=new SerializedObject(view);var list=so.FindProperty("bindings");int index=list.arraySize;list.InsertArrayElementAtIndex(index);var entry=list.GetArrayElementAtIndex(index);entry.FindPropertyRelative("key").stringValue=key;entry.FindPropertyRelative("target").objectReferenceValue=target;so.ApplyModifiedPropertiesWithoutUndo();typeof(UIView).GetField("index",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(view,null);
        }
        static void Button(UIView v,Transform parent,string key,string sprite,float x,float y,float w,float h,int size=12,bool light=false)
        {
            var b=v.Get<Button>(key);Move(b,parent,x,y,w,h);foreach(Transform child in b.transform)child.gameObject.SetActive(false);
            b.image.sprite=S(sprite);b.image.color=Color.white;b.image.type=UnityEngine.UI.Image.Type.Simple;var outline=b.GetComponent<Outline>();if(outline!=null)outline.enabled=false;
            var colors=ColorBlock.defaultColorBlock;colors.disabledColor=new Color(.5f,.5f,.5f,.85f);b.colors=colors;
            var text=v.Get<Text>(key+"/Label");Move(text,b.transform,4,0,w-8,h);Type(text,size,light?Color.white:Color.black);
        }
        static void MakeFont()
        {
            string path=Art+"font/CrewBitmap.fontsettings";font=AssetDatabase.LoadAssetAtPath<Font>(path);if(font!=null)return;
            var tex=AssetDatabase.LoadAssetAtPath<Texture2D>(Art+"font/font-atlas.png");var metrics=JsonUtility.FromJson<Metrics>(File.ReadAllText(Art+"font/font-atlas.json"));
            font=new Font("AntiqueOliCom Crew");AssetDatabase.CreateAsset(font,path);var material=new Material(Shader.Find("UI/Default")){name="Crew Font",mainTexture=tex};AssetDatabase.AddObjectToAsset(material,font);font.material=material;
            var so=new SerializedObject(font);so.FindProperty("m_FontSize").floatValue=metrics.size;so.FindProperty("m_LineSpacing").floatValue=76;so.FindProperty("m_Ascent").floatValue=64;so.ApplyModifiedPropertiesWithoutUndo();
            font.characterInfo=metrics.glyphs.Select(g=>new CharacterInfo{index=g.code,advance=g.advance,minX=g.box[0]-g.baseline[0],maxX=g.box[2]-g.baseline[0],minY=g.baseline[1]-g.box[3],maxY=g.baseline[1]-g.box[1],uvBottomLeft=new Vector2((float)g.box[0]/tex.width,1f-(float)g.box[3]/tex.height),uvBottomRight=new Vector2((float)g.box[2]/tex.width,1f-(float)g.box[3]/tex.height),uvTopLeft=new Vector2((float)g.box[0]/tex.width,1f-(float)g.box[1]/tex.height),uvTopRight=new Vector2((float)g.box[2]/tex.width,1f-(float)g.box[1]/tex.height)}).ToArray();EditorUtility.SetDirty(font);
        }
        static void ImportIcons()
        {
            foreach(string folder in new[]{"Skill_Icons","Stat_Icons"})foreach(string path in Directory.GetFiles("Assets/MegaTanks/Art/UI/"+folder,"*.png"))
            {
                var t=(TextureImporter)AssetImporter.GetAtPath(path);t.textureType=TextureImporterType.Sprite;t.spriteImportMode=SpriteImportMode.Single;t.spritePixelsPerUnit=100;t.mipmapEnabled=false;t.alphaIsTransparency=true;t.textureCompression=TextureImporterCompression.Uncompressed;var settings=new TextureImporterSettings();t.ReadTextureSettings(settings);settings.spriteMeshType=SpriteMeshType.FullRect;t.SetTextureSettings(settings);t.SaveAndReimport();
            }
        }
        static void Card(bool crew)
        {
            string target=UI+(crew?"CrewRosterItem":"EquipmentRosterItem")+".prefab";if(File.Exists(target))return;
            var root=PrefabUtility.LoadPrefabContents(UI+"InventoryItem.prefab");
            try
            {
                var v=root.GetComponent<UIView>();root.name=crew?"CrewRosterItem":"EquipmentRosterItem";foreach(Transform child in root.transform)child.gameObject.SetActive(false);
                Place((RectTransform)root.transform,0,0,67,crew?70:78);var bg=v.Get<Image>("$root");bg.sprite=S(crew?"layers/portrait_roster_normal":"layers/portrait_empty");bg.color=Color.white;bg.raycastTarget=true;
                var icon=v.Get<Image>("Icon");Move(icon,root.transform,5,4,57,crew?57:43);icon.preserveAspect=!crew;icon.color=Color.white;icon.raycastTarget=false;icon.transform.SetAsLastSibling();
                var name=v.Get<Text>("Item Name");Move(name,root.transform,4,47,59,22);Type(name,8,Color.white);name.transform.SetAsLastSibling();name.gameObject.SetActive(!crew);
                var level=v.Get<Text>("Progression Level");Move(level,root.transform,4,crew?52:2,43,11);Type(level,7,Color.white);level.alignment=TextAnchor.MiddleLeft;level.transform.SetAsLastSibling();
                var stars=v.Get<UIStarStrip>("Stars");Move(stars,root.transform,6,crew?63:69,55,6);stars.transform.SetAsLastSibling();
                var selected=Image(root.transform,"Selection","decor/roster_selected_outline",0,0,67,crew?67:78);selected.gameObject.SetActive(false);
                var tick=v.Get<Image>("Equipped Indicator");Move(tick,root.transform,49,crew?47:31,15,15);tick.sprite=S("decor/check_equipped");tick.color=Color.white;tick.raycastTarget=false;tick.transform.SetAsLastSibling();
                var outline=v.Get<Outline>("$root");outline.effectColor=Color.green;outline.effectDistance=new Vector2(1,-1);outline.enabled=false;
                PrefabUtility.SaveAsPrefabAsset(root,target);
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        static ScrollRect Scroll(Transform parent,string name,float x,float y,float w,float h,out Text text)
        {
            var scroll=new GameObject(name,typeof(RectTransform),typeof(ScrollRect)).GetComponent<ScrollRect>();scroll.transform.SetParent(parent,false);Place((RectTransform)scroll.transform,x,y,w,h);
            var viewport=new GameObject("Viewport",typeof(RectTransform),typeof(RectMask2D)).GetComponent<RectTransform>();viewport.SetParent(scroll.transform,false);Place(viewport,0,0,w,h);
            text=Label(viewport,"Content","",0,0,w,h,10);text.alignment=TextAnchor.UpperLeft;text.verticalOverflow=VerticalWrapMode.Overflow;text.raycastTarget=true;
            scroll.viewport=viewport;scroll.content=text.rectTransform;scroll.horizontal=false;scroll.movementType=ScrollRect.MovementType.Clamped;scroll.scrollSensitivity=25;return scroll;
        }
        static void Inventory(UIView v,Transform panel,bool crew)
        {
            var inv=v.Get<ScrollRect>("Inventory");Move(inv,panel,558,57,204,337);inv.horizontal=false;
            foreach(var image in inv.GetComponentsInChildren<Image>(true))if(!image.transform.IsChildOf(inv.content))image.color=Color.clear;
            var grid=inv.content.GetComponent<GridLayoutGroup>();grid.constraint=GridLayoutGroup.Constraint.FixedColumnCount;grid.constraintCount=3;grid.cellSize=new Vector2(64,crew?70:78);grid.spacing=new Vector2(3,crew?4:5);grid.padding=new RectOffset(3,3,3,3);
            var viewport=inv.viewport;
            if(viewport!=inv.transform){viewport.anchorMin=Vector2.zero;viewport.anchorMax=Vector2.one;viewport.offsetMin=viewport.offsetMax=Vector2.zero;}
            var clip=viewport.GetComponent<RectMask2D>()??viewport.gameObject.AddComponent<RectMask2D>();clip.enabled=true;
            inv.content.anchorMin=new Vector2(0,1);inv.content.anchorMax=Vector2.one;inv.content.pivot=new Vector2(.5f,1);inv.content.anchoredPosition=Vector2.zero;inv.content.sizeDelta=new Vector2(0,0);
            var fitter=inv.content.GetComponent<ContentSizeFitter>()??inv.content.gameObject.AddComponent<ContentSizeFitter>();fitter.verticalFit=ContentSizeFitter.FitMode.PreferredSize;fitter.horizontalFit=ContentSizeFitter.FitMode.Unconstrained;
            foreach(Transform child in inv.content)child.gameObject.SetActive(false);
            var empty=v.Get<Text>("Empty Inventory");Move(empty,panel,560,190,190,80);Type(empty,12,Color.white);
            if(crew){Button(v,panel,"Inventory Filter","layers/button_disabled",558,402,124,25,9,true);Button(v,panel,"Sort Direction","layers/button_disabled",686,402,76,25,9,true);}
            else
            {
                Button(v,panel,"Inventory Filter","layers/button_disabled",558,400,89,27,9,true);Button(v,panel,"Inventory Sort","layers/button_disabled",651,400,64,27,9,true);Button(v,panel,"Sort Direction","layers/button_disabled",719,400,43,27,9,true);
                var filter=v.Get<RectTransform>("Filter List");Move(filter,panel,558,219,204,174);foreach(Transform child in filter)child.gameObject.SetActive(false);
                var bg=filter.GetComponent<Image>();if(bg!=null){bg.sprite=S("slices/panel_detail");bg.type=UnityEngine.UI.Image.Type.Sliced;bg.color=Color.white;}
                string[] options={"All","Cannon","MachineGun","Mortar","Armor","Accessory"};for(int i=0;i<options.Length;i++)Button(v,filter,"Filter List/Filter "+options[i],"layers/button_disabled",5,4+28*i,194,26,11,true);filter.gameObject.SetActive(false);
            }
        }
        static void Build(bool crew)
        {
            string path=UI+(crew?"Crew":"Equipment")+".prefab";if(AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponent<LoadoutArtPresentation>()!=null)return;
            var root=PrefabUtility.LoadPrefabContents(path);
            try
            {
                var v=root.GetComponent<UIView>();var legacy=new GameObject("Legacy Bindings",typeof(RectTransform));legacy.transform.SetParent(root.transform,false);
                foreach(var child in root.transform.Cast<Transform>().Where(t=>t!=legacy.transform).ToArray()){child.gameObject.SetActive(false);child.SetParent(legacy.transform,false);}legacy.SetActive(false);
                var p=new GameObject("Artwork Layout",typeof(RectTransform)).GetComponent<RectTransform>();p.SetParent(root.transform,false);Place(p,0,0,770,436);p.localScale=new Vector3(1920f/770,1080f/436,1);
                var art=root.AddComponent<LoadoutArtPresentation>();art.crew=crew;Image(p,"Garage","illustrations/garage",0,0,770,436);
                if(crew)Image(p,"Tank Illustration","illustrations/tank",84,96,259,267);else Move(v.Get<RawImage>("Tank Avatar"),p,40,47,274,342);
                Image(p,"Detail Panel","slices/panel_detail",348.5f,0,290.5f,436,true).gameObject.AddComponent<CrewPanelSkew>();Image(p,"Inventory Panel","slices/panel_roster",565.5f,0,247,436,true).gameObject.AddComponent<CrewPanelSkew>();
                Button(v,p,"Close Popup","slices/button_back",0,0,76,47);v.Get<Button>("Close Popup").image.type=UnityEngine.UI.Image.Type.Sliced;v.Get<Text>("Close Popup/Label").text="";Image(v.Get<Button>("Close Popup").transform,"Chevron","decor/back_chevron",24,4,24,34);
                art.heading=Label(p,"Heading",crew?"Select a crew":"Equipment",366,12,177,28,17);Label(p,"Roster Heading",crew?"Select your crew:":"Select equipment:",574,16,185,27,16);
                art.ribbon=Image(p,"Name Ribbon","decor/ribbon_name",425,62,122,24).gameObject;
                var name=v.Get<Text>("Detail Name");Move(name,p,430,64,100,19);Type(name,12,Color.black);name.alignment=TextAnchor.MiddleLeft;
                art.level=Label(p,"Level","",429,88,110,17,13);art.level.alignment=TextAnchor.MiddleLeft;Move(v.Get<UIStarStrip>("Detail Stars"),p,425,105,85,15);
                if(crew)
                {
                    Move(v.Get<Image>("Detail Portrait"),p,359,65,60,60);v.Get<Image>("Detail Portrait").preserveAspect=false;v.Get<Image>("Detail Portrait").raycastTarget=true;art.portraitInfo=v.Get<Image>("Detail Portrait").gameObject.AddComponent<Button>();
                    Label(p,"Portrait Hint","TAP FOR LEVEL GAINS",427,121,117,10,6);
                    BuildCrew(v,p,art);
                }
                else
                {
                    Move(v.Get<Image>("Detail Item Icon"),p,358,64,62,62);v.Get<Image>("Detail Item Icon").preserveAspect=true;
                    Move(v.Get<Image>("Equipped Detail Tick"),p,523,111,14,14);Move(v.Get<Text>("Equipped Detail Label"),p,429,121,110,12);Type(v.Get<Text>("Equipped Detail Label"),8,Color.white);
                    var prompt=v.Get<Text>("Detail Prompt");Move(prompt,p,354,168,181,106);Type(prompt,13,Color.white);prompt.text="Select equipment\nto inspect stats\nand upgrades.";
                    Image(p,"Stat Panel","slices/panel_stats",352,138,190,169,true).gameObject.AddComponent<CrewPanelSkew>();prompt.transform.SetAsLastSibling();
                    art.statsCaption=Label(p,"Stats Caption","STATS & COMPARISON",349,134,187,14,8);art.statsSource=v.Get<Text>("Item Stats");art.statsScroll=Scroll(p,"Stats Scroll",346,151,192,153,out art.statsText);
                    art.summary=Label(p,"Upgrade Summary","",338,309,202,31,8);BuildEquipment(v,p,art);
                }
                art.detailFrame=Image(p,"Detail Frame","decor/selection_outline",354,60,70,70).gameObject;
                art.cost=Label(p,"Upgrade Cost","",329,331,192,15,8);
                Button(v,p,"Level Up","layers/button_yellow",328,347,121,37,14);Button(v,p,"Star Up","layers/button_yellow",449,347,72,37,11);
                art.fragmentFill=Image(v.Get<Button>("Star Up").transform,"Fragment Progress","layers/button_yellow",0,0,72,37);art.fragmentFill.type=UnityEngine.UI.Image.Type.Filled;art.fragmentFill.fillMethod=UnityEngine.UI.Image.FillMethod.Horizontal;art.fragmentFill.transform.SetAsFirstSibling();
                Image(v.Get<Button>("Star Up").transform,"Fragment Icon","decor/hero_fragment",3,11,14,14);Place(v.Get<Text>("Star Up/Label").rectTransform,18,0,50,37);v.Get<Text>("Star Up/Label").transform.SetAsLastSibling();
                Button(v,p,crew?"Assign Crew":"Equip Item","layers/button_green",327,390,187,37,16);
                Inventory(v,p,crew);art.ascendNormal=S("layers/button_yellow");art.ascendDisabled=S("layers/button_disabled");art.labelNormal=S("slices/slot_label_normal");art.labelSelected=S("slices/slot_label_selected");
                if(crew)BuildInfo(p,art);
                PrefabUtility.SaveAsPrefabAsset(root,path);
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        static void BuildEquipment(UIView v,Transform p,LoadoutArtPresentation art)
        {
            Label(p,"Loadout Caption","LOADOUT",111,12,202,28,17);
            art.slotKeys=new[]{"Slot MG","Slot Mortar","Slot Cannon","Slot Armor","Slot Accessory 2","Slot Accessory 3"};art.slotLabels=new Text[6];float[,] points={{156,61},{80,154},{222,154},{80,252},{222,252},{156,326}};
            for(int i=0;i<6;i++)
            {
                string key=art.slotKeys[i];var b=v.Get<Button>(key);Move(b,p,points[i,0],points[i,1],63,63);foreach(Transform child in b.transform)child.gameObject.SetActive(false);b.image.color=Color.clear;
                Image(b.transform,"Slot Frame","layers/portrait_empty",0,0,63,63).color=Color.clear;
                var icon=v.Get<Image>(key+"/Equipped Item Icon");Move(icon,b.transform,5,5,53,43);icon.preserveAspect=true;icon.color=Color.white;icon.transform.SetAsLastSibling();
                var empty=v.Get<Image>(key+"/Empty Slot Icon");Move(empty,b.transform,15,8,33,33);empty.preserveAspect=true;empty.transform.SetAsLastSibling();
                Image(b.transform,"Selection","decor/selection_outline",-2,-2,67,67).gameObject.SetActive(false);var plate=Image(b.transform,"Role Plate","slices/slot_label_normal",0,50,63,14,true);art.slotLabels[i]=Label(plate.transform,"Name","",0,0,63,14,8);
                var stars=v.Get<UIStarStrip>(key+"/Stars");Move(stars,b.transform,7,43,49,6);stars.transform.SetAsLastSibling();
            }
            Button(v,p,"Tank Selection","layers/button_disabled",18,394,282,31,11,true);var tank=v.Get<Text>("Tank Stats");Move(tank,p,8,365,130,25);Type(tank,8,Color.white);
        }
        static void BuildCrew(UIView v,Transform p,LoadoutArtPresentation art)
        {
            var preview=v.Get<RectTransform>("Tank Preview");Move(preview,p,0,0,306,380);foreach(Transform child in preview)child.gameObject.SetActive(false);var clip=preview.GetComponent<RectMask2D>();if(clip!=null)clip.enabled=false;
            float[,] points={{200,159},{106,246},{156,85},{106,159},{191,246}};string[] names={"CANNON","DRIVER","MG","MORTAR","MELEE"};art.slotLabels=new Text[5];
            for(int i=0;i<5;i++)
            {
                string key="Tank Preview/Tank Slot "+i;var b=v.Get<Button>(key);Move(b,preview,points[i,0],points[i,1],65,65);foreach(Transform child in b.transform)child.gameObject.SetActive(false);b.image.color=Color.clear;
                Image(b.transform,"Slot Frame","layers/portrait_normal",0,0,65,65).color=Color.clear;var portrait=v.Get<Image>(key+"/Portrait");Move(portrait,b.transform,4,4,57,57);portrait.preserveAspect=false;portrait.transform.SetAsLastSibling();
                var empty=v.Get<Text>(key+"/Empty");Move(empty,b.transform,4,4,57,57);Type(empty,25,Color.white);empty.text="+";empty.transform.SetAsLastSibling();
                Image(b.transform,"Selection","decor/selection_outline",-2,-2,69,69).gameObject.SetActive(false);var plate=Image(b.transform,"Role Plate","slices/slot_label_normal",0,56,65,14,true);art.slotLabels[i]=Label(plate.transform,"Name",names[i],0,0,65,14,11);
                var stars=v.Get<UIStarStrip>(key+"/Stars");Move(stars,b.transform,7,51,51,5);stars.transform.SetAsLastSibling();
            }
            var change=Object.Instantiate(v.Get<Button>("Assign Crew"),p,false);change.name="Change Tank";change.onClick=new Button.ButtonClickedEvent();Bind(v,"Change Tank",(RectTransform)change.transform);Bind(v,"Change Tank/Label",change.GetComponentInChildren<Text>(true).rectTransform);
            Button(v,p,"Change Tank","layers/button_disabled",14,390,120,30,11,true);v.Get<Text>("Change Tank/Label").text="CHANGE TANK";Button(v,p,"Unequip Crew","layers/button_disabled",144,390,118,30,11,true);
            art.statsSource=v.Get<Text>("Detail Description");Image(p,"Stat Panel","slices/panel_stats",352.3f,136,189.7f,115,true).gameObject.AddComponent<CrewPanelSkew>();
            string[] statNames={"atk","hp","accuracy","defense","move_speed","crit_rate"};art.statValues=new Text[6];art.statButtons=new Button[6];
            for(int i=0;i<6;i++)
            {
                float x=i%2==0?354:449,y=142+i/2*36;var icon=Image(p,"Stat "+statNames[i],"",x,y,26,26);icon.sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/MegaTanks/Art/UI/Stat_Icons/"+statNames[i]+".png");icon.raycastTarget=true;art.statButtons[i]=icon.gameObject.AddComponent<Button>();art.statValues[i]=Label(p,"Stat Value "+i,"-",x+32,y+1,58,24,13);art.statValues[i].alignment=TextAnchor.MiddleLeft;
            }
            string[][] skillFiles={new[]{"cannon_accuracy","cannon_fire_rate","cannon_damage"},new[]{"driver_tank_speed","driver_tank_armor","driver_team_buff"},new[]{"mg_ammo","mg_fire_rate","mg_damage"},new[]{"mortar_accuracy","mortar_ammo","mortar_damage"},new[]{"melee_shovel_damage","melee_combo","melee_resistance"}};
            art.skillSprites=skillFiles.SelectMany(a=>a).Select(s=>AssetDatabase.LoadAssetAtPath<Sprite>("Assets/MegaTanks/Art/UI/Skill_Icons/"+s+".png")).ToArray();for(int i=0;i<3;i++)art.skillSprites[i]=S("layers/icon_skill_"+(i+1));
            art.skills=new Image[3];art.skillLabels=new Text[3];art.skillButtons=new Button[3];
            for(int i=0;i<3;i++){float x=341+67*i;Image(p,"Skill Frame "+i,"layers/skill_frame",x,262,48,48);art.skills[i]=Image(p,"Skill Icon "+i,"layers/icon_skill_"+(i+1),x+4,266,40,40);art.skills[i].raycastTarget=true;art.skillButtons[i]=art.skills[i].gameObject.AddComponent<Button>();art.skillLabels[i]=Label(p,"Skill Name "+i,"",x-3,311,54,27,11);}
        }
        static void BuildInfo(Transform p,LoadoutArtPresentation art)
        {
            var panel=Image(p,"Crew Info","layers/panel_detail",328,132,217,208);panel.raycastTarget=true;art.info=panel.gameObject;art.infoScroll=Scroll(panel.transform,"Info Scroll",13,10,191,160,out art.infoText);
            var back=Image(panel.transform,"Back","layers/button_disabled",8,179,201,24);back.raycastTarget=true;art.infoClose=back.gameObject.AddComponent<Button>();Label(back.transform,"Label","BACK",0,0,201,24,12);panel.gameObject.SetActive(false);
        }
        [MenuItem("MegaTanks/UI/Rebuild Crew and Equipment Artwork")]
        public static void Apply()
        {
            AssetDatabase.Refresh();MakeFont();ImportIcons();Card(true);Card(false);Build(true);Build(false);
            foreach(string name in new[]{"Crew","Equipment"})
            {
                var popup=PrefabUtility.LoadPrefabContents(UI+name+".prefab");
                try
                {
                    foreach(var text in popup.GetComponentsInChildren<Text>(true))if(text.font==font)text.resizeTextForBestFit=false;
                    var view=popup.GetComponent<UIView>();view.Get<Text>("Detail Name").alignment=TextAnchor.MiddleLeft;
                    TransparentTankSlots(view);FitInventory(view);if(view.GetComponent<LoadoutArtPresentation>().crew)FitCrewStations(view);
                    var presentation=popup.GetComponent<LoadoutArtPresentation>();if(!presentation.crew)Place(presentation.summary.rectTransform,338,308,202,25);
                    PrefabUtility.SaveAsPrefabAsset(popup,UI+name+".prefab");
                }
                finally{PrefabUtility.UnloadPrefabContents(popup);}
            }
            var root=PrefabUtility.LoadPrefabContents(UI+"MegaTanksCanvas.prefab");
            try{var layout=root.GetComponent<UICanvasLayout>();layout.crewInventoryItem=AssetDatabase.LoadAssetAtPath<UIView>(UI+"CrewRosterItem.prefab");layout.equipmentInventoryItem=AssetDatabase.LoadAssetAtPath<UIView>(UI+"EquipmentRosterItem.prefab");PrefabUtility.SaveAsPrefabAsset(root,UI+"MegaTanksCanvas.prefab");}
            finally{PrefabUtility.UnloadPrefabContents(root);}
            SquareInventoryCards();AssetDatabase.SaveAssets();Debug.Log("LOADOUT_ARTWORK_APPLIED");
        }
        public static void ApplyAndCheck(){Apply();LoadoutArtworkAudit.Run();}
        static void FitCrewStations(UIView view)
        {
            // Feet rest on the roof, middle deck, and lower hull floor rather than portrait cards.
            float[,] positions={{205,158,53,72},{112,248,54,66},{173,79,49,66},{111,156,53,74},{207,246,53,68}};
            for(int i=0;i<5;i++)
            {
                string key="Tank Preview/Tank Slot "+i;var button=view.Get<Button>(key);float w=positions[i,2],h=positions[i,3];
                Place((RectTransform)button.transform,positions[i,0],positions[i,1],w,h);
                var sprite=view.Get<Image>(key+"/Portrait");Place(sprite.rectTransform,1,0,w-2,h-8);sprite.preserveAspect=true;
                Place(view.Get<Text>(key+"/Empty").rectTransform,0,0,w,h-8);
                Place(button.transform.Find("Slot Frame").GetComponent<RectTransform>(),0,0,w,h);
                Place(button.transform.Find("Selection").GetComponent<RectTransform>(),-1,-1,w+2,h+2);
                var plate=button.transform.Find("Role Plate");Place(plate.GetComponent<RectTransform>(),0,h-9,w,9);
                var label=plate.Find("Name").GetComponent<Text>();Place(label.rectTransform,0,0,w,9);label.fontSize=6;
                Place((RectTransform)view.Get<UIStarStrip>(key+"/Stars").transform,5,h-15,w-10,5);
            }
        }
        [MenuItem("MegaTanks/UI/Apply Equipped Crew Sprites")]
        public static void ApplyCrewStationSprites()
        {
            AssetDatabase.Refresh();
            foreach(string name in new[]{"Milo","Pip","Ember","Mochi","Olive"})
            {
                string path=Art+"Equipped/"+name+"-equipped.png";var importer=(TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Multiple;importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.maxTextureSize=512;importer.textureCompression=TextureImporterCompression.Uncompressed;
                // Trim only the imported sprite rectangle; preserve the original generated PNG and alpha.
                var texture=new Texture2D(2,2);texture.LoadImage(File.ReadAllBytes(path));var pixels=texture.GetPixels32();int left=texture.width,right=0,bottom=texture.height,top=0;
                for(int y=0;y<texture.height;y++)for(int x=0;x<texture.width;x++)if(pixels[y*texture.width+x].a>16){left=Math.Min(left,x);right=Math.Max(right,x);bottom=Math.Min(bottom,y);top=Math.Max(top,y);}
                left=Math.Max(0,left-3);bottom=Math.Max(0,bottom-3);right=Math.Min(texture.width-1,right+3);top=Math.Min(texture.height-1,top+3);
                importer.spritesheet=new[]{new SpriteMetaData{name=name+" Equipped",rect=new Rect(left,bottom,right-left+1,top-bottom+1),alignment=(int)SpriteAlignment.BottomCenter,pivot=new Vector2(.5f,0)}};Object.DestroyImmediate(texture);
                var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);settings.spriteMeshType=SpriteMeshType.FullRect;importer.SetTextureSettings(settings);importer.SaveAndReimport();
                var crew=AssetDatabase.LoadAssetAtPath<CrewDefinition>("Assets/MegaTanks/Data/Crew/"+name+".asset");crew.equippedSprite=AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().Single();EditorUtility.SetDirty(crew);
            }
            string prefab=UI+"Crew.prefab";var root=PrefabUtility.LoadPrefabContents(prefab);
            try{FitCrewStations(root.GetComponent<UIView>());PrefabUtility.SaveAsPrefabAsset(root,prefab);}
            finally{PrefabUtility.UnloadPrefabContents(root);}
            AssetDatabase.SaveAssets();Debug.Log("CREW_STATION_SPRITES_APPLIED");
        }
        public static void ApplyCrewStationSpritesAndCheck(){ApplyCrewStationSprites();LoadoutArtworkAudit.Run();}
        static void FitInventory(UIView view)
        {
            var art=view.GetComponent<LoadoutArtPresentation>();var panel=view.transform.Find("Artwork Layout");
            var background=panel.Find("Inventory Panel").GetComponent<Image>();
            var skew=background.GetComponent<CrewPanelSkew>();if(skew!=null)Object.DestroyImmediate(skew);
            Place(background.rectTransform,550,0,220,436);background.sprite=null;background.color=new Color(.25f,.28f,.35f,1);
            Place(panel.Find("Roster Heading").GetComponent<RectTransform>(),558,16,204,27);
            Inventory(view,panel,art.crew);
        }
        static void SquareInventoryCards()
        {
            foreach(bool crew in new[]{true,false})
            {
                string path=UI+(crew?"CrewRosterItem":"EquipmentRosterItem")+".prefab";var root=PrefabUtility.LoadPrefabContents(path);
                try
                {
                    var view=root.GetComponent<UIView>();float height=crew?70:78;
                    Place((RectTransform)root.transform,0,0,64,height);
                    var bg=view.Get<Image>("$root");bg.sprite=null;bg.color=new Color(.13f,.16f,.22f,1);
                    Place(view.Get<Image>("Icon").rectTransform,4,4,56,crew?56:43);
                    var name=view.Get<Text>("Item Name");Place(name.rectTransform,3,47,58,22);name.horizontalOverflow=HorizontalWrapMode.Wrap;name.fontSize=7;
                    Place(view.Get<Text>("Progression Level").rectTransform,4,crew?52:2,40,11);
                    Place((RectTransform)view.Get<UIStarStrip>("Stars").transform,6,crew?63:69,52,6);
                    Place(view.Get<Image>("Equipped Indicator").rectTransform,45,crew?45:30,15,15);
                    var selected=root.transform.Find("Selection").GetComponent<Image>();Place(selected.rectTransform,1,1,62,height-2);selected.sprite=null;selected.color=Color.clear;
                    AddBorder(selected,new Color(.25f,1,.05f,1),1.5f);
                    PrefabUtility.SaveAsPrefabAsset(root,path);
                }
                finally{PrefabUtility.UnloadPrefabContents(root);}
            }
        }
        [MenuItem("MegaTanks/UI/Fit Loadout Inventories")]
        public static void ApplyInventoryLayout()
        {
            MakeFont();SquareInventoryCards();
            foreach(string name in new[]{"Crew","Equipment"})
            {
                string path=UI+name+".prefab";var root=PrefabUtility.LoadPrefabContents(path);
                try{FitInventory(root.GetComponent<UIView>());PrefabUtility.SaveAsPrefabAsset(root,path);}
                finally{PrefabUtility.UnloadPrefabContents(root);}
            }
            AssetDatabase.SaveAssets();Debug.Log("LOADOUT_INVENTORIES_FITTED");
        }
        public static void ApplyInventoryLayoutAndCheck(){ApplyInventoryLayout();LoadoutArtworkAudit.Run();}
        static void AddBorder(Image frame,Color color,float thickness)
        {
            foreach(string edge in new[]{"Top","Bottom","Left","Right"})
            {
                var existing=frame.transform.Find("Border "+edge);
                var border=existing!=null?existing.GetComponent<Image>():Image(frame.transform,"Border "+edge,"",0,0,1,1);
                border.sprite=null;border.color=color;border.raycastTarget=false;
                var rect=border.rectTransform;rect.pivot=new Vector2(.5f,.5f);
                bool horizontal=edge=="Top"||edge=="Bottom";
                rect.anchorMin=horizontal?new Vector2(0,edge=="Top"?1:0):new Vector2(edge=="Left"?0:1,0);
                rect.anchorMax=horizontal?new Vector2(1,edge=="Top"?1:0):new Vector2(edge=="Left"?0:1,1);
                rect.sizeDelta=horizontal?new Vector2(0,thickness):new Vector2(thickness,0);rect.anchoredPosition=Vector2.zero;
            }
        }
        static void TransparentTankSlots(UIView view)
        {
            var art=view.GetComponent<LoadoutArtPresentation>();
            string[] keys=art.crew?Enumerable.Range(0,5).Select(i=>"Tank Preview/Tank Slot "+i).ToArray():art.slotKeys;
            foreach(string key in keys)
            {
                var button=view.Get<Button>(key);
                // The invisible button retains the full clickable slot rectangle.
                button.image.color=Color.clear;button.image.raycastTarget=true;
                var frame=button.transform.Find("Slot Frame").GetComponent<Image>();frame.color=Color.clear;
                // Keep the tank visible through the slot, with a neutral rim beneath selection highlights.
                foreach(string edge in new[]{"Top","Bottom","Left","Right"})
                {
                    var existing=frame.transform.Find("Border "+edge);
                    var border=existing!=null?existing.GetComponent<Image>():Image(frame.transform,"Border "+edge,"",0,0,1,1);
                    border.sprite=null;border.color=new Color(.78f,.84f,.82f,.95f);border.raycastTarget=false;
                    var rect=border.rectTransform;rect.pivot=new Vector2(.5f,.5f);
                    bool horizontal=edge=="Top"||edge=="Bottom";
                    rect.anchorMin=horizontal?new Vector2(0,edge=="Top"?1:0):new Vector2(edge=="Left"?0:1,0);
                    rect.anchorMax=horizontal?new Vector2(1,edge=="Top"?1:0):new Vector2(edge=="Left"?0:1,1);
                    rect.sizeDelta=horizontal?new Vector2(0,1):new Vector2(1,0);rect.anchoredPosition=Vector2.zero;
                }
            }
        }
        [MenuItem("MegaTanks/UI/Make Tank Slot Backgrounds Transparent")]
        public static void ApplyTankSlotTransparency()
        {
            foreach(string name in new[]{"Crew","Equipment"})
            {
                string path=UI+name+".prefab";var root=PrefabUtility.LoadPrefabContents(path);
                try{TransparentTankSlots(root.GetComponent<UIView>());PrefabUtility.SaveAsPrefabAsset(root,path);}
                finally{PrefabUtility.UnloadPrefabContents(root);}
            }
            AssetDatabase.SaveAssets();Debug.Log("TANK_SLOT_BACKGROUNDS_TRANSPARENT");
        }
        public static void ApplyTankSlotTransparencyAndCheck(){ApplyTankSlotTransparency();LoadoutArtworkAudit.Run();}
    }
}
