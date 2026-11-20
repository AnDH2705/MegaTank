using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CatTanks.MockupKit;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace CatTanks.Editor
{
    public static class MockupGameUIBuilder
    {
        public const string Root="Assets/CatTanks/Resources/UI/Mockup";
        public const string CanvasPath=Root+"/CatTanksUI.prefab";
        static readonly Dictionary<string,string> Mapping=new Dictionary<string,string>
        {
            {"panel","panel_main"},{"card","panel_card"},{"button","button_blue"},{"primary","button_gold"},
            {"selected","button_cyan"},{"input","input_field"},{"bar","bar_track"},{"fill","bar_fill_cyan"},
            {"Map","icon_map"},{"Battle","icon_battle"},{"Bag_Brown","icon_town"},{"Chest_Gold","icon_chest"},
            {"Calendar_Check","icon_quests"},{"Book_1_Purple","icon_book"},{"Money_Coin","icon_coin"},
            {"Gem_Diamond_Purple","icon_gem"},{"Ticket_Gold","icon_ticket"},{"Gear","icon_settings"},
            {"Badge_Crown","icon_crown"},{"Skull","icon_skull"},{"Chest_Premium","icon_chest"},
            {"Damage","icon_attack"},{"Gear_Sword","skill_repair"},{"Gem_Star_Blue","icon_skill_core"},
            {"Gem_Circle","icon_dust"},{"Lock","icon_lock"},{"Check","icon_check"},{"Arrow","icon_arrow_right"},
            {"Pause","icon_pause"},{"Play","icon_play"},{"Setting","icon_settings"}
        };
        [MenuItem("CatTanks/Mockup Kit/7 - Build and Install Live Game UI")]
        public static void BuildAndInstall()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play Mode before installing live UI.");
            var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if(scene.path!="Assets/CatTanks/Scenes/Main.unity")throw new InvalidOperationException("Open Main.unity before installing live UI.");
            var library=AssetDatabase.LoadAssetAtPath<MockupKitLibrary>("Assets/CatTanksMockupKit/Generated/MockupKitLibrary.asset");
            if(!library)throw new InvalidOperationException("Import the Mockup Kit first.");
            var legacyTheme=Resources.Load<StudioUITheme>("StudioUITheme");
            var template=AssetDatabase.LoadAssetAtPath<StudioUIRoot>("Assets/CatTanks/Resources/UI/CatTanksUI.prefab");
            Directory.CreateDirectory(Root+"/Screens");Directory.CreateDirectory(Root+"/Popups");Directory.CreateDirectory(Root+"/Components");AssetDatabase.Refresh();
            string themePath=Root+"/MockupGameTheme.asset";
            var theme=AssetDatabase.LoadAssetAtPath<StudioUITheme>(themePath);
            if(!theme){theme=ScriptableObject.CreateInstance<StudioUITheme>();AssetDatabase.CreateAsset(theme,themePath);}
            theme.assets=library.entries.Select(e=>new StudioUITheme.Asset{key=e.key,sprite=e.sprite}).ToList();
            foreach(var pair in Mapping)theme.assets.Add(new StudioUITheme.Asset{key=pair.Key,sprite=library.Get(pair.Value)});
            EditorUtility.SetDirty(theme);AssetDatabase.SaveAssets();
            var aliases=legacyTheme.assets.Where(a=>a.sprite).GroupBy(a=>a.sprite).ToDictionary(g=>g.Key,g=>g.First().key);
            var canvas=UnityEngine.Object.Instantiate(template.gameObject);canvas.name="CatTanks Mockup Game UI";
            var root=canvas.GetComponent<StudioUIRoot>();root.theme=theme;root.mockupKit=true;
            try
            {
                var components=new List<StudioUIRoot.ComponentPrefab>();
                foreach(var entry in template.components)
                {
                    var clone=UnityEngine.Object.Instantiate(entry.prefab);clone.name=entry.prefab.name;
                    try
                    {
                        Skin(clone,theme,aliases,null);
                        var binding=clone.GetComponent<StudioWidgetBinding>();binding.authored=false;binding.spriteKey="";binding.id="";
                        var scroll=clone.GetComponent<ScrollRect>();
                        if(scroll&&scroll.content)foreach(Transform child in scroll.content.Cast<Transform>().ToArray())UnityEngine.Object.DestroyImmediate(child.gameObject);
                        components.Add(new StudioUIRoot.ComponentPrefab{kind=entry.kind,prefab=PrefabUtility.SaveAsPrefabAsset(clone,Root+"/Components/"+clone.name+".prefab")});
                    }
                    finally{UnityEngine.Object.DestroyImmediate(clone);}
                }
                root.components=components.ToArray();
                foreach(var view in root.GetComponentsInChildren<StudioUIView>(true).ToArray())
                {
                    var clone=UnityEngine.Object.Instantiate(view.gameObject);clone.name=(view.layer=="popup"?"Popup_":"Screen_")+view.key.Replace('-','_');
                    try
                    {
                        Skin(clone,theme,aliases,view);
                        string folder=view.layer=="popup"?"Popups":view.layer=="screen"?"Screens":"Components";
                        var prefab=PrefabUtility.SaveAsPrefabAsset(clone,Root+"/"+folder+"/"+clone.name+".prefab");
                        var parent=view.transform.parent;bool active=view.gameObject.activeSelf;
                        UnityEngine.Object.DestroyImmediate(view.gameObject);
                        var instance=(GameObject)PrefabUtility.InstantiatePrefab(prefab,parent);instance.SetActive(active);
                    }
                    finally{UnityEngine.Object.DestroyImmediate(clone);}
                }
                PrefabUtility.SaveAsPrefabAsset(canvas,CanvasPath);
            }
            finally{UnityEngine.Object.DestroyImmediate(canvas);}
            foreach(var old in UnityEngine.Object.FindObjectsOfType<StudioUIRoot>(true))
                if(old.gameObject.scene==scene)UnityEngine.Object.DestroyImmediate(old.gameObject);
            PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(CanvasPath),scene);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();Validate();
            Debug.Log("MOCKUP_GAME_UI_INSTALLED: existing gameplay controllers, individual popup prefabs, no profile migration.");
        }
        static void Skin(GameObject go,StudioUITheme theme,Dictionary<Sprite,string> aliases,StudioUIView view)
        {
            foreach(var image in go.GetComponentsInChildren<Image>(true))
            {
                if(image.sprite&&aliases.TryGetValue(image.sprite,out var key))
                {
                    var binding=image.GetComponent<StudioWidgetBinding>();
                    var rect=(RectTransform)image.transform;
                    if(binding&&(key=="panel"||key=="card"))
                    {
                        key=rect.sizeDelta.x>=1100&&rect.sizeDelta.y>=700?"panel_main":rect.sizeDelta.y<115?"panel_resource":"panel_card";
                        if(view&&view.layer=="popup"&&rect.sizeDelta.x>=600&&rect.sizeDelta.y>=300)key="panel_modal";
                        if(binding.id=="image::-500:-500"){key="bg_hub";Place(rect,0,0,1600,900);}
                        binding.spriteKey=key;
                    }
                    StudioNineSlice.Apply(image,theme.Get(key),key=="bar"||key=="fill"?5:14,image.preserveAspect);image.color=Color.white;
                }
            }
            foreach(var text in go.GetComponentsInChildren<Text>(true))
            {
                text.color=text.fontSize>=30?Catalog.Color("#ffcc32"):Color.white;
                var button=text.GetComponentInParent<Button>();
                if(button&&button.image.sprite==theme.Get("primary"))text.color=Catalog.Color("#102340");
                var input=text.GetComponentInParent<InputField>();
                if(input&&input.textComponent==text){text.color=Catalog.Color("#102340");text.alignment=TextAnchor.MiddleLeft;}
            }
            foreach(var button in go.GetComponentsInChildren<Button>(true))
            {
                var layout=button.GetComponent<StudioButtonLayout>();if(!layout)layout=button.gameObject.AddComponent<StudioButtonLayout>();
                layout.label=button.GetComponentsInChildren<Text>(true).FirstOrDefault(t=>t.name=="Label")??button.GetComponentInChildren<Text>(true);
                layout.fontSize=layout.label?Mathf.Clamp(layout.label.fontSize,16,19):19;
            }
            if(view&&view.key=="battle")
            {
                var bindings=go.GetComponentsInChildren<StudioWidgetBinding>(true).ToDictionary(b=>b.id);
                for(int i=0;i<6;i++)
                {
                    int x=330+i*145;StudioWidgetBinding button;
                    if(!bindings.TryGetValue("button::"+x+":742",out button))continue;
                    var layout=button.GetComponent<StudioButtonLayout>();layout.content=StudioButtonLayout.Content.Skill;
                    layout.icon=bindings["image::"+(x+48)+":750"].GetComponent<Image>();
                    layout.hotkey=bindings["text::"+(x+10)+":750"].GetComponent<Text>();
                    layout.caption=bindings["text::"+(x+10)+":789"].GetComponent<Text>();
                    layout.cost=bindings["text::"+(x+10)+":828"].GetComponent<Text>();
                    layout.Apply();
                }
            }
            foreach(var layout in go.GetComponentsInChildren<StudioButtonLayout>(true))layout.Apply();
            if(!view||view.layer!="screen"||view.key=="battle")return;
            foreach(var b in go.GetComponentsInChildren<StudioWidgetBinding>(true))
            {
                var rect=(RectTransform)b.transform;
                switch(b.id)
                {
                    case "image::254:125": Place(rect,264,108,1324,780);break;
                    case "image::24:125": Place(rect,12,108,236,780);break;
                    case "image::30:23": Place(rect,20,14,66,66);break;
                    case "text::110:22": Place(rect,100,8,670,48);break;
                    case "text::112:68": Place(rect,102,58,650,24);break;
                    case "button::1432:27": Place(rect,1404,16,180,68);break;
                    case "text::44:150": Place(rect,32,132,205,30);break;
                    case "text::44:181": Place(rect,125,180,108,65);break;
                    case "image::38:748": Place(rect,24,760,212,108);break;
                    case "text::54:762": Place(rect,40,776,180,30);break;
                    case "text::54:791": Place(rect,40,812,180,44);break;
                }
                for(int i=0;i<3;i++)
                {
                    float x=770+i*213;
                    if(b.id=="image::"+x+":22")Place(rect,800+i*200,16,180,68);
                    if(b.id=="image::"+(x+10)+":37")Place(rect,812+i*200,28,44,44);
                    if(b.id=="text::"+(x+64)+":30"){Place(rect,862+i*200,22,110,50);b.GetComponent<Text>().alignment=TextAnchor.MiddleLeft;}
                    if(b.id=="text::"+(x+64)+":63")b.GetComponent<Text>().color=Color.clear;
                }
                for(int i=0;i<6;i++)
                {
                    if(b.id=="button::38:"+(258+i*76))Place(rect,28,285+i*76,212,62);
                    if(b.id=="image::48:"+(270+i*76))Place(rect,32,293+i*76,42,42);
                }
            }
            var widgets=go.GetComponentsInChildren<StudioWidgetBinding>(true).ToDictionary(b=>b.id);
            for(int i=0;i<6;i++)
            {
                var button=widgets["button::38:"+(258+i*76)];var layout=button.GetComponent<StudioButtonLayout>();
                layout.content=StudioButtonLayout.Content.Navigation;layout.fontSize=18;
                layout.icon=widgets["image::48:"+(270+i*76)].GetComponent<Image>();layout.Apply();
            }
            var portrait=new GameObject("Mockup Commander Portrait",typeof(RectTransform),typeof(Image));portrait.transform.SetParent(go.transform,false);
            Place((RectTransform)portrait.transform,28,164,92,92);var art=portrait.GetComponent<Image>();art.sprite=theme.Get("mascot_commander");art.preserveAspect=true;art.raycastTarget=false;
            if(view.key=="barracks-gear")
            {
                var workshop=new GameObject("Mockup Workshop",typeof(RectTransform),typeof(Image));workshop.transform.SetParent(go.transform,false);
                Place((RectTransform)workshop.transform,300,650,720,130);var image=workshop.GetComponent<Image>();image.sprite=theme.Get("bg_workshop");image.raycastTarget=false;
            }
        }
        static void Place(RectTransform rect,float x,float y,float w,float h)
        {
            rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);rect.anchoredPosition=new Vector2(x,-y);rect.sizeDelta=new Vector2(w,h);
        }
        public static void Validate()
        {
            var root=AssetDatabase.LoadAssetAtPath<StudioUIRoot>(CanvasPath);var issues=new List<string>();
            if(!root||!root.theme||!root.mockupKit)throw new Exception("Missing live Mockup UI or theme");
            var views=root.GetComponentsInChildren<StudioUIView>(true);var popups=views.Where(v=>v.layer=="popup").ToArray();
            if(popups.Length!=6)issues.Add("Expected six independent popup prefabs");
            foreach(var view in views)if(!PrefabUtility.IsPartOfPrefabInstance(view))issues.Add("Not a nested prefab: "+view.key);
            if(root.GetComponentsInChildren<PreviewIsolation>(true).Length>0||root.GetComponentsInChildren<MockupKitPreview>(true).Length>0||root.GetComponentsInChildren<MockupKitAction>(true).Length>0)issues.Add("Preview code leaked into live UI");
            foreach(var image in root.GetComponentsInChildren<Image>(true))
            {
                if(image.sprite&&AssetDatabase.GetAssetPath(image.sprite).Contains("Layer Lab"))issues.Add("Legacy skin: "+image.name);
                if(image.sprite&&image.sprite.border.sqrMagnitude>0&&!image.preserveAspect&&image.type!=Image.Type.Sliced)issues.Add("Missing slicing: "+image.name);
            }
            int meshChecks=0;
            var meshCheck=typeof(StudioUIPrefabBuilder).GetMethod("StableCorners",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static);
            foreach(var image in root.GetComponentsInChildren<Image>(true).Where(i=>i.type==Image.Type.Sliced&&i.sprite).GroupBy(i=>i.sprite).Select(g=>g.First()))
            {
                if(!(bool)meshCheck.Invoke(null,new object[]{image}))issues.Add("Corners change when resized: "+image.sprite.name);else meshChecks++;
            }
            File.WriteAllText("Logs/mockup-game-prefabs.json",JsonConvert.SerializeObject(new{verdict=issues.Count==0?"PASS":"FAIL",views=views.Length,popups=popups.Select(v=>v.key),stableCornerMeshChecks=meshChecks,issues},Formatting.Indented));
            if(issues.Count>0)throw new Exception(string.Join("\n",issues));
        }
    }
}
