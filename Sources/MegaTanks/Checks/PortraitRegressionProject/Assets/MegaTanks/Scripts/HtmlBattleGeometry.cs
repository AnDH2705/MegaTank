using System.Linq;
using UnityEngine;

namespace MegaTanks
{
    public sealed partial class HtmlBattleSimulation
    {
        public HtmlBattleArt Geometry { get;set; }
        public float EnemyGunRecoil(Actor owner)
        {
            if(owner.kind!="cannon_car"||owner.dead)return 0;
            float age=config.enemies.First(e=>e.id==owner.kind).cooldown-owner.attack;
            return age>=0&&age<.35f?Mathf.Sin(Mathf.PI*age/.35f)*16:0;
        }
        Actor AimTarget(Actor owner,int weapon,bool enemy)
        {
            if(enemy)return player;
            var gun=config.weapons[weapon];
            return enemies.Where(e=>!e.dead&&(e.kind!="boss"||bossPhase!="entry")&&e.x-owner.x>=gun.minRange&&e.x-owner.x<=gun.maxRange&&(e.kind!="dog"||weapon==0)).OrderBy(e=>e.x).FirstOrDefault();
        }
        public float AimAngle(Actor owner,int weapon,bool enemy)
        {
            if(Geometry==null)return 0;var gun=Geometry.Profile(owner.skin).guns.FirstOrDefault(g=>g.id==config.weapons[weapon].id);var target=AimTarget(owner,weapon,enemy);if(gun?.pivot?.Length!=2||gun.tip?.Length!=2||target==null)return 0;
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
                    if(vehicle?.muzzle?.Length==2&&vehicle.origin?.Length==2&&vehicle.canvas?.Length==2)
                    {
                        float native=owner.kind=="helicopter"?1400:owner.kind=="cannon_car"?1200:vehicle.canvas[0],z=owner.width/native;
                        var moving=vehicle.parts.FirstOrDefault(p=>p.name=="barrel"||p.name=="machine_gun");
                        if(moving?.pivot?.Length==2)
                        {
                            var pivot=new Vector2(owner.x+face*(moving.pivot[0]-vehicle.origin[0])*z,owner.y+(moving.pivot[1]-vehicle.origin[1])*z);
                            float dx=vehicle.muzzle[0]-moving.pivot[0],dy=vehicle.muzzle[1]-moving.pivot[1];var target=Center(player);
                            owner.gunAngle=Mathf.Clamp(Mathf.Atan2(target.y-pivot.y,(target.x-pivot.x)*face),-.6f,.65f)-Mathf.Atan2(dy,dx);
                            dx-=EnemyGunRecoil(owner);float a=owner.gunAngle;
                            return pivot+new Vector2(face*(Mathf.Cos(a)*dx-Mathf.Sin(a)*dy)*z,(Mathf.Sin(a)*dx+Mathf.Cos(a)*dy)*z);
                        }
                        return new Vector2(owner.x+face*(vehicle.muzzle[0]-vehicle.origin[0])*z,owner.y+(vehicle.muzzle[1]-vehicle.origin[1])*z);
                    }
                }
                if(owner.kind!="player"&&owner.kind!="boss")return new Vector2(owner.x+face*owner.width*.38f,Center(owner).y);
                var gun=Geometry.Profile(owner.skin).guns.FirstOrDefault(g=>g.id==config.weapons[weapon].id);
                if(gun?.pivot?.Length==2&&gun.tip?.Length==2)
                {
                    float z=owner.width/1351,angle=AimAngle(owner,weapon,enemy),dx=gun.tip[0]-gun.pivot[0],dy=gun.tip[1]-gun.pivot[1];
                    return new Vector2(owner.x+face*(gun.pivot[0]-652+dx*Mathf.Cos(angle)-dy*Mathf.Sin(angle))*z,owner.y+(gun.pivot[1]-1258+dx*Mathf.Sin(angle)+dy*Mathf.Cos(angle))*z);
                }
            }
            return new Vector2(owner.x+face*(weapon==0?220:weapon==1?290:-210),owner.y-owner.width*(weapon==0?.8f:weapon==1?.56f:.83f));
        }
    }
}
