using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object=UnityEngine.Object;

namespace MegaTanks.Editor
{
    public static class LoadoutArtworkAudit
    {
        static readonly string[] Keys={BattleGame.SelectedTankSaveKey,"MegaTanks.Crew.Loadout","MegaTanks.Equipment.Armor","MegaTanks.Equipment.Armor.1","MegaTanks.Equipment.MachineGun","MegaTanks.Equipment.Cannon","MegaTanks.Equipment.Mortar","MegaTanks.Accessory.1","MegaTanks.Accessory.2"};
        static readonly Dictionary<string,string> saves=new Dictionary<string,string>();
        static StageRewardSaveScope scope;
        static int phase,crewAction,oldLevel,oldStars;static double since;static bool errors,oldOptions;static EnterPlayModeOptions options;
        static EquipmentDefinition item;static GunDefinition mg,gatling;
        static void Require(bool value,string message){if(!value)throw new Exception(message);}
        public static void Run()
        {
            try
            {
                foreach(string key in Keys)if(PlayerPrefs.HasKey(key))saves[key]=PlayerPrefs.GetString(key);
                scope=new StageRewardSaveScope();PlayerPrefs.DeleteKey("MegaTanks.Crew.Loadout");PlayerPrefs.DeleteKey(PlayerProgression.DefaultSaveKey);PlayerPrefs.SetString(BattleGame.SelectedTankSaveKey,"tank.medium");
                EditorSceneManager.OpenScene(MegaTanksSetup.ScenePath);oldOptions=EditorSettings.enterPlayModeOptionsEnabled;options=EditorSettings.enterPlayModeOptions;
                EditorSettings.enterPlayModeOptionsEnabled=true;EditorSettings.enterPlayModeOptions=EnterPlayModeOptions.DisableDomainReload;Application.logMessageReceived+=Log;EditorApplication.playModeStateChanged+=Entered;EditorApplication.isPlaying=true;
            }
            catch(Exception e){Debug.LogException(e);Finish(1);}
        }
        static void Log(string text,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception)errors=true;}
        static void Entered(PlayModeStateChange state){if(state!=PlayModeStateChange.EnteredPlayMode)return;phase=0;since=EditorApplication.timeSinceStartup;EditorApplication.update+=Tick;}
        static void Press(Button button)
        {
            Require(button.interactable&&button.gameObject.activeInHierarchy,button.name+" must be usable.");Canvas.ForceUpdateCanvases();var rect=(RectTransform)button.transform;
            var pointer=new PointerEventData(EventSystem.current){position=RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(rect.rect.center)),button=PointerEventData.InputButton.Left};
            var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(pointer,hits);Require(hits.Count>0&&hits[0].gameObject==button.gameObject,"Pointer must reach "+button.name+", hit "+(hits.Count==0?"nothing":hits[0].gameObject.name));ExecuteEvents.Execute(button.gameObject,pointer,ExecuteEvents.pointerClickHandler);
        }
        static void Press(UIView view,string key)=>Press(view.Get<Button>(key));
        static void Select(UIView view,string prefix,string name)
        {
            var scroll=view.Get<ScrollRect>("Inventory");scroll.verticalNormalizedPosition=1;Canvas.ForceUpdateCanvases();Press(scroll.content.GetComponentsInChildren<UIView>().Single(c=>c.gameObject.activeSelf&&c.name==prefix+name).Get<Button>("$root"));
        }
        static void SelectItem(UIView view,EquipmentDefinition selected){Select(view,"Item ",selected.name);item=selected;}
        static void CheckStatIcons(UIView view)
        {
            var art=view.GetComponent<LoadoutArtPresentation>();if(art.equipmentStats==null)return;
            Require(art.equipmentStats.VisibleStatCount>0&&art.equipmentStats.MissingIconCount==0,"Every displayed equipment stat needs its corresponding sprite.");
            var rows=EquipmentStatIcons.Parse(item,art.statsSource.text);
            Require(rows.FindAll(r=>r.icon!=null).Count==art.equipmentStats.VisibleStatCount,"Icon rows retain every source stat.");
            Require(!art.statsText.enabled,"The old paragraph must not overlap the icon rows.");
            if(art.statsScroll.content.rect.height>art.statsScroll.viewport.rect.height)
            {
                float before=art.statsScroll.content.anchoredPosition.y;
                var pointer=new PointerEventData(EventSystem.current){scrollDelta=new Vector2(0,-5)};
                ExecuteEvents.Execute(art.statsScroll.gameObject,pointer,ExecuteEvents.scrollHandler);Canvas.ForceUpdateCanvases();
                Require(art.statsScroll.content.anchoredPosition.y>before,"Equipment icon stats remain scrollable.");
                if(item is WeaponDefinition)
                {
                    art.statsScroll.verticalNormalizedPosition=.5f;Capture(BattleGame.Instance,"Equipment-StatIcons-Middle");
                    art.statsScroll.verticalNormalizedPosition=0;Capture(BattleGame.Instance,"Equipment-StatIcons-Lower");
                }
                art.statsScroll.verticalNormalizedPosition=1;
            }
        }
        static void CrewStep(BattleGame game,UIView v)
        {
            int role=crewAction/7,step=crewAction%7;var member=game.Frontend.crew.members.Single(c=>(int)c.role==role);var art=v.GetComponent<LoadoutArtPresentation>();
            switch(step)
            {
                case 0:Press(v,"Tank Preview/Tank Slot "+role);Require(game.UI.CrewInventoryVisibleCount==game.Frontend.crew.members.Count(c=>game.Crew.CanEquip(c,role)),"Crew station filters by role.");break;
                case 1:Select(v,"Crew ",member.id);Require(v.Get<Outline>("Tank Preview/Tank Slot "+role).enabled,"Matching station highlights.");break;
                case 2:Press(art.statButtons[0]);Require(art.info.activeSelf&&!string.IsNullOrWhiteSpace(art.infoText.text),"Stat buttons open live info.");break;
                case 3:Press(art.infoClose);Require(!art.info.activeSelf,"Info Back closes the panel.");break;
                case 4:Press(art.skillButtons[2]);Require(art.info.activeSelf&&!string.IsNullOrWhiteSpace(art.infoText.text),"Role skill buttons open actual modifier information.");break;
                case 5:Press(art.infoClose);Require(game.Crew.Get(role)==null,"Preview and info never assign crew.");break;
                case 6:Press(v,"Tank Preview/Tank Slot "+role);Require(game.Crew.Get(role)==member,"Highlighted station click assigns matching crew.");break;
            }
            crewAction++;
        }
        static void Tick()
        {
            if(EditorApplication.timeSinceStartup-since<.5)return;since=EditorApplication.timeSinceStartup;
            try
            {
                var game=BattleGame.Instance;var ui=game.UI;var layout=ui.GetComponent<UICanvasLayout>();var crew=layout.View(UIScreen.Crew);var garage=layout.View(UIScreen.Equipment);
                switch(phase++)
                {
                    case 0:Require(layout.crewInventoryItem!=layout.equipmentInventoryItem,"Crew and equipment own separate card templates.");Press(layout.View(UIScreen.Home),"Crew");break;
                    case 1:
                        if(crewAction<35){CrewStep(game,crew);phase--;return;}
                        Require(game.Crew.AssignedCount==5&&new PlayerCrew(game.Frontend.crew).AssignedCount==5,"All crew assignments save.");Press(crew,"Inventory Filter");break;
                    case 2:Require(ui.CrewInventoryVisibleCount==5,"Clearing station filter restores all crew.");Press(crew,"Sort Direction");break;
                    case 3:Select(crew,"Crew ","milo");break;
                    case 4:
                        foreach(var cell in crew.Get<ScrollRect>("Inventory").content.GetComponentsInChildren<UIView>())
                        {Require(cell.Get<Image>("Equipped Indicator").gameObject.activeSelf,"Assigned crew keep their ticks independently of preview.");Require(cell.transform.Find("Selection").gameObject.activeSelf==(cell.name=="Crew milo"),"Only previewed crew gets green selection frame.");}
                        game.Wallet.TryAdd(CurrencyType.PetFood,1000);game.Progression.TryGrantFragments(game.Frontend.crew.members.Single(c=>c.id=="milo"),20);Press(crew,"Level Up");break;
                    case 5:Press(crew,"Star Up");break;
                    case 6:
                        var milo=game.Frontend.crew.members.Single(c=>c.id=="milo");Require(game.Progression.Get(milo).Level==2&&game.Progression.Get(milo).Stars==1,"Crew level and star actions remain separate.");Capture(game,"Crew");Press(crew,"Unequip Crew");break;
                    case 7:Require(game.Crew.AssignedCount==4,"Unequip works.");Press(crew,"Assign Crew");break;
                    case 8:Require(game.Crew.AssignedCount==5,"Equip button remains available.");Press(crew,"Change Tank");break;
                    case 9:
                        Require(ui.Popup=="Equipment"&&game.Crew.AssignedCount==5,"Change Tank opens Garage without changing crew.");
                        mg=game.Frontend.inventory.OfType<GunDefinition>().Single(g=>g.name=="MachineGun");gatling=game.Frontend.inventory.OfType<GunDefinition>().Single(g=>g.name=="GatlingGun");
                        Require(ui.InventoryVisibleCount==game.Frontend.inventory.Count(i=>i!=null),"Garage opens all equipment.");
                        foreach(string key in garage.GetComponent<LoadoutArtPresentation>().slotKeys){var icon=garage.Get<Image>(key+"/Equipped Item Icon");Require(icon.transform.GetSiblingIndex()>icon.transform.parent.Find("Slot Frame").GetSiblingIndex(),"Slot frames cannot cover equipped artwork.");}
                        Press(garage,"Sort Direction");break;
                    case 10:Press(garage,"Inventory Sort");break;
                    case 11:Press(garage,"Slot MG");break;
                    case 12:SelectItem(garage,game.Equipment.MachineGun==gatling?mg:gatling);break;
                    case 13:
                        CheckStatIcons(garage);
                        Require(garage.Get<Text>("Item Stats").text.Contains("EQUIPPED → SELECTED"),"Weapon comparisons remain intact.");
                        var art=garage.GetComponent<LoadoutArtPresentation>();Require(art.statsText.rectTransform.rect.height>=art.statsText.preferredHeight,"Full comparison is scrollable.");Capture(game,"Equipment-Comparison");Press(garage,"Equip Item");break;
                    case 14:Require(game.Equipment.MachineGun==item,"Equip changes the selected MG.");SelectItem(garage,mg);break;
                    case 15:Press(garage,"Equip Item");break;
                    case 16:Require(game.Equipment.MachineGun==mg&&garage.Get<RawImage>("Tank Avatar").texture!=null,"Restored MG and live portrait remain available.");Press(garage,"Slot Cannon");break;
                    case 17:SelectItem(garage,game.Frontend.inventory.First(i=>i.Type==EquipmentType.Cannon));break;
                    case 18:oldLevel=game.Progression.Get(item).Level;oldStars=game.Progression.Get(item).Stars;game.Wallet.TryAdd(CurrencyType.Parts,10000);game.Progression.TryGrantFragments(item,20);Press(garage,"Level Up");break;
                    case 19:Require(game.Progression.Get(item).Level==oldLevel+1,"Equipment Level Up applies.");Press(garage,"Star Up");break;
                    case 20:Require(game.Progression.Get(item).Stars==oldStars+1,"Item-specific blueprints still ascend equipment.");Capture(game,"Equipment");Press(garage,"Slot Armor");break;
                    case 21:SelectItem(garage,game.Frontend.inventory.First(i=>i.Type==EquipmentType.Armor));break;
                    case 22:CheckStatIcons(garage);Require(garage.Get<Text>("Item Stats").text.Contains("TANK TOTALS"),"Armor comparisons remain intact.");Capture(game,"Equipment-Armor-Icons");Press(garage,"Equip Item");break;
                    case 23:Press(garage,"Slot Accessory 2");break;
                    case 24:SelectItem(garage,game.Frontend.inventory.First(i=>i.Type==EquipmentType.Accessory));break;
                    case 25:CheckStatIcons(garage);Require(!garage.Get<Button>("Star Up").gameObject.activeSelf,"Accessories remain level-only.");Capture(game,"Equipment-Accessory-Icons");Press(garage,"Equip Item");break;
                    case 26:Press(garage,"Tank Selection");break;
                    case 27:Require(game.TankProfile.ArmorSlots==2&&garage.Get<Text>("Slot Accessory 3/Slot Description").text.StartsWith("Armor 2"),"Heavy tank exposes independent second armor slot.");Press(garage,"Slot Accessory 3");break;
                    case 28:SelectItem(garage,game.Frontend.inventory.First(i=>i.Type==EquipmentType.Armor&&i!=game.Equipment.Armor));break;
                    case 29:Press(garage,"Equip Item");break;
                    case 30:Require(game.Equipment.SecondArmor==item,"Heavy second armor equips independently.");Capture(game,"Equipment-Heavy");Press(garage,"Inventory Filter");break;
                    case 31:Press(garage,"Filter List/Filter All");break;
                    case 32:Require(ui.InventoryVisibleCount==game.Frontend.inventory.Count(i=>i!=null),"Filter menu restores all equipment.");Capture(game,"Equipment-All");ui.ClosePopup();ui.OpenEquipment();break;
                    case 33:Require(game.Equipment.SecondArmor==item&&game.Crew.AssignedCount==5,"Reopening retains both loadouts.");Require(!errors,"No runtime errors permitted.");Debug.Log("LOADOUT_ARTWORK_PASS: crew filtering, matching highlights, independent preview/ticks, assignment, info buttons, upgrades, saved loadouts; Garage real pointer sorting/filtering, comparison scrolling, weapon/armor/accessory equip, MG restoration, upgrades, tank switching and second heavy armor.");Finish(0);break;
                }
            }
            catch(Exception e){Debug.LogException(e);Finish(1);}
        }
        static void Capture(BattleGame game,string name)
        {
            var canvas=game.UI.GetComponent<Canvas>();var camera=game.TankCamera;var target=RenderTexture.GetTemporary(1920,1080,24);var prev=camera.targetTexture;var active=RenderTexture.active;var mode=canvas.renderMode;var world=canvas.worldCamera;float plane=canvas.planeDistance;
            var safe=game.UI.GetComponent<UICanvasLayout>().safeArea;var scale=safe.localScale;var position=safe.anchoredPosition;
            try{camera.targetTexture=target;canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;safe.localScale=Vector3.one;safe.anchoredPosition=Vector2.zero;Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=target;var image=new Texture2D(1920,1080,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1920,1080),0,0);image.Apply();Directory.CreateDirectory("Checks/LoadoutArtwork");File.WriteAllBytes("Checks/LoadoutArtwork/"+name+".png",image.EncodeToPNG());Object.DestroyImmediate(image);}
            finally{camera.targetTexture=prev;RenderTexture.active=active;canvas.renderMode=mode;canvas.worldCamera=world;canvas.planeDistance=plane;safe.localScale=scale;safe.anchoredPosition=position;RenderTexture.ReleaseTemporary(target);}
        }
        static void Finish(int code)
        {
            foreach(string key in Keys){if(saves.TryGetValue(key,out var value))PlayerPrefs.SetString(key,value);else PlayerPrefs.DeleteKey(key);}scope?.Dispose();PlayerPrefs.Save();EditorApplication.update-=Tick;EditorApplication.playModeStateChanged-=Entered;Application.logMessageReceived-=Log;EditorSettings.enterPlayModeOptionsEnabled=oldOptions;EditorSettings.enterPlayModeOptions=options;EditorApplication.Exit(code);
        }
    }
}
