using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CatTanks.Editor
{
    [InitializeOnLoad]
    public static class MockupInteractivePlay
    {
        const string Active="MockupInteractive.Active",Before="MockupInteractive.Before",TestPath="MockupInteractive.TestPath",PendingStart="MockupInteractive.PendingStart";
        static MockupInteractivePlay()
        {
            EditorApplication.playModeStateChanged+=state=>
            {
                if(!SessionState.GetBool(Active,false))return;
                if(state==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(PendingStart,false))
                {
                    SessionState.SetBool(PendingStart,false);ConfirmReady();
                }
                if(state==PlayModeStateChange.EnteredEditMode)
                {
                    SessionState.SetBool(PendingStart,false);
                    Debug.Log("MOCKUP_INTERACTIVE_STOPPED: original_profile_unchanged="+(Fingerprint()==SessionState.GetString(Before,"")));
                }
            };
        }
        public static void Open()
        {
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-catTanksTestProfile");
            if(Application.isBatchMode||at<0||at+1>=args.Length)throw new InvalidOperationException("Interactive play requires an explicit editor test profile.");
            string path=Path.GetFullPath(args[at+1]);
            if(!path.StartsWith(Path.GetFullPath("Logs")+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Test profile must be inside this project's Logs folder.");
            string original=Path.Combine(Application.persistentDataPath,"cattanks-profile.json");
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            if(!File.Exists(path)&&File.Exists(original))
            {
                File.Copy(original,path);
                if(File.Exists(original+".bak"))File.Copy(original+".bak",path+".bak",true);
            }
            SessionState.SetString(Before,Fingerprint());SessionState.SetString(TestPath,path);SessionState.SetBool(Active,true);SessionState.SetBool(PendingStart,true);
            EditorSceneManager.OpenScene("Assets/CatTanks/Scenes/Main.unity",OpenSceneMode.Single);
            FocusGameView();EditorApplication.isPlaying=true;
        }
        static async void ConfirmReady()
        {
            CatTanksGame game=null;bool ready=false;
            for(int attempt=0;attempt<100&&EditorApplication.isPlaying;attempt++)
            {
                game=UnityEngine.Object.FindObjectOfType<CatTanksGame>();
                ready=game&&game.profile!=null&&game.CanvasRoot&&game.CanvasRoot.mockupKit&&Profile.TestSavePath==SessionState.GetString(TestPath,"");
                if(ready)break;
                await Task.Delay(100);
            }
            if(!EditorApplication.isPlaying)return;
            bool unchanged=Fingerprint()==SessionState.GetString(Before,"");
            if(!ready||!unchanged)
            {
                Debug.LogError("MOCKUP_INTERACTIVE_NOT_READY: ready="+ready+", original_profile_unchanged="+unchanged);
                EditorApplication.isPlaying=false;return;
            }
            FocusGameView();
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-catTanksCombatPreview")>=0)game.StartBattle();
            Debug.Log("MOCKUP_INTERACTIVE_READY: Main.unity; Mockup game Canvas; test_profile="+Profile.TestSavePath+"; original_profile_unchanged=True");
        }
        static void FocusGameView()
        {
            var type=typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
            if(type==null)return;
            var window=EditorWindow.GetWindow(type);window.maximized=true;window.Focus();
        }
        static string Fingerprint()
        {
            string path=Path.Combine(Application.persistentDataPath,"cattanks-profile.json");
            using(var sha=SHA256.Create())return string.Join("|",new[]{path,path+".bak",path+".tmp"}.Select(p=>File.Exists(p)?Convert.ToBase64String(sha.ComputeHash(File.ReadAllBytes(p))):"absent"));
        }
    }
}
