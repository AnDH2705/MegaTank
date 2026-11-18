using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object=UnityEngine.Object;

namespace MegaTanks.Editor
{
    public static class ItemProgressionAudit
    {
        static void Require(bool ok,string message){if(!ok)throw new Exception(message);}
        static void Near(float a,float b,string message)=>Require(Mathf.Abs(a-b)<.001f,message+" ("+a+" vs "+b+")");
        static readonly string[] keys={PlaytestResources.ArmorSaveKey,"MegaTanks.Equipment.Armor",PlaytestResources.SaveKey,PlayerWallet.DefaultSaveKey,PlayerProgression.DefaultSaveKey,"MegaTanks.Crew.Loadout","MegaTanks.Equipment.MachineGun","MegaTanks.Equipment.Cannon","MegaTanks.Equipment.Mortar"};
        static readonly Dictionary<string,string> saved=new Dictionary<string,string>();
        static readonly HashSet<string> existed=new HashSet<string>();
        static int phase;static double since;static bool errors,oldOptions;static EnterPlayModeOptions options;
        static WeaponDefinition weapon;static CrewDefinition member;
        static void ModelChecks(FrontendCatalog catalog)
        {
            string walletKey="MegaTanks.Audit.Wallet."+Guid.NewGuid(),progressKey="MegaTanks.Audit.Progress."+Guid.NewGuid();
            try
            {
                var wallet=new PlayerWallet(walletKey);var model=new PlayerProgression(catalog,wallet,progressKey);
                var weapons=catalog.inventory.OfType<WeaponDefinition>().ToArray();var first=weapons[0];var second=weapons[1];var crew=catalog.crew.members[0];
                Require(ProgressionRules.LevelCost(1)==100&&ProgressionRules.LevelCost(2)==110&&ProgressionRules.LevelCost(3)==121&&ProgressionRules.LevelCost(4)==134,"Whole-number costs round upward.");
                Require(ProgressionRules.LevelCost(int.MaxValue)==-1,"Unrepresentable costs cannot wrap.");
                Require(!model.TryLevelUp(first)&&!model.TryStarUp(first),"No free upgrades.");
                wallet.TryAdd(CurrencyType.Parts,100);
                Require(model.TryLevelUp(first)&&wallet.Parts==0&&model.Get(first).Level==2,"Exact-cost level purchase.");
                Require(model.Get(second).Level==1&&model.Get(crew).Level==1,"Independent levels.");
                Near(model.Damage(first),PlayerProgression.BaseDamage(first)+1,"Level adds exactly one.");
                Require(!model.TryGrantFragments(first,0)&&!model.TryGrantFragments(first,-1),"Reject invalid grants.");
                Require(model.TryGrantFragments(first,19)&&!model.TryStarUp(first),"Require enough blueprints.");
                Require(model.TryGrantFragments(first,2)&&model.TryStarUp(first)&&model.Get(first).Fragments==1,"Consume 20 and retain leftovers.");
                Near(model.Damage(first),(PlayerProgression.BaseDamage(first)+1)*1.1f,"Stars apply after level bonus.");
                Require(!model.TryStarUp(second)&&!model.TryStarUp(crew),"Materials are item-specific.");
                model.TryGrantFragments(first,5000);
                for(int s=1;s<20;s++)
                {
                    int cost=s<5?20:s<10?40:s<15?70:100;
                    int before=model.Get(first).Fragments;
                    Require(ProgressionRules.StarCost(s)==cost&&model.TryStarUp(first)&&model.Get(first).Fragments==before-cost,"All star bands charge correctly.");
                }
                Require(!model.TryStarUp(first)&&model.Get(first).Stars==20,"Star cap is enforced.");
                var reloaded=new PlayerProgression(catalog,new PlayerWallet(walletKey),progressKey);
                Require(reloaded.Get(first).Stars==20&&reloaded.Get(first).Level==2&&reloaded.Get(first).Fragments==model.Get(first).Fragments,"Save and reload all progression fields.");
                Require(wallet.TryAdd(CurrencyType.PetFood,100)&&model.TryLevelUp(crew)&&wallet.PetFood==0,"Crew uses Pet Food.");
                model.TryGrantFragments(crew,20);Require(model.TryStarUp(crew),"Crew uses its own Pet Tags.");
                Near(model.Damage(crew),crew.stats.damage.AtLevel(model.Get(crew).Level,model.Get(crew).Stars),"Crew uses its authored per-stat level gain.");
                var outsider=ScriptableObject.CreateInstance<GunDefinition>();outsider.id=first.id;
                Require(!model.TryGrantFragments(outsider,20)&&!model.TryLevelUp(outsider),"Reject noncatalog definitions.");
                Object.DestroyImmediate(outsider);
                foreach(int stars in new[]{0,1,5,6,10,11,15,16,20})
                    Require(UIStarStrip.VisibleCount(stars)==(stars==0?0:(stars-1)%5+1),"Compact star counts.");
                var added=ScriptableObject.CreateInstance<GunDefinition>();added.id="audit-new-"+Guid.NewGuid();added.stats.damage=25;
                var expanded=Object.Instantiate(catalog);expanded.inventory=catalog.inventory.Concat(new EquipmentDefinition[]{added}).ToArray();
                var renamed=Object.Instantiate(first);renamed.name="Renamed weapon";
                try
                {
                    var more=new PlayerProgression(expanded,wallet,progressKey);
                    Require(more.TryGrantFragments(added,20)&&more.TryStarUp(added),"New weapon works by adding catalog data alone.");
                    Near(more.Damage(added),27.5f,"New content uses shared damage rules.");
                    expanded.inventory=expanded.inventory.Select(i=>i==first?renamed:i).Reverse().ToArray();
                    var reordered=new PlayerProgression(expanded,wallet,progressKey);
                    Require(reordered.Get(renamed).Stars==20&&reordered.Get(renamed).Level==2,"Renaming and catalog reordering preserve identity.");
                    expanded.inventory=expanded.inventory.Concat(new EquipmentDefinition[]{first}).ToArray();
                    bool duplicate=false;try{new PlayerProgression(expanded,wallet,progressKey);}catch(InvalidOperationException){duplicate=true;}
                    Require(duplicate,"Duplicate IDs fail clearly.");
                }
                finally{Object.DestroyImmediate(added);Object.DestroyImmediate(renamed);Object.DestroyImmediate(expanded);}

                string id="weapon:"+first.id;
                PlayerPrefs.SetString(progressKey,"{\"entries\":[{\"id\":\""+id+"\",\"level\":-2,\"stars\":99,\"fragments\":-8}]}");
                var sanitized=new PlayerProgression(catalog,wallet,progressKey).Get(first);
                Require(sanitized.Level==1&&sanitized.Stars==20&&sanitized.Fragments==0,"Sanitize invalid saved values.");
                PlayerPrefs.SetString(progressKey,"bad json");Require(new PlayerProgression(catalog,wallet,progressKey).Get(first).Level==1,"Malformed save recovery.");
                Debug.Log("ITEM_PROGRESSION_MODEL_PASS");
            }
            finally{PlayerPrefs.DeleteKey(walletKey);PlayerPrefs.DeleteKey(progressKey);PlayerPrefs.Save();}
        }
        public static void Run()
        {
            try
            {
                ProgressionSetup.Install();ProgressionSetup.Install();
                var catalog=AssetDatabase.LoadAssetAtPath<FrontendCatalog>("Assets/MegaTanks/Data/UI/Frontend.asset");
                ModelChecks(catalog);
                foreach(string key in keys){if(PlayerPrefs.HasKey(key))existed.Add(key);saved[key]=PlayerPrefs.GetString(key,"");PlayerPrefs.DeleteKey(key);}
                var wallet=new PlayerWallet();wallet.TryAdd(CurrencyType.Parts,10000);wallet.TryAdd(CurrencyType.PetFood,10000);
                PlayerPrefs.SetInt(PlaytestResources.SaveKey,1);PlayerPrefs.SetInt(PlaytestResources.ArmorSaveKey,1);
                PlayerPrefs.SetString("MegaTanks.Equipment.MachineGun", "MachineGun");
                PlayerPrefs.Save();
                EditorSceneManager.OpenScene(MegaTanksSetup.ScenePath);
                oldOptions=EditorSettings.enterPlayModeOptionsEnabled;options=EditorSettings.enterPlayModeOptions;
                EditorSettings.enterPlayModeOptionsEnabled=true;EditorSettings.enterPlayModeOptions=EnterPlayModeOptions.DisableDomainReload;
                EditorApplication.playModeStateChanged+=State;
                EditorApplication.isPlaying=true;
            }
            catch(Exception e){Debug.LogException(e);Finish(1);}
        }
        static void State(PlayModeStateChange change)
        {
            if(change==PlayModeStateChange.EnteredPlayMode){phase=0;since=EditorApplication.timeSinceStartup;Application.logMessageReceived+=Log;EditorApplication.update+=Tick;}
        }
        static void Log(string message,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception)errors=true;}
        internal static void Press(Button button)
        {
            Require(button.interactable,"Button enabled: "+button.name);Canvas.ForceUpdateCanvases();
            Debug.Log("PRESS "+button.name+" active="+button.gameObject.activeInHierarchy+" raycast="+button.image.raycastTarget);
            var rect=(RectTransform)button.transform;
            var data=new PointerEventData(EventSystem.current){position=RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(rect.rect.center)),button=PointerEventData.InputButton.Left};
            var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(data,hits);
            Require(hits.Count>0&&hits[0].gameObject==button.gameObject,"Pointer must hit "+button.name+"; hit "+(hits.Count>0?hits[0].gameObject.name:"nothing"));
            ExecuteEvents.Execute(button.gameObject,data,ExecuteEvents.pointerClickHandler);
        }
        static void Tick()
        {
            if(EditorApplication.timeSinceStartup-since<.7)return;
            since=EditorApplication.timeSinceStartup;
            try
            {
                var game=BattleGame.Instance;Require(game!=null&&game.UI!=null,"Game starts with progression UI.");
                var layout=game.UI.GetComponent<UICanvasLayout>();var garage=layout.View(UIScreen.Equipment);var crew=layout.View(UIScreen.Crew);
                CurrencyAudit.CheckCounters(game);
                switch(phase++)
                {
                    case 0: Capture(game,"Currency-Home"); Require(PlayerPrefs.GetString("MegaTanks.Equipment.MachineGun")==game.Equipment.MachineGun.id,"Legacy loadout save migrates to ID."); game.UI.OpenEquipment(); garage.Get<Button>("Slot MG").onClick.Invoke(); break;
                    case 1:
                        weapon=game.Equipment.MachineGun;member=game.Frontend.crew.members[0];
                        Press(garage.Get<ScrollRect>("Inventory").content.GetComponentsInChildren<Button>().Single(b=>b.name=="Item "+weapon.name));
                        break;
                    case 2:
                        Require(!garage.Get<Button>("Star Up").interactable,"Missing blueprints disable Star Up.");
                        game.Progression.TryGrantFragments(weapon,2000);Press(garage.Get<Button>("Level Up"));Press(garage.Get<Button>("Star Up"));
                        Require(game.Progression.Get(weapon).Level==2&&game.Progression.Get(weapon).Stars==1,"Real Garage buttons upgrade.");
                        Capture(game,"Garage-Yellow");break;
                    case 3:
                        while(game.Progression.Get(weapon).Stars<6)Require(game.Progression.TryStarUp(weapon),"Advance to purple tier.");
                        Require(garage.Get<UIStarStrip>("Detail Stars").Stars==6,"Detail stars refresh.");
                        Capture(game,"Garage-Purple");break;
                    case 4:
                        while(game.Progression.Get(weapon).Stars<11)Require(game.Progression.TryStarUp(weapon),"Advance to red tier.");
                        Capture(game,"Garage-Red");break;
                    case 5:
                        while(game.Progression.Get(weapon).Stars<16)Require(game.Progression.TryStarUp(weapon),"Advance to rainbow tier.");
                        Capture(game,"Garage-Rainbow");
                        while(game.Progression.Get(weapon).Stars<20)Require(game.Progression.TryStarUp(weapon),"Reach cap.");
                        Require(!garage.Get<Button>("Star Up").interactable&&garage.Get<UIStarStrip>("Detail Stars").Stars==20,"Max-star controls disable and show five rainbow stars.");
                        Capture(game,"Garage-Max");break;
                    case 6: game.UI.OpenPopup("Crew"); break;
                    case 7:
                        Press(crew.Get<ScrollRect>("Inventory").content.GetComponentsInChildren<Button>().Single(b=>b.name=="Crew "+member.id));
                        break;
                    case 8:
                        game.Progression.TryGrantFragments(member,2000);Press(crew.Get<Button>("Level Up"));Press(crew.Get<Button>("Star Up"));Press(crew.Get<Button>("Assign Crew"));
                        Require(game.Crew.IsEquipped(member)&&game.Progression.Get(member).Level==2&&game.Progression.Get(member).Stars==1,"Crew assignment and upgrades coexist.");
                        Capture(game,"Crew-Yellow");break;
                    case 9:
                        while(game.Progression.Get(member).Stars<16)Require(game.Progression.TryStarUp(member),"Crew rainbow tier.");
                        Capture(game,"Crew-Rainbow");
                        while(game.Progression.Get(member).Stars<20)Require(game.Progression.TryStarUp(member),"Crew reaches cap.");
                        Require(!crew.Get<Button>("Star Up").interactable,"Crew cap disables star purchase.");
                        Capture(game,"Crew-Max");
                        game.UI.ClosePopup();game.StartBattle();
                        float expected=game.Progression.Damage(weapon)+game.Progression.CrewDamage(game.Crew);
                        Near(game.MachineGun.Stats.damage,expected,"Battle gets permanent weapon and assigned crew damage.");
                        Near(game.Mortar.Stats.damage,game.Progression.Damage(game.Equipment.Mortar)+game.Progression.CrewDamage(game.Crew),"Mortar uses same progression pipeline.");
                        game.ReturnHome();game.StartBattle();Near(game.MachineGun.Stats.damage,expected,"Retry never compounds progression.");
                        Require(!errors,"No runtime errors.");
                        Debug.Log("ITEM_PROGRESSION_PLAY_PASS");Finish(0);break;
                }
            }
            catch(Exception e){Debug.LogException(e);Finish(1);}
        }
        internal static void Capture(BattleGame game,string name)
        {
            var canvas=game.UI.GetComponent<Canvas>();var camera=game.TankCamera;var target=RenderTexture.GetTemporary(1920,1080,24);
            var previous=camera.targetTexture;var active=RenderTexture.active;var mode=canvas.renderMode;var worldCamera=canvas.worldCamera;float plane=canvas.planeDistance;
            var safe=game.UI.GetComponent<UICanvasLayout>().safeArea;var scale=safe.localScale;var position=safe.anchoredPosition;
            try
            {
                camera.targetTexture=target;canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;
                Canvas.ForceUpdateCanvases();safe.localScale=Vector3.one;safe.anchoredPosition=Vector2.zero;Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=target;
                var texture=new Texture2D(1920,1080,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,1920,1080),0,0);texture.Apply();
                Directory.CreateDirectory("Checks/ItemProgression");File.WriteAllBytes("Checks/ItemProgression/"+name+".png",texture.EncodeToPNG());Object.DestroyImmediate(texture);
            }
            finally{camera.targetTexture=previous;RenderTexture.active=active;canvas.renderMode=mode;canvas.worldCamera=worldCamera;canvas.planeDistance=plane;safe.localScale=scale;safe.anchoredPosition=position;RenderTexture.ReleaseTemporary(target);}
        }
        static void Finish(int code)
        {
            EditorApplication.update-=Tick;Application.logMessageReceived-=Log;EditorApplication.playModeStateChanged-=State;
            foreach(var pair in saved){if(existed.Contains(pair.Key))PlayerPrefs.SetString(pair.Key,pair.Value);else PlayerPrefs.DeleteKey(pair.Key);}
            PlayerPrefs.Save();EditorSettings.enterPlayModeOptionsEnabled=oldOptions;EditorSettings.enterPlayModeOptions=options;EditorApplication.Exit(code);
        }
    }
}
