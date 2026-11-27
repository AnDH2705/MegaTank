using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
namespace MegaTanks.Editor
{
    [InitializeOnLoad]
    public static class OriginalPortCapture
    {
        static readonly string[] states={"main","crew","gameplay","ammo","supply","medical","wreck","upgrade","boss"};
        static int index,wait,lastFrame;static HtmlBattleScreen screen;static StageRewardSaveScope scope;
        static Dictionary<string,string> values;static HashSet<string> existing;static bool errors,oldOptionsEnabled;static EnterPlayModeOptions oldOptions;
        static OriginalPortCapture(){EditorApplication.update+=Pending;}
        static void Pending()
        {
            if(!File.Exists("Checks/port-capture.pending")||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlaying)return;
            if(File.Exists("Checks/port-originals.pending")||!File.Exists("Checks/port-ready.flag")||!OriginalPortSetup.AssembliesCurrent()||Resources.Load<HtmlBattleArt>("HtmlBattleArt")?.worldBlurShader==null)return;
            File.Delete("Checks/port-capture.pending");Run();
        }
        [MenuItem("MegaTanks/HTML Battle/Capture Original Port")]
        public static void Run()
        {
            if(EditorApplication.isPlaying)return;
            scope=new StageRewardSaveScope();values=new Dictionary<string,string>();existing=new HashSet<string>();
            foreach(string key in new[]{PlayerTankProfile.SaveKey,"MegaTanks.Crew.Loadout",BattleGame.SelectedTankSaveKey})
            {if(PlayerPrefs.HasKey(key))existing.Add(key);values[key]=PlayerPrefs.GetString(key,"");}
            PlayerPrefs.DeleteKey(PlayerTankProfile.SaveKey);index=0;wait=0;lastFrame=-1;errors=false;screen=null;
            Directory.CreateDirectory("Checks/HtmlPort");if(File.Exists("Checks/HtmlPort/runtime-errors.txt"))File.Delete("Checks/HtmlPort/runtime-errors.txt");
            var view=EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor"));MegaTanksSetup.SetLandscapeGameView(view);view.Show();
            oldOptionsEnabled=EditorSettings.enterPlayModeOptionsEnabled;oldOptions=EditorSettings.enterPlayModeOptions;
            EditorSettings.enterPlayModeOptionsEnabled=true;EditorSettings.enterPlayModeOptions=EnterPlayModeOptions.DisableDomainReload;
            EditorApplication.playModeStateChanged+=State;Application.logMessageReceived+=Log;EditorApplication.isPlaying=true;
        }
        static void Log(string text,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception){errors=true;File.AppendAllText("Checks/HtmlPort/runtime-errors.txt",text+"\n"+stack+"\n");}}
        static void State(PlayModeStateChange state)
        {
            if(state==PlayModeStateChange.EnteredPlayMode){wait=20;EditorApplication.update+=Tick;}
            if(state==PlayModeStateChange.EnteredEditMode)
            {
                EditorApplication.update-=Tick;EditorApplication.playModeStateChanged-=State;Application.logMessageReceived-=Log;
                scope?.Dispose();foreach(var pair in values){if(existing.Contains(pair.Key))PlayerPrefs.SetString(pair.Key,pair.Value);else PlayerPrefs.DeleteKey(pair.Key);}PlayerPrefs.Save();
                EditorSettings.enterPlayModeOptionsEnabled=oldOptionsEnabled;EditorSettings.enterPlayModeOptions=oldOptions;
                File.AppendAllText("Checks/HtmlPort/validation.txt",errors?"Runtime capture found errors; inspect runtime-errors.txt.\n":"Runtime capture: no exceptions in Main, Crew, Gameplay, four choices, upgrade and Boss.\n");
            }
        }
        static void Tick()
        {
            try
            {
                if(Time.frameCount==lastFrame)return;lastFrame=Time.frameCount;if(--wait>0)return;var game=BattleGame.Instance;if(game==null){wait=5;return;}
                if(index>=states.Length*2){EditorApplication.update-=Tick;EditorApplication.isPlaying=false;return;}
                string state=states[index/2];
                if(index%2==0)
                {
                    if(state=="crew")game.UI.OpenPopup("Crew");
                    else if(state!="main"){game.UI.ClosePopup();if(screen==null)screen=HtmlBattleScreen.Begin(game,0);screen.ReviewState(state);}
                    wait=15;
                }
                else {ScreenCapture.CaptureScreenshot("Checks/HtmlPort/"+state+".png");wait=8;}
                index++;
            }
            catch(Exception e){Debug.LogException(e);EditorApplication.update-=Tick;EditorApplication.isPlaying=false;}
        }
    }
}

