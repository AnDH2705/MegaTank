using System;
using UnityEngine;

namespace MegaTanks
{
    [Serializable]
    public class BossItemProgression
    {
        [Min(1)] public int level = 1;
        [Range(0, 20)] public int stars;
        public BossItemProgression Copy() => (BossItemProgression)MemberwiseClone();
        public float Damage(float baseDamage) => ProgressionRules.Damage(baseDamage, level, stars);
    }

    [Serializable]
    public sealed class BossCrewProgression : BossItemProgression
    {
        [Tooltip("Matches a station name in the boss template. Unlisted stations use level 1, zero stars.")]
        public string station;
    }

    // Authored per stage. No difficulty or player-save inputs enter these calculations.
    [Serializable]
    public sealed class BossEncounterSettings
    {
        public BossTankProfile template;
        public BossItemProgression machineGun = new BossItemProgression();
        public BossItemProgression cannon = new BossItemProgression();
        public BossItemProgression mortar = new BossItemProgression();
        public BossItemProgression armor = new BossItemProgression();
        public BossItemProgression secondArmor = new BossItemProgression();
        public BossItemProgression accessory = new BossItemProgression();
        public BossItemProgression secondAccessory = new BossItemProgression();
        public BossCrewProgression[] crew =
        {
            new BossCrewProgression { station = "Gunner" }, new BossCrewProgression { station = "Driver" },
            new BossCrewProgression { station = "Sub gunner" }, new BossCrewProgression { station = "Weapon specialist" },
            new BossCrewProgression { station = "Commando" }
        };
        public BossEncounterSettings Copy()
        {
            var copy = (BossEncounterSettings)MemberwiseClone();
            copy.machineGun = machineGun?.Copy() ?? new BossItemProgression();
            copy.cannon = cannon?.Copy() ?? new BossItemProgression();
            copy.mortar = mortar?.Copy() ?? new BossItemProgression();
            copy.armor = armor?.Copy() ?? new BossItemProgression();
            copy.secondArmor = secondArmor?.Copy() ?? new BossItemProgression();
            copy.accessory = accessory?.Copy() ?? new BossItemProgression();
            copy.secondAccessory = secondAccessory?.Copy() ?? new BossItemProgression();
            copy.crew = Array.ConvertAll(crew ?? Array.Empty<BossCrewProgression>(), slot => slot == null ? null : (BossCrewProgression)slot.Copy());
            return copy;
        }
        public BossItemProgression CrewLevel(string station)
        {
            foreach (var slot in crew ?? Array.Empty<BossCrewProgression>())
                if (slot != null && slot.station == station) return slot;
            return new BossItemProgression();
        }
        public float CrewDamage=>0; // Compatibility: no shared damage pool.
        public float AccessoryBonus(AccessoryBonusType type)
        {
            float total=0;
            if(template.accessory!=null&&template.accessory.bonusType==type)total+=template.accessory.Bonus(accessory?.level??1);
            if(template.tank.AccessorySlots>1&&template.secondAccessory!=null&&template.secondAccessory.bonusType==type)
                total+=template.secondAccessory.Bonus(secondAccessory?.level??1);
            return total;
        }
        public void ApplyAccessories(WeaponCombatStats weapon)
        {
            weapon.accuracy+=AccessoryBonus(AccessoryBonusType.Accuracy);
            weapon.reloadSpeed+=AccessoryBonus(AccessoryBonusType.ReloadSpeed);
        }
        public CrewDefinition CrewMember(CrewRole role)
        {
            foreach(var slot in template.crew??Array.Empty<BossCrewStation>())
                if(slot?.member!=null&&slot.member.role==role&&slot.station==CrewDefinition.RoleLabel(role))return slot.member;
            return null;
        }
        public float WeaponDamage(float baseDamage,BossItemProgression progression)=>(progression??new BossItemProgression()).Damage(baseDamage);
        public void ApplyCrew(CrewRole role,WeaponCombatStats weapon=null,TankStats hull=null)
        {
            foreach(var slot in template.crew??Array.Empty<BossCrewStation>())
            {
                if(slot?.member==null||slot.member.role!=role||slot.station!=CrewDefinition.RoleLabel(role))continue;
                int level=CrewLevel(slot.station).level;
                if(weapon!=null)slot.member.ApplyWeapon(weapon,level,CrewLevel(slot.station).stars);
                if(hull!=null)slot.member.ApplyHull(hull,level,CrewLevel(slot.station).stars);
                break;
            }
        }
        public TankStats CalculateTankStats()
        {
            var result = template.tank.stats.Copy();
            var progression = armor ?? new BossItemProgression();
            if (template.armor != null) template.armor.ApplyTo(result, template.armor.HitPoints(progression.level, progression.stars));
            if (template.tank.ArmorSlots > 1 && template.secondArmor != null)
            {
                var second = secondArmor ?? new BossItemProgression();
                template.secondArmor.ApplyTo(result, template.secondArmor.HitPoints(second.level, second.stars));
            }
            ApplyCrew(CrewRole.Driver,hull:result);
            result.evasion+=AccessoryBonus(AccessoryBonusType.Evasion);
            return result;
        }
        public void Validate()
        {
            if (template == null || template.tank == null || template.artworkPrefab == null || template.equipment == null)
                throw new InvalidOperationException("Assign a complete Boss Template on the Stage Definition (hull, artwork and equipment).");
            template.equipment.Validate();
            if(template.tank.ArmorSlots>1&&template.armor!=null&&template.armor==template.secondArmor)
                throw new InvalidOperationException("Use different armor items in the two boss armor slots, like the player loadout.");
            if(template.tank.AccessorySlots>1&&template.accessory!=null&&template.accessory==template.secondAccessory)
                throw new InvalidOperationException("Use different items in the two boss accessory slots, like the player loadout.");
        }
        public EnemyDefinition CreateEnemy()
        {
            Validate();
            var stats = CalculateTankStats();
            return new EnemyDefinition
            {
                id = template.id, kind = EnemyKind.Ground, baseHealth = stats.maxHull, baseSpeed = stats.advanceSpeed,
                armor = stats.armor, evasion = stats.evasion, criticalChanceReduction = stats.criticalChanceReduction,
                criticalDamageReduction = stats.criticalDamageReduction, score = template.score, baseScraps = template.scraps,
                color = template.healthBarColor, advanceInsideRange = Vector2.zero,
                weapon = new EnemyWeaponStats { range = Mathf.Min(template.equipment.machineGun.stats.range, template.equipment.cannon.stats.range) }
            };
        }
    }
}
