using System.Linq;
using UnityEngine;

namespace MegaTanks
{
    public sealed partial class HtmlBattleSimulation
    {
        public HtmlBattleArt Geometry { get;set; }
        Actor AimTarget(Actor owner,int weapon,bool enemy)
        {
            if(enemy)return player;
            var gun=config.weapons[weapon];
            return enemies.Where(e=>!e.dead&&e.x-owner.x>=gun.minRange&&e.x-owner.x<=gun.maxRange&&(e.kind!="dog"||weapon==0)).OrderBy(e=>e.x).FirstOrDefault();
        }
        public float AimAngle(Actor owner,int weapon,bool enemy)
        {
            if(Geometry==null)return 0;var gun=Geometry.Profile(owner.skin).guns.FirstOrDefault(g=>g.id==config.weapons[weapon].id);var target=AimTarget(owner,weapon,enemy);if(gun==null||target==null)return 0;
            float z=owner.width/1351,face=enemy?-1:1;var pivot=new Vector2(owner.x+face*(gun.pivot[0]-652)*z,owner.y+(gun.pivot[1]-1258)*z);
            var aim=Center(target)-pivot;float desired=Mathf.Atan2(aim.y,aim.x*face),rest=Mathf.Atan2(gun.tip[1]-gun.pivot[1],gun.tip[0]-gun.pivot[0]);
            return Mathf.Clamp(desired-rest,-.85f,.55f);
        }
        public Vector2 ShotOrigin(Actor owner,int weapon,bool enemy)
        {
            float face=enemy?-1:1;
            if(Geometry!=null)
            {
                if(owner.kind!="player"&&owner.kind!="boss")
                {
                    var vehicle=Geometry.data.enemies.FirstOrDefault(e=>e.id==owner.kind);
                    if(vehicle?.muzzle!=null&&vehicle.origin!=null&&vehicle.canvas!=null)
                    {float native=owner.kind=="helicopter"?1400:owner.kind=="cannon_car"?1200:vehicle.canvas[0];float z=owner.width/native;return new Vector2(owner.x+face*(vehicle.muzzle[0]-vehicle.origin[0])*z,owner.y+(vehicle.muzzle[1]-vehicle.origin[1])*z);}
                }
                var gun=Geometry.Profile(owner.skin).guns.FirstOrDefault(g=>g.id==config.weapons[weapon].id);
                if(gun!=null)
                {
                    float z=owner.width/1351,angle=AimAngle(owner,weapon,enemy),dx=gun.tip[0]-gun.pivot[0],dy=gun.tip[1]-gun.pivot[1];
                    return new Vector2(owner.x+face*(gun.pivot[0]-652+dx*Mathf.Cos(angle)-dy*Mathf.Sin(angle))*z,owner.y+(gun.pivot[1]-1258+dx*Mathf.Sin(angle)+dy*Mathf.Cos(angle))*z);
                }
            }
            return new Vector2(owner.x+face*(weapon==0?220:weapon==1?290:-210),owner.y-owner.width*(weapon==0?.8f:weapon==1?.56f:.83f));
        }
    }
}
