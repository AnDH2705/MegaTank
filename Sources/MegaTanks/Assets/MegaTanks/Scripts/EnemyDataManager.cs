using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace MegaTanks
{
    public enum EnemyWeaponType { MachineGun, Cannon }

    [Serializable]
    public sealed class EnemyWeaponStats : WeaponCombatStats
    {
        public EnemyWeaponType type;
        [Min(1), InspectorName("Base Damage")] public float damage = 2;
        public float damageGrow;
        public float damageBonusGrow, accuracyGrow, armorPenetrationGrow, criticalChanceGrow, criticalDamageGrow;
        [Min(0), InspectorName("Max Ammo")] public int reserveCapacity;
        public float reserveCapacityGrow;
        [Min(1)] public float range = 30;
        public float rangeGrow;
        [Min(1)] public int salvoSize = 3;
        public float salvoSizeGrow;
        [Min(.01f)] public float shotDelay = .1f;
        public float shotDelayGrow;
        [Min(.02f)] public float reloadTime = 1.8f;
        public float reloadTimeGrow;
        [Min(1)] public float projectileSpeed = 22;
        public float projectileSpeedGrow;
        [Min(0)] public float gravity = .35f;
        public float gravityGrow;
        public EnemyWeaponStats Copy() => (EnemyWeaponStats)MemberwiseClone();
        public EnemyWeaponStats AtDifficulty(int difficulty)
        {
            var result=Copy();
            result.damage=EnemyDefinition.Grow(damage,damageGrow,difficulty);
            result.damageBonus=EnemyDefinition.Grow(damageBonus,damageBonusGrow,difficulty);
            result.accuracy=EnemyDefinition.Grow(accuracy,accuracyGrow,difficulty);
            result.armorPenetration=EnemyDefinition.Grow(armorPenetration,armorPenetrationGrow,difficulty);
            result.criticalChance=EnemyDefinition.Grow(criticalChance,criticalChanceGrow,difficulty);
            result.criticalDamage=EnemyDefinition.Grow(criticalDamage,criticalDamageGrow,difficulty);
            result.reserveCapacity=EnemyDefinition.Grow(reserveCapacity,reserveCapacityGrow,difficulty);
            result.range=EnemyDefinition.Grow(range,rangeGrow,difficulty);
            result.salvoSize=EnemyDefinition.Grow(salvoSize,salvoSizeGrow,difficulty);
            result.shotDelay=EnemyDefinition.Grow(shotDelay,shotDelayGrow,difficulty);
            result.reloadTime=EnemyDefinition.Grow(reloadTime,reloadTimeGrow,difficulty);
            result.projectileSpeed=EnemyDefinition.Grow(projectileSpeed,projectileSpeedGrow,difficulty);
            result.gravity=EnemyDefinition.Grow(gravity,gravityGrow,difficulty);
            return result;
        }
    }
    [Serializable]
    public sealed class EnemyDefinition : UnitDefenseStats
    {
        public string id;
        public EnemyKind kind;
        [Min(1), InspectorName("HP")] public float baseHealth = 26;
        [InspectorName("HP Grow")] public float baseHealthGrow;
        public float armorGrow, evasionGrow, criticalChanceReductionGrow, criticalDamageReductionGrow;
        [Min(0), InspectorName("Speed")] public float baseSpeed = .72f;
        [InspectorName("Speed Grow")] public float baseSpeedGrow;
        [Tooltip("Random extra horizontal travel after entering weapon range, in world units.")] public Vector2 advanceInsideRange = new Vector2(1,5);
        public Vector2 advanceInsideRangeGrow;
        [Min(0)] public int score = 75;
        public float scoreGrow;
        [Min(0), InspectorName("Scraps")] public int baseScraps = 20;
        [InspectorName("Scraps Grow")] public float baseScrapsGrow;
        public Color color = new Color(1,.63f,.25f);
        public EnemyVehicleArtwork artworkPrefab;
        [Min(.1f)] public float artworkWidth = 2.2f;
        [Min(.1f)] public float scale = 1.25f;
        public float ScaledArtworkWidth => artworkWidth*Mathf.Max(.1f,scale);
        public EnemyWeaponStats weapon = new EnemyWeaponStats();
        public EnemyDefinition Copy(){var copy=(EnemyDefinition)MemberwiseClone();copy.weapon=weapon.Copy();return copy;}
        public static int Grow(float value,float grow,int difficulty)=>Mathf.FloorToInt(value+difficulty*grow);
        public EnemyDefinition AtDifficulty(int difficulty)
        {
            var result=Copy();
            result.baseHealth=Grow(baseHealth,baseHealthGrow,difficulty);
            result.armor=Grow(armor,armorGrow,difficulty);
            result.evasion=Grow(evasion,evasionGrow,difficulty);
            result.criticalChanceReduction=Grow(criticalChanceReduction,criticalChanceReductionGrow,difficulty);
            result.criticalDamageReduction=Grow(criticalDamageReduction,criticalDamageReductionGrow,difficulty);
            result.baseSpeed=baseSpeed+difficulty*baseSpeedGrow;
            result.advanceInsideRange=new Vector2(Grow(advanceInsideRange.x,advanceInsideRangeGrow.x,difficulty),Grow(advanceInsideRange.y,advanceInsideRangeGrow.y,difficulty));
            result.score=Grow(score,scoreGrow,difficulty);
            result.baseScraps=Grow(baseScraps,baseScrapsGrow,difficulty);
            result.weapon=weapon.AtDifficulty(difficulty);
            return result;
        }
    }
    [CreateAssetMenu(menuName="MegaTanks/Enemy Data Manager")]
    public sealed class EnemyDataManager : ScriptableObject
    {
        [Header("World UI prefab")] public EnemyHealthBarUI healthBarPrefab;
        [Tooltip("Edit each prefab's Enemy Stats Profile to tune its base stats and Grow values.")]
        public EnemyStatsProfile[] enemyPrefabs = Array.Empty<EnemyStatsProfile>();
        // Retained for migration and databases created before prefab-based stat profiles.
        [SerializeField, HideInInspector] EnemyDefinition[] enemies =
        {
            new EnemyDefinition{id="FlyingEnemy1",kind=EnemyKind.Flyer},
            new EnemyDefinition{id="GroundEnemy1",kind=EnemyKind.Ground,baseHealth=70,baseSpeed=.42f,score=100,baseScraps=30,color=new Color(.96f,.37f,.4f),weapon=new EnemyWeaponStats{type=EnemyWeaponType.Cannon,damage=8,range=40,salvoSize=1,reloadTime=4,projectileSpeed=18,gravity=3}}
        };
        public IEnumerable<EnemyDefinition> Definitions
        {
            get
            {
                if(enemyPrefabs!=null)foreach(var prefab in enemyPrefabs)if(prefab!=null)yield return prefab.stats;
                foreach(var enemy in enemies)if(enemy!=null&&(enemyPrefabs==null||!enemyPrefabs.Any(p=>p!=null&&p.stats.kind==enemy.kind)))yield return enemy;
            }
        }
        public EnemyDefinition Get(EnemyKind kind){foreach(var enemy in Definitions)if(enemy.kind==kind)return enemy;throw new InvalidOperationException("No enemy definition for "+kind);}
        public EnemyDefinition Get(string id){foreach(var enemy in Definitions)if(enemy.id==id)return enemy;throw new InvalidOperationException("No enemy definition named "+id);}
    }
}
