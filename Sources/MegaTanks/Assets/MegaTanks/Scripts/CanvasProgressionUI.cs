using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace MegaTanks
{
    public sealed partial class CanvasGameUI
    {
        void BindProgression()
        {
            game.Progression.Changed += RefreshProgressionViews;
            game.Wallet.Changed += RefreshUpgradeControls;
            RefreshUpgradeControls();
        }
        void RefreshProgressionViews()
        {
            game.RefreshTankLoadoutStats();
            if (popup == "Equipment") DrawEquipment();
            else if (popup == "Crew") DrawCrew();
        }
        void RefreshUpgradeControls()
        {
            if(popup=="Equipment")DrawEquipment();else DrawProgression(View(UIScreen.Equipment), selected, false);
            if(popup=="Crew")DrawCrew();else DrawProgression(View(UIScreen.Crew), crewSelected, true);
        }
        void DrawProgression(UIView v, Object definition, bool crew)
        {
            bool supported = game.Progression.Supports(definition);
            foreach (string key in new[] { "Progression Panel", "Progression Summary", "Level Up", "Star Up" }) Active(v, key, supported);
            v.Get<UIStarStrip>("Detail Stars").Stars = supported ? game.Progression.Get(definition).Stars : 0;
            if (!supported) return;
            var state = game.Progression.Get(definition);
            int levelCost = ProgressionRules.LevelCost(state.Level), starCost = ProgressionRules.StarCost(state.Stars);
            if(definition is AccessoryDefinition accessory)
            {
                Active(v,"Star Up",false);v.Get<UIStarStrip>("Detail Stars").Stars=0;
                v.Get<Button>("Star Up").onClick.RemoveAllListeners();
                Text(v,"Progression Summary","LEVEL "+state.Level+"   •   "+accessory.displayName.ToUpperInvariant()+
                    "\n"+accessory.StatLabel.ToUpperInvariant()+" +"+accessory.Bonus(state.Level).ToString("0.##")+"%   •   NEXT LEVEL +"+accessory.Bonus(state.Level+1).ToString("0.##")+"%");
                Text(v,"Level Up/Label",levelCost>0?"LEVEL UP • "+levelCost.ToString("N0")+" PARTS":"LEVEL COST LIMIT");
                v.Get<Button>("Level Up").interactable=!game.Deployed&&game.Progression.CanLevelUp(definition);
                Click(v,"Level Up",()=>{if(!game.Deployed)game.Progression.TryLevelUp(definition);});return;
            }
            var armor = definition as ArmorDefinition;
            float damage = armor != null ? game.Progression.ArmorHP(armor) : game.Progression.Damage(definition);
            float nextLevel = armor != null ? armor.HitPoints(state.Level + 1, state.Stars) : ProgressionRules.Damage(PlayerProgression.BaseDamage(definition), state.Level + 1, state.Stars);
            float nextStar = armor != null ? armor.HitPoints(state.Level, state.Stars + 1) : ProgressionRules.Damage(PlayerProgression.BaseDamage(definition), state.Level, state.Stars + 1);
            string fragments = crew ? "PET TAGS" : "BLUEPRINTS";
            Text(v, "Progression Summary", "LEVEL " + state.Level + "   •   " + state.Stars + " / 20 STARS   •   " + fragments + " " + state.Fragments + (starCost > 0 ? " / " + starCost : " (MAX STARS)") +
                "\n" + (crew ? "ASSIGNED DAMAGE BONUS " : armor != null ? "BONUS HP " : "BASE DAMAGE ") + damage.ToString("0.##") + "   •   Next level " + nextLevel.ToString("0.##") + (starCost > 0 ? "   •   Next star " + nextStar.ToString("0.##") : ""));
            if(definition is CrewDefinition member)
            {
                Text(v,"Progression Summary","LEVEL "+state.Level+" • PET TAGS "+state.Fragments+" • "+state.Stars+" / 20 STARS\nEach stat uses its own level gain; stars add 10% per star.");
                Text(v,"Detail Description",CrewStatSummary(member,state.Level,state.Stars));

            }
            Text(v, "Level Up/Label", levelCost > 0 ? "LEVEL UP • " + levelCost.ToString("N0") + (crew ? " FOOD" : " PARTS") : "LEVEL COST LIMIT");
            Text(v, "Star Up/Label", starCost > 0 ? "STAR UP • " + starCost + (crew ? " PET TAGS" : " BLUEPRINTS") : "MAX STARS");
            v.Get<Button>("Level Up").interactable = !game.Deployed && game.Progression.CanLevelUp(definition);
            v.Get<Button>("Star Up").interactable = !game.Deployed && game.Progression.CanStarUp(definition);
            Click(v, "Level Up", () => { if (!game.Deployed) game.Progression.TryLevelUp(definition); });
            Click(v, "Star Up", () => { if (!game.Deployed) game.Progression.TryStarUp(definition); });
        }
        string CrewStatSummary(CrewDefinition member,int level,int stars)
        {
            string S(string label,CrewStat stat,string suffix="%")=>label+" "+stat.AtLevel(level,stat==member.stats.baseReloadTime?0:stars).ToString("0.##")+suffix+" (+"+(Mathf.Max(0,stat.gain)*(stat==member.stats.baseReloadTime?1:1+.1f*Mathf.Clamp(stars,0,20))).ToString("0.##")+"/lv)";
            var s=member.stats;
            if(member.role==CrewRole.Driver)return S("HP",s.hp,"")+" | "+S("EVA",s.evasion)+" | "+S("SPD",s.speed,"");
            string weapon=S("HIT",s.accuracy)+" | "+S("DMG BONUS",s.damageBonus)+" | "+S("RELOAD",s.reloadSpeed)+"\n"+S("CRIT",s.criticalChance)+" | "+S("CRIT DMG",s.criticalDamage);
            if(member.role!=CrewRole.Commando)return weapon;
            return (s.weaponRange.AtLevel(level,stars)<=0?"MELEE • CLOSE COMBAT":"RANGED")+" | "+S("REGEN",s.healthRegeneration," HP/s")+"\n"+S("HP",s.hp,"")+" | "+S("ARM",s.armor,"")+" | "+S("SPD",s.speed,"")+" | "+S("EVA",s.evasion)+"\n"+
                weapon+"\n"+S("DMG",s.damage,"")+" | "+S("PEN",s.armorPenetration)+" | "+S("ACTIVATION",s.activationRange,"")+"\n"+
                S("RANGE",s.weaponRange,"")+" | "+S("RELOAD TIME",s.baseReloadTime,"s")+" | "+S("PROJECTILE SPD",s.projectileSpeed,"")+"\n"+
                S("CRIT RESIST",s.criticalChanceReduction)+" | "+S("CRIT DMG RESIST",s.criticalDamageReduction);
        }
        void ProgressionCell(UIView cell, Object definition)
        {
            bool supported = game.Progression.Supports(definition);
            var state = game.Progression.Get(definition);
            cell.Get<UIStarStrip>("Stars").Stars = supported ? state.Stars : 0;
            Text(cell, "Progression Level", supported ? "Lv. " + state.Level : "");
        }
        string ArmorDetail(ArmorDefinition armor)
        {
            var primary=game.Equipment.Armor;
            var secondary=game.Equipment.ArmorSlotCount>1?game.Equipment.SecondArmor:null;
            var current=game.CalculateTankStats(primary,secondary);
            int index=activeSlot==EquipmentType.Armor?accessoryIndex:0;
            if(index==1){if(primary==armor)primary=secondary;secondary=armor;}
            else {if(secondary==armor)secondary=primary;primary=armor;}
            var next=game.CalculateTankStats(primary,secondary);
            current.evasion+=game.EvasionBonus;next.evasion+=game.EvasionBonus;
            var state=game.Progression.Get(armor);
            return "ARMOR BONUSES: HP +" + armor.HitPoints(state.Level,state.Stars).ToString("0.##") + " → +" + armor.HitPoints(state.Level+1,state.Stars).ToString("0.##") + " | ARMOR +" + armor.armor.ToString("0.##") +
                "\nSPEED +" + armor.speed.ToString("0.##") + " | EVASION +" + armor.evasion.ToString("0.##") + "%"+
                "\nTANK TOTALS (equipped → selected)\n" + CompareStat("HP", current.maxHull, next.maxHull) + " | " +
                CompareStat("ARM", current.armor, next.armor) + "\n" + CompareStat("SPD", current.advanceSpeed, next.advanceSpeed) + " | " + CompareStat("EVA", current.evasion, next.evasion, "%");
        }
        float PreviewDamage(EquipmentDefinition item) => item is WeaponDefinition weapon ?
            game.Progression.Damage(weapon) : 0;
        string AccessoryDetail(AccessoryDefinition accessory)
        {
            int slot=activeSlot==EquipmentType.Accessory?accessoryIndex:0;
            int level=game.Progression.Get(accessory).Level;
            string header=accessory.StatLabel.ToUpperInvariant()+" +"+accessory.Bonus(level).ToString("0.##")+"% → +"+accessory.Bonus(level+1).ToString("0.##")+"%"+
                "\n+"+accessory.bonusPerLevel.ToString("0.##")+" percentage points per level";
            if(accessory.bonusType==AccessoryBonusType.Accuracy)
            {
                float current=game.AccuracyBonus,preview=game.Equipment.AccessoryBonus(AccessoryBonusType.Accuracy,game.Progression,accessory,slot);
                string Accuracy(string label,float value)=>label+" "+(value+current).ToString("0.##")+"% → "+(value+preview).ToString("0.##")+"%";
                return header+"\n"+Accuracy("MG",game.PreviewWeaponStats(game.Equipment.MachineGun).accuracy)+" | "+Accuracy("CANNON",game.PreviewWeaponStats(game.Equipment.Cannon).accuracy)+
                    "\n"+Accuracy("MORTAR",game.PreviewWeaponStats(game.Equipment.Mortar).accuracy)+"\nAccuracy above 100% offsets enemy evasion.";
            }
            if(accessory.bonusType==AccessoryBonusType.Evasion)
            {
                float preview=game.Equipment.AccessoryBonus(AccessoryBonusType.Evasion,game.Progression,accessory,slot);
                return header+"\n"+CompareStat("TANK EVASION",game.BaseTankStats.evasion,game.BaseTankStats.evasion-game.EvasionBonus+preview,"%")+
                    "\nReduces the enemy's chance to hit the tank.";
            }
            float before=game.ReloadSpeedBonus;
            float after=game.Equipment.ReloadSpeedBonus(game.Progression,accessory,slot);
            string Times(string title,float seconds,float speed)=>title+" "+AccessoryDefinition.ReloadTime(seconds,speed+before).ToString("0.##")+"s → "+AccessoryDefinition.ReloadTime(seconds,speed+after).ToString("0.##")+"s";
            return header+
                "\n"+Times("MG",game.Equipment.MachineGun.stats.reloadTime,game.PreviewWeaponStats(game.Equipment.MachineGun).reloadSpeed)+" | "+Times("CANNON",game.Equipment.Cannon.stats.reloadTime,game.PreviewWeaponStats(game.Equipment.Cannon).reloadSpeed)+
                "\n"+Times("MORTAR",game.Equipment.Mortar.stats.reloadTime,game.PreviewWeaponStats(game.Equipment.Mortar).reloadSpeed);
        }
    }
}
