using System;
using System.Linq;
using UnityEngine;

namespace MegaTanks
{
    [Serializable] public sealed class EquipmentHtmlWeapon
    {
        public WeaponDefinition definition; public string title,variant,artKey; public int slot;
    }
    [Serializable] public sealed class HtmlWheelFit { public string hull;public HtmlArtPart[] parts; }
    [CreateAssetMenu(menuName="MegaTanks/UI/HTML Equipment Catalog")]
    public sealed class EquipmentHtmlCatalog : ScriptableObject
    {
        public EquipmentHtmlWeapon[] weapons;
        public TankDefinition[] hulls;
        public WheelsDefinition[] wheels;
        public HtmlWheelFit[] offroadFits;
        public HtmlArtImage[] wheelImages;
        public static EquipmentHtmlCatalog Load()=>Resources.Load<EquipmentHtmlCatalog>("EquipmentHtmlCatalog");
        public EquipmentHtmlWeapon Weapon(WeaponDefinition d)=>weapons.FirstOrDefault(w=>w.definition==d);
        public static string Skin(TankDefinition tank)=>!string.IsNullOrEmpty(tank?.htmlSkin)?tank.htmlSkin:tank!=null&&tank.id=="tank.heavy"?"heavy":"base";
        public static int SkinIndex(string skin)=>skin=="speed"?1:skin=="heavy"?2:0;
        public static bool IsWheel(HtmlArtPart part)=>part.name.Contains("wheel_");
        public static Sprite Sprite(HtmlBattleArt art,string key)=>art.Image(key)?.sprite??Load()?.wheelImages?.FirstOrDefault(i=>i.key==key)?.sprite;
        // Original placement manifests are converted to the same native registration as the hull.
        public static HtmlArtPart[] Parts(HtmlBattleArt art,string hull,string wheels)
        {
            var target=art.Profile(hull).parts;
            if(wheels!="offroad")return target;
            var fit=Load()?.offroadFits?.FirstOrDefault(f=>f.hull==hull);
            if(fit==null)throw new InvalidOperationException("Missing original offroad placement for "+hull);
            return target.Where(p=>!IsWheel(p)&&p.name!="01_track_chassis").Concat(fit.parts).OrderBy(p=>p.order).ToArray();
        }
        public static float WeaponBaseDamage(HtmlBattleConfig config,PlayerProgression progression,EquipmentHtmlWeapon weapon)
        {
            var state=progression.Get(weapon.definition);
            return ProgressionRules.Scale(config.weapons[weapon.slot].damage,config.permanentWeaponDamagePerLevel,state.Level,state.Stars);
        }
        public static float VariantMultiplier(HtmlBattleConfig config,string variant)=>variant=="cannon_ap"?config.piercingMultiplier:variant=="mg_plasma"?config.plasmaMultiplier:variant=="mortar_blast"?config.explosiveMultiplier:1;
        public static float HullHP(HtmlBattleConfig config,HtmlBattleArt art,PlayerProgression progression,TankDefinition hull)
            =>art.Profile(Skin(hull)).hp+Mathf.Max(0,progression.Get(hull).Level-1)*config.permanentHullHPPerLevel;
        public static float WheelSpeed(HtmlBattleConfig config,PlayerProgression progression,WheelsDefinition wheels)
            =>wheels==null?1:config.wheelSpeedMultipliers[wheels.skin=="offroad"?1:0]+Mathf.Max(0,progression.Get(wheels).Level-1)*config.permanentWheelSpeedPerLevel;
        public static float WheelDefense(HtmlBattleConfig config,WheelsDefinition wheels)=>wheels==null?0:config.wheelDefense[wheels.skin=="offroad"?1:0];
        public static float GroundY(HtmlBattleArt art,string hull,string wheels,float width,float ground)
        {
            if(wheels!="offroad")return ground;
            float original=art.Profile(hull).parts.Where(p=>IsWheel(p)||p.name=="01_track_chassis").Max(p=>p.position[1]+p.size[1]);
            float replacement=Load().offroadFits.First(f=>f.hull==hull).parts.Max(p=>p.position[1]+p.size[1]);
            return ground-(replacement-original)*width/1351;
        }
        public static void Apply(BattleGame game,HtmlBattleSimulation simulation,bool combat)
        {
            var catalog=Load();if(catalog==null)return;
            simulation.player.skin=Skin(game.TankProfile);simulation.player.wheelSkin=game.Equipment.Wheels?.skin;
            simulation.player.y=GroundY(simulation.Geometry,simulation.player.skin,simulation.player.wheelSkin,simulation.player.width,simulation.config.groundY);
            simulation.wheelSpeedMultiplier=WheelSpeed(simulation.config,game.Progression,game.Equipment.Wheels);
            simulation.permanentDefense=WheelDefense(simulation.config,game.Equipment.Wheels);
            simulation.player.hp=simulation.player.maxHP=HullHP(simulation.config,simulation.Geometry,game.Progression,game.TankProfile);
            var driver=game.Crew.Get(CrewRole.Driver);
            if(driver!=null){var p=game.Progression.Get(driver);simulation.player.maxHP+=driver.stats.hp.AtLevel(p.Level,p.Stars);simulation.player.hp=simulation.player.maxHP;}
            if(!combat)return;
            for(int i=0;i<3;i++)
            {
                // HTML's green mortar remains authoritative even for a legacy missile save.
                var definition=i==0?(WeaponDefinition)game.Equipment.MachineGun:i==1?game.Equipment.Cannon:game.Equipment.Mortar;
                var entry=catalog.Weapon(definition)??catalog.weapons.First(w=>w.slot==i);
                simulation.playerGuns[i].damage=WeaponBaseDamage(simulation.config,game.Progression,entry);
                simulation.weaponVariants[i]=entry.variant;
                var member=game.WeaponCrew(entry.definition.Type);
                if(member!=null){var p=game.Progression.Get(member);simulation.weaponCriticalChance[i]=member.stats.criticalChance.AtLevel(p.Level,p.Stars);}
            }
        }
    }
}
