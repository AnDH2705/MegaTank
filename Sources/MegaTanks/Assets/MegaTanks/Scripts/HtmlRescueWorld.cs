using UnityEngine;
namespace MegaTanks
{
    public sealed partial class HtmlBattleWorld
    {
        void DrawRescue(float dt)
        {
            if(!s.CageVisible)return;float lift=s.CageLift,size=s.config.rescue.cageSize;
            if(s.rescuedFighter==null)
                PersonAt(s.prisonerToken,art.catDriver,scene.transform,"idle",World(s.landmarkX-5,s.config.groundY-36),s.config.rescue.prisonerHeight/100,215,dt,false);
            Quad("prisoner_cage",s.landmarkX,s.config.groundY-size*.5f+s.config.rescue.cageFeetInset-lift*180,size,size,220,1-lift,false,-lift*8);
        }
    }
}
