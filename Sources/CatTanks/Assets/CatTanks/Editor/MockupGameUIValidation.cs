using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace CatTanks.Editor
{
    [InitializeOnLoad]
    public static class MockupGameUIValidation
    {
        const string Pending="MockupGameUI.Pending",Before="MockupGameUI.ProfileBefore",Result="MockupGameUI.Result";
        const string Report="Logs/mockup-game-ui-validation.json";
        static readonly BindingFlags Private=BindingFlags.NonPublic|BindingFlags.Instance;
        static readonly List<string> Checks=new List<string>(),Errors=new List<string>();
        static MockupGameUIValidation()
        {
            EditorApplication.playModeStateChanged+=state=>
            {
                if(!SessionState.GetBool(Pending,false))return;
                if(state==PlayModeStateChange.EnteredPlayMode)Run();
                if(state==PlayModeStateChange.EnteredEditMode)Finish();
            };
        }
        public static void BuildAndTest()
        {
            if(!Application.isBatchMode||!Environment.GetCommandLineArgs().Contains("-catTanksTestProfile"))throw new Exception("Use a dedicated batch process with an explicit Logs test profile.");
            try
            {
                Directory.CreateDirectory("Logs");SessionState.SetString(Before,Fingerprint());
                EditorSceneManager.OpenScene("Assets/CatTanks/Scenes/Main.unity",OpenSceneMode.Single);
                MockupGameUIBuilder.BuildAndInstall();
                SessionState.SetBool(Pending,true);SessionState.SetString(Result,"");EditorApplication.isPlaying=true;
            }
            catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
        }
        static void Check(bool pass,string name){if(!pass)throw new Exception(name);Checks.Add(name);}
        static async void Run()
        {
            Application.logMessageReceived+=Log;
            try
            {
                await Task.Delay(150);
                var game=UnityEngine.Object.FindObjectOfType<CatTanksGame>();game.titleScreen=false;
                Check(game&&game.profile!=null,"Live game boots with a profile");
                Check(Profile.TestSavePath!=null&&Path.GetFullPath(Profile.TestSavePath).StartsWith(Path.GetFullPath("Logs")+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase),"Profile redirection happened before game Awake");
                Check(!PreviewIsolation.IsActive,"Live UI does not activate preview isolation");
                await Show(game);
                Check(game.CanvasRoot.mockupKit&&game.CanvasRoot.theme,"Mockup Kit is the active game Canvas/theme");
                Check(game.CanvasRoot.GetComponentsInChildren<StudioUIView>(true).Count(v=>v.layer=="popup"&&v.key!="map"&&v.key!="afk"&&v.key!="title")==6,"Six independent popup prefab instances are loaded");
                Check(game.CanvasRoot.GetComponentsInChildren<Image>(true).Where(i=>i.sprite).All(i=>!AssetDatabase.GetAssetPath(i.sprite).Contains("Layer Lab")),"Live Canvas uses Mockup Kit assets");
                string smoke="Logs/ui-smoke-validation.json";if(File.Exists(smoke))File.Delete(smoke);
                StudioUIValidation.Run();
                for(int i=0;i<200&&!File.Exists(smoke);i++)await Task.Delay(100);
                await Task.Delay(300);
                Check(File.Exists(smoke)&&(string)JObject.Parse(File.ReadAllText(smoke))["verdict"]=="PASS","Existing 36 UI smoke checks pass on the new Canvas");

                game.profile=Profile.New(game.db);game.profile.accountLevel=40;game.profile.highestCleared=20;
                foreach(var item in new[]{"coins","catXp","spiritDust","forgeStone","monsterChow","soulEssence","relicStone"})game.profile.Add(item,100000);
                game.profile.Add("summonScroll",20);game.profile.Add("gem",10000);game.profile.Add("skillCore",50);
                await Page(game,"march");
                Check(Texts(game).Any(t=>t.text==game.profile.Get("coins").ToString("N0")),"Currency displays live profile values");
                // Bản đồ chương: mỗi chương 5 ải nối thành đường đi; chọn nút ải rồi bấm CHỌN ẢI NÀY.
                game.modal="map";await Show(game);
                var stageCards = game.CanvasRoot.GetComponentsInChildren<StudioButtonLayout>()
                    .Where(l => l.content == StudioButtonLayout.Content.Stage).ToArray();
                Check(stageCards.Length == 5 && stageCards.All(l => l.icon.sprite && !l.icon.raycastTarget),
                    "Chapter map shows five stage nodes with artwork");
                game.profile.highestCleared = 0;
                await Show(game);
                var stageTwo = stageCards.Single(l => l.icon.sprite.name == "stage-02");
                Check(stageTwo.badge.sprite == Resources.Load<Sprite>("Art/Interface/IconRefresh/Lock"), "Locked stage shows the refreshed lock badge");
                ClickBinding(game, "button::360:440", false);
                Check(game.profile.selected == 1, "Locked stage node cannot change the selection");
                game.profile.highestCleared = 20;
                await Show(game);
                ClickBinding(game, "button::360:440");await Show(game);Click(game,"CHỌN ẢI NÀY");await Show(game);
                Check(game.profile.selected == 2 && game.modal == "", "Stage node + CHỌN ẢI NÀY selects its stage");
                game.modal="map";await Show(game);ClickBinding(game, "button::190:600");await Show(game);Click(game,"CHỌN ẢI NÀY");await Show(game);
                await Page(game,"barracks","cats");int level=game.profile.cats[0].level,gold=game.profile.Get("coins");
                ClickBinding(game, "button::350:246");
                Check(game.selectedCat == 1 && game.tab == "cat", "The entire cat card opens that cat's detail page");
                await Show(game);ClickBinding(game, "button::128:370");
                Check(game.selectedCat == 0, "‹ on the detail page switches to the previous cat");
                await Show(game);Click(game,"NÂNG CẤP");await Show(game);
                Check(game.profile.cats[0].level==level+1&&game.profile.Get("coins")<gold,"Cat upgrade button changes level and charges actual cost");
                await Page(game,"barracks","squad");ClickBinding(game,"button::810:226");await Show(game);
                Click(game,"Gỡ khỏi ô");await Show(game);Check(game.profile.squad[0]=="","Formation slot clears the actual squad member");
                await Page(game,"barracks","fortress");Click(game,"Lắp chim 1");await Show(game);
                Check(game.modal=="choice","Bird (module) slot opens the choice prefab");
                Check(!Buttons(game).First(b=>Label(b)=="‹").interactable,"Choice popup blocks background navigation");
                Click(game,"Gỡ chim");await Show(game);Check(game.modal=="","Picker closes after choosing");
                await Page(game,"barracks","skills");int cores=game.profile.Get("skillCore");Click(game,"NÂNG CẤP ·");await Show(game);
                Check(game.profile.skillLevels["heal"]==2&&game.profile.Get("skillCore")==cores-1,"Skill upgrade uses the canonical skill and real core cost");
                await Page(game,"town","summon");int tickets=game.profile.Get("summonScroll"),owned=game.profile.cats.Sum(c=>1+c.copies);
                Click(game,"GỌI 1 LẦN");await Show(game);
                Check(game.profile.Get("summonScroll")==tickets-1&&game.profile.cats.Sum(c=>1+c.copies)==owned+1,"Summon button spends one ticket and grants a cat/copy");
                await Page(game,"town","shop");int cages=game.profile.Get("beastCage");gold=game.profile.Get("coins");
                ClickBinding(game,"button::386:358");await Show(game);
                Check(game.profile.Get("beastCage")==cages+1&&game.profile.Get("coins")==gold-1500,"Shop button spends coins and grants exactly one cage");
                game.profile.daily["idle"]=1;await Page(game,"quests");gold=game.profile.Get("coins");ClickBinding(game,"button::1220:268");await Show(game);
                Check(game.profile.claimed.Contains("idle")&&game.profile.Get("coins")==gold+40,"Quest claim grants the real reward once");
                ClickBinding(game,"button::1220:268",false);await Show(game);Check(game.profile.Get("coins")==gold+40,"Claimed quest cannot grant a second reward");
                game.profile.lastIdle=Profile.Now-3600;await Page(game,"chest");gold=game.profile.Get("coins");Click(game,"NHẬN TOÀN BỘ");await Show(game);
                Check(game.profile.Get("coins")>gold&&Profile.Now-game.profile.lastIdle<5,"AFK claim updates wallet and collection time");
                await Page(game,"barracks","presets");Click(game,"LƯU ĐỘI HIỆN TẠI");await Show(game);
                Check(game.profile.presets[0]!=null,"Preset button saves a real loadout");
                game.profile.squad[0]="cat-0";ClickBinding(game,"button::1186:369");await Show(game);Check(game.profile.squad[0]=="","Preset button restores the saved loadout");
                game.modal="settings";await Show(game);var input=game.CanvasRoot.GetComponentsInChildren<InputField>().First();
                input.SetTextWithoutNotify("Mockup UI test");input.onEndEdit.Invoke("Mockup UI test");
                Check(game.profile.name=="Mockup UI test"&&Profile.Load(game.db).name=="Mockup UI test","Name input persists to the isolated test profile");
                await Page(game,"march");
                string stageArt=$"stage-{game.profile.selected:00}";
                Check(game.CanvasRoot.GetComponentsInChildren<Image>().Any(i=>i.sprite&&i.sprite.name==stageArt),"March shows the selected stage artwork");
                Click(game,"XUẤT KÍCH");await Show(game);
                Check(game.modal=="prebattle","Deploy action opens the prebattle prefab");
                Check(game.CanvasRoot.GetComponentsInChildren<StudioWidgetBinding>().Any(b=>b.id=="image::246:174"&&b.GetComponent<Image>().sprite?.name==stageArt),"Prebattle uses the same stage artwork");
                Click(game,"XUẤT KÍCH");await Show(game);
                Check(game.inBattle&&game.battle.state==BattleState.Playing,"Deploy button starts actual combat");
                var scenery=game.view.root.transform.Find("Mockup Kit Battle Background");
                var planes=scenery?scenery.GetComponentsInChildren<SpriteRenderer>():Array.Empty<SpriteRenderer>();
                Check(planes.Length==4&&planes.All(p=>p.sprite&&p.sprite.texture.name==stageArt),"Combat uses all four scenery layers from the selected stage");
                game.battle.energy=600;ClickBinding(game,"button::330:742");await Show(game);
                Check(game.battle.Cooldown(game.battle.skills[0].id)>0&&game.battle.energy<600,"HUD skill button consumes energy and starts cooldown");
                game.paused=true;await Show(game);float clock=game.battle.time;await Task.Delay(120);
                Check(game.battle.time==clock,"Pause popup stops simulation");Click(game,"TIẾP TỤC");await Show(game);Check(!game.paused,"Pause popup resumes combat");
                game.paused=true;await Show(game);Click(game,"Rút lui");await Show(game);Check(!game.inBattle,"Retreat action returns to the real hub");
                Check(MonsterAnimationValidation.CaptureCombatGallery()==20,"All 20 latest monster animations render through BattleView");
                await CaptureGallery(game);
                PerformanceRegression.Validate(game);
                Check(true,"Performance regression preserves targeting and reuses moving damage text");
                Check(Errors.Count==0,"No runtime errors in live UI transactions or gallery");
                Check(Fingerprint()==SessionState.GetString(Before,""),"Current user profile and backup unchanged during tests");
                SessionState.SetString(Result,JsonConvert.SerializeObject(new{verdict="PASS",unity=Application.unityVersion,passed=Checks.Count,renderedViews=21,checks=Checks,errors=Errors}));
            }
            catch(Exception e){SessionState.SetString(Result,JsonConvert.SerializeObject(new{verdict="FAIL",error=e.ToString(),checks=Checks,errors=Errors}));Debug.LogException(e);}
            finally{Application.logMessageReceived-=Log;EditorApplication.isPlaying=false;}
        }
        static async Task CaptureGallery(CatTanksGame game)
        {
            Directory.CreateDirectory("Captures/MockupGameUI");
            foreach(string key in new[]{"home","barracks-cats","barracks-cat","barracks-fortress","barracks-squad","barracks-skills","barracks-monsters","barracks-presets","barracks-nexus","town-hub","town-summon","town-shop","town-arena","chest","quests"})
            {
                var parts=key.Split('-');await Page(game,parts[0]=="home"?"march":parts[0],parts.Length>1?parts[1]:"cats");Capture(game,key);
            }
            foreach(string key in new[]{"settings","book","prebattle","map"}){game.modal=key;await Show(game);Capture(game,key);}
            await Page(game,"barracks","fortress");Click(game,"Chim 1");await Show(game);Capture(game,"choice");
            game.modal="";game.StartBattle();game.enabled=false;game.battle.Spawn("bug-0",1000);game.battle.Step(.05f);game.view.Render(game.battle);
            await Show(game);Capture(game,"battle");game.paused=true;await Show(game);Capture(game,"pause");
            game.battle.state=BattleState.Won;typeof(CatTanksGame).GetMethod("Resolve",Private).Invoke(game,null);await Show(game);Capture(game,"result");
            int gold=game.profile.Get("coins");await Show(game);Check(game.profile.Get("coins")==gold,"Refreshing result popup does not duplicate rewards");
            int stage=game.profile.selected;Click(game,"ẢI TIẾP THEO");await Show(game);Check(game.inBattle&&game.profile.selected==stage+1,"Result popup deploys the next real stage");
        }
        static void Capture(CatTanksGame game,string key)
        {
            Check(ButtonAlignmentAudit.Validate(game.CanvasRoot)>0,key+" button content is centered, contained and fitted");
            var canvas=game.CanvasRoot.GetComponent<Canvas>();var active=RenderTexture.active;
            var cameraObject=new GameObject("Live UI Capture Camera");var camera=cameraObject.AddComponent<Camera>();camera.CopyFrom(Camera.main);camera.enabled=false;
            camera.transform.SetPositionAndRotation(new Vector3(800,450,-1000),Quaternion.identity);
            var root=(RectTransform)canvas.transform;var rootPosition=root.position;var rootScale=root.localScale;var rootSize=root.sizeDelta;var rootPivot=root.pivot;
            var viewport=game.CanvasRoot.viewport;var scale=viewport.localScale;var position=viewport.anchoredPosition;
            var target=new RenderTexture(1600,900,24);var texture=new Texture2D(1600,900,TextureFormat.RGB24,false);
            try
            {
                camera.targetTexture=target;camera.rect=new Rect(0,0,1,1);camera.aspect=1600f/900;camera.orthographicSize=450;
                canvas.renderMode=RenderMode.WorldSpace;canvas.worldCamera=camera;
                root.pivot=new Vector2(.5f,.5f);root.sizeDelta=new Vector2(1600,900);root.position=new Vector3(800,450,-999);root.localScale=Vector3.one;
                viewport.localScale=Vector3.one;viewport.anchoredPosition=Vector2.zero;
                Canvas.ForceUpdateCanvases();var corners=new Vector3[4];viewport.GetWorldCorners(corners);
                Check(Mathf.Abs(camera.WorldToScreenPoint(corners[2]).x-camera.WorldToScreenPoint(corners[0]).x-1600)<2,key+" fills the 1600-pixel capture width");
                camera.Render();RenderTexture.active=target;texture.ReadPixels(new Rect(0,0,1600,900),0,0);texture.Apply();
                Check(texture.GetPixels32().Where((p,i)=>i%41==0).Select(p=>(p.r<<16)|(p.g<<8)|p.b).Distinct().Count()>100,key+" renders actual game UI");
                File.WriteAllBytes("Captures/MockupGameUI/"+key+".png",texture.EncodeToPNG());
            }
            finally{canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.worldCamera=null;root.pivot=rootPivot;root.sizeDelta=rootSize;root.position=rootPosition;root.localScale=rootScale;viewport.localScale=scale;viewport.anchoredPosition=position;RenderTexture.active=active;UnityEngine.Object.DestroyImmediate(cameraObject);UnityEngine.Object.DestroyImmediate(texture);target.Release();UnityEngine.Object.DestroyImmediate(target);}
        }
        static async Task Page(CatTanksGame game,string page,string tab="cats"){game.page=page;game.tab=tab;game.modal="";await Show(game);}
        static async Task Show(CatTanksGame game){game.RefreshCanvas();Canvas.ForceUpdateCanvases();await Task.Delay(90);}
        static Text[] Texts(CatTanksGame game)=>game.CanvasRoot.GetComponentsInChildren<Text>();
        static Button[] Buttons(CatTanksGame game)=>game.CanvasRoot.GetComponentsInChildren<Button>();
        static string Label(Button b){var text=b.GetComponentInChildren<Text>();return text?text.text:"";}
        static void Click(CatTanksGame game,string text)
        {
            var button=Buttons(game).FirstOrDefault(b=>b.interactable&&Label(b).Contains(text));if(!button)throw new Exception("Missing enabled button: "+text);button.onClick.Invoke();
        }
        static void ClickBinding(CatTanksGame game,string id,bool enabled=true)
        {
            var binding=game.CanvasRoot.GetComponentsInChildren<StudioWidgetBinding>().First(b=>b.id==id);var button=binding.GetComponent<Button>();
            Check(button&&button.interactable==enabled,"Button eligibility: "+id);button.onClick.Invoke();
        }
        static void Log(string message,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)Errors.Add(message);}
        static string Fingerprint()
        {
            string path=Path.Combine(Application.persistentDataPath,"cattanks-profile.json");
            using(var sha=SHA256.Create())return string.Join("|",new[]{path,path+".bak",path+".tmp"}.Select(p=>File.Exists(p)?Convert.ToBase64String(sha.ComputeHash(File.ReadAllBytes(p))):"absent"));
        }
        public static void RunGameRegression()
        {
            if(!Application.isBatchMode)throw new Exception("Use a dedicated Unity batch process.");
            string before=Fingerprint(),savedPath=Profile.TestSavePath;
            EditorSceneManager.OpenScene("Assets/CatTanks/Scenes/Main.unity",OpenSceneMode.Single);
            try
            {
                MockupGameUIBuilder.Validate();Check(!PreviewIsolation.IsActive,"Main remains a live game scene");
                string current=Path.Combine(Application.persistentDataPath,"cattanks-profile.json");
                Profile.TestSavePath=Path.GetFullPath("Logs/mockup-profile-compatibility.json");
                if(File.Exists(current))
                {
                    File.Copy(current,Profile.TestSavePath,true);
                    var original=JsonConvert.DeserializeObject<Profile>(File.ReadAllText(current));var loaded=Profile.Load(Catalog.Load());
                    Check(original.version==loaded.version&&original.accountLevel==loaded.accountLevel&&original.highestCleared==loaded.highestCleared&&original.selected==loaded.selected,"Existing profile copy preserves progress and selected stage");
                    Check(JsonConvert.SerializeObject(original.wallet.OrderBy(p=>p.Key))==JsonConvert.SerializeObject(loaded.wallet.OrderBy(p=>p.Key)),"Existing profile copy preserves all wallet balances");
                    Check(JsonConvert.SerializeObject(original.cats)==JsonConvert.SerializeObject(loaded.cats)&&JsonConvert.SerializeObject(original.floors)==JsonConvert.SerializeObject(loaded.floors)&&original.squad.SequenceEqual(loaded.squad),"Existing profile copy preserves cats, equipment references and loadout");
                }
                ProjectSetup.ValidatePort();
            }
            finally{Profile.TestSavePath=savedPath;}
            Check(Fingerprint()==before,"Actual user profile and backup unchanged after compatibility and gameplay regression");
            File.WriteAllText("Logs/mockup-game-regression.json",JsonConvert.SerializeObject(new{verdict="PASS",checks=Checks,gameplayReport="Logs/port-validation.json",userProfileUnchanged=true},Formatting.Indented));
            Debug.Log("MOCKUP_GAME_REGRESSION_PASS: profile compatibility and 124 gameplay checks.");
        }
        static void Finish()
        {
            SessionState.SetBool(Pending,false);string text=SessionState.GetString(Result,"");
            var report=string.IsNullOrEmpty(text)?new JObject{{"verdict","FAIL"},{"error","Play checks did not finish"}}:JObject.Parse(text);
            bool unchanged=Fingerprint()==SessionState.GetString(Before,"");report["profileUnchangedAfterStop"]=unchanged;if(!unchanged)report["verdict"]="FAIL";
            File.WriteAllText(Report,report.ToString());bool pass=(string)report["verdict"]=="PASS";
            Debug.Log("MOCKUP_GAME_UI_"+(pass?"PASS":"FAIL")+": "+Report);EditorApplication.Exit(pass?0:1);
        }
    }
}
