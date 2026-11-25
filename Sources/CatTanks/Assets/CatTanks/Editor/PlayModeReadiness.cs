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
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CatTanks.Editor
{
    // Runs against committed assets, without rebuilding the Canvas or skipping entry UI.
    [InitializeOnLoad]
    public static class PlayModeReadiness
    {
        const string Pending="CatTanks.Readiness.Pending",Before="CatTanks.Readiness.Before",Result="CatTanks.Readiness.Result";
        const string Report="Logs/playmode-readiness.json";
        static readonly List<string> Errors=new List<string>();
        static PlayModeReadiness()
        {
            EditorApplication.playModeStateChanged+=state=>
            {
                if(!SessionState.GetBool(Pending,false))return;
                if(state==PlayModeStateChange.EnteredPlayMode)RunChecks();
                if(state==PlayModeStateChange.EnteredEditMode)Finish();
            };
        }
        public static void Run()
        {
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-catTanksTestProfile");
            if(!Application.isBatchMode||at<0||at+1>=args.Length||
               !Path.GetFullPath(args[at+1]).StartsWith(Path.GetFullPath("Logs")+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Run batch mode with an explicit Logs test profile.");
            Directory.CreateDirectory("Logs");SessionState.SetString(Before,Fingerprint());SessionState.SetString(Result,"");
            EditorSceneManager.OpenScene("Assets/CatTanks/Scenes/Main.unity",OpenSceneMode.Single);
            // Reproduce launcher state carried across imports/domain reloads. A normal
            // Play request must not be stopped by an old interactive test session.
            SessionState.SetBool("MockupInteractive.Active",true);
            SessionState.SetBool("MockupInteractive.PendingStart",false);
            SessionState.SetString("MockupInteractive.Before","stale-launcher-session");
            SessionState.SetString("MockupInteractive.TestPath",Path.GetFullPath("Logs/previous-launch-profile.json"));
            SessionState.SetBool(Pending,true);EditorApplication.isPlaying=true;
        }
        static void Check(bool pass,string message,List<string> checks)
        {
            if(!pass)throw new Exception(message);checks.Add(message);
        }
        static void Draw(CatTanksGame game){game.RefreshCanvas();Canvas.ForceUpdateCanvases();}
        static GameObject Hit(CatTanksGame game,Vector2 point)
        {
            var viewport=game.CanvasRoot.viewport;
            var position=RectTransformUtility.WorldToScreenPoint(null,viewport.TransformPoint(new Vector3(viewport.rect.xMin+point.x,viewport.rect.yMax-point.y,0)));
            var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=position},hits);
            if(hits.Count==0)throw new Exception("No UI hit at "+point);
            return hits[0].gameObject;
        }
        static void Tap(CatTanksGame game,Vector2 point,string expectedView,List<string> checks)
        {
            var target=ExecuteEvents.GetEventHandler<IPointerClickHandler>(Hit(game,point));
            var view=target?target.GetComponentInParent<StudioUIView>():null;
            Check(view&&(expectedView==null||view.key==expectedView),"Pointer reaches button at "+point,checks);
            ExecuteEvents.Execute(target,new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left},ExecuteEvents.pointerClickHandler);
        }
        public static async Task ValidateTitle(CatTanksGame game,List<string> checks)
        {
            if(!EditorApplication.isPlaying||!game||game.inBattle||Profile.TestSavePath==null)throw new InvalidOperationException("An isolated live hub is required.");
            var start=typeof(CatTanksGame).GetField("titleStart",BindingFlags.Instance|BindingFlags.NonPublic);
            bool savedTitle=game.titleScreen;float savedStart=(float)start.GetValue(game);
            try
            {
                game.titleScreen=true;start.SetValue(game,Time.unscaledTime);Draw(game);
                Tap(game,new Vector2(800,706),"title",checks);Check(game.titleScreen,"Loading cannot be skipped by an early tap",checks);
                await Task.Delay(1800);Draw(game);
                foreach(var point in new[]{new Vector2(800,706),new Vector2(800,812),new Vector2(800,200),new Vector2(80,120),new Vector2(1510,850)})
                {
                    var target=ExecuteEvents.GetEventHandler<IPointerClickHandler>(Hit(game,point));
                    Check(target&&target.GetComponentInParent<StudioUIView>().key=="title","Title decorations pass taps at "+point,checks);
                }
                Tap(game,new Vector2(800,706),"title",checks);
                Check(!game.titleScreen&&!game.CanvasRoot.Find("title").gameObject.activeInHierarchy,"Visible start prompt dismisses the title",checks);
                Check(game.CanvasRoot.GetComponentsInChildren<Button>().Any(b=>b.IsActive()&&b.interactable),"Home controls are enabled",checks);
            }
            finally{game.titleScreen=savedTitle;start.SetValue(game,savedStart);Draw(game);}
        }
        static async void RunChecks()
        {
            Application.logMessageReceived+=Log;
            var checks=new List<string>();
            try
            {
                await Task.Delay(250);var game=UnityEngine.Object.FindObjectOfType<CatTanksGame>();
                Check(game&&game.profile!=null&&game.CanvasRoot,"Main boots using the tracked Canvas",checks);
                Check(Profile.TestSavePath!=null&&Path.GetFullPath(Profile.TestSavePath).StartsWith(Path.GetFullPath("Logs")+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase),"Profile redirected before Awake",checks);
                Check(!PreviewIsolation.IsActive,"Main is not a preview scene",checks);
                Check(EditorApplication.isPlaying,"Stale launcher state does not cancel manual Play",checks);
                await ValidateTitle(game,checks);
                game.titleScreen=true;Draw(game);Tap(game,new Vector2(800,706),"title",checks);
                Check(!game.titleScreen,"Start button remains usable when title is shown again",checks);
                game.modal="settings";Draw(game);await Task.Delay(100);Draw(game);
                var panel=Hit(game,new Vector2(800,450));
                Check(panel.GetComponentInParent<StudioUIView>()?.key=="settings"&&ExecuteEvents.GetEventHandler<IPointerClickHandler>(panel)==null,"Ordinary popup panel blocks clicks to its dismiss scrim",checks);
                game.modal="";Draw(game);await Task.Delay(100);Draw(game);
                Tap(game,new Vector2(1381,723),null,checks);Check(game.modal=="prebattle","Home deploy opens preparation",checks);await Task.Delay(100);Draw(game);
                Tap(game,new Vector2(1130,688),"prebattle",checks);
                Check(game.inBattle&&game.battle.state==BattleState.Playing&&!game.titleScreen,"Preparation deploy enters live combat",checks);
                float clock=game.battle.time;await Task.Delay(350);
                Check(game.battle.time>clock&&game.UiReady,"Combat advances and HUD remains ready",checks);
                typeof(CatTanksGame).GetMethod("Hub",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(game,null);Draw(game);
                Check(!game.inBattle&&!game.titleScreen,"Returning from combat stays in the hub",checks);
                Check(Errors.Count==0,"No runtime errors while starting and leaving combat",checks);
                SessionState.SetString(Result,JsonConvert.SerializeObject(new{verdict="PASS",unity=Application.unityVersion,passed=checks.Count,checks,errors=Errors},Formatting.Indented));
            }
            catch(Exception e){SessionState.SetString(Result,JsonConvert.SerializeObject(new{verdict="FAIL",error=e.ToString(),checks,errors=Errors},Formatting.Indented));Debug.LogException(e);}
            finally{Application.logMessageReceived-=Log;EditorApplication.isPlaying=false;}
        }
        static void Log(string message,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)Errors.Add(message);}
        static string Fingerprint()
        {
            string path=Path.Combine(Application.persistentDataPath,"cattanks-profile.json");
            using(var sha=SHA256.Create())return string.Join("|",new[]{path,path+".bak",path+".tmp"}.Select(p=>File.Exists(p)?Convert.ToBase64String(sha.ComputeHash(File.ReadAllBytes(p))):"absent"));
        }
        static void Finish()
        {
            SessionState.SetBool(Pending,false);var text=SessionState.GetString(Result,"");
            var report=string.IsNullOrEmpty(text)?new JObject{{"verdict","FAIL"},{"error","Play Mode checks did not complete"}}:JObject.Parse(text);
            bool unchanged=Fingerprint()==SessionState.GetString(Before,"");report["profileUnchanged"]=unchanged;if(!unchanged)report["verdict"]="FAIL";
            File.WriteAllText(Report,report.ToString());bool pass=(string)report["verdict"]=="PASS";
            Debug.Log("PLAYMODE_READINESS_"+(pass?"PASS":"FAIL")+": "+Report);EditorApplication.Exit(pass?0:1);
        }
    }
}
