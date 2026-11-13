using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CatTanks.Editor
{
    [InitializeOnLoad]
    public static class StudioUIPrefabBuilder
    {
        const string Root="Assets/CatTanks/Resources/UI";
        static readonly BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        static bool running;
        static StudioUIPrefabBuilder()
        {
            EditorApplication.playModeStateChanged+=state=>
            {
                if(state==PlayModeStateChange.EnteredEditMode&&SessionState.GetBool("CatTanks.InstallCanvas",false))
                {SessionState.SetBool("CatTanks.InstallCanvas",false);EditorApplication.delayCall+=InstallInScene;}
            };
        }
        [MenuItem("CatTanks/UI/Build Canvas and Popup Prefabs (Play Mode)")]
        public static async void Build()
        {
            if(running)return;
            if(!EditorApplication.isPlaying){Debug.LogError("Enter Play Mode to capture the authored UI templates.");return;}
            running=true;var game=UnityEngine.Object.FindObjectOfType<CatTanksGame>();var saved=game.profile;string savePath=Profile.TestSavePath;
            try
            {
                Directory.CreateDirectory(Root+"/Screens");Directory.CreateDirectory(Root+"/Popups");Directory.CreateDirectory(Root+"/Components");
                AssetDatabase.Refresh();Profile.TestSavePath=Path.GetFullPath("Logs/ui-prefab-authoring-profile.json");
                game.BeginUiAuthoring();
                for(int phase=0;phase<2;phase++)
                {
                    game.profile=Profile.New(game.db);
                    if(phase==1)
                    {
                        game.profile.accountLevel=100;game.profile.highestCleared=20;
                        foreach(var def in game.db.cats)if(!game.profile.cats.Any(c=>c.id==def.id))game.profile.cats.Add(new OwnedCat{id=def.id});
                        foreach(var def in game.db.monsters)game.profile.GainPet(def.asset,game.db);
                        foreach(var id in new[]{"gun-0","gear-helm","gear-chest","gear-boots"})game.profile.gear.Add(new Gear{id=id});
                        game.profile.artifacts.Add(new Artifact{id="preview-artifact",name="Di vật ánh trăng"});
                        game.profile.Add("coins",100000);game.profile.Add("catXp",10000);game.profile.Add("skillCore",30);
                    }
                    string[] pages={"march","barracks","barracks","barracks","barracks","barracks","barracks","barracks","barracks","town","town","town","chest","quests"};
                    string[] tabs={"cats","cats","fortress","squad","skills","monsters","gear","presets","nexus","summon","shop","arena","cats","cats"};
                    for(int i=0;i<pages.Length;i++){game.page=pages[i];game.tab=tabs[i];game.modal="";await Show(game);}
                    game.modal="settings";await Show(game);game.modal="prebattle";await Show(game);
                    game.modal="book";
                    for(int i=0;i<5;i++){game.bookTab=i;await Show(game);}
                    typeof(CatTanksGame).GetMethod("Pick",Private).Invoke(game,new object[]{"Chọn đồng đội",game.db.cats.Select(c=>c.name).Concat(new[]{"Để trống vị trí"}),new Action<int>(i=>{})});await Show(game);
                }
                game.modal="";game.StartBattle();game.paused=true;await Show(game);
                game.battle.state=BattleState.Won;typeof(CatTanksGame).GetMethod("Resolve",Private).Invoke(game,null);await Show(game);
                typeof(CatTanksGame).GetMethod("Hub",Private).Invoke(game,null);game.StartBattle(true);game.paused=true;await Show(game);
                typeof(CatTanksGame).GetMethod("Hub",Private).Invoke(game,null);
                typeof(CatTanksGame).GetMethod("Notify",Private).Invoke(game,new object[]{"Đã lưu thay đổi"});await Show(game);
                game.profile=Profile.New(game.db);game.page="march";game.tab="cats";game.modal="";await Show(game);
                Export(game.CanvasRoot);
                SessionState.SetBool("CatTanks.InstallCanvas",true);
                Debug.Log("CANVAS_PREFABS_EXPORTED: screens, individual popups and component prefabs");
            }
            catch(Exception e){Debug.LogError("UI_PREFAB_BUILD_FAILED: "+e);}
            finally
            {
                if(game.inBattle)typeof(CatTanksGame).GetMethod("Hub",Private).Invoke(game,null);
                game.profile=saved;Profile.TestSavePath=savePath;running=false;
                if(SessionState.GetBool("CatTanks.InstallCanvas",false))EditorApplication.isPlaying=false;
            }
        }
        static async Task Show(CatTanksGame game){game.RefreshCanvas();Canvas.ForceUpdateCanvases();await Task.Delay(35);}
        static void Export(StudioUIRoot source)
        {
            var componentPaths=new List<StudioUIRoot.ComponentPrefab>();
            foreach(var group in source.GetComponentsInChildren<StudioWidgetBinding>(true).GroupBy(b=>b.kind))
            {
                var clone=UnityEngine.Object.Instantiate(group.First().gameObject);clone.name=char.ToUpperInvariant(group.Key[0])+group.Key.Substring(1)+"Widget";
                clone.SetActive(true);clone.GetComponent<StudioWidgetBinding>().authored=false;
                string path=Root+"/Components/"+clone.name+".prefab";
                var prefab=PrefabUtility.SaveAsPrefabAsset(clone,path);UnityEngine.Object.DestroyImmediate(clone);
                componentPaths.Add(new StudioUIRoot.ComponentPrefab{kind=group.Key,prefab=prefab});
            }
            var paths=new List<(string key,string layer,string path)>();
            foreach(var view in source.GetComponentsInChildren<StudioUIView>(true))
            {
                var clone=UnityEngine.Object.Instantiate(view.gameObject);clone.SetActive(true);
                foreach(var binding in clone.GetComponentsInChildren<StudioWidgetBinding>(true))binding.authored=true;
                string folder=view.layer=="popup"?"Popups":view.layer=="notification"?"Components":"Screens";
                string name=(view.layer=="popup"?"Popup_":view.layer=="notification"?"Notification_":"Screen_")+view.key.Replace('-','_');
                clone.name=name;string path=Root+"/"+folder+"/"+name+".prefab";PrefabUtility.SaveAsPrefabAsset(clone,path);UnityEngine.Object.DestroyImmediate(clone);paths.Add((view.key,view.layer,path));
            }
            var root=new GameObject("CatTanks Studio UI",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster),typeof(StudioUIRoot));
            var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=100;
            root.GetComponent<CanvasScaler>().uiScaleMode=CanvasScaler.ScaleMode.ConstantPixelSize;
            var config=root.GetComponent<StudioUIRoot>();config.viewport=Rect("Safe Area • 1600 × 900",root.transform);
            config.viewport.anchorMin=config.viewport.anchorMax=config.viewport.pivot=new Vector2(.5f,.5f);config.viewport.sizeDelta=new Vector2(1600,900);
            config.screens=Rect("Screens",config.viewport);config.popups=Rect("Popups",config.viewport);config.notifications=Rect("Notifications",config.viewport);
            foreach(var item in paths)
            {
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(item.path);var instance=(GameObject)PrefabUtility.InstantiatePrefab(prefab,item.layer=="popup"?config.popups:item.layer=="notification"?config.notifications:config.screens);
                instance.SetActive(item.key=="march");
            }
            config.components=componentPaths.ToArray();PrefabUtility.SaveAsPrefabAsset(root,Root+"/CatTanksUI.prefab");UnityEngine.Object.DestroyImmediate(root);AssetDatabase.SaveAssets();
        }
        static RectTransform Rect(string name,Transform parent)
        {
            var rt=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();rt.SetParent(parent,false);rt.anchorMin=rt.anchorMax=rt.pivot=new Vector2(0,1);rt.sizeDelta=new Vector2(1600,900);return rt;
        }
        [MenuItem("CatTanks/UI/Install Canvas Prefab in Scene")]
        public static void InstallInScene()
        {
            if(EditorApplication.isPlaying)return;
            var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if(scene.path!="Assets/CatTanks/Scenes/Main.unity"){Debug.LogError("Open the CatTanks Main scene before installing the Canvas.");return;}
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/CatTanksUI.prefab");if(!prefab){Debug.LogError("Build UI prefabs first.");return;}
            foreach(var existing in UnityEngine.Object.FindObjectsOfType<StudioUIRoot>())Undo.DestroyObjectImmediate(existing.gameObject);
            var instance=(GameObject)PrefabUtility.InstantiatePrefab(prefab,scene);Undo.RegisterCreatedObjectUndo(instance,"Install CatTanks Canvas");
            if(!UnityEngine.Object.FindObjectOfType<EventSystem>()){var events=new GameObject("UI Event System",typeof(EventSystem),typeof(StandaloneInputModule));Undo.RegisterCreatedObjectUndo(events,"Create UI Event System");}
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);Selection.activeGameObject=instance;
            Validate();Debug.Log("CANVAS_INSTALLED_IN_MAIN_SCENE");
        }
        [MenuItem("CatTanks/UI/Validate Canvas Prefabs and 9-Slice")]
        public static void Validate()
        {
            var root=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/CatTanksUI.prefab");
            var issues=new List<string>();int sliced=0;
            if(!root)throw new Exception("Missing Canvas prefab");
            var views=root.GetComponentsInChildren<StudioUIView>(true);
            foreach(var view in views)
            {
                if(!PrefabUtility.IsPartOfPrefabInstance(view))issues.Add("View is not a nested prefab: "+view.key);
                foreach(var image in view.GetComponentsInChildren<Image>(true))
                {
                    if(!image.sprite)continue;
                    if(image.sprite.border.sqrMagnitude>0&&!image.preserveAspect)
                    {
                        sliced++;if(image.type!=Image.Type.Sliced)issues.Add("Not sliced: "+view.key+"/"+image.name);
                        var b=image.sprite.border;var r=image.sprite.rect;
                        if(r.width-b.x-b.z<1||r.height-b.y-b.w<1)issues.Add("Empty sprite center: "+image.sprite.name);
                    }
                    if(!EditorUtility.IsPersistent(image.sprite))issues.Add("Transient sprite: "+image.name);
                }
                foreach(var text in view.GetComponentsInChildren<Text>(true))if(!text.font)issues.Add("Missing font: "+text.name);
            }
            int popups=views.Count(v=>v.layer=="popup");if(popups!=6)issues.Add("Expected 6 individual popup prefabs, got "+popups);
            int meshChecks=0;
            foreach(var sample in root.GetComponentsInChildren<Image>(true).Where(i=>i.type==Image.Type.Sliced&&i.sprite).GroupBy(i=>i.sprite).Select(g=>g.First()))
            {
                if(!StableCorners(sample))issues.Add("9-slice corners change on resize: "+sample.sprite.name);else meshChecks++;
            }
            File.WriteAllText("Logs/ui-prefab-validation.json",JsonConvert.SerializeObject(new{verdict=issues.Count==0?"PASS":"FAIL",views=views.Length,popups,slicedImages=sliced,stableCornerMeshChecks=meshChecks,issues},Formatting.Indented));
            if(issues.Count>0)throw new Exception(string.Join("\n",issues));
            Debug.Log("UI_PREFABS_VALIDATED: "+views.Length+" views, "+popups+" popups, "+sliced+" sliced images");
        }
        static bool StableCorners(Image source)
        {
            var go=new GameObject("9-slice mesh check",typeof(RectTransform),typeof(Image));go.hideFlags=HideFlags.HideAndDontSave;
            var image=go.GetComponent<Image>();image.sprite=source.sprite;image.type=Image.Type.Sliced;image.pixelsPerUnitMultiplier=source.pixelsPerUnitMultiplier;
            var method=typeof(Image).GetMethod("OnPopulateMesh",BindingFlags.Instance|BindingFlags.NonPublic,null,new[]{typeof(VertexHelper)},null);
            var mesh=new Mesh();float[] previous=null;bool stable=true;
            try
            {
                foreach(float width in new[]{180f,420f})
                {
                    ((RectTransform)go.transform).sizeDelta=new Vector2(width,80);
                    using(var helper=new VertexHelper()){method.Invoke(image,new object[]{helper});helper.FillMesh(mesh);}
                    var xs=mesh.vertices.Select(v=>v.x).Distinct().OrderBy(x=>x).ToArray();
                    var ys=mesh.vertices.Select(v=>v.y).Distinct().OrderBy(y=>y).ToArray();
                    if(xs.Length!=4||ys.Length!=4){stable=false;break;}
                    var corners=new[]{xs[1]-xs[0],xs[3]-xs[2],ys[1]-ys[0],ys[3]-ys[2]};
                    if(previous!=null&&corners.Where((v,i)=>Mathf.Abs(v-previous[i])>.01f).Any())stable=false;
                    previous=corners;
                }
            }
            finally{UnityEngine.Object.DestroyImmediate(mesh);UnityEngine.Object.DestroyImmediate(go);}
            return stable;
        }
    }
}
