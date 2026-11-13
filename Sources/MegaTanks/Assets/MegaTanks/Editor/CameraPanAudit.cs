using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
namespace MegaTanks.Editor
{
    public static class CameraPanAudit
    {
        static int step,frameAtStep;
        static double since;
        static bool capture,errors;
        static float held,initialMax,tankX;
        static Vector3 cameraRest;
        static PointerEventData pointer;
        static BattlefieldPanInput input;
        static void Require(bool value,string message){if(!value)throw new Exception(message);}
        public static void Run(){Begin(false);}
        public static void Capture(){Begin(true);}
        static void Begin(bool screenshots)
        {
            capture=screenshots;EditorSceneManager.OpenScene(MegaTanksSetup.ScenePath);
            EditorSettings.enterPlayModeOptionsEnabled=true;EditorSettings.enterPlayModeOptions=EnterPlayModeOptions.DisableDomainReload;
            var window=EditorWindow.GetWindow(typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameView"));window.position=new Rect(20,20,1000,600);MegaTanksSetup.SetLandscapeGameView(window);window.Show();
            EditorApplication.playModeStateChanged+=Entered;EditorApplication.isPlaying=true;
        }
        static void Entered(PlayModeStateChange mode)
        {
            if(mode!=PlayModeStateChange.EnteredPlayMode)return;
            step=0;errors=false;since=EditorApplication.timeSinceStartup;Application.logMessageReceived+=Log;EditorApplication.update+=Check;
        }
        static void Log(string a,string b,LogType type){if(type==LogType.Error||type==LogType.Exception)errors=true;}
        static void Next(){step++;since=EditorApplication.timeSinceStartup;frameAtStep=Time.frameCount;}
        static void Shot(string name){if(!capture)return;System.IO.Directory.CreateDirectory("Checks/CameraPan");ScreenCapture.CaptureScreenshot(System.IO.Path.GetFullPath("Checks/CameraPan/"+name+".png"));}
        static GameObject Hit(Vector2 point)
        {
            Canvas.ForceUpdateCanvases();var data=new PointerEventData(EventSystem.current){position=point};var results=new List<RaycastResult>();EventSystem.current.RaycastAll(data,results);
            Require(results.Count>0,"Pointer must hit a Canvas surface.");return results[0].gameObject;
        }
        static void Drag(float delta,int id=-1)
        {
            pointer=new PointerEventData(EventSystem.current){pointerId=id,button=PointerEventData.InputButton.Left,delta=new Vector2(delta,0)};
            ExecuteEvents.Execute(input.gameObject,pointer,ExecuteEvents.beginDragHandler);ExecuteEvents.Execute(input.gameObject,pointer,ExecuteEvents.dragHandler);ExecuteEvents.Execute(input.gameObject,pointer,ExecuteEvents.endDragHandler);
        }
        static void Check()
        {
            if(EditorApplication.timeSinceStartup-since<.35||(step>0&&Time.frameCount<=frameAtStep+1))return;
            try
            {
                var game=BattleGame.Instance;var pan=game.CameraPan;
                switch(step)
                {
                    case 0:
                        game.UI.Play();game.SetStance(TankStance.Defensive);StageProgressionAudit.DisableWeapons(game);Next();break;
                    case 1:
                        input=UnityEngine.Object.FindObjectOfType<BattlefieldPanInput>();
                        Require(UnityEngine.Object.FindObjectsOfType<Camera>().Count(c=>c.enabled&&c.targetTexture==null)==1,"Only one gameplay camera may render.");
                        Require(GameObject.Find("Nearest Enemy Camera")==null,"Second battle camera must be removed.");
                        Require(Mathf.Abs(game.TankCamera.rect.width-Screen.safeArea.width/Screen.width)<.001f,"Camera must fill battlefield width.");
                        Require(Mathf.Abs(game.TankCamera.rect.yMax-1)<.001f,"The main camera must extend to the physical top of the screen.");
                        var headerGraphic=game.UI.transform.Find("Safe Area/Battle Screens/BattleHUD/Header").GetComponent<UnityEngine.UI.Image>();
                        Require(headerGraphic.color.a==0&&!headerGraphic.raycastTarget,"The top HUD panel must leave the battlefield visible and pannable.");
                        Require(Mathf.Abs(pan.MinX-game.Artwork.transform.position.x)<.001f&&Mathf.Abs(pan.MaxX-pan.MinX-game.EnemySpawnDistance)<.001f&&pan.X==pan.StartX,"Camera limits must include player spawn, initial framing, and right enemy spawn.");
                        Require(Mathf.Abs(game.TankCamera.WorldToViewportPoint(game.Artwork.transform.position).x-game.TankStartViewportX)<.001f,"Stage start must frame the tank near the left edge.");
                        cameraRest=game.TankCamera.transform.position;initialMax=pan.MaxX;tankX=game.Artwork.transform.position.x;
                        var at=game.TankCamera.ViewportToScreenPoint(new Vector3(.6f,.6f,0));
                        var hitTarget=Hit(at);
                        Require(ExecuteEvents.GetEventHandler<IDragHandler>(hitTarget)==input.gameObject,"Battlefield drag must target the dedicated pan surface; hit "+hitTarget.name+" at "+at+" on "+Screen.width+"x"+Screen.height);
                        var topPoint=game.TankCamera.ViewportToScreenPoint(new Vector3(.2f,.95f,0));
                        Require(ExecuteEvents.GetEventHandler<IDragHandler>(Hit(topPoint))==input.gameObject,"The exposed top battlefield must support dragging between HUD controls.");
                        Shot("PlayerSpawn");Next();break;
                    case 2:
                        float units=2*game.TankCamera.orthographicSize*game.TankCamera.aspect/game.TankCamera.pixelWidth;
                        float before=pan.X;Drag(-300);Require(Mathf.Abs(pan.X-before-300*units)<.001f,"Drag movement must match pixel-to-world scale.");
                        Drag(-999999);Require(pan.X==pan.MaxX,"Right enemy-spawn limit must clamp exactly.");Next();break;
                    case 3:
                        var tankViewport=game.TankCamera.WorldToViewportPoint(game.Artwork.transform.position);bool visible=tankViewport.z>0&&tankViewport.x>=0&&tankViewport.x<=1&&tankViewport.y>=0&&tankViewport.y<=1;
                        Require(game.UI.transform.Find("Safe Area/Battle Screens/BattleHUD/Hull Anchor").gameObject.activeInHierarchy==visible,"Tank health visibility must follow the actual viewport, including shortened enemy-spawn tuning.");
                        var header=game.UI.transform.Find("Safe Area/Battle Screens/BattleHUD/Header").GetComponent<RectTransform>();
                        float hudBottom=RectTransformUtility.WorldToScreenPoint(null,header.TransformPoint(new Vector3(0,header.rect.yMin))).y;
                        foreach(var flyer in UnityEngine.Object.FindObjectsOfType<EnemyEntity>().Where(e=>e.Kind==EnemyKind.Flyer))
                        {
                            var bounds=flyer.Artwork.OwnerLocalBounds;
                            var top=game.TankCamera.WorldToScreenPoint(flyer.transform.position+Vector3.up*(bounds.max.y+.2f));
                            var bottom=game.TankCamera.WorldToScreenPoint(flyer.transform.position+Vector3.up*bounds.min.y);
                            Require(top.y<hudBottom&&bottom.y>game.TankCamera.pixelRect.yMin,"Flying enemy artwork and health bars must be entirely below the HUD and inside the vertical battle view.");
                            Require(flyer.Artwork.GetComponentsInChildren<SpriteRenderer>().Any(r=>r.isVisible),"Flying enemies must actually render when the camera pans to the formation.");
                        }
                        var footerPoint=RectTransformUtility.WorldToScreenPoint(null,game.UI.transform.Find("Safe Area/Battle Screens/BattleHUD").TransformPoint(new Vector3(960,-1000)));
                        Require(ExecuteEvents.GetEventHandler<IDragHandler>(Hit(footerPoint))==input.gameObject,"The freed bottom area must accept battlefield drags.");
                        Shot("EnemySpawn");Next();break;
                    case 4:
                        Drag(999999,7);Require(pan.X==pan.MinX,"Touch-pointer drag must clamp at player spawn.");
                        Drag(-300);held=pan.X;
                        pointer=new PointerEventData(EventSystem.current){pointerId=7,button=PointerEventData.InputButton.Left,delta=new Vector2(0,500)};
                        input.OnBeginDrag(pointer);input.OnDrag(pointer);input.OnEndDrag(pointer);
                        Require(pan.X==held&&game.TankCamera.transform.position.y==cameraRest.y&&game.TankCamera.transform.position.z==cameraRest.z,"Vertical drag must not change X or camera height/depth.");
                        var pause=game.UI.transform.Find("Safe Area/Battle Screens/BattleHUD/Pause").GetComponent<RectTransform>();
                        var hit=Hit(RectTransformUtility.WorldToScreenPoint(null,pause.TransformPoint(pause.rect.center)));
                        Require(ExecuteEvents.GetEventHandler<IDragHandler>(hit)!=input.gameObject,"HUD input must not reach the battlefield pan handler.");
                        game.UI.OpenPopup("Settings");Drag(-300);Require(pan.X==held,"Popup must block panning.");game.UI.ClosePopup();
                        game.SetPaused(true);Drag(-300);Require(pan.X==held,"Pause must block panning.");Next();break;
                    case 5:
                        game.SetPaused(false);game.SetStance(TankStance.Aggressive);Next();break;
                    case 6:
                        Require(game.Artwork.transform.position.x>tankX&&pan.X==held,"Tank motion and enemy approach must not override manual camera position.");
                        Require(pan.MaxX==initialMax&&pan.MinX==tankX,"Bounds remain tied to spawn points while the wave advances.");
                        game.SetStance(TankStance.Defensive);StageProgressionAudit.ClearWave(game);Drag(-500);Require(pan.X==held,"Reward screen must block panning.");
                        game.Rewards.ChooseReward(0);StageProgressionAudit.EnterType(game,StageNodeType.Battle);Next();break;
                    case 7:
                        Require(Mathf.Abs(pan.MaxX-game.Artwork.transform.position.x-game.EnemySpawnDistance)<.001f&&pan.MaxX>initialMax&&pan.MinX==tankX,"New wave must update enemy spawn bound while retaining initial player framing.");
                        Require(pan.X==held,"Wave changes must preserve manual camera position.");
                        game.StartBattle();StageProgressionAudit.DisableWeapons(game);
                        Require(pan.X==pan.StartX&&pan.MaxX==initialMax,"Restart restores left-edge tank framing and spawn bounds.");
                        game.ReturnHome();held=pan.X;pan.PanByPixels(-100);Require(pan.X==held,"Home must block panning.");
                        Require(!errors,"Camera audit must have no runtime errors.");
                        Debug.Log("MEGATANKS_CAMERA_PAN_OK: single full-width camera, mouse/touch drag, spawn clamps, fixed horizon, HUD/popup/pause/reward blocking, no auto tracking, wave bounds, restart and Home.");Finish(0);break;
                }
            }
            catch(Exception e){Debug.LogException(e);Finish(1);}
        }
        static void Finish(int code){EditorApplication.update-=Check;Application.logMessageReceived-=Log;EditorApplication.playModeStateChanged-=Entered;EditorSettings.enterPlayModeOptionsEnabled=false;EditorApplication.Exit(code);}
    }
}
