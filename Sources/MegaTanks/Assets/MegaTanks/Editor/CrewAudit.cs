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
    public static class CrewAudit
    {
        const string SaveKey="MegaTanks.Crew.Loadout";
        static bool hadSave,errors,interactionsOnly;static string saved;static int phase,assignAction,moveAction;static double since;
        static Vector2 slotPosition;static byte[] portraitPng;
        static void Require(bool value,string message){if(!value)throw new Exception(message);}
        static void ModelChecks(CrewCatalog catalog)
        {
            string key="MegaTanks.Crew.Audit."+Guid.NewGuid();var model=new PlayerCrew(catalog,key);
            try
            {
                Require(catalog.members.Length==5&&catalog.members.Select(m=>m.id).Distinct().Count()==5&&catalog.members.All(m=>m.portrait!=null),"Five unique crew definitions need actual portraits.");
                Require(model.AssignedCount==0,"Fresh crew loadout starts empty.");
                for(int i=0;i<5;i++)Require(model.Equip(catalog.members[i],i),"Each sample crew member can be assigned.");
                var loaded=new PlayerCrew(catalog,key);Require(loaded.AssignedCount==5&&Enumerable.Range(0,5).All(i=>loaded.Get(i)==catalog.members[i]),"Assignments persist using stable IDs.");
                Require(!loaded.Equip(catalog.members[0],1)&&loaded.Get(0)==catalog.members[0]&&loaded.Get(1)==catalog.members[1]&&loaded.AssignedCount==5,"Roles prevent moving a member into another station.");
                var reordered=Object.Instantiate(catalog);reordered.members=reordered.members.Reverse().ToArray();Require(new PlayerCrew(reordered,key).Get(0)==catalog.members[0],"Catalog sorting cannot change saved identities.");Object.DestroyImmediate(reordered);
                Require(loaded.Unequip(1)&&!loaded.Unequip(1)&&!loaded.Equip(null,0)&&!loaded.Equip(catalog.members[0],-1)&&!loaded.Equip(catalog.members[0],5),"Unequip and invalid assignment guards.");
                var outsider=ScriptableObject.CreateInstance<CrewDefinition>();outsider.id=catalog.members[0].id;Require(!loaded.Equip(outsider,0),"Only owned catalog members may be assigned.");Object.DestroyImmediate(outsider);
                PlayerPrefs.SetString(key,"{\"memberIds\":[\"milo\",\"milo\",\"unknown\",\"\",\"\"]}");Require(new PlayerCrew(catalog,key).AssignedCount==1,"Unknown and duplicate saved IDs must be ignored.");
                Require(new PlayerCrew(null,key).SlotCount==0,"An unavailable catalog must be safe even with saved data.");
                PlayerPrefs.SetString(key,"not valid json");Require(new PlayerCrew(catalog,key).AssignedCount==0,"Malformed saves must safely fall back to an empty crew.");
            }
            finally{PlayerPrefs.DeleteKey(key);PlayerPrefs.Save();}
        }
        public static void RunInteractions(){interactionsOnly=true;Run();}
        public static void Run()
        {
            hadSave=PlayerPrefs.HasKey(SaveKey);saved=PlayerPrefs.GetString(SaveKey,"");
            try
            {
                if(!interactionsOnly)CrewSetup.Install();var catalog=AssetDatabase.LoadAssetAtPath<CrewCatalog>(CrewSetup.CatalogPath);ModelChecks(catalog);
                EditorSceneManager.OpenScene(MegaTanksSetup.ScenePath);var layout=Object.FindObjectOfType<UICanvasLayout>();layout.Preview(UIScreen.Crew);
                var v=layout.View(UIScreen.Crew);Require(PrefabUtility.IsPartOfPrefabInstance(v),"Crew screen must be a nested prefab available outside Play.");
                var slot=v.Get<RectTransform>("Slot 0");slot.anchoredPosition+=new Vector2(9,4);slotPosition=slot.anchoredPosition;PrefabUtility.RecordPrefabInstancePropertyModifications(slot);
                PlayerPrefs.DeleteKey(SaveKey);PlayerPrefs.Save();
                EditorSettings.enterPlayModeOptionsEnabled=true;EditorSettings.enterPlayModeOptions=EnterPlayModeOptions.DisableDomainReload;
                EditorApplication.playModeStateChanged+=State;EditorApplication.isPlaying=true;
            }
            catch(Exception e){Debug.LogException(e);Finish(1);}
        }
        static void State(PlayModeStateChange state)
        {
            if(state==PlayModeStateChange.EnteredPlayMode){phase=0;since=EditorApplication.timeSinceStartup;Application.logMessageReceived+=Log;EditorApplication.update+=Tick;}
            if(state==PlayModeStateChange.EnteredEditMode&&portraitPng!=null)
            {
                try
                {
                    string path="Assets/MegaTanks/Art/Crew/TankPreview.png";File.WriteAllBytes(path,portraitPng);AssetDatabase.ImportAsset(path);
                    var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.mipmapEnabled=false;importer.alphaIsTransparency=true;importer.maxTextureSize=1024;importer.SaveAndReimport();
                    var root=PrefabUtility.LoadPrefabContents(CrewSetup.PrefabPath);
                    try
                    {
                        root.GetComponent<UIView>().Get<RawImage>("Tank Preview/Tank Avatar").texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                        foreach(var image in root.GetComponentsInChildren<Image>(true))if(image.name=="Portrait"&&image.sprite==null)image.gameObject.SetActive(false);
                        PrefabUtility.SaveAsPrefabAsset(root,CrewSetup.PrefabPath);
                    }
                    finally{PrefabUtility.UnloadPrefabContents(root);}
                    AssetDatabase.SaveAssets();EditorApplication.playModeStateChanged-=State;EditorSettings.enterPlayModeOptionsEnabled=false;Debug.Log("CREW_EDITOR_PREVIEW_SAVED");EditorApplication.Exit(0);
                }
                catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
            }
        }
        static void Log(string message,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception)errors=true;}
        static void Press(UIView v,string key)=>Click(v.Get<Button>(key));
        static void Click(Button button)
        {
            Require(button.interactable,button.name+" must be interactable.");Canvas.ForceUpdateCanvases();var r=(RectTransform)button.transform;
            var data=new PointerEventData(EventSystem.current){position=RectTransformUtility.WorldToScreenPoint(null,r.TransformPoint(r.rect.center)),button=PointerEventData.InputButton.Left};
            var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(data,hits);Require(hits.Count>0&&hits[0].gameObject==button.gameObject,"Pointer must hit "+button.name+", hit "+(hits.Count>0?hits[0].gameObject.name:"nothing"));ExecuteEvents.Execute(button.gameObject,data,ExecuteEvents.pointerClickHandler);
        }
        static void Member(UIView v,string id)=>Click(v.Get<ScrollRect>("Inventory").content.GetComponentsInChildren<Button>().Single(b=>b.name=="Crew "+id));
        static void Tick()
        {
            if(EditorApplication.timeSinceStartup-since<.4)return;since=EditorApplication.timeSinceStartup;
            try
            {
                var game=BattleGame.Instance;var ui=game.UI;var layout=ui.GetComponent<UICanvasLayout>();var v=layout.View(UIScreen.Crew);var catalog=game.Frontend.crew;
                switch(phase++)
                {
                    case 0:Require(ui.Popup==""&&layout.View(UIScreen.Home).gameObject.activeSelf,"Editor crew preview must not change Home startup.");Press(layout.View(UIScreen.Home),"Crew");break;
                    case 1:
                        Require(ui.Popup=="Crew","Crew popup remains open.");
                        if(assignAction==0)Require(ui.CrewInventoryVisibleCount==5,"Home Crew opens the full roster without a station filter.");
                        if(assignAction<15)
                        {
                            int i=assignAction/3;
                            switch(assignAction%3)
                            {
                                case 0:
                                    Press(v,"Slot "+i);
                                    Require(ui.CrewInventoryVisibleCount==catalog.members.Count(m=>game.Crew.CanEquip(m,i)),"Station filters roster by matching role.");
                                    Require(v.Get<ScrollRect>("Inventory").content.GetComponentsInChildren<Button>().All(b=>b.name=="Crew "+catalog.members[i].id),"Wrong-role crew must not appear in a station roster.");break;
                                case 1:
                                    Member(v,catalog.members[i].id);
                                    Require(v.Get<Outline>("Slot "+i).enabled&&v.Get<Outline>("Tank Preview/Tank Slot "+i).enabled,"Both matching slot views must highlight.");
                                    Require(v.Get<Button>("Slot "+i).colors.normalColor==layout.routeCurrentText,"Matching station artwork needs a visible gold tint.");
                                    if(i==0)Capture(game,"Crew-Matching-Station");break;
                                case 2:
                                    Press(v,i%2==0?"Slot "+i:"Tank Preview/Tank Slot "+i);
                                    Require(game.Crew.Get(i)==catalog.members[i],"Clicking either highlighted station view must assign the selected crew.");
                                    Require(v.Get<Image>("Tank Preview/Tank Slot "+i+"/Portrait").sprite==catalog.members[i].portrait,"Assigned portrait must appear on the tank.");break;
                            }
                            assignAction++;phase--;return;
                        }
                        Require(game.Crew.AssignedCount==5,"All five stations can be occupied.");
                        if(!interactionsOnly)SavePortrait(v.Get<RawImage>("Tank Preview/Tank Avatar").texture as RenderTexture);Capture(game);break;
                    case 2:
                        switch(moveAction++)
                        {
                            case 0:Press(v,"Inventory Filter");Require(ui.CrewInventoryVisibleCount==5,"Filter button clears the station restriction.");phase--;return;
                            case 1:Press(v,"Inventory Filter");Require(ui.CrewInventoryVisibleCount==0&&v.Get<RectTransform>("Empty Inventory").gameObject.activeSelf,"Available filter reflects a full crew.");phase--;return;
                            case 2:Press(v,"Inventory Filter");Require(ui.CrewInventoryVisibleCount==5,"Assigned filter lists all equipped members.");phase--;return;
                            case 3:Press(v,"Inventory Filter");phase--;return;
                            case 4:Press(v,"Sort Direction");phase--;return;
                            case 5:Member(v,"milo");Require(v.Get<Outline>("Slot 0").enabled&&!v.Get<Outline>("Slot 1").enabled,"Selecting crew highlights its matching station only.");phase--;return;
                            case 6:Press(v,"Tank Preview/Tank Slot 1");Require(ui.CrewInventoryVisibleCount==1,"Tank marker also filters the roster by role.");phase--;return;
                            case 7:Require(!game.Crew.CanEquip(catalog.members[0],1)&&game.Crew.Get(0)==catalog.members[0]&&game.Crew.Get(1)==catalog.members[1],"A nonmatching slot click filters without moving the wrong-role member.");phase--;return;
                            case 8:Press(v,"Inventory Filter");phase--;return;
                            case 9:Member(v,"milo");phase--;return;
                        }
                        Require(game.Crew.Get(0).id=="milo"&&game.Crew.Get(1)==catalog.members[1]&&game.Crew.AssignedCount==5,"Rejected moves preserve all assignments.");
                        Require(new PlayerCrew(catalog).Get(0)==game.Crew.Get(0),"UI assignments save immediately.");Press(v,"Unequip Crew");Require(game.Crew.AssignedCount==4,"Unequip returns a cat to available inventory.");break;
                    case 3:
                        Press(v,"Assign Crew");Require(game.Crew.AssignedCount==5,"Assign button still works without a station filter.");Press(v,"Unequip Crew");
                        Require(v.Get<RectTransform>("Slot 0").anchoredPosition==slotPosition,"Refreshing crew data must preserve authored slot positions.");Press(v,"Close Popup");Press(layout.View(UIScreen.Home),"Play");Require(game.Crew.AssignedCount==4,"Starting a battle retains the crew loadout.");
                        ui.OpenPopup("Crew");break;
                    case 4:
                        Member(v,"milo");Press(v,"Slot 0");Require(game.Crew.Get(0)==null&&!v.Get<Button>("Assign Crew").interactable,"Direct station clicks must not edit a deployed crew loadout.");ui.ClosePopup();game.ReturnHome();break;
                    case 5:
                        Press(layout.View(UIScreen.Home),"Crew");Require(game.Crew.AssignedCount==4,"Returning Home retains crew assignments.");Require(!errors,"No runtime errors permitted.");
                        Debug.Log("CREW_PASS: five generated cat portraits, owned inventory, role-restricted equip/unequip, unique assignments, save/reload recovery, real UI raycasts, tank portraits, filters/sorting, editable prefab geometry, battle/Home retention.");Finish(0);break;
                }
            }
            catch(Exception e){Debug.LogException(e);Finish(1);}
        }
        static void SavePortrait(RenderTexture source)
        {
            var old=RenderTexture.active;try{RenderTexture.active=source;var texture=new Texture2D(source.width,source.height,TextureFormat.RGBA32,false);texture.ReadPixels(new Rect(0,0,source.width,source.height),0,0);texture.Apply();portraitPng=texture.EncodeToPNG();Object.DestroyImmediate(texture);}finally{RenderTexture.active=old;}
        }
        static void Capture(BattleGame game,string name="Crew-Assigned")
        {
            var canvas=game.UI.GetComponent<Canvas>();var camera=game.TankCamera;var target=RenderTexture.GetTemporary(1920,1080,24);
            var previous=camera.targetTexture;var active=RenderTexture.active;var mode=canvas.renderMode;var worldCamera=canvas.worldCamera;float plane=canvas.planeDistance;
            var safe=game.UI.GetComponent<UICanvasLayout>().safeArea;var scale=safe.localScale;var position=safe.anchoredPosition;
            try
            {
                camera.targetTexture=target;canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;Canvas.ForceUpdateCanvases();safe.localScale=Vector3.one;safe.anchoredPosition=Vector2.zero;Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=target;
                var texture=new Texture2D(1920,1080,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,1920,1080),0,0);texture.Apply();Directory.CreateDirectory("Checks/Crew");File.WriteAllBytes("Checks/Crew/"+name+".png",texture.EncodeToPNG());Object.DestroyImmediate(texture);
            }
            finally{camera.targetTexture=previous;RenderTexture.active=active;canvas.renderMode=mode;canvas.worldCamera=worldCamera;canvas.planeDistance=plane;safe.localScale=scale;safe.anchoredPosition=position;RenderTexture.ReleaseTemporary(target);}
        }
        static void Finish(int code)
        {
            EditorApplication.update-=Tick;Application.logMessageReceived-=Log;if(hadSave)PlayerPrefs.SetString(SaveKey,saved);else PlayerPrefs.DeleteKey(SaveKey);PlayerPrefs.Save();
            if(code==0&&!interactionsOnly){EditorApplication.isPlaying=false;return;}EditorApplication.playModeStateChanged-=State;EditorSettings.enterPlayModeOptionsEnabled=false;EditorApplication.Exit(code);
        }
    }
}
