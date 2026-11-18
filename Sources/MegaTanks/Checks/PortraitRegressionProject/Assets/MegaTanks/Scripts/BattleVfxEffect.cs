using UnityEngine;

namespace MegaTanks
{
    public sealed class BattleVfxEffect : MonoBehaviour
    {
        BattleVfxClip clip;
        SpriteRenderer sprite;
        Transform anchor;
        bool attached;
        Vector3 anchorDirection;
        float age, lifetime;
        GameObject particleObject;
        bool particleMode;
        public GameObject ParticleObject => particleObject;

        public void ConfigureParticle(GameObject prefab,BattleVfxKind kind,Vector2 position,Vector2 direction,
            float scale,Transform follow,bool projectile)
        {
            Kind=kind;particleMode=true;anchor=follow;attached=follow!=null;
            transform.position=position;
            transform.rotation=Quaternion.Euler(0,0,Mathf.Atan2(direction.y,direction.x)*Mathf.Rad2Deg);
            if(attached)anchorDirection=anchor.InverseTransformVector(direction);
            particleObject=MegaTankVFX.VfxAutoRelease.Spawn(prefab,position,transform.rotation,scale);
            particleObject.transform.SetParent(transform,true);
            if(projectile)
            {
                var motion=particleObject.GetComponent<MegaTankVFX.FireBoltDemoMotion>();
                if(motion!=null){motion.enabled=false;Destroy(motion);}
                particleObject.GetComponent<MegaTankVFX.VfxAutoRelease>().Lifetime=float.PositiveInfinity;
            }
        }

        // Stop emission without removing the existing world-space smoke and trail.
        public void Detach(Vector2 position)
        {
            if(!particleMode)return;
            transform.position=position;
            attached=false;anchor=null;
            if(particleObject!=null&&particleObject.transform.parent==transform)
                particleObject.GetComponent<MegaTankVFX.VfxAutoRelease>().StopEmission();
        }

        void OnDestroy()
        {
            if(particleObject!=null&&particleObject.transform.parent==transform)Destroy(particleObject);
        }
        public BattleVfxKind Kind { get; private set; }
        public float Age => age;

        public void Configure(BattleVfxKind kind, BattleVfxClip data, float size, int order,
            Transform follow, float seconds, Vector2 direction)
        {
            Kind=kind;clip=data;anchor=follow;attached=follow!=null;lifetime=seconds;
            sprite=gameObject.AddComponent<SpriteRenderer>();sprite.sprite=clip.frames[0];
            sprite.sortingOrder=order;
            transform.localScale=Vector3.one*(size/(sprite.sprite.rect.width/sprite.sprite.pixelsPerUnit));
            transform.rotation=Quaternion.Euler(0,0,Mathf.Atan2(direction.y,direction.x)*Mathf.Rad2Deg);
            if(attached) anchorDirection=anchor.InverseTransformVector(direction);
        }

        public bool Tick(float delta)
        {
            if(attached)
            {
                if(anchor==null || !anchor.gameObject.activeInHierarchy)
                {
                    if(particleMode)Detach(transform.position);
                    else return false;
                }
                if(attached)
                {
                transform.position=anchor.position;
                Vector3 direction=anchor.TransformVector(anchorDirection);
                transform.rotation=Quaternion.Euler(0,0,Mathf.Atan2(direction.y,direction.x)*Mathf.Rad2Deg);
                }
            }
            age+=delta;
            if(particleMode)return particleObject!=null&&particleObject.activeSelf&&particleObject.transform.parent==transform;
            int frame=Mathf.FloorToInt(age*clip.framesPerSecond);
            if((lifetime>0&&age>=lifetime)||(!clip.loop&&frame>=clip.frames.Length))return false;
            sprite.sprite=clip.frames[clip.loop?frame%clip.frames.Length:frame];
            if(Kind==BattleVfxKind.GroundFire&&lifetime>0)
                sprite.color=new Color(1,1,1,Mathf.Clamp01((lifetime-age)/.4f));
            return true;
        }
    }
}
