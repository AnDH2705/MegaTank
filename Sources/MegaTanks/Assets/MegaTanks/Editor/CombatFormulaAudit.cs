using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MegaTanks.Editor
{
    public static class CombatFormulaAudit
    {
        static int step;
        static double since;
        static bool errors;
        static EnemyEntity target,shooter;
        static float health,hull;
        static string profiles;
        static void Require(bool value,string message){if(!value)throw new Exception(message);}
        static void Near(float value,float expected,string message)=>Require(Mathf.Abs(value-expected)<.001f,message+": "+value+" vs "+expected);
        static void Next(){step++;since=EditorApplication.timeSinceStartup;}
        static string Profiles(BattleGame game)=>JsonUtility.ToJson(game.TankProfile)+JsonUtility.ToJson(game.StartingLoadout.machineGun)+JsonUtility.ToJson(game.StartingLoadout.cannon)+JsonUtility.ToJson(game.StartingLoadout.mortar)+JsonUtility.ToJson(game.EnemyDatabase);
        static void PureCalculations()
        {
            var weapon=new WeaponCombatStats{damageBonus=20,accuracy=80,armorPenetration=25,criticalChance=60,criticalDamage=100,damageType=DamageType.Fire};
            var defense=new UnitDefenseStats{armor=40,evasion=30,criticalChanceReduction=10,criticalDamageReduction=25};
            var attack=new DamageAttack(100,weapon);
            var hit=CombatDamage.Calculate(attack,defense,.499f,.499f);
            Require(hit.Hit&&hit.Critical&&hit.DamageType==DamageType.Fire,"Hit/critical probabilities and damage tags must survive resolution.");
            Near(hit.Damage,180,"Bonus, critical reduction and penetration order");
            Near(CombatDamage.Calculate(attack,defense,.499f,.5f).Damage,90,"Critical threshold must be exclusive");
            Near(CombatDamage.Calculate(attack,defense,.499f,.499f,.5f).Damage,75,"Half AoE applies before armor");
            Require(!CombatDamage.Calculate(attack,defense,.5f,0).Hit,"Hit threshold must use accuracy minus evasion once.");
            weapon.accuracy=0;
            Require(!CombatDamage.Calculate(new DamageAttack(100,weapon),new UnitDefenseStats(),0,0).Hit,"Zero accuracy must always miss, including roll zero.");
            weapon.accuracy=100;weapon.armorPenetration=100;weapon.criticalChance=100;weapon.criticalDamage=100;
            Near(CombatDamage.Calculate(new DamageAttack(100,weapon),new UnitDefenseStats{armor=1000,criticalChanceReduction=100},1,0).Damage,120,"100% hit, full penetration, and full critical chance reduction");
            Near(CombatDamage.Calculate(new DamageAttack(100,weapon),new UnitDefenseStats{criticalDamageReduction=200},1,1).Damage,120,"Critical reduction must never make a critical weaker than an ordinary hit");
            Require(!CombatDamage.Calculate(new DamageAttack(100,weapon),new UnitDefenseStats{evasion=100},0,0).Hit,"100 evasion must negate 100 accuracy.");
            weapon.armorPenetration=0;
            Near(CombatDamage.Calculate(new DamageAttack(100,weapon),new UnitDefenseStats{armor=1000},0,0).Damage,0,"Armor must not heal the target");
            Near(attack.WeaponDamage,120,"In-flight attacks must retain their original bonus snapshot");
            var high=new WeaponCombatStats{accuracy=170,criticalChance=180,damageBonus=150,criticalDamage=250,armorPenetration=150};
            var highDefense=new UnitDefenseStats{evasion=130,criticalChanceReduction=130,criticalDamageReduction=125,armor=100};
            var highAttack=new DamageAttack(100,high);
            var highHit=CombatDamage.Calculate(highAttack,highDefense,.399f,.499f);
            Require(highHit.Hit&&highHit.Critical,"Over-100 raw accuracy and critical chance must be subtracted before roll clamping.");
            Near(highHit.Damage,612.5f,"Over-100 damage, critical reduction and penetration must retain their full values");
            Require(!CombatDamage.Calculate(highAttack,highDefense,.4f,0).Hit,"170 accuracy minus 130 evasion must yield exactly 40% hit chance.");
            Require(!CombatDamage.Calculate(highAttack,highDefense,.399f,.5f).Critical,"180 critical chance minus 130 reduction must yield exactly 50% critical chance.");
            high.accuracy=250;high.criticalChance=250;
            var guaranteed=CombatDamage.Calculate(new DamageAttack(100,high),highDefense,1,1);
            Require(guaranteed.Hit&&guaranteed.Critical,"Only final roll probabilities above 100% must become guaranteed.");
            high.accuracy=100;
            Require(!CombatDamage.Calculate(new DamageAttack(100,high),highDefense,0,0).Hit,"A final probability below zero must never hit.");
            foreach(var field in typeof(WeaponCombatStats).GetFields().Concat(typeof(UnitDefenseStats).GetFields()))
                Require(!Attribute.IsDefined(field,typeof(RangeAttribute)),"Percentage properties must not have Inspector upper caps: "+field.Name);
        }
        public static void Run()
        {
            PureCalculations();EditorSceneManager.OpenScene(MegaTanksSetup.ScenePath);
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
        static EnemyEntity Dummy(BattleGame game,Vector2 position)
        {
            var enemy=game.SpawnEnemy(EnemyKind.Ground,position);enemy.enabled=enemy.Weapon.enabled=false;
            enemy.transform.position=position;return enemy;
        }
        static void Shot(Vector2 destination,WeaponCombatStats stats,ProjectileTeam team)
        {
            var direction=team==ProjectileTeam.Player?Vector2.right:Vector2.left;
            var projectile=new GameObject("Formula projectile probe").AddComponent<Projectile2D>();
            projectile.Initialize(destination-direction*2,direction,30,10,Color.white,false,10,combatStats:stats,faction:team);
            Require(projectile.Attack.DamageType==stats.damageType,"Projectile must carry its damage tag.");
        }
        static void Check()
        {
            if(EditorApplication.timeSinceStartup-since<(step==5?2.2:.35))return;
            try
            {
                var game=BattleGame.Instance;
                switch(step)
                {
                    case 0:
                        game.StartBattle();game.SetStance(TankStance.Defensive);StageProgressionAudit.DisableWeapons(game);profiles=Profiles(game);
                        Require(game.MachineGun.Stats.accuracy==0&&game.Mortar.Stats.accuracy==0,"Saved zero accuracy must stay zero.");
                        foreach(var enemy in UnityEngine.Object.FindObjectsOfType<EnemyEntity>())
                        {
                            Require(enemy.Weapon.Ammo==0&&enemy.Weapon.MaxAmmo==0,"Enemy zero Max Ammo must create empty weapons.");
                            game.RemoveEnemy(enemy,false);enemy.gameObject.SetActive(false);UnityEngine.Object.Destroy(enemy.gameObject);
                        }
                        target=Dummy(game,new Vector2(game.TankAimPoint.x+12,BattleGame.GroundHeight+1));target.Data.armor=4;health=target.Health;
                        Shot(target.transform.position,new WeaponCombatStats{damageBonus=50,accuracy=0},ProjectileTeam.Player);Next();break;
                    case 1:
                        Near(target.Health,health,"A physical projectile with zero accuracy must not damage its target");
                        var stats=new WeaponCombatStats{damageBonus=50,accuracy=100,criticalChance=100,criticalDamage=100,damageType=DamageType.Fire};
                        Shot(target.transform.position,stats,ProjectileTeam.Player);stats.accuracy=0;stats.damageBonus=999;Next();break;
                    case 2:
                        Near(target.Health,health-26,"Physical player projectile must apply snapshotted bonus, critical and target armor");
                        game.BaseTankStats.armor=3;hull=game.Hull;
                        game.ReceiveAttack(new DamageAttack(10,new WeaponCombatStats()));Near(game.Hull,hull,"Player must also reject zero-accuracy attacks");
                        Shot(game.TankAimPoint,new WeaponCombatStats{damageBonus=50,accuracy=100},ProjectileTeam.Enemy);Next();break;
                    case 3:
                        Near(game.Hull,hull-12,"Physical enemy projectile must respect the player's armor");
                        var impact=new Vector2(50,0);var full=Dummy(game,impact+Vector2.right*2);var half=Dummy(game,impact+Vector2.right*4);var evade=Dummy(game,impact+Vector2.left);
                        foreach(var enemy in new[]{full,half,evade}){enemy.Data.armor=4;enemy.Data.criticalDamageReduction=50;}
                        evade.Data.evasion=100;float fullHp=full.Health,halfHp=half.Health,evadeHp=evade.Health;
                        game.DetonateMortar(impact,new DamageAttack(20,new WeaponCombatStats()),2.5f,5);
                        Near(full.Health,fullHp,"Zero accuracy must also suppress mortar AoE");
                        game.DetonateMortar(impact,new DamageAttack(20,new WeaponCombatStats{damageBonus=50,accuracy=100,criticalChance=100,criticalDamage=100}),2.5f,5);
                        Near(full.Health,fullHp-41,"Full mortar zone must apply critical reduction and armor");
                        Near(half.Health,halfHp-18.5f,"Half mortar zone must subtract armor after the zone multiplier");Near(evade.Health,evadeHp,"AoE must evaluate evasion independently per target");
                        game.MachineGun.Stats.damageBonus=25;game.MachineGun.IncreaseDamage(.1f);game.MachineGun.IncreaseDamage(.1f);
                        Near(game.MachineGun.DamagePerShot,game.MachineGun.Stats.damage*1.45f,"Card bonuses must add percentage points without compounding");Next();break;
                    case 4:
                        shooter=game.SpawnEnemy(EnemyKind.Flyer,new Vector2(game.TankAimPoint.x+15,2.1f));shooter.enabled=false;
                        shooter.Weapon.Stats.reserveCapacity=1;shooter.Weapon.Stats.accuracy=100;shooter.Weapon.Stats.damage=0;shooter.Weapon.Resupply(1);Next();break;
                    case 5:
                        Require(shooter.Weapon.ShotsFired==1&&shooter.Weapon.Ammo==0,"An enemy must stop after its finite ammo is depleted, without reload refilling it.");
                        shooter.Weapon.Resupply(1);Next();break;
                    case 6:
                        Require(shooter.Weapon.ShotsFired==2&&shooter.Weapon.Ammo==0,"An explicitly resupplied enemy must be able to fire again.");
                        Require(Profiles(game)==profiles,"Runtime defense and weapon changes must not mutate profiles.");
                        game.StartBattle();StageProgressionAudit.DisableWeapons(game);
                        Require(game.BaseTankStats.armor==0&&game.MachineGun.Stats.damageBonus==0&&game.MachineGun.Stats.accuracy==0,"Restart must restore saved zero-default stats.");
                        Require(!errors,"Formula integration must not emit runtime errors.");
                        Debug.Log("MEGATANKS_COMBAT_FORMULAS_OK: exact percentage thresholds, zero accuracy, armor/penetration, critical reductions, physical shots both teams, snapshot damage tags, per-target mortar AoE, additive cards, enemy ammo/resupply, profile isolation and restart.");Finish(0);break;
                }
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
