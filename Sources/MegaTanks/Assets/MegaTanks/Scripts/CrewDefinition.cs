using System;
using UnityEngine;
namespace MegaTanks
{
    // Fixed station identifiers; do not reorder saved roles.
    public enum CrewRole { Gunner, Driver, SubGunner, WeaponSpecialist, Commando }
    [Serializable]
    public sealed class CrewStat
    {
        [Min(0)] public float value;
        [Min(0), Tooltip("Amount added for each level after level 1.")] public float gain;
        public float AtLevel(int level,int stars=0) => ProgressionRules.Scale(value,gain,level,stars);
    }
    [Serializable]
    public sealed class CrewStats
    {
        public CrewStat accuracy=new CrewStat(), damageBonus=new CrewStat(), reloadSpeed=new CrewStat();
        public CrewStat criticalChance=new CrewStat(), criticalDamage=new CrewStat();
        public CrewStat hp=new CrewStat(), evasion=new CrewStat(), speed=new CrewStat();
        public CrewStat damage=new CrewStat(), armor=new CrewStat(), armorPenetration=new CrewStat();
        public CrewStat criticalChanceReduction=new CrewStat(), criticalDamageReduction=new CrewStat();
        public CrewStat weaponRange=new CrewStat(), baseReloadTime=new CrewStat(), projectileSpeed=new CrewStat();
        public CrewStat activationRange=new CrewStat();
        [Tooltip("Player commando HP restored each second aboard the tank.")]
        public CrewStat healthRegeneration=new CrewStat();
    }
    [CreateAssetMenu(menuName="MegaTanks/Crew/Crew Member")]
    public sealed class CrewDefinition : ScriptableObject
    {
        public string id;
        [HideInInspector] public float baseDamage; // Legacy serialization only; no longer contributes to combat.
        public string displayName;
        public CrewRole role;
        [HideInInspector] public string specialty;
        [TextArea] public string description;
        public Sprite portrait;
        [Tooltip("Transparent full-body sprite used at the tank station in the equip screen and battle.")]
        public Sprite equippedSprite;
        public CommandoArtwork commandoPrefab;
        [Tooltip("Optional material using the same atlas layout as the combat prefab.")]
        public Material combatMaterial;
        public CrewStats stats=new CrewStats();
        public string RoleName=>RoleLabel(role);
        public static string RoleLabel(CrewRole role)=>role==CrewRole.SubGunner?"Sub gunner":role==CrewRole.WeaponSpecialist?"Weapon specialist":role.ToString();
        public static string AssociatedSlot(CrewRole role)=>role==CrewRole.Gunner?"Main gun (cannon)":role==CrewRole.SubGunner?"Sub gun (machine gun / gatling)":role==CrewRole.WeaponSpecialist?"Special weapon (mortar)":role==CrewRole.Driver?"Main hull":"Independent unit / integrated weapon";
        public void ApplyWeapon(WeaponCombatStats weapon,int level,int stars=0)
        {
            if(role==CrewRole.Driver||role==CrewRole.Commando)return;
            weapon.accuracy+=stats.accuracy.AtLevel(level,stars);weapon.damageBonus+=stats.damageBonus.AtLevel(level,stars);
            weapon.reloadSpeed+=stats.reloadSpeed.AtLevel(level,stars);weapon.criticalChance+=stats.criticalChance.AtLevel(level,stars);
            weapon.criticalDamage+=stats.criticalDamage.AtLevel(level,stars);
        }
        public void ApplyHull(TankStats hull,int level,int stars=0)
        {
            if(role!=CrewRole.Driver)return;
            hull.maxHull+=stats.hp.AtLevel(level,stars);hull.evasion+=stats.evasion.AtLevel(level,stars);hull.advanceSpeed+=stats.speed.AtLevel(level,stars);
        }
        public TankStats CommandoHull(int level,int stars=0)=>new TankStats {
            maxHull=stats.hp.AtLevel(level,stars),advanceSpeed=stats.speed.AtLevel(level,stars),evasion=stats.evasion.AtLevel(level,stars),
            armor=stats.armor.AtLevel(level,stars),criticalChanceReduction=stats.criticalChanceReduction.AtLevel(level,stars),criticalDamageReduction=stats.criticalDamageReduction.AtLevel(level,stars)
        };
        public GunStats CommandoWeapon(int level,int stars=0)=>new GunStats {
            damage=stats.damage.AtLevel(level,stars),damageBonus=stats.damageBonus.AtLevel(level,stars),accuracy=stats.accuracy.AtLevel(level,stars),
            reloadSpeed=stats.reloadSpeed.AtLevel(level,stars),criticalChance=stats.criticalChance.AtLevel(level,stars),criticalDamage=stats.criticalDamage.AtLevel(level,stars),
            armorPenetration=stats.armorPenetration.AtLevel(level,stars),range=stats.weaponRange.AtLevel(level,stars),reloadTime=stats.baseReloadTime.AtLevel(level),
            projectileSpeed=stats.projectileSpeed.AtLevel(level,stars),gravity=0,salvoSize=1,reserveCapacity=0
        };
        void OnValidate() { if (string.IsNullOrWhiteSpace(id)) id = System.Guid.NewGuid().ToString("N"); }
    }
}
