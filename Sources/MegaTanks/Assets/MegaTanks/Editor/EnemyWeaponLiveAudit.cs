using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace MegaTanks.Editor
{
    // Uses the saved range, speed, ammo and accuracy values without test-only weapon buffs.
    public static class EnemyWeaponLiveAudit
    {
        static int step;
        static double since;
        static bool errors;
        static EnemyEntity enemy;
        static float hp,startX;
        static string database;
        public static void Run()
        {
            EditorSceneManager.OpenScene(MegaTanksSetup.ScenePath);
            EditorSettings.enterPlayModeOptionsEnabled=true;EditorSettings.enterPlayModeOptions=EnterPlayModeOptions.DisableDomainReload;
            EditorApplication.playModeStateChanged+=Entered;EditorApplication.isPlaying=true;
        }
        static void Entered(PlayModeStateChange state)
        {
            if(state!=PlayModeStateChange.EnteredPlayMode)return;
            step=0;errors=false;since=EditorApplication.timeSinceStartup;
            Application.logMessageReceived+=Log;EditorApplication.update+=Check;
        }
        static void Log(string text,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception)errors=true;}
        static void Require(bool value,string text){if(!value)throw new Exception(text);}
        static void BeginCase(BattleGame game,int index)
        {
            game.StartBattle();game.SetStance(TankStance.Defensive);StageProgressionAudit.DisableWeapons(game);
            foreach(var old in UnityEngine.Object.FindObjectsOfType<EnemyEntity>())
            {game.RemoveEnemy(old,false);old.gameObject.SetActive(false);UnityEngine.Object.Destroy(old.gameObject);}
            var kind=index==2?EnemyKind.Ground:EnemyKind.Flyer;
            var data=game.EnemyDatabase.Get(kind);float y=index==0?2.1f:2.55f;
            if(kind==EnemyKind.Ground)y=BattleGame.GroundHeight+1;
            enemy=game.SpawnEnemy(kind,new Vector2(game.TankAimPoint.x+data.weapon.range+2,y));
            float height=enemy.transform.position.y-game.TankAimPoint.y;
            float distance=data.weapon.range+2;
            enemy.transform.position=new Vector3(game.TankAimPoint.x+Mathf.Sqrt(Mathf.Max(0,distance*distance-height*height)),enemy.transform.position.y,0);
            if(index==3){game.MachineGun.Stats.range=4;game.SetStance(TankStance.Aggressive);}
            hp=game.Hull;startX=game.Artwork.transform.position.x;
            Debug.Log("ENEMY_WEAPON_LIVE_START case="+index+" kind="+kind+" tank="+game.TankAimPoint+" enemy="+enemy.transform.position+" muzzle="+enemy.Artwork.Muzzle.position+" range="+enemy.Weapon.Stats.range+" ammo="+enemy.Weapon.Ammo+" accuracy="+enemy.Weapon.Stats.accuracy+" stopX="+enemy.StoppingX);
            since=EditorApplication.timeSinceStartup;
        }
        static void Check()
        {
            try
            {
                var game=BattleGame.Instance;
                if(step==0){database=JsonUtility.ToJson(game.EnemyDatabase);BeginCase(game,0);step=1;return;}
                if(EditorApplication.timeSinceStartup-since<6)return;
                bool canSolve=Ballistics2D.TryIntercept(enemy.Artwork.Muzzle.position,game.TankAimPoint,game.TankVelocity,enemy.Weapon.Stats.projectileSpeed,enemy.Weapon.Stats.gravity,out var velocity);
                Debug.Log("ENEMY_WEAPON_LIVE_RESULT case="+(step-1)+" centerDistance="+Vector2.Distance(enemy.transform.position,game.TankAimPoint)+" muzzleDistance="+Vector2.Distance(enemy.Artwork.Muzzle.position,game.TankAimPoint)+" inRange="+enemy.InWeaponRange+" shots="+enemy.Weapon.ShotsFired+" ammo="+enemy.Weapon.Ammo+" hull="+game.Hull+" intercept="+canSolve+" velocity="+velocity);
                Require(enemy.Weapon.ShotsFired>0,"Enemy did not fire in live case "+(step-1));
                Require(enemy.Weapon.Ammo==enemy.Weapon.MaxAmmo-enemy.Weapon.ShotsFired,"Enemy must consume saved reserves per shot.");
                Require(game.Hull<hp&&game.Hull>0,"Enemy projectiles must damage the tank in live case "+(step-1));
                if(step==4)Require(game.Artwork.transform.position.x>startX,"The moving-tank case must exercise actual Aggressive movement.");
                if(step<4){BeginCase(game,step);step++;return;}
                Require(JsonUtility.ToJson(game.EnemyDatabase)==database,"Live checks must preserve the enemy data manager.");
                Require(!errors,"Live enemy weapon checks must not emit runtime errors.");
                Debug.Log("MEGATANKS_ENEMY_WEAPONS_LIVE_OK: saved ranges/ammo/accuracy, both flight lanes, ground cannon, real approach/firing/damage, and a moving player tank.");Finish(0);
            }
            catch(Exception exception){Debug.LogException(exception);Finish(1);}
        }
        static void Finish(int code)
        {
            EditorApplication.update-=Check;EditorApplication.playModeStateChanged-=Entered;Application.logMessageReceived-=Log;
            EditorSettings.enterPlayModeOptionsEnabled=false;EditorApplication.Exit(code);
        }
    }
}
