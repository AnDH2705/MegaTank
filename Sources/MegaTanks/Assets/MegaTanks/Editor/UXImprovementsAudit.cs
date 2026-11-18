using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object=UnityEngine.Object;

namespace MegaTanks.Editor
{
    public static class UXImprovementsAudit
    {
        const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
        static double since;static int phase;static bool errors;
        static string directory="Checks/UXImprovements";
        static int nextId;static StageNode previousNode;
        static void Require(bool condition,string message){if(!condition)throw new Exception(message);}
        static void Set(object o,string property,object value)=>o.GetType().GetField("<"+property+">k__BackingField",Flags).SetValue(o,value);
        static object Call(CanvasGameUI ui,string method,params object[] args)=>typeof(CanvasGameUI).GetMethod(method,Flags).Invoke(ui,args);
        static void Refresh(CanvasGameUI ui){typeof(CanvasGameUI).GetField("screenKey",Flags).SetValue(ui,"");Call(ui,"Refresh");Call(ui,"UpdateHud");}
        static void Shot(string name)
        {
            // Batch mode has no presented Game view. Render the same Canvas through the battle camera.
            var game=BattleGame.Instance;var canvas=game.UI.GetComponent<Canvas>();var camera=game.TankCamera;
            var target=RenderTexture.GetTemporary(1920,1080,24);var oldTarget=camera.targetTexture;var oldActive=RenderTexture.active;
            var oldMode=canvas.renderMode;var oldCamera=canvas.worldCamera;float oldPlane=canvas.planeDistance;
            var safe=game.UI.GetComponent<UICanvasLayout>().safeArea;var oldScale=safe.localScale;var oldPosition=safe.anchoredPosition;
            try
            {
                camera.targetTexture=target;canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;
                Canvas.ForceUpdateCanvases();safe.localScale=Vector3.one;safe.anchoredPosition=Vector2.zero;Canvas.ForceUpdateCanvases();camera.Render();
                RenderTexture.active=target;var texture=new Texture2D(1920,1080,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,1920,1080),0,0);texture.Apply();
                File.WriteAllBytes(directory+"/"+name+".png",texture.EncodeToPNG());Object.DestroyImmediate(texture);
            }
            finally{canvas.renderMode=oldMode;canvas.worldCamera=oldCamera;canvas.planeDistance=oldPlane;camera.targetTexture=oldTarget;RenderTexture.active=oldActive;safe.localScale=oldScale;safe.anchoredPosition=oldPosition;RenderTexture.ReleaseTemporary(target);}
        }
        static void Press(UIView v,string key){Require(v.Get<Button>(key).interactable,key+" must be interactable");v.Get<Button>(key).onClick.Invoke();}
        public static void Run()
        {
            try
            {
                UXImprovementsSetup.Install();Directory.CreateDirectory(directory);
                EditorSceneManager.OpenScene(MegaTanksSetup.ScenePath);
                EditorSettings.enterPlayModeOptionsEnabled=true;EditorSettings.enterPlayModeOptions=EnterPlayModeOptions.DisableDomainReload;
                EditorApplication.playModeStateChanged+=Entered;
                EditorApplication.isPlaying=true;
            }
            catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
        }
        static void Entered(PlayModeStateChange state)
        {
            if(state!=PlayModeStateChange.EnteredPlayMode)return;
            phase=0;errors=false;since=EditorApplication.timeSinceStartup;Application.logMessageReceived+=Log;EditorApplication.update+=Tick;
        }
        static void Stopped(PlayModeStateChange state)
        {
            if(state!=PlayModeStateChange.EnteredEditMode)return;
            EditorApplication.playModeStateChanged-=Stopped;EditorApplication.delayCall+=CanvasUIAudit.Run;
        }
        static void Log(string message,string trace,LogType type){if(type==LogType.Error||type==LogType.Exception)errors=true;}
        static void FitText(Text t)
        {
            Canvas.ForceUpdateCanvases();
            Require(t.preferredHeight<=t.rectTransform.rect.height+2,t.name+" text overflows: "+t.preferredHeight+" > "+t.rectTransform.rect.height+" ("+t.text+")");
        }
        static void Tick()
        {
            if(EditorApplication.timeSinceStartup-since<.8)return;since=EditorApplication.timeSinceStartup;
            try
            {
                var game=BattleGame.Instance;var ui=game.UI;var layout=ui.GetComponent<UICanvasLayout>();
                var home=layout.View(UIScreen.Home);var route=layout.View(UIScreen.Route);
                switch(phase++)
                {
                    case 0:
                        Require(home.Get<Text>("Equipment Caption").text=="GARAGE","Home needs readable navigation");
                        Require(home.Get<Text>("Crew Availability").text=="COMING SOON"&&home.Get<Text>("Tech Availability").text=="COMING SOON","Placeholders need availability badges");
                        Require(!home.Get<RectTransform>("Previous Stage").gameObject.activeSelf&&!home.Get<RectTransform>("Next Stage").gameObject.activeSelf,"One-stage arrows should be hidden");
                        Require(!home.Get<Image>("Stage Swipe Area").preserveAspect,"Stage background must cover its labels");
                        Shot("Home");break;
                    case 1:
                        ui.OpenEquipment();var equipment=layout.View(UIScreen.Equipment);
                        var selected=ScriptableObject.CreateInstance<GunDefinition>();selected.displayName="Tradeoff preview";selected.icon=game.Equipment.MachineGun.icon;
                        selected.stats=game.Equipment.MachineGun.stats.Copy();selected.stats.damage+=4;selected.stats.reloadTime+=.5f;selected.stats.range-=2;selected.stats.reserveCapacity-=10;
                        typeof(CanvasGameUI).GetField("selected",Flags).SetValue(ui,selected);Call(ui,"DrawEquipment");
                        string stats=equipment.Get<Text>("Item Stats").text;
                        Require(stats.Contains("EQUIPPED → SELECTED")&&stats.Contains("+0.5s")&&stats.Contains("-10"),"Comparison must show both benefits and costs");
                        FitText(equipment.Get<Text>("Item Stats"));Shot("GarageComparison");break;
                    case 2:
                        ui.ClosePopup();ui.Play();StageProgressionAudit.DisableWeapons(game);
                        Require(CanvasGameUI.RestoredAmmo(149,150,.33f)==1&&CanvasGameUI.RestoredAmmo(0,20,.33f)==7&&CanvasGameUI.RestoredAmmo(150,150,1)==0,"Preview must ceil and cap ammo gains");
                        Set(game.MachineGun,"Ammo",149);Set(game.MainGun,"Ammo",49);Set(game.Mortar,"Ammo",19);Set(game,"Hull",97f);
                        var supply=new CardDefinition{effect=CardEffect.Resupply,fraction=.33f};string preview=(string)Call(ui,"CardPreview",supply);
                        Require(preview.Contains("MG +1 · CANNON +1")&&preview.Contains("MORTAR +1"),"Ammo preview must use actual available space");
                        game.ApplyCardEffect(supply);Require(game.MachineGun.Ammo==150&&game.MainGun.Ammo==50&&game.Mortar.Ammo==20,"Preview must match applied resupply");
                        Require(((string)Call(ui,"CardPreview",supply)).Contains("no benefit"),"Full-ammo card must warn about waste");
                        var repair=new CardDefinition{effect=CardEffect.Repair,fraction=.33f};Require(((string)Call(ui,"CardPreview",repair)).Contains("HULL 97 → 100"),"Repair must cap at maximum");
                        game.ApplyCardEffect(repair);Require(game.Hull==100&&((string)Call(ui,"CardPreview",repair)).Contains("no benefit"),"Full repair must show no benefit");
                        Set(game.MachineGun,"Ammo",30);Refresh(ui);Require(layout.View(UIScreen.BattleHUD).Get<Text>("mg Status").text=="! LOW AMMO","20 percent must trigger low ammo");
                        Set(game.MachineGun,"Ammo",0);Refresh(ui);Require(layout.View(UIScreen.BattleHUD).Get<Text>("mg Status").text=="! EMPTY","Empty must use explicit text");
                        Require(layout.View(UIScreen.BattleHUD).Get<RectTransform>("mg Empty Alert").gameObject.activeSelf,"Going empty must show a brief alert");Shot("AmmoWarnings");break;
                    case 3:
                        StageProgressionAudit.ClearWave(game);Refresh(ui);Require(game.EnemiesDefeated>0,"Actual kills must be tracked");
                        var catalog=AssetDatabase.LoadAssetAtPath<CardCatalog>("Assets/MegaTanks/Data/Cards/CardCatalog.asset");
                        var offers=game.Rewards.Offers;offers[0].card=catalog.cards.First(c=>c.effect==CardEffect.Resupply&&c.rarity==CardRarity.Normal);
                        offers[1].card=catalog.cards.First(c=>c.effect==CardEffect.Repair&&c.rarity==CardRarity.Normal);
                        offers[2].card=catalog.cards.First(c=>c.effect==CardEffect.CannonDamage&&c.rarity==CardRarity.Normal);
                        Refresh(ui);var rewards=layout.View(UIScreen.Rewards);foreach(int i in new[]{0,1,2})FitText(rewards.Get<Text>("Card "+i+"/Effect"));Shot("RewardPreviews");break;
                    case 4:
                        Require(game.Rewards.ChooseReward(0),"Choose reward");Refresh(ui);previousNode=game.Stage.Current;nextId=previousNode.next[0];
                        var node=game.Stage.Route.Get(nextId);node.type=StageNodeType.HardBattle;Call(ui,"DrawMap");
                        Require(!route.Get<Button>("Enter Route Node").interactable,"Enter must be disabled before inspection");
                        Press(route.Get<ScrollRect>("Route Scroll").content.Cast<Transform>().First(t=>t.name=="Node "+nextId&&t.gameObject.activeSelf).GetComponent<UIView>(),"$root");
                        Require(game.Stage.Current==previousNode&&game.Stage.Mode==StageMode.Map,"Inspection must not commit the route");
                        Require(route.Get<Text>("Route Inspection").text.Contains("3 waves"),"Hard battle preview must explain reward timing");FitText(route.Get<Text>("Route Inspection"));FitText(route.Get<Text>("Route Resources"));Shot("RouteInspection");break;
                    case 5:
                        Press(route,"Enter Route Node");Require(game.Stage.Current.id==nextId&&game.Stage.Mode==StageMode.Combat&&game.Stage.EncounterWaves==3,"Enter must commit once");
                        Require(!game.Stage.SelectNode(nextId),"Repeated entry must be rejected");StageProgressionAudit.DisableWeapons(game);
                        Set(game,"Hull",0f);Refresh(ui);var defeat=layout.View(UIScreen.Defeat);
                        Require(defeat.Get<Text>("Result Detail").text.Contains("ENEMIES DEFEATED")&&defeat.Get<Text>("Result Detail").text.Contains("LOADOUT"),"Defeat report must show actual run and loadout");FitText(defeat.Get<Text>("Result Detail"));Shot("DefeatReport");break;
                    case 6:
                        Press(layout.View(UIScreen.Defeat),"Result Garage");Require(!game.Deployed&&ui.Popup=="Equipment","Defeat Garage must safely end the run and open equipment");
                        ui.ClosePopup();ui.Play();Require(game.EnemiesDefeated==0&&game.Hull==game.MaxHull&&game.MachineGun.Ammo==game.MachineGun.ReserveCapacity,"Retry must reset run data");
                        Refresh(ui);Require(!layout.View(UIScreen.BattleHUD).Get<RectTransform>("mg Empty Alert").gameObject.activeSelf,"New runs must clear prior ammo alerts");
                        StageProgressionAudit.DisableWeapons(game);game.Rewards.OpenForWave(5);Refresh(ui);
                        var shop=layout.View(UIScreen.Shop);foreach(int i in Enumerable.Range(0,game.Rewards.Offers.Length))FitText(shop.Get<Text>("Card "+i+"/Effect"));Shot("ShopPreviews");break;
                    case 7:
                        game.Rewards.AddScraps(10000);Require(game.Rewards.Buy(0),"Shop purchase");Refresh(ui);
                        Require(layout.View(UIScreen.Shop).Get<Text>("Card 0/Effect").text.EndsWith("APPLIED"),"Purchased cards must show applied rather than another prospective gain");
                        game.Rewards.LeaveShop();StageProgressionAudit.ClearWave(game);game.Rewards.ChooseReward(0);
                        StageProgressionAudit.EnterType(game,StageNodeType.Supply);Refresh(ui);
                        var service=layout.View(UIScreen.Service);FitText(service.Get<Text>("Card 0/Effect"));FitText(service.Get<Text>("Card 1/Effect"));Shot("ServicePreviews");break;
                    case 8:
                        ui.OpenPopup("Settings");Shot("Settings");ui.ClosePopup();
                        game.Stage.LeaveService();Set(game.Stage,"Mode",StageMode.Combat);game.SetPaused(true);Refresh(ui);Shot("Pause");game.SetPaused(false);
                        Set(game.Stage,"Mode",StageMode.Victory);Refresh(ui);Shot("Victory");
                        Require(!errors,"No Unity errors during UX audit");Debug.Log("UX_IMPROVEMENTS_PASS: Home, comparisons, capped previews, warnings, route inspection/commit, defeat and reset.");
                        File.WriteAllText(directory+"/result.txt","PASS: Home navigation; comparison tradeoffs; ammo ceil/caps; repair caps; no-benefit states; low/empty alerts; reward text fit; route inspection/commit; defeat report; Garage and restart.\n");
                        EditorApplication.update-=Tick;Application.logMessageReceived-=Log;EditorApplication.playModeStateChanged-=Entered;
                        EditorApplication.playModeStateChanged+=Stopped;EditorApplication.isPlaying=false;break;
                }
            }
            catch(Exception e){Debug.LogException(e);EditorApplication.update-=Tick;EditorApplication.Exit(1);}
        }
    }
}
