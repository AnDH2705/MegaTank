using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace MegaTanks.Editor
{
    public static class MissFeedbackAudit
    {
        static StageRewardSaveScope saves;
        static bool errors, oldOptions;
        static EnterPlayModeOptions options;
        static void Require(bool value,string message){if(!value)throw new Exception(message);}
        public static void Run()
        {
            saves=new StageRewardSaveScope();
            EditorSceneManager.OpenScene(MegaTanksSetup.ScenePath);
            oldOptions=EditorSettings.enterPlayModeOptionsEnabled;options=EditorSettings.enterPlayModeOptions;
            EditorSettings.enterPlayModeOptionsEnabled=true;EditorSettings.enterPlayModeOptions=EnterPlayModeOptions.DisableDomainReload;
            EditorApplication.playModeStateChanged+=State;EditorApplication.isPlaying=true;
        }
        static void State(PlayModeStateChange state)
        {
            if(state==PlayModeStateChange.EnteredPlayMode)EditorApplication.delayCall+=Check;
            if(state==PlayModeStateChange.EnteredEditMode)
            {
                saves.Dispose();EditorSettings.enterPlayModeOptionsEnabled=oldOptions;EditorSettings.enterPlayModeOptions=options;
                EditorApplication.Exit(errors?1:0);
            }
        }
        static void Check()
        {
            try
            {
                var game=BattleGame.Instance;game.StartBattle();StageProgressionAudit.DisableWeapons(game);
                var enemy=game.SpawnEnemy(EnemyKind.Ground,game.TankAimPoint+Vector2.right*10);
                enemy.enabled=enemy.Weapon.enabled=false;
                var attack=new DamageAttack(10,new WeaponCombatStats{accuracy=100});
                enemy.Data.evasion=100;float hp=enemy.Health;
                int count=game.DamageNumbers.Count;
                var result=enemy.ReceiveAttack(attack);
                Require(!result.Hit&&enemy.Health==hp&&game.DamageNumbers.Count==count+1,"Evaded enemy attack emits exactly one label without HP loss.");
                Require(game.DamageNumbers.Last().IsMiss&&game.DamageNumbers.Last().DisplayText=="Miss"&&!game.DamageNumbers.Last().IsTank,"Enemy label says Miss.");
                game.BaseTankStats.evasion=100;hp=game.Hull;
                result=game.ReceiveAttack(attack);
                Require(!result.Hit&&game.Hull==hp&&game.DamageNumbers.Last().DisplayText=="Miss"&&game.DamageNumbers.Last().IsTank,"Player evasion says Miss with player styling.");
                enemy.Data.evasion=0;enemy.Data.armor=1000;count=game.DamageNumbers.Count;
                result=enemy.ReceiveAttack(attack);
                Require(result.Hit&&result.Damage==0&&game.DamageNumbers.Count==count,"Armor block is not mislabeled Miss.");
                enemy.Data.armor=0;enemy.Damage(1.9f);
                Require(!game.DamageNumbers.Last().IsMiss&&game.DamageNumbers.Last().DisplayText=="-1","Damage still rounds down.");
                enemy.Data.evasion=100;
                game.DetonateMortar(enemy.transform.position,attack,2.5f,5);
                Require(game.DamageNumbers.Last().DisplayText=="Miss","AoE also shows evasion feedback.");
                game.ReturnHome();
                Debug.Log("MISS_FEEDBACK_PASS: enemy/player evasion and AoE show Miss without HP loss, armor blocks stay distinct, damage rounding preserved.");
            }
            catch(Exception e){errors=true;Debug.LogException(e);}
            finally{EditorApplication.isPlaying=false;}
        }
    }
}
