using System;
using System.Collections.Generic;
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
    public static class MissilePackAudit
    {
        static StageRewardSaveScope scope;
        static readonly string[] keys={"MegaTanks.Equipment.Cannon","MegaTanks.Equipment.Mortar",BattleGame.SelectedTankSaveKey};
        static readonly Dictionary<string,string> saves=new Dictionary<string,string>();
        static GunDefinition at,aa;
        static string beforeAT,beforeAA,captures;
        static int phase;
        static bool errors;
        static double since;
        static void Require(bool ok,string message){if(!ok)throw new Exception(message);}
        static void Near(float value,float expected,string message)=>Require(Mathf.Abs(value-expected)<.001f,message+": "+value+" expected "+expected);
        static T Field<T>(object owner,string name)=>(T)owner.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(owner);
        static void Step(Projectile2D missile,float seconds)=>typeof(Projectile2D).GetMethod("Tick",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(missile,new object[]{seconds});
        static EnemyEntity Dummy(BattleGame game,EnemyKind kind,float x,float height)
        {
            var pos=new Vector2(x,BattleGame.GroundHeight+height);var enemy=game.SpawnEnemy(kind,pos);enemy.transform.position=pos;
            enemy.enabled=false;if(enemy.Weapon!=null)enemy.Weapon.enabled=false;enemy.Data.armor=enemy.Data.evasion=0;
            var body=enemy.GetComponent<BoxCollider2D>();body.offset=Vector2.zero;body.size=Vector2.one*.5f;return enemy;
        }
        static Projectile2D Missile(GunDefinition definition,Vector2 origin,EnemyEntity target,float lifetime=5)
        {
            var stats=definition.stats.Copy();stats.damage=1;stats.accuracy=100;stats.armorPenetration=100;stats.missileLifetime=lifetime;
            var missile=new GameObject("Missile audit probe").AddComponent<Projectile2D>();missile.InitializeMissile(origin,Vector2.right,stats,target);missile.enabled=false;return missile;
        }
        static void Equip(BattleGame game,GunDefinition item)
        {
            var cannon=game.Equipment.Cannon;
            game.UI.OpenEquipment();var view=game.UI.GetComponent<UICanvasLayout>().View(UIScreen.Equipment);view.Get<Button>("Slot Mortar").onClick.Invoke();
            var cell=view.Get<RectTransform>("Inventory/Content").Cast<Transform>().Single(t=>t.gameObject.activeSelf&&t.name=="Item "+item.name);
            cell.GetComponent<Button>().onClick.Invoke();view.Get<Button>("Equip Item").onClick.Invoke();
            Require(game.Equipment.MissilePack==item,"Garage must equip "+item.displayName);
            Require(game.Equipment.Cannon==cannon&&!game.MainGun.Stats.homingMissile,"Missile equip must preserve the main cannon.");
            Require(view.Get<Text>("Item Stats").text.Contains(item==at?"GROUND ONLY":"AIR ONLY"),"Garage must show target restrictions.");
            Require(game.Artwork.MissilePack.name.StartsWith(item.artworkPrefab.name),"Tank must mount the selected pack.");
            var portrait=Field<GameObject>(game.UI,"portraitTank").GetComponent<TankArtwork>();
            Require(portrait.MissilePack.name.StartsWith(item.artworkPrefab.name)&&portrait.MissilePack.GetComponentsInChildren<SpriteRenderer>().Any(r=>r.enabled&&r.gameObject.layer==30),"Portrait must show the selected launcher.");
            var defaults=(WeaponLoadout)new SerializedObject(game).FindProperty("startingLoadout").objectReferenceValue;
            Require(new PlayerEquipment(defaults,game.Frontend).MissilePack==item,"Saved selection must restore by permanent ID.");
        }
        public static void Run()
        {
            scope=new StageRewardSaveScope();foreach(var key in keys)if(PlayerPrefs.HasKey(key))saves[key]=PlayerPrefs.GetString(key);
            PlayerPrefs.SetString(BattleGame.SelectedTankSaveKey,"tank.medium");PlayerPrefs.Save();
            at=AssetDatabase.LoadAssetAtPath<GunDefinition>(MissilePackSetup.ATPath);aa=AssetDatabase.LoadAssetAtPath<GunDefinition>(MissilePackSetup.AAPath);
            beforeAT=JsonUtility.ToJson(at);beforeAA=JsonUtility.ToJson(aa);captures="Checks/MissilePacks/"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff");Directory.CreateDirectory(captures);
            EditorSceneManager.OpenScene(MegaTanksSetup.ScenePath);
            var window=EditorWindow.GetWindow(typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameView"));window.position=new Rect(20,20,1000,600);MegaTanksSetup.SetLandscapeGameView(window);window.Show();
            EditorSettings.enterPlayModeOptionsEnabled=true;EditorSettings.enterPlayModeOptions=EnterPlayModeOptions.DisableDomainReload;
            EditorApplication.playModeStateChanged+=Entered;Application.logMessageReceived+=Log;EditorApplication.isPlaying=true;
        }
        static void Log(string message,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception)errors=true;}
        static void Entered(PlayModeStateChange state){if(state!=PlayModeStateChange.EnteredPlayMode)return;phase=0;since=EditorApplication.timeSinceStartup;EditorApplication.update+=Check;}
        static void ClearEnemies(BattleGame game)
        {foreach(var enemy in Object.FindObjectsOfType<EnemyEntity>()){game.RemoveEnemy(enemy,false);enemy.gameObject.SetActive(false);Object.Destroy(enemy.gameObject);}}
        static void CheckHtmlSimulation()
        {
            var sim=new HtmlBattleSimulation(HtmlBattleScreen.ReadConfig(),0,123);sim.bossSpawned=true;sim.player.weapons[0]=false;
            var stats=at.stats.Copy();sim.EquipMissile(stats);
            var air=sim.Spawn("plane");air.x=1000;air.y=500;air.speed=0;air.attack=999;
            int ammo=sim.ammo[2];sim.Tick(.05f);Require(sim.ammo[2]==ammo,"Current simulator AT must ignore air-only encounters.");
            Require(sim.bullets.Any(b=>b.weapon=="cannon"),"Current simulator must keep the cannon firing alongside the missile pack.");
            var ground=sim.Spawn("car");ground.x=1100;ground.speed=0;ground.attack=999;ground.hp=ground.maxHP=1000;
            sim.Tick(.05f);var live=sim.bullets.Single(b=>b.homing);Require(live.target==ground&&sim.ammo[2]==ammo-1,"Current simulator must fire AT missiles at ground.");
            var replacement=sim.Spawn("car");replacement.x=1250;replacement.speed=0;replacement.attack=999;replacement.hp=1000;
            var heading=live.velocity;ground.dead=true;sim.Tick(.05f);
            Require(live.target==replacement&&Vector2.Angle(heading,live.velocity)<=4.001f,"Current simulator retargeting must respect steering limit.");
            var update=typeof(HtmlBattleSimulation).GetMethod("UpdateMissile",BindingFlags.Instance|BindingFlags.NonPublic);
            foreach(bool aaOnly in new[]{false,true})
            {
                sim=new HtmlBattleSimulation(HtmlBattleScreen.ReadConfig(),0,123);float y=sim.config.groundY-90;
                var target=new HtmlBattleSimulation.Actor{kind=aaOnly?"plane":"car",x=1200,y=y,width=300,hp=100,maxHP=100};
                var wrong=new HtmlBattleSimulation.Actor{kind=aaOnly?"car":"plane",x=1100,y=y,width=300,hp=100,maxHP=100};sim.enemies.Add(target);sim.enemies.Add(wrong);
                var bullet=new HtmlBattleSimulation.Bullet{weapon="missile",homing=true,airOnly=aaOnly,x=1000,y=y,target=target,velocity=Vector2.right*1200,turnRate=80,lockRange=1400,life=5,damage=1,attack=new DamageAttack(1,new WeaponCombatStats{accuracy=100})};sim.bullets.Add(bullet);
                for(int i=0;i<20&&sim.bullets.Contains(bullet);i++)update.Invoke(sim,new object[]{bullet,.05f});
                Near(target.hp,99,"Current simulator missile must hit its class once.");Near(wrong.hp,100,"Current simulator missile must pass through the wrong class.");
                Require(!sim.effects.Any(f=>f.clip=="GroundFire"),"Missile impacts must not create persistent fire.");
                target.dead=true;var next=new HtmlBattleSimulation.Actor{kind=aaOnly?"plane":"car",x=1600,y=y,width=300,hp=100,maxHP=100};sim.enemies.Add(next);
                bullet=new HtmlBattleSimulation.Bullet{weapon="missile",homing=true,airOnly=aaOnly,x=1300,y=y,target=target,velocity=Vector2.right*1200,turnRate=80,lockRange=1400,life=.15f};sim.bullets.Add(bullet);
                update.Invoke(sim,new object[]{bullet,.05f});Require(bullet.target==next,"Current simulator must reacquire the next same-class target.");
                update.Invoke(sim,new object[]{bullet,.11f});Require(!sim.bullets.Contains(bullet),"Current simulator missiles must expire.");
            }
        }
        static void Check()
        {
            try
            {
                if(phase>0&&EditorApplication.timeSinceStartup-since<.6)return;
                var game=BattleGame.Instance;if(game==null)return;
                switch(phase++)
                {
                    case 0:
                        Equip(game,aa);Equip(game,at);Require(game.Progression.Supports(at)&&game.Progression.Supports(aa),"Both packs must support progression.");
                        game.UI.ClosePopup();game.StartBattle();game.SetStance(TankStance.Defensive);StageProgressionAudit.DisableWeapons(game);ClearEnemies(game);
                        var mount=game.Mortar.MissileLauncher.transform.position;var air=Dummy(game,EnemyKind.Flyer,mount.x+5,mount.y-BattleGame.GroundHeight);
                        int ammo=game.Mortar.MissileLauncher.Ammo;game.Mortar.MissileLauncher.TickWeapon();Require(game.Mortar.MissileLauncher.Target==null&&game.Mortar.MissileLauncher.Ammo==ammo,"AT must not fire on flyers.");
                        var ground=Dummy(game,EnemyKind.Ground,mount.x+6,mount.y-BattleGame.GroundHeight);game.Mortar.MissileLauncher.TickWeapon();
                        Require(game.Mortar.MissileLauncher.Target==ground&&game.Mortar.MissileLauncher.ShotsFired==1&&game.Mortar.MissileLauncher.Ammo==ammo-1,"AT must launch a missile at ground and consume ammo.");
                        game.Mortar.ResetWeapon();typeof(MortarWeapon).GetMethod("Update",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(game.Mortar,null);
                        Require(game.Mortar.ShotsFired==1&&game.Mortar.Ammo==ammo-1,"Mortar slot controller must fire and report missile ammo exactly once.");
                        game.MainGun.TickWeapon();Require(game.MainGun.ShotsFired==1&&!game.MainGun.Stats.homingMissile,"Main cannon must fire alongside mortar-slot missiles.");
                        Require(Object.FindObjectsOfType<Projectile2D>().Any(p=>p.IsMissile&&p.LockedTarget==ground),"Launched missile must retain its lock.");
                        game.Mortar.MissileLauncher.Configure(aa,game.Progression,0,game.ReloadSpeedBonus,game.AccuracyBonus);game.Mortar.MissileLauncher.TickWeapon();
                        Require(game.Mortar.MissileLauncher.Target==air&&game.Mortar.MissileLauncher.ShotsFired==1,"AA must launch against flyers.");
                        Near(game.Mortar.MissileLauncher.ReloadTime,6/(1+(100+game.ReloadSpeedBonus)/100),"Missiles must use the shared reload formula.");
                        air.gameObject.SetActive(false);game.Mortar.MissileLauncher.ResetWeapon();ammo=game.Mortar.MissileLauncher.Ammo;game.Mortar.MissileLauncher.TickWeapon();Require(game.Mortar.MissileLauncher.Target==null&&game.Mortar.MissileLauncher.Ammo==ammo,"AA must not fall back to ground.");
                        foreach(var p in Object.FindObjectsOfType<Projectile2D>()){p.gameObject.SetActive(false);Object.Destroy(p.gameObject);}ground.gameObject.SetActive(false);
                        float y=BattleGame.GroundHeight+8;var hit=Dummy(game,EnemyKind.Ground,110,8);var wrong=Dummy(game,EnemyKind.Flyer,104,8);
                        float hp=hit.Health,wrongHP=wrong.Health;var missile=Missile(at,new Vector2(100,y),hit);Step(missile,.5f);
                        Near(wrong.Health,wrongHP,"AT must pass through flyers.");Require(missile.GetComponentInChildren<SpriteRenderer>().enabled,"Missile body must remain visible with exhaust.");
                        for(int i=0;i<12&&!Field<bool>(missile,"released");i++)Step(missile,.05f);
                        Near(hit.Health,hp-1,"AT must deal one swept contact hit.");Require(Field<bool>(missile,"released"),"Missile must release on impact.");
                        var next=Dummy(game,EnemyKind.Ground,112,11);missile=Missile(at,new Vector2(100,y),hit);Step(missile,.1f);Require(missile.LockedTarget==hit,"Living lock must persist.");
                        hit.Damage(100000);Step(missile,.05f);Require(missile.LockedTarget==next,"Destroyed target must be replaced by same-class lock.");
                        Require(Vector2.Angle(Vector2.right,missile.Velocity)<=4.001f,"Retargeting must respect 80 degrees/sec.");
                        next.gameObject.SetActive(false);var heading=missile.Velocity;Step(missile,.05f);Require(missile.LockedTarget==null,"Wrong class cannot replace lost lock.");Near(Vector2.Angle(heading,missile.Velocity),0,"No lock preserves heading.");
                        var original=Dummy(game,EnemyKind.Ground,180,8);var late=Dummy(game,EnemyKind.Ground,160,10);hp=late.Health;
                        missile=Missile(at,new Vector2(150,y),original,1.25f);Step(missile,1);original.Damage(100000);Step(missile,.1f);
                        Require(missile.LockedTarget==late&&missile.transform.position.x>162&&Vector2.Angle(Vector2.right,missile.Velocity)<=8.001f,"Late retarget must not snap backwards.");
                        Step(missile,.2f);Require(Field<bool>(missile,"released"),"Missiles must burn out.");Near(late.Health,hp,"Late target change can miss without remote damage.");
                        var flyer=Dummy(game,EnemyKind.Flyer,210,8);var blocker=Dummy(game,EnemyKind.Ground,204,8);hp=flyer.Health;wrongHP=blocker.Health;missile=Missile(aa,new Vector2(200,y),flyer);
                        for(int i=0;i<25&&!Field<bool>(missile,"released");i++)Step(missile,.05f);
                        Near(flyer.Health,hp-1,"AA must hit flyer.");Near(blocker.Health,wrongHP,"AA must pass through ground.");
                        missile=Missile(aa,new Vector2(220,y),null);Step(missile,.05f);var position=missile.transform.position;game.SetPaused(true);Step(missile,.5f);Require(missile.transform.position==position,"Pause must freeze missiles.");game.SetPaused(false);
                        game.ReturnHome();Equip(game,at);break;
                    case 1:ScreenCapture.CaptureScreenshot(Path.GetFullPath(captures+"/ATGarage.png"));break;
                    case 2:Equip(game,aa);break;
                    case 3:ScreenCapture.CaptureScreenshot(Path.GetFullPath(captures+"/AAGarage.png"));break;
                    case 4:
                        game.UI.ChangeTank();Require(game.TankProfile.id=="tank.heavy","Heavy tank must select.");Equip(game,aa);
                        var mortar=game.Frontend.inventory.OfType<MortarDefinition>().First();
                        Require(game.Equipment.Equip(mortar,EquipmentType.Mortar),"Original mortar must re-equip.");game.UI.RefreshEquipmentArtwork();Require(game.Artwork.MissilePack==null&&!game.Mortar.IsMissile,"Original mortar artwork and firing mode must restore.");
                        Equip(game,aa);CheckHtmlSimulation();game.UI.Play();
                        var screen=Object.FindObjectOfType<HtmlBattleScreen>();Require(screen!=null&&screen.Simulation.MissileStats!=null,"Current Play flow must equip the selected missile pack.");
                        var sim=screen.Simulation;sim.player.weapons[0]=false;var plane=sim.Spawn("plane");plane.x=1250;plane.y=420;plane.attack=999;plane.speed=0;plane.hp=plane.maxHP=1000;
                        break;
                    case 5:
                        var battle=Object.FindObjectOfType<HtmlBattleScreen>();Require(battle.Simulation.ammo[2]<battle.Simulation.MissileStats.reserveCapacity,"Current battle must launch missiles and consume ammo.");
                        Require(battle.GetComponentsInChildren<Text>().Any(t=>t.text.Contains("AA MISSILES")),"Current HUD must identify missile ammo.");ScreenCapture.CaptureScreenshot(Path.GetFullPath(captures+"/HeavyAABattle.png"));break;
                    case 6:
                        Require(JsonUtility.ToJson(at)==beforeAT&&JsonUtility.ToJson(aa)==beforeAA,"Runtime must preserve source definitions.");Require(!errors,"No runtime errors allowed.");
                        foreach(var file in new[]{"ATGarage.png","AAGarage.png","HeavyAABattle.png"})Require(File.Exists(captures+"/"+file),"Missing capture "+file);
                        Debug.Log("MISSILE_PACK_AUDIT_OK: restored garage, saves, both tanks, real firing/ammo/reload, strict AT/AA collision, retained lock, retargeting, turn limits, late misses, lifetime and pause. Captures: "+Path.GetFullPath(captures));Finish(0);break;
                }
                since=EditorApplication.timeSinceStartup;
            }
            catch(Exception e){Debug.LogException(e);Finish(1);}
        }
        static void Finish(int code)
        {
            foreach(var key in keys){if(saves.TryGetValue(key,out var value))PlayerPrefs.SetString(key,value);else PlayerPrefs.DeleteKey(key);}scope?.Dispose();PlayerPrefs.Save();
            EditorApplication.update-=Check;EditorApplication.playModeStateChanged-=Entered;Application.logMessageReceived-=Log;EditorSettings.enterPlayModeOptionsEnabled=false;EditorApplication.Exit(code);
        }
    }
}
