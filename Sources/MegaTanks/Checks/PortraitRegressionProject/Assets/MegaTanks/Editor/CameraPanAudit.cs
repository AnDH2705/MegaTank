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
        static bool capture,errors,followOnly;
        static float held,initialMax,tankX;
        static Vector3 tankViewportBeforeAdvance;
        static Vector3 cameraRest;
        static PointerEventData pointer;
        static BattlefieldPanInput input;
        static void Require(bool value,string message){if(!value)throw new Exception(message);}
        public static void Run(){followOnly=false;Begin(false);}
        public static void Capture(){followOnly=false;Begin(true);}
        public static void FollowRun(){followOnly=true;Begin(false);}
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
                if(followOnly){CheckFollow(game,pan);return;}
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
                        tankViewportBeforeAdvance=game.TankCamera.WorldToViewportPoint(game.Artwork.transform.position);
                        tankX=game.Artwork.transform.position.x;held=pan.X;initialMax=pan.MaxX;
                        game.SetPaused(false);game.SetStance(TankStance.Aggressive);Next();break;
                    case 6:
                        float advanced=game.Artwork.transform.position.x-tankX;
                        Require(advanced>0&&Mathf.Abs(pan.X-held-advanced)<.001f,"Camera must move exactly with the advancing tank, preserving the manual pan offset.");
                        Require(Vector3.Distance(game.TankCamera.WorldToViewportPoint(game.Artwork.transform.position),tankViewportBeforeAdvance)<.001f,"Advancing must keep the tank at the same viewport position.");
                        Require(game.TankCamera.transform.position.y==cameraRest.y&&game.TankCamera.transform.position.z==cameraRest.z,"Following must preserve camera height and depth.");
                        Require(Mathf.Abs(pan.MaxX-initialMax-advanced)<.001f&&Mathf.Abs(pan.MinX-game.Artwork.transform.position.x)<.001f,"Drag limits must follow tank movement.");
                        held=pan.X;tankX=game.Artwork.transform.position.x;
                        game.SetStance(TankStance.Defensive);StageProgressionAudit.ClearWave(game,resolveCamp:false);Drag(-500);Require(pan.X==held,"Camp approach must block panning.");
                        StageProgressionAudit.EnterNextBattle(game);Next();break;
                    case 7:
                        Require(Mathf.Abs(pan.MaxX-game.Artwork.transform.position.x-game.EnemySpawnDistance)<.001f&&pan.MaxX>initialMax
                            &&Mathf.Abs(pan.MinX-game.Artwork.transform.position.x)<.001f,"New wave must update the spawn bounds relative to the advanced tank.");
                        Require(Mathf.Abs(pan.X-held-(game.Artwork.transform.position.x-tankX))<.001f,"The camera must preserve its manual offset through camp travel.");
                        game.StartBattle();StageProgressionAudit.DisableWeapons(game);
                        Require(pan.X==pan.StartX&&pan.MaxX==initialMax,"Restart restores left-edge tank framing and spawn bounds.");
                        game.ReturnHome();held=pan.X;pan.PanByPixels(-100);Require(pan.X==held,"Home must block panning.");
                        Require(!errors,"Camera audit must have no runtime errors.");
                        Debug.Log("MEGATANKS_CAMERA_PAN_OK: single full-width camera, mouse/touch drag, spawn clamps, fixed horizon, HUD/popup/pause/reward blocking, tank follow with stable viewport and manual offset, wave bounds, restart and Home.");Finish(0);break;
                }
            }
            catch(Exception e){Debug.LogException(e);Finish(1);}
        }
        static void CheckFollow(BattleGame game,BattleCameraPan pan)
        {
            switch(step)
            {
                case 0:
                    game.UI.Play();StageProgressionAudit.DisableWeapons(game);
                    tankX=game.Artwork.transform.position.x;held=pan.X;
                    tankViewportBeforeAdvance=game.TankCamera.WorldToViewportPoint(game.Artwork.transform.position);
                    cameraRest=game.TankCamera.transform.position;Next();break;
                case 1:
                case 2:
                    float advanced=game.Artwork.transform.position.x-tankX;
                    Require(advanced>0&&Mathf.Abs(pan.X-held-advanced)<.001f,"Camera must follow tank advance exactly.");
                    Require(Vector3.Distance(game.TankCamera.WorldToViewportPoint(game.Artwork.transform.position),tankViewportBeforeAdvance)<.001f,"Tank screen position must remain stable with default framing and after dragging.");
                    Require(game.TankCamera.transform.position.y==cameraRest.y&&game.TankCamera.transform.position.z==cameraRest.z,"Follow must keep camera height and depth fixed.");
                    if(step==1)
                    {
                        pan.PanByPixels(-100);
                        tankX=game.Artwork.transform.position.x;held=pan.X;
                        tankViewportBeforeAdvance=game.TankCamera.WorldToViewportPoint(game.Artwork.transform.position);
                    }
                    else {game.SetPaused(true);held=pan.X;tankX=game.Artwork.transform.position.x;}
                    Next();break;
                case 3:
                    Require(pan.X==held&&game.Artwork.transform.position.x==tankX,"Pause must stop tank and camera movement.");
                    game.SetPaused(false);game.SetStance(TankStance.Defensive);Next();break;
                case 4:
                    Require(pan.X==held&&game.Artwork.transform.position.x==tankX,"Defensive stance must stop tank and camera movement.");
                    game.StartBattle();StageProgressionAudit.DisableWeapons(game);game.SetStance(TankStance.Defensive);
                    Require(pan.X==pan.StartX&&Mathf.Abs(game.TankCamera.WorldToViewportPoint(game.Artwork.transform.position).x-game.TankStartViewportX)<.001f,"Restart must restore initial tank framing.");
                    game.ReturnHome();Require(pan.X==pan.StartX,"Home must reset camera framing.");
                    Require(!errors,"Follow audit must have no runtime errors.");
                    Debug.Log("MEGATANKS_CAMERA_FOLLOW_OK: stable screen position before and after manual pan, fixed height/depth, pause, defensive stance, restart and Home.");Finish(0);break;
            }
        }
        static void Finish(int code){EditorApplication.update-=Check;Application.logMessageReceived-=Log;EditorApplication.playModeStateChanged-=Entered;EditorSettings.enterPlayModeOptionsEnabled=false;EditorApplication.Exit(code);}
    }
}
