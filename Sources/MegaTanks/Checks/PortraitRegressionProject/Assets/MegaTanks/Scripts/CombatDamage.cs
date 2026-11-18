using UnityEngine;

namespace MegaTanks
{
    // Shots retain a value snapshot of the firing weapon's stats.
    public readonly struct DamageAttack
    {
        public readonly float BaseDamage,DamageBonus,Accuracy,ArmorPenetration,CriticalChance,CriticalDamage;
        public readonly DamageType DamageType;
        public DamageAttack(float baseDamage,WeaponCombatStats stats)
        {
            BaseDamage=baseDamage;DamageBonus=stats.damageBonus;Accuracy=stats.accuracy;
            ArmorPenetration=stats.armorPenetration;CriticalChance=stats.criticalChance;
            CriticalDamage=stats.criticalDamage;DamageType=stats.damageType;
        }
        public float WeaponDamage => Mathf.Max(0,BaseDamage)*Mathf.Max(0,1+DamageBonus/100);
    }
    public readonly struct DamageResult
    {
        public readonly bool Hit,Critical;
        public readonly float Damage;
        public readonly DamageType DamageType;
        public DamageResult(bool hit,bool critical,float damage,DamageType type)
        {Hit=hit;Critical=critical;Damage=damage;DamageType=type;}
    }
    public static class CombatDamage
    {
        static bool Roll(float chance,float value)
        {chance=Mathf.Clamp(chance,0,100);return chance>=100||(chance>0&&value<chance/100);}
        public static DamageResult Resolve(DamageAttack attack,UnitDefenseStats target,float areaMultiplier=1)
            => Calculate(attack,target,Random.value,Random.value,areaMultiplier);
        public static DamageResult Calculate(DamageAttack attack,UnitDefenseStats target,float hitRoll,float criticalRoll,float areaMultiplier=1)
        {
            // Evasion reduces accuracy once; a second evade roll would count it twice.
            if(!Roll(attack.Accuracy-target.evasion,hitRoll))
                return new DamageResult(false,false,0,attack.DamageType);
            bool critical=Roll(attack.CriticalChance-target.criticalChanceReduction,criticalRoll);
            float criticalBonus=critical?Mathf.Max(0,attack.CriticalDamage-target.criticalDamageReduction):0;
            float damage=attack.WeaponDamage*(1+criticalBonus/100)*Mathf.Max(0,areaMultiplier);
            float armor=Mathf.Max(0,target.armor)*(1-attack.ArmorPenetration/100);
            return new DamageResult(true,critical,Mathf.Max(0,damage-armor),attack.DamageType);
        }
    }
}
