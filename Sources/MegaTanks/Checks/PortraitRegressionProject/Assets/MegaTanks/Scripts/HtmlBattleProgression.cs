using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MegaTanks
{
    public sealed partial class HtmlBattleSimulation
    {
        public int baseTankLevel=1,temporaryTankLevel=1;
        public int EffectiveLevel=>Math.Max(baseTankLevel,temporaryTankLevel);
        public string runId=Guid.NewGuid().ToString("N");
        public string[] weaponVariants={"mg","cannon","mortar"};
        public readonly List<Loot> loot=new List<Loot>();
        public sealed class Loot { public float x,y,fromY,age,flyAge,flyX,flyY;public int amount;public bool gear,chest;public int phase; }
        public int collectedChests;public float coinTargetX=1377,gearTargetX=1659,counterTargetY=70;
        public float temporaryDefense,temporaryCrit;
        public readonly List<Actor> abandoned=new List<Actor>();
        readonly HashSet<int> upgradedStations=new HashSet<int>();
        int stationSerial;bool initialUpgrade,landmarkConsumed;float upgradeClock;
        public bool LandmarkConsumed=>landmarkConsumed;
        public float UpgradeClock=>upgradeClock;
        public bool UpgradeApplied=>upgradedStations.Contains(stationSerial);
        public bool HasEntryUpgrade { get;private set; }
        public bool IsEntryUpgrade=>initialUpgrade;
        public bool SlotUnlocked(int weapon)=>EffectiveLevel>=config.unlockLevels[weapon];
        public bool RoleUnlocked(string role)=>EffectiveLevel>=config.unlockLevels[role=="mg"?0:role=="cannon"?1:role=="mortar"?2:role=="driver"?3:4];
        public void ConfigureProfile(int permanentLevel)
        {
            HasEntryUpgrade=initialUpgrade=false;
            baseTankLevel=temporaryTankLevel=Math.Max(1,permanentLevel);
            foreach(var f in fighters)f.locked=!RoleUnlocked(f.role);
            if(config.initialUpgradeStation&&!SlotUnlocked(0)){HasEntryUpgrade=initialUpgrade=true;BeginStation("upgrade");}
        }
        public void ConfigureCrew(PlayerCrew crew,PlayerProgression progression)
        {
            foreach(var f in fighters.ToArray())
            {
                var member=crew.Get(Role(f.role));if(member==null){fighters.Remove(f);continue;}
                var stat=new ResolvedCrewStats(member,progression.Get(member),config);
                f.crewId=member.id;f.displayName=member.displayName;f.hp=f.maxHP=stat.hp;f.atk=stat.attack;f.accuracy=stat.accuracy;f.defense=stat.defense;f.moveSpeed=stat.speed;f.crit=stat.crit;
            }
        }
        public static CrewRole Role(string role)=>role=="mg"?CrewRole.SubGunner:role=="cannon"?CrewRole.Gunner:role=="mortar"?CrewRole.WeaponSpecialist:role=="driver"?CrewRole.Driver:CrewRole.Commando;
        public static int CrewLane(string role)=>role=="mg"?-2:role=="cannon"?-1:role=="driver"?1:role=="mortar"?2:0;
        float CrewGround(Fighter f)=>config.groundY+CrewLane(f.role)*config.crewLaneSpacing;
        void DeployBossCrew(Actor owner)
        {
            owner.bossCrew=true;var profile=Geometry?.Profile(owner.skin);float z=owner.width/1351;
            foreach(string role in new[]{"front","driver","cannon","mg"})
            {
                int weapon=role=="mg"?0:role=="cannon"?1:-1;float distance=owner.x-player.x;
                if(weapon>=0&&(!owner.weapons[weapon]||distance>=config.weapons[weapon].minRange&&distance<=config.weapons[weapon].maxRange))continue;
                owner.deployedRoles.Add(role);var seat=profile?.seats.FirstOrDefault(s=>s.id==role);var dog=Spawn("dog");dog.bossCrew=true;dog.crewRole=role;dog.lane=CrewLane(role);dog.bossOwner=owner;
                dog.hp=dog.maxHP=stageIndex==0?180:220;dog.x=dog.landingStartX=owner.x-(seat==null?0:seat.x-652)*z;
                dog.y=dog.landingStartY=config.groundY+(seat==null?-130:seat.y+(role=="front"?0:seat.scale*100)-1258)*z;
                dog.landingEndX=owner.x-(1150-652)*z-30-dog.lane*5;dog.landing=dog.landingDuration=seat!=null&&seat.y<800?.85f:.65f;
            }
        }
        void BeginStation(string type)
        {
            encounter=type;stationSerial++;landmarkX=config.stationSpawnX;landmarkConsumed=false;phase="approach";upgradeClock=0;
        }
        bool TickStation(float dt)
        {
            foreach(var f in fighters)if(f.dead){f.deathAge+=dt;f.x-=travelSpeed*dt*config.roadScrollFactor;}
            foreach(var a in abandoned)a.x-=travelSpeed*dt*config.roadScrollFactor;
            foreach(var item in loot.ToArray())
            {
                item.age+=dt;
                if(item.phase==2)
                {
                    item.flyAge+=dt;float t=Mathf.Clamp01(item.flyAge/.5f),e=1-Mathf.Pow(1-t,3);
                    item.x=Mathf.Lerp(item.flyX,item.chest?player.x:item.gear?gearTargetX:coinTargetX,e);item.y=Mathf.Lerp(item.flyY,item.chest?config.groundY-player.width*.35f:counterTargetY,e)-90*Mathf.Sin(t*Mathf.PI);
                    if(t>=1){if(item.chest)collectedChests+=item.amount;else if(item.gear)gears+=item.amount;else money+=item.amount;loot.Remove(item);}continue;
                }
                item.x-=travelSpeed*dt*config.roadScrollFactor;float u=Mathf.Clamp01(item.age/.62f);item.y=Mathf.Lerp(item.fromY,config.groundY-20,u)-110*Mathf.Sin(u*Mathf.PI);
                if(u<1)continue;item.phase=1;
                bool near=Mathf.Abs(item.x-player.x)<player.width*.48f+65||fighters.Any(f=>!f.locked&&!f.dead&&!f.aboard&&Mathf.Abs(item.x-f.x)<130);
                bool sweep=bossKilled&&!fighters.Any(f=>!f.dead&&!f.locked&&!f.aboard&&f.motion!="following");
                if(near||sweep){item.phase=2;item.flyX=item.x;item.flyY=item.y;}
            }
            if(phase!="upgrade")return false;
            travelSpeed=0;upgradeClock+=dt;
            if(upgradeClock>=config.upgradeApplySeconds&&upgradedStations.Add(stationSerial))ApplyUpgrade();
            if(upgradeClock>=config.upgradeDepartSeconds)phase="depart";
            return true;
        }
        void ApplyUpgrade()
        {
            int next=config.unlockLevels.Where(l=>l>EffectiveLevel).DefaultIfEmpty(0).Min();
            if(next>0){int previous=EffectiveLevel;temporaryTankLevel=next;foreach(var f in fighters)f.locked=!RoleUnlocked(f.role);if(config.grantNewSlotWeapon)for(int i=0;i<3;i++)if(config.unlockLevels[i]>previous&&SlotUnlocked(i)){player.weapons[i]=true;ammo[i]=Mathf.Max(ammo[i],config.newSlotAmmo[i]);}}
            else {float bonus=config.playerHP*config.stationHPBonus;player.maxHP+=bonus;player.hp+=bonus;for(int i=0;i<3;i++)weaponDamageBonus[i]+=config.stationDamageBonus;temporaryDefense+=config.stationDefenseBonus;temporaryCrit+=config.stationCritBonus;}
            AddEffect("UpgradeAura",player.x,config.groundY,config.playerWidth,config.upgradeAuraSeconds,true);
        }
        bool CanChooseVariant(string choice,bool ad)
        {
            int w=choice=="cannon_ap"?1:choice=="mortar_blast"?2:choice=="mg_plasma"?0:-1;
            if(w<0||!SlotUnlocked(w)||!player.weapons[w]||w==0&&!ad)return false;
            weaponVariants[w]=choice;return true;
        }
        float VariantDamage(int w)=>weaponVariants[w]=="cannon_ap"?config.piercingMultiplier:weaponVariants[w]=="mortar_blast"?config.explosiveMultiplier:weaponVariants[w]=="mg_plasma"?config.plasmaMultiplier:1;
        void HurtFighter(Fighter f,float damage)
        {
            if(f.dead||f.locked)return;f.hp=Mathf.Max(0,f.hp-Mathf.Max(0,damage-f.defense));
            if(f.hp<=0){f.dead=true;f.aboard=false;f.target=null;f.deathAge=0;f.motion="death";}
        }
        public bool RevealTrojan(Actor carrier)
        {
            if(carrier.kind!="trojan"||carrier.revealed)return false;carrier.revealed=true;
            // Register all payloads before the carrier can leave the active set.
            for(int i=0;i<3;i++){var dog=Spawn("dog");dog.carrier=carrier;float z=carrier.width/1200;dog.x=dog.landingStartX=carrier.x+(520+i*130-800)*z;dog.y=dog.landingStartY=config.groundY-112*z;dog.landingEndX=carrier.x+(120+i*190-800)*z;dog.landing=dog.landingDuration=.45f+i*.12f;}
            carrier.dead=true;AddEffect("Explosion",carrier.x,Center(carrier).y,140,.5f,true);return true;
        }
        void Drop(Actor actor,bool bossDrop)
        {
            if(bossDrop){LootAt(actor,actor.x-54,config.bossMoney);LootAt(actor,actor.x,config.bossGears,true);LootAt(actor,actor.x+54,1,false,true);}
            else LootAt(actor,actor.x,actor.id%5<3?config.enemyMoney:config.enemyGears,actor.id%5>=3);
        }
        void LootAt(Actor actor,float x,int amount,bool gear=false,bool chest=false){float y=Center(actor).y;loot.Add(new Loot{x=x,y=y,fromY=y,amount=amount,gear=gear,chest=chest});}
    }
}
