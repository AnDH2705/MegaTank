using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object=UnityEngine.Object;
namespace MegaTanks.Editor
{
    public static class CrewRoleAudit
    {
        static StageRewardSaveScope scope;
        static string crewSave;
        static bool hadCrewSave,errors;
        static readonly Dictionary<CrewDefinition,string> snapshots=new Dictionary<CrewDefinition,string>();
        static int phase,frame;
        static double deadline;
        static bool requireZeroValues=true;
        public static void RunWithAuthoredTuning(){requireZeroValues=false;Run();}
        static void Require(bool result,string message){if(!result)throw new Exception(message);}
        static void Near(float value,float expected,string message)=>Require(Mathf.Abs(value-expected)<.001f,message+": "+value+" expected "+expected);
        public static void Run()
        {
            CrewSetup.ConfigureStats();
            scope=new StageRewardSaveScope();hadCrewSave=PlayerPrefs.HasKey("MegaTanks.Crew.Loadout");crewSave=PlayerPrefs.GetString("MegaTanks.Crew.Loadout","");
            try
            {
                foreach(string guid in AssetDatabase.FindAssets("t:CrewDefinition"))
                {
                    var member=AssetDatabase.LoadAssetAtPath<CrewDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                    snapshots[member]=JsonUtility.ToJson(member);
                    foreach(var field in typeof(CrewStats).GetFields())
                    {
                        var stat=(CrewStat)field.GetValue(member.stats);if(requireZeroValues){Near(stat.value,0,member.name+" "+field.Name+" base reset");Near(stat.gain,0,member.name+" "+field.Name+" gain reset");}
                    }
                    Near(member.baseDamage,0,"Legacy crew damage reset");
                    // Keep authored tuning in snapshots, but make fixture hit rolls and gains deterministic.
                    if(!requireZeroValues)member.stats=new CrewStats();
                }
                PlayerPrefs.DeleteKey("MegaTanks.Crew.Loadout");
                PlayerPrefs.DeleteKey(PlayerProgression.DefaultSaveKey);
                EditorSceneManager.OpenScene(MegaTanksSetup.ScenePath);
                EditorSettings.enterPlayModeOptionsEnabled=true;EditorSettings.enterPlayModeOptions=EnterPlayModeOptions.DisableDomainReload;
                EditorApplication.playModeStateChanged+=State;Application.logMessageReceived+=Log;
                deadline=EditorApplication.timeSinceStartup+90;EditorApplication.isPlaying=true;
            }
            catch(Exception e){Debug.LogException(e);Finish(1);}
        }
        static void Log(string message,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception)errors=true;}
        static void State(PlayModeStateChange state)
        {
            if(state==PlayModeStateChange.EnteredPlayMode){phase=0;EditorApplication.update+=Tick;}
            if(state==PlayModeStateChange.EnteredEditMode){EditorApplication.playModeStateChanged-=State;EditorApplication.Exit(errors?1:0);}
        }
        static void Tick()
        {
            try
            {
                if(EditorApplication.timeSinceStartup>deadline)throw new Exception("Crew role audit timeout");
                if(phase>0&&Time.frameCount<=frame+2)return;
                var game=BattleGame.Instance;
                if(phase==0)
                {
                    var members=game.Frontend.crew.members;
                    Require(members.Select(m=>m.role).Distinct().Count()==5,"Roster must include all five roles");
                    foreach(var m in members)
                    {
                        for(int i=0;i<5;i++)if(i!=(int)m.role)Require(!game.Crew.Equip(m,i),"Wrong-role assignment rejected");
                        Require(game.Crew.Equip(m,(int)m.role),"Matching role accepted");
                    }
                    Require(new PlayerCrew(game.Frontend.crew).AssignedCount==5,"Assignments survive reload");
                    // Old unrestricted saves recover members in their correct role stations.
                    string legacy="{\"memberIds\":["+string.Join(",",members.Reverse().Select(m=>"\""+m.id+"\""))+"]}";
                    PlayerPrefs.SetString("MegaTanks.Crew.RoleAudit",legacy);
                    var recovered=new PlayerCrew(game.Frontend.crew,"MegaTanks.Crew.RoleAudit");
                    foreach(var m in members)Require(recovered.Get(m.role)==m,"Legacy assignment migrates by role");
                    PlayerPrefs.DeleteKey("MegaTanks.Crew.RoleAudit");
                    var gunner=game.Crew.Get(CrewRole.Gunner);gunner.stats.accuracy.value=11;gunner.stats.accuracy.gain=3;
                    gunner.stats.damageBonus.value=20;gunner.stats.reloadSpeed.value=30;gunner.stats.criticalChance.value=40;gunner.stats.criticalDamage.value=50;
                    Near(gunner.stats.accuracy.AtLevel(4),20,"Individual additive level gains");
                    Near(gunner.stats.accuracy.AtLevel(4,2),24,"Existing star bonus applies after individual level gains");
                    var boss=game.BossSettings.Copy();
                    foreach(var slot in boss.crew){slot.level=3;slot.stars=2;}
                    var bossDriver=boss.template.crew.First(slot=>slot.member.role==CrewRole.Driver).member;
                    float bossHP=boss.CalculateTankStats().maxHull;
                    bossDriver.stats.hp.value=5;bossDriver.stats.hp.gain=2;
                    Near(boss.CalculateTankStats().maxHull-bossHP,10.8f,"Boss driver gains apply once with own progression");
                    var bossGunner=boss.template.crew.First(slot=>slot.member.role==CrewRole.Gunner).member;
                    bossGunner.stats.damageBonus.value=10;bossGunner.stats.damageBonus.gain=2;
                    var bossWeapon=new WeaponCombatStats();boss.ApplyCrew(CrewRole.Gunner,bossWeapon);
                    Near(bossWeapon.damageBonus,16.8f,"Boss gunner uses per-stat gains and its own stars");
                    var bossSub=new WeaponCombatStats();boss.ApplyCrew(CrewRole.SubGunner,bossSub);Near(bossSub.damageBonus,0,"Boss gunner does not affect sub gun");
                    game.Wallet.TryAdd(CurrencyType.PetFood,100000);
                    int level=game.Progression.Get(gunner).Level;
                    Require(game.Progression.TryLevelUp(gunner),"Crew level up succeeds");
                    Near(game.Progression.Get(gunner).Level,level+1,"Crew level saved");
                    var sub=game.Crew.Get(CrewRole.SubGunner);sub.stats.damageBonus.value=7;
                    var specialist=game.Crew.Get(CrewRole.WeaponSpecialist);specialist.stats.damageBonus.value=9;
                    var driver=game.Crew.Get(CrewRole.Driver);driver.stats.hp.value=37;driver.stats.evasion.value=13;driver.stats.speed.value=2;
                    var commando=game.Crew.Get(CrewRole.Commando);var cs=commando.stats;
                    cs.hp.value=50;cs.damage.value=8;cs.accuracy.value=100;cs.criticalChance.value=100;cs.criticalDamage.value=50;
                    cs.speed.value=2;cs.weaponRange.value=20;cs.activationRange.value=12;cs.baseReloadTime.value=2;cs.reloadSpeed.value=100;cs.projectileSpeed.value=25;
                    game.RefreshTankLoadoutStats();
                    Near(game.MainGun.Stats.damageBonus,game.Equipment.Cannon.stats.damageBonus+20,"Gunner modifies cannon only");
                    Near(game.MachineGun.Stats.damageBonus,game.Equipment.MachineGun.stats.damageBonus+7,"Sub gunner modifies sub gun only");
                    Near(game.Mortar.Stats.damageBonus,game.Equipment.Mortar.stats.damageBonus+9,"Specialist modifies mortar only");
                    Near(game.MainGun.Stats.accuracy,game.Equipment.Cannon.stats.accuracy+game.AccuracyBonus+gunner.stats.accuracy.AtLevel(level+1),"Leveled accuracy applies");
                    Near(game.MainGun.Stats.reloadSpeed,game.Equipment.Cannon.stats.reloadSpeed+game.ReloadSpeedBonus+30,"Crew reload bonus applies once");
                    Near(game.MainGun.Stats.criticalChance,game.Equipment.Cannon.stats.criticalChance+40,"Crew critical chance applies");
                    Near(game.MainGun.Stats.criticalDamage,game.Equipment.Cannon.stats.criticalDamage+50,"Crew critical damage applies");
                    var hull=game.BaseTankStats;game.Crew.Unequip((int)CrewRole.Driver);
                    Near(hull.maxHull-game.BaseTankStats.maxHull,37,"Driver HP isolated");Near(hull.evasion-game.BaseTankStats.evasion,13,"Driver evasion isolated");Near(hull.advanceSpeed-game.BaseTankStats.advanceSpeed,2,"Driver speed isolated");
                    Require(game.Crew.Equip(driver,(int)CrewRole.Driver),"Driver reassignment");
                    var gatling=game.Frontend.inventory.OfType<GunDefinition>().FirstOrDefault(g=>g.name.Contains("Gatling"));
                    if(gatling!=null){var mg=game.Equipment.MachineGun;Require(game.Equipment.Equip(gatling,EquipmentType.MachineGun),"Gatling equips in sub gun slot");game.RefreshEquipmentArtwork();Near(game.MachineGun.Stats.damageBonus,gatling.stats.damageBonus+7,"Sub gunner supports gatling");game.Equipment.Equip(mg,EquipmentType.MachineGun);game.RefreshEquipmentArtwork();}
                    Require(game.Crew.Unequip((int)CrewRole.Gunner),"Gunner can be removed");
                    Near(game.MainGun.Stats.damageBonus,game.Equipment.Cannon.stats.damageBonus,"Unequip removes bonus immediately");
                    game.Crew.Equip(gunner,(int)CrewRole.Gunner);
                    game.UI.OpenPopup("Crew");
                    var view=game.UI.GetComponent<UICanvasLayout>().View(UIScreen.Crew);
                    view.Get<Button>("Slot 0").onClick.Invoke();
                    Require(view.Get<Text>("Detail Description").text.Contains("/lv"),"Crew UI shows stat gains");
                    view.Get<Button>("Slot 1").onClick.Invoke();
                    var cell=view.Get<RectTransform>("Inventory/Content").Cast<Transform>().Single(t=>t.gameObject.activeSelf&&t.name=="Crew "+gunner.id);
                    cell.GetComponent<Button>().onClick.Invoke();
                    Require(!view.Get<Button>("Assign Crew").interactable,"UI disables incompatible roles");
                    view.Get<Button>("Close Popup").onClick.Invoke();
                    phase++;frame=Time.frameCount;
                }
                else if(phase==1)
                {
                    game.StartBattle();game.SetStance(TankStance.Defensive);StageProgressionAudit.DisableWeapons(game);
                    foreach(var enemy in Object.FindObjectsOfType<EnemyEntity>()){game.RemoveEnemy(enemy,false);enemy.gameObject.SetActive(false);Object.Destroy(enemy.gameObject);}
                    var target=game.SpawnEnemy(EnemyKind.Ground,game.TankAimPoint+Vector2.right*30);target.enabled=target.Weapon.enabled=false;
                    game.Commando.Tick(.1f);Require(!game.Commando.Active,"Commando remains stowed outside activation range");
                    target.transform.position=game.TankAimPoint+Vector2.right*5;Physics2D.SyncTransforms();
                    game.Commando.Tick(.1f);Require(game.Commando.Active,"Nearby enemy activates commando");
                    Near(game.Commando.Health,50,"Independent commando HP");Require(game.Commando.ShotsFired==1,"Integrated weapon fires");
                    Near(game.Commando.Weapon.damage,8,"Commando base damage independent of tank");Near(game.Commando.Weapon.EffectiveReloadTime,1,"Integrated reload formula");
                    if(game.Crew.Get(CrewRole.Commando).commandoPrefab!=null)
                    {
                        var art=game.Commando.Artwork;Require(art!=null,"Assigned warrior prefab is instantiated on deployment");
                        Require(art.CurrentAnimation=="shovel_attack","Warrior plays its attack animation when firing");
                        art.Advance(2);float range=game.Commando.Weapon.range;game.Commando.Weapon.range=.1f;
                        game.Commando.Tick(.1f);Require(art.CurrentAnimation=="walk","Warrior walks while advancing into weapon range");
                        game.Commando.Weapon.range=range;art.Face(-1);Require(art.Animation.Skeleton.ScaleX<0,"Warrior faces left targets");art.Face(1);
                        if(SystemInfo.graphicsDeviceType!=UnityEngine.Rendering.GraphicsDeviceType.Null)ItemProgressionAudit.Capture(game,"Commando-Warrior");
                    }
                    target.Data.armor=target.Data.evasion=target.Data.criticalChanceReduction=target.Data.criticalDamageReduction=0;
                    var round=Object.FindObjectsOfType<Projectile2D>().Single(shot=>shot.name=="Commando round");
                    float enemyHP=target.Health;
                    round.transform.position=(Vector2)target.transform.position-round.Velocity.normalized*.05f;
                    round.SendMessage("Update");
                    Near(target.Health,Mathf.Max(0,enemyHP-12),"Commando projectile applies its own accuracy and critical damage");
                    Require(target.Alive,"Commando test target survives for retreat checks");
                    float hull=game.Hull;
                    var enemyWeapon=target.Weapon.Stats;enemyWeapon.accuracy=100;enemyWeapon.damage=3;enemyWeapon.criticalChance=0;enemyWeapon.range=100;enemyWeapon.gravity=0;enemyWeapon.projectileSpeed=20;
                    target.Weapon.SendMessage("Update");
                    var incoming=Object.FindObjectsOfType<Projectile2D>().Single(shot=>shot.Team==ProjectileTeam.Enemy);
                    Require(Vector2.Dot(incoming.Velocity.normalized,(game.Commando.AimPoint-(Vector2)incoming.transform.position).normalized)>.99f,"Enemy targets the nearby commando");
                    incoming.transform.position=game.Commando.AimPoint+Vector2.right*.1f;incoming.SendMessage("Update");
                    Near(game.Commando.Health,47,"Enemy projectile hits commando collider");Near(game.Hull,hull,"Commando collider is outside the main hull");
                    game.Commando.ReceiveBlast(game.Commando.AimPoint,new DamageAttack(2,new WeaponCombatStats{accuracy=100}),1,2);
                    Near(game.Commando.Health,45,"Enemy splash damages commando independently");
                    var hit=new DamageAttack(5,new WeaponCombatStats{accuracy=100});game.Commando.ReceiveAttack(hit);Near(game.Commando.Health,40,"Commando takes damage");Near(game.Hull,hull,"Commando damage does not hurt tank");
                    game.SetPaused(true);int shots=game.Commando.ShotsFired;var pausedPosition=game.Commando.transform.position;
                    game.Commando.Tick(5);Require(game.Commando.ShotsFired==shots&&game.Commando.transform.position==pausedPosition,"Pause freezes commando movement and firing");game.SetPaused(false);
                    target.transform.position=game.TankAimPoint+Vector2.right*30;game.Commando.Tick(.1f);Require(!game.Commando.Active,"Commando stows when enemies leave activation range");
                    target.transform.position=game.TankAimPoint+Vector2.right*5;game.Commando.Tick(.1f);Near(game.Commando.Health,40,"Reactivation preserves HP");
                    game.Commando.ReceiveAttack(new DamageAttack(100,new WeaponCombatStats{accuracy=100}));Require(!game.Commando.Active,"Defeated commando stops taking hits");
                    if(game.Commando.Artwork!=null)Require(game.Commando.Artwork.CurrentAnimation=="death","Warrior plays its death animation");
                    game.Commando.Tick(2);Require(!game.Commando.Active,"No immediate respawn after death");
                    game.ReturnHome();Require(!game.Commando.Active,"Returning home clears commando");
                    game.StartBattle();game.SetStance(TankStance.Defensive);StageProgressionAudit.DisableWeapons(game);
                    foreach(var enemy in Object.FindObjectsOfType<EnemyEntity>()){game.RemoveEnemy(enemy,false);enemy.gameObject.SetActive(false);Object.Destroy(enemy.gameObject);}
                    var next=game.SpawnEnemy(EnemyKind.Ground,game.TankAimPoint+Vector2.right*5);next.enabled=next.Weapon.enabled=false;
                    game.Commando.Tick(.1f);Require(game.Commando.Active,"New battle restores defeated commando");Near(game.Commando.Health,50,"New run restores commando health");
                    game.ReturnHome();
                    phase++;frame=Time.frameCount;
                }
                else
                {
                    Require(!errors,"No runtime errors");
                    Debug.Log("CREW_ROLES_PASS: five fixed roles; invalid assignments rejected; save migration; individual gains; isolated weapon and hull bonuses; immediate unequip; independent commando activation, shooting, damage, retreat and death; authored crew tuning restored after validation.");
                    Finish(0);
                }
            }
            catch(Exception e){Debug.LogException(e);Finish(1);}
        }
        static void Finish(int code)
        {
            EditorApplication.update-=Tick;Application.logMessageReceived-=Log;
            foreach(var pair in snapshots)if(pair.Key!=null)JsonUtility.FromJsonOverwrite(pair.Value,pair.Key);
            if(hadCrewSave)PlayerPrefs.SetString("MegaTanks.Crew.Loadout",crewSave);else PlayerPrefs.DeleteKey("MegaTanks.Crew.Loadout");
            PlayerPrefs.DeleteKey("MegaTanks.Crew.RoleAudit");scope?.Dispose();PlayerPrefs.Save();
            if(code==0){EditorApplication.isPlaying=false;return;}
            EditorApplication.playModeStateChanged-=State;EditorSettings.enterPlayModeOptionsEnabled=false;EditorApplication.Exit(code);
        }
    }
}
