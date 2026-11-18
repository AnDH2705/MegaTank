using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

namespace MegaTanks.Editor
{
    public static class MortarGroundAudit
    {
        static StageRewardSaveScope saves;
        static int phase;
        static float since,hull,airHP,innerHP,outerHP,outsideHP,fastAirHP,fastInnerHP;
        static double began;
        static bool errors;
        static EnemyEntity air,inner,outer,outside,fastAir,fastInner;
        static Projectile2D shell;
        static readonly WeaponCombatStats attack=new WeaponCombatStats {accuracy=100,armorPenetration=100};
        static void Require(bool ok,string message){if(!ok)throw new Exception(message);}
        static void Near(float actual,float expected,string message){Require(Mathf.Abs(actual-expected)<.001f,message+": "+actual+" expected "+expected);}
        static void Next(){phase++;since=Time.time;}
        public static void Run()
        {
            saves=new StageRewardSaveScope();
            EditorSceneManager.OpenScene(MegaTanksSetup.ScenePath);
            EditorSettings.enterPlayModeOptionsEnabled=true;EditorSettings.enterPlayModeOptions=EnterPlayModeOptions.DisableDomainReload;
            EditorApplication.playModeStateChanged+=Entered;Application.logMessageReceived+=Log;EditorApplication.isPlaying=true;
        }
        static void Log(string message,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception)errors=true;}
        static void Entered(PlayModeStateChange state)
        {
            if(state!=PlayModeStateChange.EnteredPlayMode)return;
            phase=0;errors=false;began=EditorApplication.timeSinceStartup;EditorApplication.update+=Check;
        }
        static EnemyEntity Dummy(BattleGame game,float x,float height)
        {
            Vector2 position=new Vector2(x,BattleGame.GroundHeight+height);
            var enemy=game.SpawnEnemy(EnemyKind.Ground,position);
            enemy.transform.position=position;enemy.enabled=enemy.Weapon.enabled=false;
            enemy.Data.armor=enemy.Data.evasion=0;return enemy;
        }
        static Projectile2D Mortar(BattleGame game,float x,float height,Vector2 velocity,float gravity,ProjectileTeam team=ProjectileTeam.Player)
        {
            var result=new GameObject("Ground-only mortar probe").AddComponent<Projectile2D>();
            result.InitializeMortar(new Vector2(x,BattleGame.GroundHeight+height),velocity,4,gravity,1,3,.45f,
                game.Mortar.Definition.projectileSprite,attack,team);
            return result;
        }
        static void Check()
        {
            try
            {
                if(EditorApplication.timeSinceStartup-began>30)throw new Exception("Ground-only mortar audit timed out.");
                var game=BattleGame.Instance;float elapsed=Time.time-since;
                switch(phase)
                {
                    case 0:
                        game.StartBattle();game.SetStance(TankStance.Defensive);StageProgressionAudit.DisableWeapons(game);
                        foreach(var enemy in Object.FindObjectsOfType<EnemyEntity>())
                        {game.RemoveEnemy(enemy,false);enemy.gameObject.SetActive(false);Object.Destroy(enemy.gameObject);}
                        air=Dummy(game,40,5);inner=Dummy(game,40,.4f);outer=Dummy(game,42,.4f);outside=Dummy(game,44.5f,.4f);
                        airHP=air.Health;innerHP=inner.Health;outerHP=outer.Health;outsideHP=outside.Health;
                        shell=Mortar(game,40,3.5f,Vector2.up*10,20);Next();break;
                    case 1:
                        if(elapsed<.3f)return;
                        Require(shell!=null&&shell.transform.position.y>air.transform.position.y,"Mortar must survive passing upward through an enemy collider.");
                        Near(air.Health,airHP,"Passing through an airborne enemy must not deal damage");
                        Near(inner.Health,innerHP,"No damage may occur before ground contact");Next();break;
                    case 2:
                        if(elapsed<1.2f)return;
                        Require(shell==null,"Mortar must detonate and despawn on ground contact.");
                        Near(air.Health,airHP,"An enemy above the ground blast radius must remain unharmed");
                        Near(inner.Health,innerHP-4,"Inner blast must deal full damage exactly once");
                        Near(outer.Health,outerHP-2,"Outer blast must deal half damage exactly once");
                        Near(outside.Health,outsideHP,"Enemies outside the blast must remain unharmed");
                        fastAir=Dummy(game,60,4);fastInner=Dummy(game,60,.4f);fastAirHP=fastAir.Health;fastInnerHP=fastInner.Health;
                        shell=Mortar(game,60,6,Vector2.down*1000,20);Next();break;
                    case 3:
                        if(elapsed<.15f)return;
                        Require(shell==null,"Fast mortar shells must hit the swept ground without tunneling.");
                        Near(fastAir.Health,fastAirHP,"A descending fast mortar must pass through enemies above its blast radius");
                        Near(fastInner.Health,fastInnerHP-4,"Fast ground impact must apply a single area hit");
                        game.BaseTankStats.armor=game.BaseTankStats.evasion=0;hull=game.Hull;
                        float top=game.HullHitbox.GetComponent<Collider2D>().bounds.max.y;
                        shell=Mortar(game,game.TankAimPoint.x,top-BattleGame.GroundHeight+1,Vector2.down*4,0,ProjectileTeam.Enemy);
                        Next();break;
                    case 4:
                        if(elapsed<.4f)return;
                        Require(shell!=null,"Enemy mortar must also pass through the tank hull before ground impact.");
                        Near(game.Hull,hull,"Enemy mortar must not deal direct hull-contact damage");Next();break;
                    case 5:
                        if(elapsed<2)return;
                        Require(shell==null,"Enemy mortar must detonate at the ground.");
                        Near(game.Hull,hull-4,"Enemy mortar must apply its ground area damage exactly once");
                        Near(inner.Health,innerHP-4,"Enemy mortar must not damage friendly enemies");
                        shell=new GameObject("Ordinary cannon contact probe").AddComponent<Projectile2D>();
                        shell.Initialize((Vector2)air.transform.position+Vector2.left*3,Vector2.right,30,4,Color.white,true,10,0,combatStats:attack);
                        Next();break;
                    case 6:
                        if(elapsed<.25f)return;
                        Require(shell==null,"Ordinary cannon rounds must retain unit-contact collision.");
                        Near(air.Health,airHP-4,"Ordinary cannon rounds must still damage enemies on contact");
                        Require(!errors,"Ground-only mortar audit must have no runtime errors.");
                        Debug.Log("MORTAR_GROUND_ONLY_OK: ascending and descending pass-through, no contact damage, swept fast ground impacts, full/half/outside AoE, exactly-once hits, enemy mortar hull pass-through and ordinary cannon collision.");
                        Finish(0);break;
                }
            }
            catch(Exception exception){Debug.LogException(exception);Finish(1);}
        }
        static void Finish(int code)
        {
            saves?.Dispose();EditorApplication.update-=Check;EditorApplication.playModeStateChanged-=Entered;Application.logMessageReceived-=Log;
            EditorSettings.enterPlayModeOptionsEnabled=false;EditorApplication.Exit(code);
        }
    }
}
