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
    // Reproduce startup with a saved Gatling: its inactive original MG must still be
    // prepared for the portrait camera before the player switches back to it.
    public static class GaragePortraitAudit
    {
        const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
        const string Preference="MegaTanks.Equipment.MachineGun";
        static GunDefinition mg,gatling;
        static string saved;
        static bool hadSaved,errors;
        static int phase;
        static double since;
        static void Require(bool ok,string message){if(!ok)throw new Exception(message);}
        static T Field<T>(object obj,string name)=>(T)obj.GetType().GetField(name,Flags).GetValue(obj);
        public static void Run()
        {
            try
            {
                Require(!EditorApplication.isPlaying,"Start this audit outside Play mode.");
                Debug.Log("GARAGE_PORTRAIT_STARTED");
                hadSaved=PlayerPrefs.HasKey(Preference);saved=PlayerPrefs.GetString(Preference,"");
                mg=AssetDatabase.LoadAssetAtPath<GunDefinition>("Assets/MegaTanks/Data/Weapons/MachineGun.asset");
                gatling=AssetDatabase.LoadAssetAtPath<GunDefinition>(GatlingGunImporter.DefinitionPath);
                Require(mg!=null&&gatling!=null,"Both weapon definitions are required.");
                PlayerPrefs.SetString(Preference,gatling.name);PlayerPrefs.Save();
                EditorSceneManager.OpenScene(MegaTanksSetup.ScenePath);
                EditorSettings.enterPlayModeOptionsEnabled=true;EditorSettings.enterPlayModeOptions=EnterPlayModeOptions.DisableDomainReload;
                Application.logMessageReceived+=Log;EditorApplication.playModeStateChanged+=Entered;EditorApplication.isPlaying=true;
            }
            catch(Exception e){Debug.LogException(e);Finish(1);}
        }
        static void Log(string message,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception)errors=true;}
        static void Entered(PlayModeStateChange state)
        {
            if(state!=PlayModeStateChange.EnteredPlayMode)return;
            Debug.Log("GARAGE_PORTRAIT_ENTERED_PLAY");
            phase=0;since=EditorApplication.timeSinceStartup;EditorApplication.update+=Tick;
        }
        static void Equip(BattleGame game,GunDefinition definition)
        {
            var view=game.UI.GetComponent<UICanvasLayout>().View(UIScreen.Equipment);
            var content=view.Get<RectTransform>("Inventory/Content");
            var cell=content.Cast<Transform>().Single(t=>t.gameObject.activeSelf&&t.name=="Item "+definition.name);
            cell.GetComponent<Button>().onClick.Invoke();view.Get<Button>("Equip Item").onClick.Invoke();
            Require(game.Equipment.MachineGun==definition,"The Garage equip action must update the selected weapon.");
            Require(view.Get<Image>("Slot MG/Equipped Item Icon").sprite==definition.icon,"The equipped slot icon must match.");
        }
        static void CheckPortrait(BattleGame game,bool original,string capture)
        {
            var ui=game.UI;var tank=Field<GameObject>(ui,"portraitTank").GetComponent<TankArtwork>();var camera=Field<Camera>(ui,"portraitCamera");
            Require(tank.MachineGun.name==(original?"roof_gun":gatling.artworkPrefab.name+"(Clone)"),"Portrait must display the selected weapon.");
            var renderers=tank.MachineGun.GetComponentsInChildren<SpriteRenderer>(true).Where(r=>r.sprite!=null&&r.enabled&&r.gameObject.activeInHierarchy).ToArray();
            Require(renderers.Length>0,"The selected weapon needs active sprite artwork.");
            Require(renderers.All(r=>(camera.cullingMask&(1<<r.gameObject.layer))!=0),"Every active weapon sprite must be visible to the portrait camera.");
            Require(tank.GetComponentsInChildren<MonoBehaviour>(true).All(b=>!b.enabled),"Portrait behaviours, including reactivated MG parts, must remain disabled.");
            var texture=Field<RenderTexture>(ui,"portrait");camera.Render();var visible=Pixels(texture);
            foreach(var r in renderers)r.enabled=false;
            camera.Render();var hidden=Pixels(texture);
            foreach(var r in renderers)r.enabled=true;
            camera.Render();
            int changed=visible.Zip(hidden,(a,b)=>a.r!=b.r||a.g!=b.g||a.b!=b.b||a.a!=b.a?1:0).Sum();
            Require(changed>20,"Weapon artwork must contribute visible pixels, not just exist in the hierarchy.");
            Directory.CreateDirectory("Checks/GaragePortrait");Save(texture,"Checks/GaragePortrait/"+capture+".png");
        }
        static Color32[] Pixels(RenderTexture target)
        {
            var previous=RenderTexture.active;var image=new Texture2D(target.width,target.height,TextureFormat.RGBA32,false);
            try{RenderTexture.active=target;image.ReadPixels(new Rect(0,0,target.width,target.height),0,0);image.Apply();return image.GetPixels32();}
            finally{RenderTexture.active=previous;Object.DestroyImmediate(image);}
        }
        static void Save(RenderTexture target,string path)
        {
            var image=new Texture2D(target.width,target.height,TextureFormat.RGBA32,false);
            try{image.SetPixels32(Pixels(target));image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());}
            finally{Object.DestroyImmediate(image);}
        }
        static void Tick()
        {
            if(EditorApplication.timeSinceStartup-since<.5)return;since=EditorApplication.timeSinceStartup;
            try
            {
                var game=BattleGame.Instance;Require(game!=null&&game.UI!=null,"Game and UI initialize.");
                switch(phase++)
                {
                    case 0:
                        Require(game.Equipment.MachineGun==gatling,"Reproduction must start with saved Gatling equipped.");
                        game.UI.OpenEquipment();CheckPortrait(game,false,"InitialGatling");break;
                    case 1:Equip(game,mg);CheckPortrait(game,true,"RestoredMG");break;
                    case 2:Equip(game,gatling);CheckPortrait(game,false,"ReequippedGatling");break;
                    case 3:Equip(game,mg);CheckPortrait(game,true,"RestoredMGAgain");break;
                    case 4:
                        game.UI.ClosePopup();game.UI.OpenEquipment();CheckPortrait(game,true,"ReopenedGarageMG");
                        game.UI.Play();game.SetPaused(true);
                        Require(game.MachineGun.Definition==mg&&game.Artwork.MachineGun.name=="roof_gun","Starting a run must retain the equipped original MG.");
                        Require(!errors,"No runtime errors during the regression.");
                        File.WriteAllText("Checks/GaragePortrait/result.txt","PASS: saved Gatling startup, MG and Gatling switching in both directions, repeat switching, reopening Garage, portrait pixel visibility, slot icons and starting a run.\n");
                        Debug.Log("GARAGE_PORTRAIT_PASS");Finish(0);break;
                }
            }
            catch(Exception e){Debug.LogException(e);Finish(1);}
        }
        static void Finish(int code)
        {
            if(hadSaved)PlayerPrefs.SetString(Preference,saved);else PlayerPrefs.DeleteKey(Preference);PlayerPrefs.Save();
            EditorApplication.update-=Tick;EditorApplication.playModeStateChanged-=Entered;Application.logMessageReceived-=Log;
            EditorApplication.Exit(code);
        }
    }
}
