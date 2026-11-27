using UnityEngine;
namespace MegaTanks
{
    public sealed partial class HtmlBattleScreen
    {
        bool reviewFreeze;
        public void ReviewState(string state)
        {
            StartStage(0);reviewFreeze=true;paused=false;Simulation.ConfigureProfile(20);
            foreach(var f in Simulation.fighters)f.locked=false;
            Simulation.phase="wave";Simulation.encounter="";
            if(state=="boss"){var boss=Simulation.Spawn("boss");boss.x=1450;boss.weapons=new[]{true,true,false};}
            else if(state=="upgrade"){Simulation.phase="upgrade";Simulation.wave=4;Simulation.encounter="upgrade";Simulation.landmarkX=1080;Simulation.AddEffect("UpgradeAura",Simulation.player.x,config.groundY,580,2.6f);}
            else if(state=="gameplay")
            {var a=Simulation.Spawn("cannon_car");a.x=1250;var b=Simulation.Spawn("helicopter");b.x=1630;b.y=400;var c=Simulation.Spawn("trojan");c.x=1850;}
            else {Simulation.phase="choice";Simulation.wave=2;Simulation.encounter=state;Simulation.encounterIndex=1;Simulation.offeredSkin="speed";Simulation.landmarkX=1080;}
            modalKey="";Render();UpdateHUD();UpdateModal();
        }
    }
}
