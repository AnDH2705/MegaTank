using System;
using UnityEditor;
using UnityEngine;

namespace MegaTanks.Editor
{
    public static class CombatPropertiesAudit
    {
        static readonly string[] DefenseFields={"armor","evasion","criticalChanceReduction","criticalDamageReduction"};
        static readonly string[] WeaponFields={"damageBonus","accuracy","armorPenetration","criticalChance","criticalDamage","damageType"};
        static void Require(bool value,string message){if(!value)throw new Exception(message);}
        static void Defaults(UnitDefenseStats stats)
        {
            Require(stats.armor==0&&stats.evasion==0&&stats.criticalChanceReduction==0&&stats.criticalDamageReduction==0,"New unit defenses must default to zero.");
        }
        static void Defaults(WeaponCombatStats stats)
        {
            Require(stats.damageBonus==0&&stats.accuracy==0&&stats.armorPenetration==0&&stats.criticalChance==0&&stats.criticalDamage==0&&stats.damageType==DamageType.Normal,"New weapon properties must default to zero / Normal.");
        }
        static void Fields(SerializedProperty stats,string[] names)
        {
            Require(stats!=null,"Stats must remain serialized in the existing profile.");
            foreach(var name in names)Require(stats.FindPropertyRelative(name)!=null,"Missing serialized property: "+name);
        }
        static void CopyValues(WeaponCombatStats stats)
        {
            stats.damageBonus=25;stats.accuracy=80;stats.armorPenetration=35;stats.criticalChance=12;stats.criticalDamage=150;stats.damageType=DamageType.Fire;
        }
        public static void Validate()
        {
            Defaults(new TankStats());Defaults(new EnemyDefinition());Defaults(new GunStats());Defaults(new MortarStats());Defaults(new EnemyWeaponStats());
            Require(new EnemyWeaponStats().reserveCapacity==0,"Enemy Max Ammo is new and must default to zero.");
            Require((int)DamageType.Normal==0&&(int)DamageType.Fire==1&&Enum.GetValues(typeof(DamageType)).Length==2,"Normal and Fire damage tags must remain distinct from gun firing types.");
            foreach(var guid in AssetDatabase.FindAssets("t:TankDefinition",new[]{"Assets/MegaTanks/Data"}))
            {
                var asset=AssetDatabase.LoadAssetAtPath<TankDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                var stats=new SerializedObject(asset).FindProperty("stats");Fields(stats,DefenseFields);Fields(stats,new[]{"maxHull","advanceSpeed"});
                var copy=asset.stats.Copy();string original=JsonUtility.ToJson(asset);
                copy.armor=37;copy.evasion=18;copy.criticalChanceReduction=7;copy.criticalDamageReduction=22;
                Require(JsonUtility.ToJson(asset)==original&&!ReferenceEquals(copy,asset.stats),"Tank defense changes must stay independent of saved profiles.");
            }
            foreach(var guid in AssetDatabase.FindAssets("t:WeaponDefinition",new[]{"Assets/MegaTanks/Data/Weapons"}))
            {
                var asset=AssetDatabase.LoadAssetAtPath<WeaponDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                var stats=new SerializedObject(asset).FindProperty("stats");Fields(stats,WeaponFields);Fields(stats,new[]{"damage","range","reserveCapacity"});
                string original=JsonUtility.ToJson(asset);
                if(asset is GunDefinition gun)
                {
                    var copy=gun.stats.Copy();CopyValues(copy);var second=copy.Copy();
                    Require(JsonUtility.ToJson(second)==JsonUtility.ToJson(copy)&&!ReferenceEquals(copy,second),"Gun copies must retain every modifier and damage tag.");
                }
                if(asset is MortarDefinition mortar)
                {
                    var copy=mortar.stats.Copy();CopyValues(copy);var second=copy.Copy();
                    Require(JsonUtility.ToJson(second)==JsonUtility.ToJson(copy)&&!ReferenceEquals(copy,second),"Mortar copies must retain every modifier and damage tag.");
                }
                Require(JsonUtility.ToJson(asset)==original,"Weapon modifier changes must not mutate definition assets.");
            }
            var data=AssetDatabase.LoadAssetAtPath<EnemyDataManager>(EnemyDataSetup.AssetPath);
            var enemies=new SerializedObject(data).FindProperty("enemies");
            for(int i=0;i<enemies.arraySize;i++)
            {
                var enemy=enemies.GetArrayElementAtIndex(i);Fields(enemy,DefenseFields);Fields(enemy,new[]{"baseHealth","baseSpeed"});
                var weapon=enemy.FindPropertyRelative("weapon");Fields(weapon,WeaponFields);Fields(weapon,new[]{"damage","range","reserveCapacity"});
            }
            Debug.Log("MEGATANKS_COMBAT_PROPERTIES_OK: tank/enemy defenses, all player/enemy weapon fields, zero defaults, Normal/Fire tags, serialized legacy fields and independent stat copies.");
        }
    }
}
