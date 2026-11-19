using UnityEngine;
namespace MegaTanks
{
    // Use body edges, so wide tanks are reached at their hull rather than their center.
    public static class CommandoMelee
    {
        public const float Reach=.15f;
        const float StandingGap=.08f;
        static Vector2 Gaps(Bounds a,Bounds b)=>new Vector2(Mathf.Max(0,Mathf.Max(a.min.x-b.max.x,b.min.x-a.max.x)),Mathf.Max(0,Mathf.Max(a.min.y-b.max.y,b.min.y-a.max.y)));
        public static bool Valid(Collider2D target)=>target!=null&&target.enabled&&target.gameObject.activeInHierarchy;
        public static bool VerticallyReachable(Collider2D attacker,Collider2D target)
        {
            Physics2D.SyncTransforms();return Valid(attacker)&&Valid(target)&&Gaps(attacker.bounds,target.bounds).y<=Reach;
        }
        public static float DistanceSquared(Collider2D attacker,Collider2D target)
        {
            Physics2D.SyncTransforms();return Gaps(attacker.bounds,target.bounds).sqrMagnitude;
        }
        public static bool InReach(Collider2D attacker,Collider2D target)=>Valid(attacker)&&Valid(target)&&DistanceSquared(attacker,target)<=Reach*Reach;
        public static Vector2 Approach(Collider2D attacker,Collider2D target,float speed,float deltaTime)
        {
            if(!VerticallyReachable(attacker,target)||InReach(attacker,target)||deltaTime<=0)return Vector2.zero;
            var body=attacker.bounds;var other=target.bounds;float offset=body.center.x-attacker.transform.position.x;
            float x=other.center.x>=body.center.x?other.min.x-body.extents.x-StandingGap-offset:other.max.x+body.extents.x+StandingGap-offset;
            var position=attacker.transform.position;float before=position.x;position.x=Mathf.MoveTowards(before,x,Mathf.Max(0,speed)*deltaTime);
            attacker.transform.position=position;Physics2D.SyncTransforms();return Vector2.right*((position.x-before)/deltaTime);
        }
        public static Collider2D PlayerTarget(BattleGame game,Collider2D attacker)
        {
            var hull=game.HullHitbox.GetComponent<Collider2D>();Collider2D target=VerticallyReachable(attacker,hull)?hull:null;
            if(game.Commando!=null&&game.Commando.Active)
            {
                var crew=game.Commando.GetComponent<Collider2D>();
                if(VerticallyReachable(attacker,crew)&&(target==null||DistanceSquared(attacker,crew)<DistanceSquared(attacker,target)))target=crew;
            }
            return target;
        }
    }
}
