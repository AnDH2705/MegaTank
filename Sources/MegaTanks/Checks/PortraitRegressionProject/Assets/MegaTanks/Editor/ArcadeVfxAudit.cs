using System;
using System.IO;
using System.Linq;
using System.Reflection;
using MegaTankVFX;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

namespace MegaTanks.Editor
{
    public static class ArcadeVfxAudit
    {
        static int phase;
        static double since;
        static bool errors;
        static StageRewardSaveScope saves;
        static Projectile2D live;
        static BattleVfxEffect tail;
        static GameObject pooledTail;
        static EnemyEntity target;
        static float health,hull,pausedAge;
        static string profiles;
        static string Profiles(BattleGame game)=>JsonUtility.ToJson(game.TankProfile)+JsonUtility.ToJson(game.StartingLoadout)+JsonUtility.ToJson(game.EnemyDatabase);
        static void Require(bool ok,string message){if(!ok)throw new Exception(message);}
        static void Near(float actual,float expected,string message){Require(Mathf.Abs(actual-expected)<.01f,message+": "+actual+" expected "+expected);}
        static void Next(){phase++;since=EditorApplication.timeSinceStartup;}
        static BattleVfxEffect[] Effects()=>Object.FindObjectsOfType<BattleVfxEffect>();
        static readonly WeaponCombatStats HitStats=new WeaponCombatStats{accuracy=100,damageBonus=50,criticalChance=100,criticalDamage=100,damageType=DamageType.Fire};
        public static void Run()
        {
            Directory.CreateDirectory("Checks/ArcadeVFX");
            ArcadeVfxSetup.Apply();
            saves=new StageRewardSaveScope();
            EditorSceneManager.OpenScene(MegaTanksSetup.ScenePath);
            EditorSettings.enterPlayModeOptionsEnabled=true;
            EditorSettings.enterPlayModeOptions=EnterPlayModeOptions.DisableDomainReload;
            EditorApplication.playModeStateChanged+=Entered;
            Application.logMessageReceived+=Log;
            EditorApplication.isPlaying=true;
        }
        static void Log(string message,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception)errors=true;}
        static void Entered(PlayModeStateChange state)
        {
            if(state!=PlayModeStateChange.EnteredPlayMode)return;
            phase=0;since=EditorApplication.timeSinceStartup;EditorApplication.update+=Tick;
        }
        static Projectile2D Shot(Vector2 origin,Vector2 direction,float speed=30,float range=100,ProjectileTeam team=ProjectileTeam.Player)
        {
            var shot=new GameObject("Arcade projectile probe").AddComponent<Projectile2D>();
            shot.Initialize(origin,direction,speed,10,Color.white,false,range,faction:team,combatStats:HitStats);
            return shot;
        }
        static void Capture(Camera camera,string name)
        {
            var old=camera.targetTexture;var active=RenderTexture.active;
            var texture=new RenderTexture(1280,720,24);var image=new Texture2D(1280,720,TextureFormat.RGB24,false);
            try
            {
                camera.targetTexture=texture;camera.Render();RenderTexture.active=texture;
                image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();
                File.WriteAllBytes("Checks/ArcadeVFX/"+name+".png",image.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture=old;RenderTexture.active=active;texture.Release();Object.DestroyImmediate(texture);Object.DestroyImmediate(image);
            }
        }
        static void Tick()
        {
            try
            {
                if(EditorApplication.timeSinceStartup-since>35)throw new Exception("Arcade VFX playtest timed out.");
                float wait=phase==1?1.3f:phase==3?1.2f:.18f;
                if(EditorApplication.timeSinceStartup-since<wait)return;
                var game=BattleGame.Instance;
                switch(phase)
                {
                    case 0:
                        Require(game!=null&&game.Vfx.Catalog.fireBoltPrefab!=null,"Shipped scene must resolve the bound arcade catalog.");
                        Require(game.Vfx.Catalog.fireBoltPrefab.GetComponent<FireBoltDemoMotion>()==null,"Gameplay prefab must exclude demo motion.");
                        typeof(CombatFormulaAudit).GetMethod("PureCalculations",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,null);
                        profiles=Profiles(game);
                        game.StartBattle();game.SetStance(TankStance.Defensive);StageProgressionAudit.DisableWeapons(game);
                        foreach(var enemy in Object.FindObjectsOfType<EnemyEntity>())
                        {game.RemoveEnemy(enemy,false);enemy.gameObject.SetActive(false);Object.Destroy(enemy.gameObject);}
                        target=game.SpawnEnemy(EnemyKind.Ground,(Vector2)game.Artwork.transform.position+new Vector2(5,1));
                        target.enabled=target.Weapon.enabled=false;
                        game.MachineGun.Stats.damage=game.MainGun.Stats.damage=game.Mortar.Stats.damage=0;
                        game.MachineGun.SendMessage("Update");game.MainGun.SendMessage("Update");game.Mortar.SendMessage("Update");
                        Require(game.MachineGun.ShotsFired>0&&game.MainGun.ShotsFired>0&&game.Mortar.ShotsFired>0,"Real MG/cannon/mortar must fire.");
                        Require(Effects().Count(fx=>fx.Kind==BattleVfxKind.Muzzle&&fx.ParticleObject!=null)==3,"Each real firing event must produce one particle muzzle.");
                        live=Shot(new Vector2(game.TankAimPoint.x+1,BattleGame.GroundHeight+5),Vector2.right,2);
                        tail=Effects().Single(fx=>fx.ParticleObject!=null&&Vector3.Distance(fx.transform.position,live.transform.position)<.01f&&fx.Kind==BattleVfxKind.Tracer);
                        Next();break;
                    case 1:
                        Require(live!=null&&tail.ParticleObject.activeSelf,"FireBolt must outlive the demo's 0.32s and prefab's 1s lifetime.");
                        Near(tail.transform.position.x,live.transform.position.x,"FireBolt must follow the real projectile");
                        Require(tail.ParticleObject.GetComponent<FireBoltDemoMotion>()==null,"No demo movement may run in battle.");
                        Capture(game.TankCamera,"Firing");
                        pooledTail=tail.ParticleObject;pausedAge=tail.Age;game.SetPaused(true);Next();break;
                    case 2:
                        Near(tail.Age,pausedAge,"Pause must freeze VFX lifetime");game.SetPaused(false);
                        Object.Destroy(live.gameObject);Next();break;
                    case 3:
                        Require(live==null,"Projectile must be destroyed independently.");
                        Require(!pooledTail.activeSelf,"Detached FireBolt must drain smoke and return to pool.");
                        target.Data.armor=4;target.Data.evasion=target.Data.criticalChanceReduction=target.Data.criticalDamageReduction=0;
                        health=target.Health;
                        Shot((Vector2)target.transform.position-Vector2.right*2,Vector2.right);Next();break;
                    case 4:
                        Near(target.Health,health-26,"Physical collision must preserve bonus/critical/armor damage exactly once");
                        Require(Effects().Any(fx=>fx.Kind==BattleVfxKind.Impact&&fx.ParticleObject!=null),"Collision must spawn HitSpark");
                        game.BaseTankStats.armor=3;game.BaseTankStats.evasion=game.BaseTankStats.criticalChanceReduction=game.BaseTankStats.criticalDamageReduction=0;hull=game.Hull;
                        Shot(game.TankAimPoint+Vector2.right*2,Vector2.left,team:ProjectileTeam.Enemy);Next();break;
                    case 5:
                        Near(game.Hull,hull-27,"Enemy collision must preserve damage exactly once");
                        var mortar=new GameObject("Arcade mortar ground probe").AddComponent<Projectile2D>();
                        mortar.InitializeMortar(new Vector2(game.TankAimPoint.x+12,BattleGame.GroundHeight+.3f),new Vector2(0,-100),0,12,1,2,.45f);
                        var ranged=Shot(new Vector2(game.TankAimPoint.x+2,8),Vector2.right,100,.1f);
                        var safety=Shot(new Vector2(game.TankAimPoint.x+2,9),Vector2.zero,0);
                        typeof(Projectile2D).GetField("age",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(safety,30f);
                        Next();break;
                    case 6:
                        Require(Effects().Any(fx=>fx.Kind==BattleVfxKind.Explosion&&fx.ParticleObject!=null),"Mortar ground contact must spawn explosion");
                        Require(!Object.FindObjectsOfType<Projectile2D>().Any(p=>p.name=="Arcade mortar ground probe"||p.transform.position.y>7),"Mortar/range/safety expiry must destroy gameplay projectiles");
                        Capture(game.TankCamera,"Impacts");
                        // Verify particles survive the first frame after destroying their projectile.
                        live=Shot(new Vector2(game.TankAimPoint.x+2,5),Vector2.right,2);Next();break;
                    case 7:
                        tail=Effects().Single(fx=>fx.ParticleObject!=null&&fx.Kind==BattleVfxKind.Tracer&&Vector3.Distance(fx.transform.position,live.transform.position)<.02f);
                        pooledTail=tail.ParticleObject;
                        Object.Destroy(live.gameObject);Next();break;
                    case 8:
                        Require(live==null&&pooledTail!=null&&pooledTail.activeSelf,"Smoke must survive projectile destruction before fading");
                        Require(pooledTail.GetComponentsInChildren<ParticleSystem>().All(ps=>!ps.isEmitting),"Detached tail must stop emitting");
                        var impact=new Vector2(game.TankAimPoint.x+40,BattleGame.GroundHeight+1);
                        var full=Dummy(game,impact+Vector2.right*2);var half=Dummy(game,impact+Vector2.right*4);
                        var evade=Dummy(game,impact-Vector2.right);var outside=Dummy(game,impact+Vector2.right*6);
                        evade.Data.evasion=100;
                        float fullHp=full.Health,halfHp=half.Health,evadeHp=evade.Health,outsideHp=outside.Health;
                        game.DetonateMortar(impact,new DamageAttack(20,new WeaponCombatStats()),2.5f,5);
                        Near(full.Health,fullHp,"Zero-accuracy AoE must miss");Near(half.Health,halfHp,"Zero-accuracy half AoE must miss");
                        game.DetonateMortar(impact,new DamageAttack(20,HitStats),2.5f,5);
                        Near(full.Health,fullHp-41,"Full AoE must preserve critical reduction and armor");
                        Near(half.Health,halfHp-18.5f,"Half AoE must apply zone multiplier before armor");
                        Near(evade.Health,evadeHp,"AoE must preserve per-target evasion");Near(outside.Health,outsideHp,"AoE must preserve radius");
                        Require(Profiles(game)==profiles,"VFX playtest must not mutate source tank/weapon/enemy profiles");
                        game.ReturnHome();Next();break;
                    case 9:
                        Require(game.Vfx.ActiveCount==0&&!Object.FindObjectsOfType<VfxAutoRelease>().Any(),"Returning home must clear all active effects");
                        Require(!errors,"Playtest must have no runtime errors");
                        Finish(0,"ARCADE_VFX_PLAYTEST_OK: eight menu-built prefabs; real MG/cannon/mortar muzzle events; projectile attachment >1s; pause; detached smoke drains and pools; player/enemy damage 26/27 exactly once; mortar ground explosion; range and safety expiry; transition cleanup; percentage formulas; zero-accuracy/full/half/outside/evasion AoE; immutable source profiles.");break;
                }
            }
            catch(Exception exception){Debug.LogException(exception);Finish(1,exception.ToString());}
        }
        static EnemyEntity Dummy(BattleGame game,Vector2 position)
        {
            var enemy=game.SpawnEnemy(EnemyKind.Ground,position);enemy.enabled=enemy.Weapon.enabled=false;
            enemy.transform.position=position;enemy.Data.armor=4;
            enemy.Data.evasion=enemy.Data.criticalChanceReduction=0;enemy.Data.criticalDamageReduction=50;
            return enemy;
        }
        static void Finish(int code,string result)
        {
            EditorApplication.update-=Tick;EditorApplication.playModeStateChanged-=Entered;Application.logMessageReceived-=Log;
            saves?.Dispose();File.WriteAllText("Checks/ArcadeVFX/result.txt",result);Debug.Log(result);
            EditorSettings.enterPlayModeOptionsEnabled=false;
            if(Application.isBatchMode)EditorApplication.Exit(code);else EditorApplication.isPlaying=false;
        }
    }
}
