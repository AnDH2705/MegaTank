using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

namespace MegaTanks.Editor
{
    public static class TanketteAudit
    {
        static StageRewardSaveScope saves;
        static EnemyEntity tankette,car;
        static Transform[] wheels;
        static Quaternion[] rotations;
        static string sourceBefore,carBefore,captures;
        static bool errors;
        static int phase;
        static double since;
        static UnityEngine.Random.State random;
        static void Require(bool value,string message){if(!value)throw new Exception(message);}
        static void Near(float value,float expected,string message)=>Require(Mathf.Abs(value-expected)<.005f,message+": "+value+" expected "+expected);
        public static void Run()
        {
            saves=new StageRewardSaveScope();random=UnityEngine.Random.state;
            captures="Checks/Tankette/"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff");Directory.CreateDirectory(captures);
            EditorSceneManager.OpenScene(MegaTanksSetup.ScenePath);
            var window=EditorWindow.GetWindow(typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameView"));window.position=new Rect(20,20,1000,600);MegaTanksSetup.SetLandscapeGameView(window);window.Show();
            EditorSettings.enterPlayModeOptionsEnabled=true;EditorSettings.enterPlayModeOptions=EnterPlayModeOptions.DisableDomainReload;
            EditorApplication.playModeStateChanged+=Entered;Application.logMessageReceived+=Log;EditorApplication.isPlaying=true;
        }
        static void Log(string message,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception)errors=true;}
        static void Entered(PlayModeStateChange state){if(state!=PlayModeStateChange.EnteredPlayMode)return;phase=0;since=EditorApplication.timeSinceStartup;EditorApplication.update+=Check;}
        static void Clear(BattleGame game)
        {foreach(var enemy in Object.FindObjectsOfType<EnemyEntity>()){game.RemoveEnemy(enemy,false);enemy.gameObject.SetActive(false);Object.Destroy(enemy.gameObject);}}
        static void Check()
        {
            try
            {
                if(phase>0&&EditorApplication.timeSinceStartup-since<.3)return;
                var game=BattleGame.Instance;if(game==null)return;
                switch(phase++)
                {
                    case 0:
                        var definition=game.EnemyDatabase.Get(TanketteSetup.EnemyId);var original=game.EnemyDatabase.Get("GroundEnemy1");
                        sourceBefore=JsonUtility.ToJson(definition);carBefore=JsonUtility.ToJson(original);
                        Require(game.EnemyDatabase.Get(EnemyKind.Ground).id=="GroundEnemy1","Adding the tankette must preserve the default car.");
                        Require(game.EnemyDatabase.Definitions.Count(d=>d.id==TanketteSetup.EnemyId)==1,"Tankette identity must be unique.");
                        Near(definition.ScaledArtworkWidth,original.ScaledArtworkWidth,"Tankette must use the car's size");Near(definition.armor,3,"Tankette light armor");
                        Require(definition.armor<6&&definition.baseSpeed<original.baseSpeed,"Tankette must have penetrable light armor and move slightly slower.");
                        Near(definition.baseHealth,original.baseHealth,"Tankette preserves the car's base HP");
                        Require(definition.weapon.type==EnemyWeaponType.Cannon&&definition.weapon.salvoSize==1,"Tankette must use a single-shot cannon.");
                        foreach(var stage in game.Frontend.stages.Where(s=>s.spawnableEnemies.Any(p=>p!=null&&p.stats.id=="GroundEnemy1")))
                            Require(stage.spawnableEnemies.Count(p=>p!=null&&p.stats.id==TanketteSetup.EnemyId)==1,"Car stages must include the tankette exactly once.");
                        var grown=definition.AtDifficulty(10);Near(grown.armor,3,"Light armor must not grow into heavy armor");Near(grown.baseHealth,EnemyDefinition.Grow(definition.baseHealth,definition.baseHealthGrow,10),"Tankette HP growth");
                        var mg=new DamageAttack(6,new WeaponCombatStats{accuracy=100});Near(CombatDamage.Calculate(mg,definition,0,1).Damage,3,"Starting MG must penetrate light armor");
                        var cannon=new DamageAttack(45,new WeaponCombatStats{accuracy=100});Near(CombatDamage.Calculate(cannon,definition,0,1).Damage,42,"Cannon remains effective against tankette");
                        game.SelectStage(game.Frontend.stages.First(s=>s.stageNumber==1));UnityEngine.Random.InitState(14425);
                        bool seenTank=false,seenCar=false;
                        for(int i=0;i<12&&(!seenTank||!seenCar);i++)
                        {
                            game.StartBattle();StageProgressionAudit.DisableWeapons(game);
                            var spawned=Object.FindObjectsOfType<EnemyEntity>().Where(e=>e.Alive&&e.gameObject.activeInHierarchy).ToArray();
                            seenTank|=spawned.Any(e=>e.Data.id==TanketteSetup.EnemyId);seenCar|=spawned.Any(e=>e.Data.id=="GroundEnemy1");
                            foreach(var enemy in spawned)enemy.gameObject.SetActive(false);
                        }
                        Require(seenTank&&seenCar,"Normal waves must spawn both cars and tankettes.");Clear(game);game.SetStance(TankStance.Defensive);
                        float x=game.TankAimPoint.x;
                        car=game.SpawnEnemy("GroundEnemy1",new Vector2(x+20,BattleGame.GroundHeight));tankette=game.SpawnEnemy(TanketteSetup.EnemyId,new Vector2(x+25,BattleGame.GroundHeight));
                        car.enabled=car.Weapon.enabled=tankette.enabled=tankette.Weapon.enabled=false;
                        Near(car.GetComponent<Collider2D>().bounds.size.x,tankette.GetComponent<Collider2D>().bounds.size.x,"Actual grounded vehicle sizes must match");
                        Near(tankette.GetComponent<Collider2D>().bounds.min.y,BattleGame.GroundHeight,"Tankette track must rest on ground");
                        Require(tankette.Artwork.transform.localScale.x<0&&tankette.Artwork.Muzzle.position.x<tankette.transform.position.x,"Tankette gun/art must face the approaching player.");
                        wheels=tankette.Artwork.GetComponentsInChildren<SpriteRenderer>().Where(r=>r.name.StartsWith("Tankette road wheel")).Select(r=>r.transform).ToArray();
                        Require(wheels.Length==4&&wheels.All(w=>w.GetComponent<SpriteRenderer>().sprite!=null),"Tankette needs four independently rotating reused road wheels.");
                        rotations=wheels.Select(w=>w.localRotation).ToArray();float start=tankette.transform.position.x;tankette.Tick(.25f);
                        Require(tankette.transform.position.x<start,"Tankette must advance towards player.");break;
                    case 1:
                        Require(wheels.Zip(rotations,(w,q)=>Quaternion.Angle(w.localRotation,q)>.1f).All(changed=>changed),"Every tankette road wheel must animate while advancing.");
                        game.SetPaused(true);rotations=wheels.Select(w=>w.localRotation).ToArray();break;
                    case 2:
                        Require(wheels.Zip(rotations,(w,q)=>Quaternion.Angle(w.localRotation,q)<.01f).All(stopped=>stopped),"Pause must stop road wheels.");game.SetPaused(false);
                        tankette.transform.position+=Vector3.right*(game.TankAimPoint.x+7-tankette.transform.position.x);car.transform.position+=Vector3.right*(game.TankAimPoint.x+3.5f-car.transform.position.x);
                        float hp=tankette.Health;tankette.Data.evasion=0;tankette.ReceiveAttack(new DamageAttack(6,new WeaponCombatStats{accuracy=100}));Near(tankette.Health,hp-3,"Live tankette must apply its light armor");
                        tankette.Weapon.Tick(0);Require(tankette.Weapon.ShotsFired==1&&tankette.Weapon.Ammo==tankette.Weapon.MaxAmmo-1,"Tankette gun must fire and consume ammo.");
                        var shot=Object.FindObjectsOfType<Projectile2D>().FirstOrDefault(p=>p.Team==ProjectileTeam.Enemy&&Vector2.Distance(p.transform.position,tankette.Artwork.Muzzle.position)<.05f);
                        Require(shot!=null,"Tankette projectile must originate from its illustrated cannon muzzle.");
                        Near(shot.Attack.BaseDamage,tankette.Weapon.Stats.damage,"Cannon projectile must snapshot its configured damage");
                        float reload=tankette.Weapon.Stats.EffectiveReloadTime;tankette.Weapon.Tick(reload-.01f);
                        Require(tankette.Weapon.ShotsFired==1,"Small cannon must reload instead of firing an MG burst.");tankette.Weapon.Tick(.02f);
                        Require(tankette.Weapon.ShotsFired==2&&tankette.Weapon.Ammo==tankette.Weapon.MaxAmmo-2,"Small cannon must fire exactly once after reload.");break;
                    case 3:
                        ScreenCapture.CaptureScreenshot(Path.GetFullPath(captures+"/CarAndTankette.png"));break;
                    case 4:
                        Require(JsonUtility.ToJson(game.EnemyDatabase.Get(TanketteSetup.EnemyId))==sourceBefore&&JsonUtility.ToJson(game.EnemyDatabase.Get("GroundEnemy1"))==carBefore,"Runtime must not mutate enemy source profiles.");
                        tankette.Damage(100000);Require(!tankette.Alive&&!tankette.GetComponent<Collider2D>().enabled,"Tankette must use normal defeat handling.");
                        Require(!errors&&File.Exists(captures+"/CarAndTankette.png"),"Tankette audit requires a capture and no runtime errors.");
                        Debug.Log("TANKETTE_AUDIT_OK: same car size/style pipeline, 3 armor, ground placement/facing, four animated wheels, pause, gun/muzzle/ammo, real armor damage, growth, natural mixed waves, defeat and immutable source profiles. Capture: "+Path.GetFullPath(captures));Finish(0);break;
                }
                since=EditorApplication.timeSinceStartup;
            }
            catch(Exception e){Debug.LogException(e);Finish(1);}
        }
        static void Finish(int code)
        {
            UnityEngine.Random.state=random;saves?.Dispose();EditorApplication.update-=Check;EditorApplication.playModeStateChanged-=Entered;Application.logMessageReceived-=Log;
            EditorSettings.enterPlayModeOptionsEnabled=false;EditorApplication.Exit(code);
        }
    }
}
