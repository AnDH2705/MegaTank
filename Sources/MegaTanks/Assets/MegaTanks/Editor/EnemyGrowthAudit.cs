using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace MegaTanks.Editor
{
    public static class EnemyGrowthAudit
    {
        static bool errors;
        static void Require(bool value,string message){if(!value)throw new Exception(message);}
        static void CheckNumericPairs(object source,Func<object> calculate)
        {
            var fields=source.GetType().GetFields();
            foreach(var field in fields)
            {
                if(field.Name.EndsWith("Grow")||field.Name=="scale"||field.Name=="artworkWidth"||!(field.FieldType==typeof(float)||field.FieldType==typeof(int)))continue;
                var grow=source.GetType().GetField(field.Name+"Grow");Require(grow!=null,field.Name+" needs a Grow field.");
                Require((float)grow.GetValue(source)==0,field.Name+" Grow must default to zero.");
                if(field.FieldType==typeof(float))field.SetValue(source,10.4f);else field.SetValue(source,10);
                grow.SetValue(source,.61f);var result=calculate();
                float sum=Convert.ToSingle(field.GetValue(source))+7*.61f;
                Require(Convert.ToSingle(field.GetValue(result))==(field.Name=="baseSpeed"?sum:Mathf.Floor(sum)),field.Name+" must apply its growth rounding rule.");
                grow.SetValue(source,0f);
            }
        }
        static void PureChecks()
        {
            var definition=new EnemyDefinition();CheckNumericPairs(definition,()=>definition.AtDifficulty(7));
            var weapon=new EnemyWeaponStats();CheckNumericPairs(weapon,()=>weapon.AtDifficulty(7));
            definition.advanceInsideRange=new Vector2(1.7f,5.2f);definition.advanceInsideRangeGrow=new Vector2(.4f,-.2f);
            Require(definition.AtDifficulty(7).advanceInsideRange==new Vector2(4,3),"Both advance-range endpoints must grow and floor independently.");
            weapon.accuracy=99.8f;weapon.accuracyGrow=1.3f;Require(weapon.AtDifficulty(10).accuracy==112,"Growth must preserve raw percentages above 100.");
            weapon.reloadTime=3.7f;weapon.reloadTimeGrow=-.4f;Require(weapon.AtDifficulty(4).reloadTime==2,"Negative Grow must reduce a stat before flooring.");
            Require(new EnemyDefinition{baseSpeed=.75f}.AtDifficulty(100).baseSpeed==.75f,"Fractional movement speed must be preserved.");
            var stage=ScriptableObject.CreateInstance<StageDefinition>();stage.InitializeStage(3);Require(stage.stageDifficulty==20,"Stage 3 must default to 20.");stage.stageDifficulty=17;Require(stage.stageDifficulty==17,"Custom stage difficulty must remain editable.");UnityEngine.Object.DestroyImmediate(stage);
            var database=AssetDatabase.LoadAssetAtPath<EnemyDataManager>(EnemyDataSetup.AssetPath);
            Require(database.enemyPrefabs.Length>=2,"Existing enemies need prefab stat profiles.");
            foreach(var profile in database.enemyPrefabs)
            {
                Require(profile!=null&&profile.stats.artworkPrefab==profile.GetComponent<EnemyVehicleArtwork>(),"Prefab profiles must reference their own artwork.");
            }
        }
        public static void Run()
        {
            try
            {
                PureChecks();EditorSceneManager.OpenScene(MegaTanksSetup.ScenePath);
                EditorSettings.enterPlayModeOptionsEnabled=true;EditorSettings.enterPlayModeOptions=EnterPlayModeOptions.DisableDomainReload;
                EditorApplication.playModeStateChanged+=Entered;EditorApplication.isPlaying=true;
            }
            catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
        }
        static void Entered(PlayModeStateChange state){if(state==PlayModeStateChange.EnteredPlayMode)EditorApplication.delayCall+=LiveChecks;}
        static void Silence(BattleGame game)
        {
            game.SetPaused(true);StageProgressionAudit.DisableWeapons(game);
            foreach(var enemy in UnityEngine.Object.FindObjectsOfType<EnemyEntity>())if(enemy.Weapon!=null)enemy.Weapon.enabled=false;
        }
        static EnemyEntity Flyer(BattleGame game)=>UnityEngine.Object.FindObjectsOfType<EnemyEntity>().First(e=>e.Alive&&e.WaveNumber==game.Wave&&e.Kind==EnemyKind.Flyer);
        static void RequireWave(BattleGame game,int difficulty,float hp,float damage)
        {
            var enemy=Flyer(game);Require(game.BattleDifficulty==difficulty&&enemy.Difficulty==difficulty,"Battle and enemy must use stage difficulty plus the 1-based round.");
            Require(enemy.Health==hp&&enemy.Weapon.Stats.damage==damage,"HP and actual weapon stats must use the calculated snapshot.");
            Require(enemy.Speed==2.75f+difficulty*.25f&&enemy.ScrapsReward==Mathf.FloorToInt(20+difficulty*1.5f),"Speed must keep fractional growth; scraps must remain rounded down.");
            Require(enemy.Weapon.Stats.accuracy==Mathf.Floor(100+difficulty*.75f),"Actual weapon accuracy must preserve scaled values above 100.");
        }
        static void Clear(BattleGame game){StageProgressionAudit.ClearWave(game);}
        static void AdvanceWave(BattleGame game)
        {
            typeof(StageProgression).GetField("waveBreak",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(game.Stage,-1f);
            typeof(BattleGame).GetField("paused",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(game,false);Time.timeScale=1;typeof(StageProgression).GetMethod("Update",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(game.Stage,null);Silence(game);
        }
        static void LiveChecks()
        {
            Application.logMessageReceived+=Log;
            try
            {
                var game=BattleGame.Instance;var database=ScriptableObject.CreateInstance<EnemyDataManager>();database.healthBarPrefab=game.EnemyDatabase.healthBarPrefab;
                database.enemyPrefabs=game.EnemyDatabase.Definitions.Select(definition=>
                {
                    var profile=new GameObject("Audit "+definition.id).AddComponent<EnemyStatsProfile>();profile.stats=definition.Copy();
                    // Use deterministic temporary balance; keep this check valid after Inspector tuning.
                    foreach(var value in new object[]{profile.stats,profile.stats.weapon})foreach(var f in value.GetType().GetFields().Where(f=>f.Name.EndsWith("Grow")))
                        f.SetValue(value,f.FieldType==typeof(Vector2)?(object)Vector2.zero:0f);
                    profile.stats.baseHealth=definition.kind==EnemyKind.Flyer?26:70;profile.stats.baseScraps=definition.kind==EnemyKind.Flyer?20:30;
                    profile.stats.weapon.damage=definition.kind==EnemyKind.Flyer?2:8;profile.stats.weapon.accuracy=100;
                    profile.stats.baseSpeed=2.75f;profile.stats.baseSpeedGrow=.25f;profile.stats.baseHealthGrow=2.5f;profile.stats.baseScrapsGrow=1.5f;
                    profile.stats.weapon.damageGrow=.5f;profile.stats.weapon.accuracyGrow=.75f;return profile;
                }).ToArray();
                typeof(BattleGame).GetField("enemyDataManager",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(game,database);
                var stage=UnityEngine.Object.Instantiate(game.Stage.Definition);stage.InitializeStage(3);stage.stageDifficulty=13;game.SelectStage(stage);
                game.StartBattle();Silence(game);RequireWave(game,14,61,9);
                Require(database.Get(EnemyKind.Flyer).baseHealth==26&&database.Get(EnemyKind.Flyer).weapon.damage==2,"Calculation must not mutate prefab base stats.");
                Clear(game);Require(game.Rewards.ChooseReward(0),"First battle must still award a card.");
                var node=game.Stage.Route.Get(game.Stage.Current.next[0]);node.type=StageNodeType.HardBattle;Require(game.Stage.SelectNode(node.id),"Hard battle round must be reachable.");Silence(game);
                RequireWave(game,15,63,9);
                database.Get(EnemyKind.Flyer).baseHealth=900;database.Get(EnemyKind.Flyer).weapon.damage=200;
                Clear(game);Require(game.Stage.Mode==StageMode.WaveBreak,"Hard battles need the next wave.");AdvanceWave(game);RequireWave(game,15,63,9);
                Clear(game);AdvanceWave(game);RequireWave(game,15,63,9);Require(game.Stage.EncounterWave==3,"All three hard-battle waves must retain the same round difficulty.");
                Clear(game);Require(game.Rewards.ChooseReward(0),"Hard battle must still offer its reward.");
                node=game.Stage.Route.Get(game.Stage.Current.next[0]);node.type=StageNodeType.Battle;Require(game.Stage.SelectNode(node.id),"Next battle round must be reachable.");Silence(game);RequireWave(game,16,940,208);
                Require(!errors,"No runtime errors permitted.");Debug.Log("ENEMY_GROWTH_OK: numeric Grow coverage, zero creation defaults, floor-after-sum, uncapped percentages, custom stage difficulty, 1-based rounds, live HP/weapons/speed/scraps, stable hard-battle snapshots, next-round recalculation, prefab bases unchanged.");Finish(0);
            }
            catch(Exception e){Debug.LogException(e);Finish(1);}
        }
        static void Log(string message,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception)errors=true;}
        static void Finish(int code){Application.logMessageReceived-=Log;EditorApplication.playModeStateChanged-=Entered;EditorSettings.enterPlayModeOptionsEnabled=false;EditorApplication.Exit(code);}
    }
}
