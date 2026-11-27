using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MegaTanks
{
    public sealed partial class HtmlBattleSimulation
    {
        PlayerCrew crewSource;PlayerProgression crewProgression;
        public readonly object prisonerToken=new object();
        public readonly List<string> encounterHistory=new List<string>();
        public Fighter rescuedFighter { get;private set; }
        public CrewDefinition rescuedMember { get;private set; }
        public float rescueClock { get;private set; }
        bool rescueApplied;
        public RescueStageStory RescueStory=>config.rescue.Stage(stageIndex);
        public bool RescueNeeded=>config.rescue.enabled&&RescueStory!=null&&crewSource!=null&&!crewSource.IsRecruited(crewSource.Member(RescueStory.crewId))&&rescuedMember==null;
        public bool RescueReady=>RescueNeeded&&crewSource.Member(RescueStory.crewId)!=null;
        public bool HasOperator(int weapon)=>fighters.Any(f=>f.role==config.weapons[weapon].id&&!f.dead&&!f.locked&&f.aboard&&f.jump<=0);
        public bool HasAssignedOperator(int weapon)=>fighters.Any(f=>f.role==config.weapons[weapon].id&&!f.dead);
        public float EncounterStopX=>encounter=="rescue"?config.rescue.stopX:config.encounterStopX;
        public float CageLift=>rescueApplied?Mathf.Clamp01((rescueClock-config.rescue.unlockSeconds)/config.rescue.cageLiftSeconds):0;
        public bool CageVisible=>encounter=="rescue"&&(phase=="approach"||phase=="choice"||phase=="depart"||phase=="rescue_release")&&CageLift<1;
        public string EncounterType(int index)
        {
            if(index<encounterHistory.Count)return encounterHistory[index];
            if(RescueReady&&(index==config.rescue.firstEncounter||index>config.rescue.firstEncounter&&config.rescue.repeatIfSkipped))return "rescue";
            string type=stage.requireRecovery&&index==0?"wreck":config.encounterDeck[index%config.encounterDeck.Length];
            return (index+1)%config.upgradeEveryGroups==0?"upgrade":type;
        }
        Fighter ResolveFighter(CrewDefinition member,ProgressionState state)
        {
            var stats=new ResolvedCrewStats(member,state,config);string role=CrewHtmlScreen.RoleKey(member.role);if(role=="melee")role="front";
            return new Fighter{role=role,crewId=member.id,displayName=member.displayName,skin=role=="front"?"fat":"cat",hp=stats.hp,maxHP=stats.hp,atk=stats.attack,accuracy=stats.accuracy,defense=stats.defense,moveSpeed=stats.speed,crit=stats.crit,x=player.x,y=config.groundY};
        }
        bool BeginRescue(string choice)
        {
            if(encounter!="rescue"||phase!="choice")return false;
            if(choice=="skip"){phase="depart";return true;}
            if(choice!="rescue"||!RescueReady)return false;
            rescueClock=0;rescueApplied=false;phase="rescue_release";return true;
        }
        bool TickRescue(float dt)
        {
            if(phase!="rescue_release")return false;
            travelSpeed=0;rescueClock+=dt;
            if(!rescueApplied&&rescueClock>=config.rescue.unlockSeconds)
            {
                rescueApplied=true;var member=crewSource.Member(RescueStory.crewId);rescuedMember=member;
                rescuedFighter=ResolveFighter(member,crewProgression.Get(member));
                rescuedFighter.aboard=false;rescuedFighter.locked=!RoleUnlocked(rescuedFighter.role);
                rescuedFighter.x=landmarkX;rescuedFighter.y=config.groundY-36;rescuedFighter.motion="return";
                fighters.Add(rescuedFighter);
                int slot=Array.FindIndex(config.weapons,w=>w.id==rescuedFighter.role);var state=crewProgression.Get(member);
                if(slot>=0){weaponDamageBonus[slot]=member.stats.damageBonus.AtLevel(state.Level,state.Stars);weaponReloadBonus[slot]=member.stats.reloadSpeed.AtLevel(state.Level,state.Stars);weaponCriticalChance[slot]=member.stats.criticalChance.AtLevel(state.Level,state.Stars);}
                AddEffect("Hit",landmarkX+65,config.groundY-100,65,.2f);
            }
            var f=rescuedFighter;if(f==null)return true;
            if(f.jump>0)AdvanceJump(f,dt);
            else if(!f.aboard)
            {
                float goal=player.x+(1150-652)*player.width/1351+30+CrewLane(f.role)*5;
                f.x=Mathf.MoveTowards(f.x,goal,config.rescue.walkSpeed*dt);f.y=Mathf.MoveTowards(f.y,CrewGround(f),120*dt);f.motion="return";f.motionTime+=dt;
                if(Mathf.Abs(f.x-goal)<25)BeginJump(f,true);
            }
            if(f.aboard&&CageLift>=1)phase="depart";
            return true;
        }
        void AdvanceJump(Fighter f,float dt)
        {
            f.jump=Mathf.Max(0,f.jump-dt);float t=1-f.jump/f.jumpDuration;f.x=Mathf.Lerp(f.jumpStartX,f.jumpEndX,t);f.y=Mathf.Lerp(f.jumpStartY,f.jumpEndY,t)-Mathf.Sin(t*Mathf.PI)*90;
            if(f.jump<=0&&f.jumpIn){f.aboard=true;f.motion="aboard";}
        }
        public int StageExperienceReward(PlayerTankProfile profile)
        {
            int reward=config.tankXPReward;if(RescueStory==null)return reward;
            int requiredLevel=config.unlockLevels[RescueStory.weaponSlot],total=0;
            for(int level=1;level<requiredLevel;level++)total+=profile.Cost(level);
            return Math.Max(reward,total-profile.TotalXP);
        }
    }
}
