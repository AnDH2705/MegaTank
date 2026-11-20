using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object=UnityEngine.Object;

namespace CatTanks.Editor
{
    // Captures the real campaign renderer and HUD without rebuilding game assets.
    [InitializeOnLoad]
    public static class StageScreenshotGallery
    {
        const string Pending="StageGallery.Pending",Before="StageGallery.ProfileBefore",Result="StageGallery.Result";
        const string Output="Captures/Stages";
        static readonly List<string> Errors=new List<string>();
        static StageScreenshotGallery()
        {
            EditorApplication.playModeStateChanged+=state=>
            {
                if(!SessionState.GetBool(Pending,false))return;
                if(state==PlayModeStateChange.EnteredPlayMode)CaptureAll();
                if(state==PlayModeStateChange.EnteredEditMode)Finish();
            };
        }
        public static void Run()
        {
            if(!Application.isBatchMode)throw new InvalidOperationException("Use a dedicated batch Editor with a Logs test profile.");
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-catTanksTestProfile");
            if(at<0||at+1>=args.Length||!Path.GetFullPath(args[at+1]).StartsWith(Path.GetFullPath("Logs")+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("An isolated Logs test profile is required.");
            Directory.CreateDirectory(Output);SessionState.SetString(Before,Fingerprint());SessionState.SetString(Result,"");
            EditorSceneManager.OpenScene("Assets/CatTanks/Scenes/Main.unity",OpenSceneMode.Single);
            SessionState.SetBool(Pending,true);EditorApplication.isPlaying=true;
        }
        static async void CaptureAll()
        {
            Application.logMessageReceived+=OnLog;
            Texture2D overview=null;
            try
            {
                await Task.Delay(200);
                var game=Object.FindObjectOfType<CatTanksGame>();
                if(!game||Profile.TestSavePath==null||PreviewIsolation.IsActive)throw new Exception("The isolated live game did not boot.");
                game.enabled=false;game.muted=true;
                var stages=game.db.stages.OrderBy(s=>s.index).ToArray();
                if(stages.Length!=20)throw new Exception("Expected all 20 campaign stages.");
                overview=new Texture2D(1600,1125,TextureFormat.RGB24,false);
                var entries=new List<object>();var digests=new HashSet<string>();var html=new StringBuilder();
                html.Append("<!doctype html><html lang=\"vi\"><meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width,initial-scale=1\"><title>CatTanks — 20 màn chơi</title><style>body{margin:24px;background:#0b1729;color:#edf4ff;font:16px system-ui}main{display:grid;grid-template-columns:repeat(auto-fit,minmax(320px,1fr));gap:20px}a{color:inherit;text-decoration:none}img{width:100%;border-radius:8px}h2{font-size:17px;margin:8px 0}</style><h1>CatTanks — 20 màn chơi</h1><p>Ảnh chụp trong Unity, 1600 × 900. Bấm vào ảnh để xem kích thước đầy đủ.</p><main>");
                for(int i=0;i<stages.Length;i++)
                {
                    var stage=stages[i];UnityEngine.Random.InitState(1700+stage.index);
                    game.profile=Profile.New(game.db);game.profile.highestCleared=20;game.profile.accountLevel=100;game.profile.selected=stage.index;
                    game.StartBattle();game.RefreshCanvas();
                    // Deterministic early combat; only the isolated test profile is saved.
                    for(int step=0;step<480&&game.battle.state==BattleState.Playing;step++)
                    {
                        game.battle.Step(1f/60);
                        if(step>=360)game.view.Render(game.battle);
                    }
                    game.view.Render(game.battle);game.RefreshCanvas();Canvas.ForceUpdateCanvases();
                    var background=game.view.root.transform.Find("Mockup Kit Battle Background");
                    if(!background||background.childCount!=4||background.GetComponentsInChildren<SpriteRenderer>().Any(r=>!r.sprite||r.sprite.texture.name!=$"stage-{stage.index:00}"))
                        throw new Exception("Stage-specific scenery is missing for stage "+stage.index);
                    string file=$"stage-{stage.index:00}.png";
                    Capture(game,Output+"/"+file,overview,i);
                    string digest;using(var sha=SHA256.Create())digest=BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(Output+"/"+file))).Replace("-","");
                    if(!digests.Add(digest))throw new Exception("Duplicate stage capture: "+file);
                    entries.Add(new{stage.index,stage.name,file,width=1600,height=900,battleTime=game.battle.time,enemies=game.battle.enemies.Count,sceneryLayers=4,sha256=digest});
                    html.Append("<a href=\"").Append(file).Append("\"><img loading=\"lazy\" src=\"").Append(file).Append("\" alt=\"Màn ").Append(stage.index).Append("\"><h2>").Append(stage.index.ToString("00")).Append(" — ").Append(System.Net.WebUtility.HtmlEncode(stage.name)).Append("</h2></a>");
                    Debug.Log("STAGE_SCREENSHOT_CAPTURED: "+file);
                    await Task.Delay(25);
                }
                overview.Apply();File.WriteAllBytes(Output+"/overview.png",overview.EncodeToPNG());
                File.WriteAllText(Output+"/index.html",html.Append("</main></html>").ToString(),new UTF8Encoding(false));
                if(Errors.Count>0)throw new Exception(string.Join("\n",Errors));
                if(Fingerprint()!=SessionState.GetString(Before,""))throw new Exception("The current user profile changed.");
                SessionState.SetString(Result,JsonConvert.SerializeObject(new{verdict="PASS",unity=Application.unityVersion,scene="Assets/CatTanks/Scenes/Main.unity",captured=entries.Count,profileUnchanged=true,snapshot="Deterministic early campaign combat using a fresh isolated test profile per stage",stages=entries},Formatting.Indented));
            }
            catch(Exception e){SessionState.SetString(Result,JsonConvert.SerializeObject(new{verdict="FAIL",error=e.ToString(),errors=Errors}));Debug.LogException(e);}
            finally{if(overview)Object.DestroyImmediate(overview);Application.logMessageReceived-=OnLog;EditorApplication.isPlaying=false;}
        }
        static void Capture(CatTanksGame game,string path,Texture2D overview,int index)
        {
            var canvas=game.CanvasRoot.GetComponent<Canvas>();var previousActive=RenderTexture.active;
            var cameraObject=new GameObject("Stage Screenshot Camera");var camera=cameraObject.AddComponent<Camera>();camera.CopyFrom(Camera.main);camera.enabled=false;
            camera.transform.SetPositionAndRotation(new Vector3(800,450,-1000),Quaternion.identity);
            var root=(RectTransform)canvas.transform;var rootPosition=root.position;var rootScale=root.localScale;var rootSize=root.sizeDelta;var rootPivot=root.pivot;
            var viewport=game.CanvasRoot.viewport;var scale=viewport.localScale;var position=viewport.anchoredPosition;
            var target=new RenderTexture(1600,900,24);var texture=new Texture2D(1600,900,TextureFormat.RGB24,false);
            var thumbnailTarget=new RenderTexture(400,225,0);var thumbnail=new Texture2D(400,225,TextureFormat.RGB24,false);
            try
            {
                camera.targetTexture=target;camera.rect=new Rect(0,0,1,1);camera.aspect=1600f/900;camera.orthographicSize=450;
                canvas.renderMode=RenderMode.WorldSpace;canvas.worldCamera=camera;
                root.pivot=new Vector2(.5f,.5f);root.sizeDelta=new Vector2(1600,900);root.position=new Vector3(800,450,-999);root.localScale=Vector3.one;
                viewport.localScale=Vector3.one;viewport.anchoredPosition=Vector2.zero;Canvas.ForceUpdateCanvases();
                camera.Render();RenderTexture.active=target;texture.ReadPixels(new Rect(0,0,1600,900),0,0);texture.Apply();
                if(texture.GetPixels32().Where((p,i)=>i%41==0).Select(p=>(p.r<<16)|(p.g<<8)|p.b).Distinct().Count()<100)throw new Exception("Blank render: "+path);
                File.WriteAllBytes(path,texture.EncodeToPNG());
                Graphics.Blit(target,thumbnailTarget);RenderTexture.active=thumbnailTarget;thumbnail.ReadPixels(new Rect(0,0,400,225),0,0);thumbnail.Apply();
                overview.SetPixels32((index%4)*400,(4-index/4)*225,400,225,thumbnail.GetPixels32());
            }
            finally
            {
                canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.worldCamera=null;root.pivot=rootPivot;root.sizeDelta=rootSize;root.position=rootPosition;root.localScale=rootScale;viewport.localScale=scale;viewport.anchoredPosition=position;
                RenderTexture.active=previousActive;Object.DestroyImmediate(cameraObject);Object.DestroyImmediate(texture);Object.DestroyImmediate(thumbnail);target.Release();thumbnailTarget.Release();Object.DestroyImmediate(target);Object.DestroyImmediate(thumbnailTarget);
            }
        }
        static void OnLog(string message,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)Errors.Add(message);}
        static string Fingerprint()
        {
            string path=Path.Combine(Application.persistentDataPath,"cattanks-profile.json");
            using(var sha=SHA256.Create())return string.Join("|",new[]{path,path+".bak",path+".tmp"}.Select(p=>File.Exists(p)?Convert.ToBase64String(sha.ComputeHash(File.ReadAllBytes(p))):"absent"));
        }
        static void Finish()
        {
            SessionState.SetBool(Pending,false);string result=SessionState.GetString(Result,"");
            bool pass=!string.IsNullOrEmpty(result)&&Fingerprint()==SessionState.GetString(Before,"")&&result.Contains("\"PASS\"");
            File.WriteAllText(Output+"/manifest.json",string.IsNullOrEmpty(result)?"{\"verdict\":\"FAIL\"}":result);
            Debug.Log("STAGE_SCREENSHOT_GALLERY_"+(pass?"PASS":"FAIL"));EditorApplication.Exit(pass?0:1);
        }
    }
}
