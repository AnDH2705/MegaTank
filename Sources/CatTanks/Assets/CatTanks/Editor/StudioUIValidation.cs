using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace CatTanks.Editor
{
    public static class StudioUIValidation
    {
        static bool running;
        static readonly BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        [MenuItem("CatTanks/UI/Capture UI Gallery (Play Mode)")]
        public static async void CaptureGallery()
        {
            if(running||!EditorApplication.isPlaying)return;running=true;
            var game=UnityEngine.Object.FindObjectOfType<CatTanksGame>();var saved=game.profile;string savedPath=Profile.TestSavePath;
            try
            {
                Profile.TestSavePath=Path.GetFullPath("Logs/ui-gallery-profile.json");
                game.profile=JsonConvert.DeserializeObject<Profile>(JsonConvert.SerializeObject(saved));
                Directory.CreateDirectory("Captures/StudioUI");
                string[] pages={"march","barracks","barracks","barracks","barracks","barracks","barracks","barracks","barracks","town","town","town","chest","quests"};
                string[] tabs={"cats","cats","fortress","squad","skills","monsters","gear","presets","nexus","summon","shop","arena","cats","cats"};
                for(int i=0;i<pages.Length;i++)
                {
                    game.page=pages[i];game.tab=tabs[i];game.modal="";
                    await Capture(game,pages[i]+(pages[i]=="barracks"||pages[i]=="town"?"-"+tabs[i]:""));
                }
                foreach(string modal in new[]{"settings","book","prebattle"}){game.modal=modal;await Capture(game,modal);}
                game.modal="";game.StartBattle();await Capture(game,"battle");
                game.paused=true;await Capture(game,"pause");
                game.battle.state=BattleState.Won;typeof(CatTanksGame).GetMethod("Resolve",Private).Invoke(game,null);await Capture(game,"result");
                Debug.Log("CATTANKS_UI_GALLERY_READY: Captures/StudioUI");
            }
            catch(Exception e){Debug.LogError(e);}
            finally
            {
                if(game.inBattle)typeof(CatTanksGame).GetMethod("Hub",Private).Invoke(game,null);
                game.profile=saved;Profile.TestSavePath=savedPath;game.page="march";game.tab="cats";game.modal="";game.paused=false;
                typeof(CatTanksGame).GetMethod("Preview",Private).Invoke(game,null);await Show(game);running=false;
            }
        }
        static async Task Capture(CatTanksGame game,string name)
        {
            await Show(game);UnityEngine.ScreenCapture.CaptureScreenshot("Captures/StudioUI/"+name+".png");await Task.Delay(160);
        }
        [MenuItem("CatTanks/UI/Run UI Smoke Checks (Play Mode)")]
        public static async void Run()
        {
            if (running) return;
            if (!EditorApplication.isPlaying) {Debug.LogWarning("Enter Play Mode before running UI smoke checks.");return;}
            running = true;
            var game = UnityEngine.Object.FindObjectOfType<CatTanksGame>();
            var savedProfile = game.profile; string savedPath = Profile.TestSavePath;
            var checks = new List<string>();
            Action<bool,string> check = (pass,name) => {if (!pass) throw new Exception(name); checks.Add(name);};
            try
            {
                Profile.TestSavePath = Path.GetFullPath("Logs/ui-smoke-profile.json");
                game.profile = Profile.New(game.db); game.page="march";game.tab="cats";game.modal="";
                await Show(game);
                check(game.UiReady,"LayerLab theme and native Canvas loaded");
                check(!(bool)typeof(CatTanksGame).GetField("authoringUi",Private).GetValue(game)&&game.CanvasRoot.GetComponentsInChildren<StudioWidgetBinding>(true).Any(b=>b.authored),"Runtime uses the authored Canvas and widget templates");
                check(game.CanvasRoot.GetComponentsInChildren<StudioUIView>(true).Count(v=>v.layer=="popup")==6,"Six separate popup prefab instances are present");
                var authoredTitle=game.CanvasRoot.GetComponentsInChildren<Text>(true).First(t=>t.text=="Chuyến phiêu lưu dưới trăng");
                var authoredRect=(RectTransform)authoredTitle.transform;var originalPosition=authoredRect.anchoredPosition;
                authoredRect.anchoredPosition=originalPosition+new Vector2(7,0);await Show(game);
                check(authoredRect.anchoredPosition==originalPosition+new Vector2(7,0),"UI data refresh preserves prefab-authored layout");
                authoredRect.anchoredPosition=originalPosition;
                check(game.CanvasRoot.GetComponent<Canvas>()!=null,"Native Canvas present");
                check(Resources.Load<StudioUITheme>("StudioUITheme").assets.All(a=>a.sprite),"All theme references resolve");
                Click("Doanh trại");await Show(game);check(game.page=="barracks","Sidebar navigates through real button commands");
                foreach(string tab in new[]{"cats","fortress","squad","skills","monsters","gear","presets","nexus"})
                {
                    game.tab=tab;await Show(game);check(ActiveButtons().Length>8,"Barracks renders: "+tab);
                }
                game.tab="squad";await Show(game);
                // Use a known squad slot label, regardless of which starting cat occupies it.
                var slot=ActiveButtons().FirstOrDefault(b=>b.GetComponentInChildren<Text>().text.Contains("Bộ binh 1:"));
                check(slot!=null,"Squad picker trigger present");slot.onClick.Invoke();await Show(game);
                check(game.modal=="choice","Explicit squad picker opens");
                check(!ActiveButtons().First(b=>b.GetComponentInChildren<Text>().text.Contains("Hành quân")).interactable,"Modal blocks background controls");
                Click("Để trống vị trí");await Show(game);check(game.modal==""&&game.profile.squad[0]=="","Picker applies selected squad member");
                game.page="town";
                foreach(string tab in new[]{"summon","shop","arena"}) {game.tab=tab;await Show(game);check(ActiveButtons().Length>=10,"Town renders: "+tab);}
                foreach(string page in new[]{"chest","quests","march"}) {game.page=page;await Show(game);check(ActiveButtons().Length>=7,"Hub renders: "+page);}
                game.modal="settings";await Show(game);check(UnityEngine.Object.FindObjectsOfType<InputField>().Any(i=>i.isActiveAndEnabled),"Editable commander name present");
                game.modal="book";
                for(int i=0;i<5;i++){game.bookTab=i;await Show(game);check(UnityEngine.Object.FindObjectsOfType<ScrollRect>().Any(s=>s.isActiveAndEnabled),"Encyclopedia scroll view: "+i);}
                game.modal="prebattle";await Show(game);check(ActiveButtons().Any(b=>b.interactable&&b.GetComponentInChildren<Text>().text=="XUẤT KÍCH"),"Deploy confirmation available");
                game.modal="";game.page="march";await Show(game);
                int before=game.CanvasRoot.GetComponentsInChildren<RectTransform>(true).Length;
                for(int i=0;i<5;i++)await Show(game);
                check(game.CanvasRoot.GetComponentsInChildren<RectTransform>(true).Length==before,"Repeated refresh reuses authored widgets");
                game.StartBattle();await Show(game);check(game.inBattle&&ActiveButtons().Any(b=>b.GetComponentInChildren<Text>().text.Contains("BÁCH KHOA")),"Battle HUD renders");
                game.paused=true;await Show(game);Click("Tiếp tục");await Show(game);check(!game.paused,"Pause overlay resumes gameplay");
                game.paused=true;await Show(game);Click("Rút lui");await Show(game);check(!game.inBattle,"Retreat returns to hub");
                File.WriteAllText("Logs/ui-smoke-validation.json",JsonConvert.SerializeObject(new{verdict="PASS",count=checks.Count,checks},Formatting.Indented));
                Debug.Log("CATTANKS_UI_SMOKE_PASS: "+checks.Count+" checks");
            }
            catch(Exception e)
            {
                File.WriteAllText("Logs/ui-smoke-validation.json",JsonConvert.SerializeObject(new{verdict="FAIL",checks,error=e.ToString()},Formatting.Indented));
                Debug.LogError("CATTANKS_UI_SMOKE_FAIL: "+e);
            }
            finally
            {
                if(game.inBattle)typeof(CatTanksGame).GetMethod("Hub",Private).Invoke(game,null);
                game.profile=savedProfile;Profile.TestSavePath=savedPath;game.page="march";game.tab="cats";game.modal="";game.paused=false;
                typeof(CatTanksGame).GetMethod("Preview",Private).Invoke(game,null);
                await Show(game);running=false;
            }
        }
        static UnityEngine.UI.Button[] ActiveButtons() => UnityEngine.Object.FindObjectsOfType<UnityEngine.UI.Button>().Where(b=>b.gameObject.activeInHierarchy).ToArray();
        static void Click(string text,bool required=true)
        {
            var button=ActiveButtons().FirstOrDefault(b=>b.interactable&&b.GetComponentInChildren<Text>().text.Contains(text));
            if(button)button.onClick.Invoke();else if(required)throw new Exception("Button not found: "+text);
        }
        static async Task Show(CatTanksGame game)
        {
            typeof(CatTanksGame).GetMethod("RefreshUi",Private).Invoke(game,null);
            Canvas.ForceUpdateCanvases();await Task.Delay(90);
        }
    }
}
