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
            var game=UnityEngine.Object.FindObjectOfType<CatTanksGame>();game.titleScreen=false;var saved=game.profile;string savedPath=Profile.TestSavePath;
            try
            {
                Profile.TestSavePath=Path.GetFullPath("Logs/ui-gallery-profile.json");
                game.profile=JsonConvert.DeserializeObject<Profile>(JsonConvert.SerializeObject(saved));
                Directory.CreateDirectory("Captures/StudioUI");
                string[] pages={"march","barracks","barracks","barracks","barracks","barracks","barracks","barracks","barracks","town","town","town","chest","quests"};
                string[] tabs={"cats","cats","cat","fortress","squad","skills","monsters","presets","nexus","summon","shop","arena","cats","cats"};
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
            var game = UnityEngine.Object.FindObjectOfType<CatTanksGame>();game.titleScreen=false;
            var savedProfile = game.profile; string savedPath = Profile.TestSavePath;
            var checks = new List<string>();
            Action<bool,string> check = (pass,name) => {if (!pass) throw new Exception(name); checks.Add(name);};
            try
            {
                Profile.TestSavePath = Path.GetFullPath("Logs/ui-smoke-profile.json");
                game.profile = Profile.New(game.db); game.page="march";game.tab="cats";game.modal="";
                await Show(game);
                check(game.UiReady,"Game theme and native Canvas loaded");
                check(!(bool)typeof(CatTanksGame).GetField("authoringUi",Private).GetValue(game)&&game.CanvasRoot.GetComponentsInChildren<StudioWidgetBinding>(true).Any(b=>b.authored),"Runtime uses the authored Canvas and widget templates");
                check(game.CanvasRoot.GetComponentsInChildren<StudioUIView>(true).Count(v=>v.layer=="popup"&&v.key!="map"&&v.key!="afk"&&v.key!="title")==6,"Six separate popup prefab instances are present");
                game.page="barracks";game.tab="cats";await Show(game);
                // Danh sách mèo dùng view tạo lúc chạy (toạ độ theo code) thay cho prefab tab MÈO cũ.
                check(game.CanvasRoot.GetComponentsInChildren<Text>().Any(t=>t.text=="Những người giữ pháo đài"),"Barracks opens on the cat roster");
                game.tab="cat";await Show(game);
                check(game.CanvasRoot.GetComponentsInChildren<Button>().Any(b=>b.GetComponentInChildren<Text>()?.text=="NÂNG CẤP"),"Cat detail page renders the level-up button");
                game.tab="cats";await Show(game);
                check(game.CanvasRoot.GetComponent<Canvas>()!=null,"Native Canvas present");
                var activeTheme=game.CanvasRoot.theme?game.CanvasRoot.theme:Resources.Load<StudioUITheme>("StudioUITheme");
                check(activeTheme.assets.All(a=>a.sprite),"All active theme references resolve");
                Click("Doanh trại");await Show(game);check(game.page=="barracks","Home buttons navigate through real button commands");
                foreach(string tab in new[]{"cats","cat","fortress","squad","skills","monsters","presets","nexus"})
                {
                    game.tab=tab;await Show(game);check(ActiveButtons().Length>=6,"Barracks renders: "+tab);
                }
                // Đội hình: chạm ô bộ binh 1 rồi "Gỡ khỏi ô".
                game.tab="squad";await Show(game);
                var slot=ActiveButtons().FirstOrDefault(b=>b.TryGetComponent<StudioWidgetBinding>(out var w)&&w.id=="button::810:226");
                check(slot!=null,"Formation infantry slot present");slot.onClick.Invoke();await Show(game);
                Click("Gỡ khỏi ô");await Show(game);check(game.profile.squad[0]=="","Formation slot can be cleared");
                // Popup chọn (module pháo) chặn thao tác phía sau.
                game.tab="fortress";await Show(game);
                Click("Chim 1");await Show(game);
                check(game.modal=="choice","Explicit module picker opens");
                check(!ActiveButtons().First(b=>b.GetComponentInChildren<Text>().text=="‹").interactable,"Modal blocks background controls");
                Click("Gỡ chim");await Show(game);check(game.modal==""&&game.profile.turrets[0].modules[0]=="","Picker applies selected module");
                game.page="town";
                foreach(string tab in new[]{"hub","summon","shop","arena"}) {game.tab=tab;await Show(game);check(ActiveButtons().Length>=4,"Town renders: "+tab);}
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
                game.paused=true;await Show(game);Click("TIẾP TỤC");await Show(game);check(!game.paused,"Pause overlay resumes gameplay");
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
