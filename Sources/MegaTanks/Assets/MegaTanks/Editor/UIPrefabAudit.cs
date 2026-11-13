using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
namespace MegaTanks.Editor
{
    public static class UIPrefabAudit
    {
        static readonly BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
        static Vector2 playPosition,playSize,hullPosition,equipPosition;
        static Color playColor;
        static UIView home,equipment;static int phase;static double since;static bool errors;
        static void Require(bool value,string message){if(!value)throw new Exception(message);}
        static void Property(object target,string name,object value)=>target.GetType().GetField("<"+name+">k__BackingField",Flags).SetValue(target,value);
        static void Refresh(CanvasGameUI ui){typeof(CanvasGameUI).GetField("screenKey",Flags).SetValue(ui,"");typeof(CanvasGameUI).GetMethod("Refresh",Flags).Invoke(ui,null);}
        static void Press(UIView v,string key)=>v.Get<Button>(key).onClick.Invoke();
        public static void Run()
        {
            try
            {
                EditorSceneManager.OpenScene(MegaTanksSetup.ScenePath);var layout=UnityEngine.Object.FindObjectOfType<UICanvasLayout>();
                Require(layout!=null&&layout.screens.Length==16,"All 16 authored screen prefabs must be in the scene.");
                Require(PrefabUtility.IsPartOfPrefabInstance(layout),"The scene Canvas must be a prefab instance.");
                foreach(var entry in layout.screens)
                {
                    Require(entry.view!=null&&PrefabUtility.IsPartOfPrefabInstance(entry.view),entry.screen+" must be a nested prefab instance.");
                    layout.Preview(entry.screen);Require(entry.view.gameObject.activeInHierarchy,"Every screen must be previewable without Play mode.");
                    var data=new SerializedObject(entry.view);var bindings=data.FindProperty("bindings");
                    for(int i=0;i<bindings.arraySize;i++)Require(bindings.GetArrayElementAtIndex(i).FindPropertyRelative("target").objectReferenceValue!=null,"No missing UI object bindings.");
                }
                layout.Preview(UIScreen.Home);home=layout.View(UIScreen.Home);equipment=layout.View(UIScreen.Equipment);
                var play=home.Get<RectTransform>("Play");play.anchoredPosition+=new Vector2(-27,13);play.sizeDelta+=new Vector2(11,-7);playPosition=play.anchoredPosition;playSize=play.sizeDelta;play.name="Designer renamed Play";
                var text=home.Get<Text>("Play/Label");text.fontSize=43;text.color=new Color(.9f,.8f,.7f);playColor=text.color;
                PrefabUtility.RecordPrefabInstancePropertyModifications(play);PrefabUtility.RecordPrefabInstancePropertyModifications(play.gameObject);
                PrefabUtility.RecordPrefabInstancePropertyModifications(text);EditorUtility.SetDirty(text);
                var hull=layout.View(UIScreen.BattleHUD).Get<RectTransform>("Hull Anchor/Hull");hull.anchoredPosition+=new Vector2(17,-9);hullPosition=hull.anchoredPosition;
                var equip=equipment.Get<RectTransform>("Equip Item");equip.anchoredPosition+=new Vector2(-21,5);equipPosition=equip.anchoredPosition;
                PrefabUtility.RecordPrefabInstancePropertyModifications(hull);PrefabUtility.RecordPrefabInstancePropertyModifications(equip);
                EditorSceneManager.MarkSceneDirty(layout.gameObject.scene);
                layout.Preview(UIScreen.Equipment);
                EditorSettings.enterPlayModeOptionsEnabled=true;EditorSettings.enterPlayModeOptions=EnterPlayModeOptions.DisableDomainReload;
                EditorApplication.playModeStateChanged+=Entered;EditorApplication.isPlaying=true;
            }
            catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
        }
        static void Entered(PlayModeStateChange state)
        {
            if(state!=PlayModeStateChange.EnteredPlayMode)return;phase=0;since=EditorApplication.timeSinceStartup;
            Application.logMessageReceived+=Log;EditorApplication.update+=Tick;
        }
        static void Log(string message,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception)errors=true;}
        static void Tick()
        {
            if(EditorApplication.timeSinceStartup-since<.35)return;since=EditorApplication.timeSinceStartup;
            try
            {
                var game=BattleGame.Instance;var ui=game.UI;var layout=ui.GetComponent<UICanvasLayout>();
                home=layout.View(UIScreen.Home);equipment=layout.View(UIScreen.Equipment);
                Require(home.Get<RectTransform>("Play").anchoredPosition==playPosition&&home.Get<RectTransform>("Play").sizeDelta==playSize,"Authored Play position and size must survive refresh.");
                Require(home.Get<Text>("Play/Label").fontSize==43&&home.Get<Text>("Play/Label").color==playColor,"Authored font and color must survive refresh. Actual font="+home.Get<Text>("Play/Label").fontSize+" color="+home.Get<Text>("Play/Label").color+" expected="+playColor+" phase="+phase);
                Require(layout.View(UIScreen.BattleHUD).Get<RectTransform>("Hull Anchor/Hull").anchoredPosition==hullPosition,"World health UI must preserve its authored child offsets.");
                Require(equipment.Get<RectTransform>("Equip Item").anchoredPosition==equipPosition,"Equipment refresh must preserve authored positions.");
                switch(phase++)
                {
                    case 0:Require(home.gameObject.activeSelf&&!equipment.gameObject.activeSelf,"An editor preview must not change the Home startup flow.");Require(home.Get<RectTransform>("Play").name=="Designer renamed Play","Bindings must tolerate renamed objects.");ui.OpenEquipment();break;
                    case 1:Press(equipment,"Sort Direction");Press(equipment,"Inventory Sort");Require(equipment.Get<RectTransform>("Inventory/Content").GetComponent<GridLayoutGroup>()!=null,"Inventory uses an editable layout component.");ui.ClosePopup();Press(home,"Play");StageProgressionAudit.DisableWeapons(game);
                        foreach(var enemy in UnityEngine.Object.FindObjectsOfType<EnemyEntity>()){var health=enemy.GetComponentInChildren<EnemyHealthBarUI>();Require(health!=null&&health.GetComponent<Canvas>().renderMode==RenderMode.WorldSpace,"Enemy health must use the world-space UI prefab.");var fill=health.GetComponentsInChildren<Image>().Single(i=>i.name=="Fill");Require(fill.sprite!=null&&fill.type==Image.Type.Filled,"Enemy health fill needs an authored sprite and filled image.");float hp=enemy.Health;enemy.Damage(1);Require(Mathf.Abs(fill.fillAmount-(hp-1)/hp)<.001f,"Enemy health damage must update fill amount.");}
                        game.ShowDamageNumber(game.TankCamera.ViewportToWorldPoint(new Vector3(.5f,.5f,10)),25);typeof(CanvasGameUI).GetMethod("UpdateDamage",Flags).Invoke(ui,null);Require(layout.damageLayer.GetComponentsInChildren<UIView>(true).Any(v=>v.Get<Text>("Label").text=="-25"),"Floating damage must instantiate its authored label prefab.");break;
                    case 2:game.SetPaused(true);Refresh(ui);Require(layout.View(UIScreen.Pause).gameObject.activeSelf,"Pause prefab opens.");Press(layout.View(UIScreen.Pause),"Battle Settings");break;
                    case 3:Require(layout.View(UIScreen.Settings).gameObject.activeSelf,"Settings prefab opens over pause.");ui.ClosePopup();Press(layout.View(UIScreen.Pause),"Resume");game.Rewards.AddScraps(100000);game.Rewards.OpenForWave(5);Refresh(ui);break;
                    case 4:
                        var shop=layout.View(UIScreen.Shop);Require(shop.gameObject.activeSelf&&game.Rewards.Offers.Length==8,"Shop must bind all eight authored card slots.");int money=game.Rewards.Scraps;Press(shop,"Card 0/Action");Refresh(ui);Require(game.Rewards.Scraps<money&&!shop.Get<Button>("Card 0/Action").interactable,"Shop purchase updates its existing card.");game.Rewards.ResetRun();
                        Property(game.Stage,"Mode",StageMode.Service);var cards=AssetDatabase.LoadAssetAtPath<CardCatalog>(AssetDatabase.GUIDToAssetPath(AssetDatabase.FindAssets("t:CardCatalog").First())).cards;
                        Property(game.Stage,"FreeServiceCard",cards.First(c=>c.effect==CardEffect.Resupply&&c.rarity==CardRarity.Normal));Property(game.Stage,"PaidServiceCard",cards.First(c=>c.effect==CardEffect.Resupply&&c.rarity==CardRarity.Rare));game.Rewards.AddScraps(100000);Refresh(ui);break;
                    case 5:
                        var service=layout.View(UIScreen.Service);Require(service.gameObject.activeSelf,"Service prefab opens.");Press(service,"Card 0/Action");Press(service,"Card 1/Action");Refresh(ui);Require(game.Stage.FreeClaimed&&game.Stage.PaidPurchased&&!service.Get<Button>("Card 0/Action").interactable&&!service.Get<Button>("Card 1/Action").interactable,"Both service buttons bind and update.");Property(game.Stage,"Mode",StageMode.Reveal);Refresh(ui);break;
                    case 6:Require(layout.View(UIScreen.Reveal).gameObject.activeSelf,"Reveal prefab opens.");game.Stage.GetType().GetField("waveBreak",Flags).SetValue(game.Stage,10f);Property(game.Stage,"Mode",StageMode.WaveBreak);Refresh(ui);break;
                    case 7:Require(layout.View(UIScreen.WaveBreak).gameObject.activeSelf,"Wave break prefab opens.");Property(game.Stage,"Mode",StageMode.Victory);Refresh(ui);break;
                    case 8:Require(layout.View(UIScreen.Victory).gameObject.activeSelf,"Victory prefab opens.");Property(game.Stage,"Mode",StageMode.Map);game.Stage.Current.completed=true;Refresh(ui);break;
                    case 9:
                        var map=layout.View(UIScreen.Route);Require(map.gameObject.activeSelf,"Route prefab opens.");Require(map.Get<ScrollRect>("Route Scroll").content.GetComponentsInChildren<UIView>().Any(v=>v.name.StartsWith("Node ")),"Route nodes instantiate the editable template.");
                        Property(game,"Hull",0f);Refresh(ui);break;
                    case 10:Require(layout.View(UIScreen.Defeat).gameObject.activeSelf,"Defeat prefab opens.");Require(!errors,"No runtime errors permitted.");Debug.Log("UI_PREFABS_OK: 16 edit-mode screens, nested prefabs, renamed bindings, authored position/size/font/color retained, equipment sort, pause/settings, eight-card shop purchase, services, reveal/wave/victory/route.");Finish(0);break;
                }
            }
            catch(Exception e){Debug.LogException(e);Finish(1);}
        }
        static void Finish(int code){EditorApplication.update-=Tick;Application.logMessageReceived-=Log;EditorApplication.playModeStateChanged-=Entered;EditorSettings.enterPlayModeOptionsEnabled=false;EditorApplication.Exit(code);}
    }
}
