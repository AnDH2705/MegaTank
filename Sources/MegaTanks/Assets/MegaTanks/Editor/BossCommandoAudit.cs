using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;
namespace MegaTanks.Editor
{
    public static class BossCommandoAudit
    {
        static StageRewardSaveScope saves;
        static StageDefinition stage;
        static BossTankProfile template;
        static CrewDefinition member;
        static readonly Dictionary<CrewDefinition,string> tuning=new Dictionary<CrewDefinition,string>();
        static bool errors,oldOptions;
        static EnterPlayModeOptions options;
        static int phase,frame;
        static double deadline;
        static EnemyEntity boss;
        static EnemyEntity unit;
        static void Require(bool condition,string message){if(!condition)throw new Exception(message);}
        static void Near(float actual,float expected,string message)=>Require(Mathf.Abs(actual-expected)<.001f,message+": "+actual+" expected "+expected);
        public static void Run()
        {
            saves=new StageRewardSaveScope();
            foreach(string guid in AssetDatabase.FindAssets("t:CrewDefinition"))
            {
                var asset=AssetDatabase.LoadAssetAtPath<CrewDefinition>(AssetDatabase.GUIDToAssetPath(guid));tuning[asset]=JsonUtility.ToJson(asset.stats);
            }
            EditorSceneManager.OpenScene(MegaTanksSetup.ScenePath);
            oldOptions=EditorSettings.enterPlayModeOptionsEnabled;options=EditorSettings.enterPlayModeOptions;
            EditorSettings.enterPlayModeOptionsEnabled=true;EditorSettings.enterPlayModeOptions=EnterPlayModeOptions.DisableDomainReload;
            Application.logMessageReceived+=Log;EditorApplication.playModeStateChanged+=State;
            deadline=EditorApplication.timeSinceStartup+100;EditorApplication.isPlaying=true;
        }
        static void Log(string message,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception)errors=true;}
        static void State(PlayModeStateChange state)
        {
            if(state==PlayModeStateChange.EnteredPlayMode)EditorApplication.update+=Tick;
            if(state==PlayModeStateChange.EnteredEditMode){EditorApplication.playModeStateChanged-=State;EditorSettings.enterPlayModeOptionsEnabled=oldOptions;EditorSettings.enterPlayModeOptions=options;EditorApplication.Exit(errors?1:0);}
        }
        static EnemyEntity Begin(BattleGame game)
        {
            game.StartBattle();game.SetStance(TankStance.Defensive);StageProgressionAudit.DisableWeapons(game);game.Commando.enabled=false;
            foreach(var enemy in Object.FindObjectsOfType<EnemyEntity>()){game.RemoveEnemy(enemy,false);enemy.gameObject.SetActive(false);Object.Destroy(enemy.gameObject);}
            game.Stage.Current.type=StageNodeType.Boss;var enemyBoss=game.SpawnBoss();Require(enemyBoss!=null,"Boss fixture spawns");enemyBoss.enabled=false;enemyBoss.Boss.enabled=false;return enemyBoss;
        }
        static void MoveNear(BattleGame game,EnemyEntity owner)=>owner.transform.position+=Vector3.right*(game.TankAimPoint.x+8-owner.transform.position.x);
        static void Tick()
        {
            try
            {
                if(EditorApplication.timeSinceStartup>deadline)throw new Exception("Boss commando audit timeout");
                if(phase>0&&Time.frameCount<=frame+2)return;
                var game=BattleGame.Instance;
                if(phase==0)
                {
                    stage=Object.Instantiate(game.Stage.Definition);stage.boss=stage.boss.Copy();template=Object.Instantiate(stage.boss.template);stage.boss.template=template;
                    template.crew=Array.ConvertAll(template.crew,slot=>new BossCrewStation{station=slot.station,member=slot.member});
                    var slot=template.crew.First(s=>s.member!=null&&s.member.role==CrewRole.Commando);member=Object.Instantiate(slot.member);slot.member=member;
                    Require(member.commandoPrefab==AssetDatabase.LoadAssetAtPath<CommandoArtwork>(BossCommandoArtImporter.PrefabPath),"Boss has the enemy dog prefab");
                    member.stats=new CrewStats();var stats=member.stats;
                    stats.hp.value=30;stats.hp.gain=5;stats.damage.value=5;stats.damage.gain=2;stats.accuracy.value=100;
                    stats.speed.value=2;stats.weaponRange.value=12;stats.baseReloadTime.value=2;stats.projectileSpeed.value=25;stats.activationRange.value=15;
                    var progression=stage.boss.crew.First(s=>s.station=="Commando");progression.level=3;progression.stars=1;
                    game.SelectStage(stage);boss=Begin(game);boss.Boss.TickCommando();Require(boss.Boss.Commando==null,"No spawn outside boss activation range");
                    MoveNear(game,boss);Physics2D.SyncTransforms();boss.Boss.TickCommando();unit=boss.Boss.Commando;
                    Require(unit!=null&&unit.Alive&&unit.gameObject.activeSelf,"Boss deploys its commando when the player enters range");unit.enabled=false;unit.Weapon.enabled=false;
                    Near(unit.Health,44,"Boss commando uses its own level and star HP");Near(unit.Weapon.Stats.damage,9.9f,"Boss commando weapon gains use stage crew progression");
                    Require(unit.Difficulty==0&&unit.Data.baseScraps==0&&unit.Data.score==0,"Summon has no difficulty scaling or repeatable rewards");
                    Require(unit.CommandoArtwork!=null&&unit.CommandoArtwork.Animation.skeletonDataAsset.name=="dog-enemy-v2_SkeletonData","Correct dog skeleton is instantiated");
                    Require(game.EnemyCount==2&&game.FindTarget(unit.transform.position,EnemyKind.Ground,1)==unit,"Boss commando is registered as a targetable enemy");
                    var player=game.Frontend.crew.members.First(m=>m.role==CrewRole.Commando);
                    Require(player.commandoPrefab==AssetDatabase.LoadAssetAtPath<CommandoArtwork>(CommandoArtImporter.PrefabPath),"Player warrior remains independent");
                    unit.Weapon.Stats.accuracy=1000;unit.Weapon.Stats.armorPenetration=100;
                    unit.Weapon.SendMessage("Update");Require(unit.Weapon.ShotsFired==1&&unit.CommandoArtwork.CurrentAnimation=="melee_attack","Enemy integrated weapon fires and plays melee animation");
                    var shot=Object.FindObjectsOfType<Projectile2D>().Single(p=>p.Team==ProjectileTeam.Enemy);
                    float hull=game.Hull;shot.transform.position=game.TankAimPoint+Vector2.right*.1f;shot.SendMessage("Update");Near(game.Hull,hull-9.9f,"Boss commando projectile damages the player hull");
                    var friendly=new GameObject("Enemy friendly-fire test").AddComponent<Projectile2D>();
                    friendly.Initialize((Vector2)unit.transform.position+Vector2.right*.1f,Vector2.left,10,4,Color.red,false,20,0,ProjectileTeam.Enemy,combatStats:new WeaponCombatStats{accuracy=100});
                    friendly.SendMessage("Update");Near(unit.Health,44,"Enemy projectiles cannot hit their own commando");Object.Destroy(friendly.gameObject);
                    var playerShot=new GameObject("Player hit boss commando").AddComponent<Projectile2D>();
                    playerShot.Initialize((Vector2)unit.transform.position+Vector2.left*.1f,Vector2.right,10,4,Color.green,false,20,0,ProjectileTeam.Player,combatStats:new WeaponCombatStats{accuracy=100});
                    playerShot.SendMessage("Update");Near(unit.Health,40,"Player projectile damages boss commando independently");
                    unit.CommandoArtwork.Advance(2);unit.Data.weapon.range=unit.Weapon.Stats.range=.1f;unit.SendMessage("Update");
                    Require(unit.CommandoArtwork.CurrentAnimation=="walk"&&unit.CommandoArtwork.Animation.Skeleton.ScaleX<0,"Enemy dog walks and faces the player");unit.Data.weapon.range=unit.Weapon.Stats.range=12;
                    game.UI.SendMessage("Refresh");ItemProgressionAudit.Capture(game,"Enemy-Boss-Commando");
                    phase++;frame=Time.frameCount;
                }
                else if(phase==1)
                {
                    boss.transform.position+=Vector3.right*40;boss.Boss.TickCommando();Require(!unit.gameObject.activeSelf&&game.EnemyCount==1,"Leaving activation range stows and unregisters the summon");
                    Require(game.FindTarget(unit.transform.position,EnemyKind.Ground,1)==null,"Stowed commando cannot be targeted");
                    MoveNear(game,boss);Physics2D.SyncTransforms();boss.Boss.TickCommando();Require(boss.Boss.Commando==unit&&unit.gameObject.activeSelf,"Reactivation reuses the same summon");Near(unit.Health,40,"Stowing preserves commando HP");
                    game.SetPaused(true);int shots=unit.Weapon.ShotsFired;var position=unit.transform.position;unit.SendMessage("Update");unit.Weapon.SendMessage("Update");boss.Boss.TickCommando();
                    Require(shots==unit.Weapon.ShotsFired&&unit.transform.position==position,"Pause freezes boss commando movement and weapon");game.SetPaused(false);
                    unit.ReceiveAttack(new DamageAttack(1000,new WeaponCombatStats{accuracy=100}));Require(!unit.Alive&&unit.CommandoArtwork.CurrentAnimation=="death","Enemy commando plays death and leaves combat");
                    boss.Boss.TickCommando();Require(game.EnemyCount==1&&boss.Boss.Commando==unit,"Defeated commando cannot immediately respawn");
                    boss.Damage(boss.Health+1);Require(game.EnemyCount==0&&game.Stage.Mode!=StageMode.Combat,"Boss death cleans up summon and permits encounter completion");
                    game.ReturnHome();boss=Begin(game);MoveNear(game,boss);Physics2D.SyncTransforms();boss.Boss.TickCommando();
                    Require(boss.Boss.Commando!=null&&boss.Boss.Commando.Alive,"Retry creates a fresh boss commando");Near(boss.Boss.Commando.Health,44,"Retry resets summoned HP from boss crew snapshot");
                    game.ReturnHome();phase++;frame=Time.frameCount;
                }
                else
                {
                    foreach(var pair in tuning)Require(JsonUtility.ToJson(pair.Key.stats)==pair.Value,"Authored crew tuning preserved: "+pair.Key.name);
                    Require(!errors,"No runtime errors during boss commando audit");
                    Debug.Log("BOSS_COMMANDO_PASS: enemy dog prefab; stage crew progression; activation and retreat; own hull and weapon; player targeting and projectiles; no friendly fire; walk/attack/death; pause; owner death cleanup; encounter completion; retry; player warrior and authored crew stats preserved.");Finish(0);
                }
            }
            catch(Exception e){Debug.LogException(e);Finish(1);}
        }
        static void Finish(int code)
        {
            EditorApplication.update-=Tick;Application.logMessageReceived-=Log;saves?.Dispose();PlayerPrefs.Save();
            if(code==0){EditorApplication.isPlaying=false;return;}
            EditorApplication.playModeStateChanged-=State;EditorSettings.enterPlayModeOptionsEnabled=oldOptions;EditorSettings.enterPlayModeOptions=options;EditorApplication.Exit(code);
        }
    }
}
