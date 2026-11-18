using System.Collections.Generic;
using UnityEngine;

namespace MegaTanks
{
    public sealed class BattleVfx : MonoBehaviour
    {
        readonly List<BattleVfxEffect> active=new List<BattleVfxEffect>();
        BattleGame game;
        public BattleVfxCatalog Catalog { get; private set; }
        public int ActiveCount => active.Count;
        public void Configure(BattleGame battle,BattleVfxCatalog catalog){game=battle;Catalog=catalog;}

        public BattleVfxEffect Play(BattleVfxKind kind,Vector2 position,float size,Vector2 direction,
            Transform follow=null,float lifetime=-1)
        {
            var clip=Catalog!=null?Catalog.Get(kind):null;
            if(clip==null||clip.frames==null||clip.frames.Length==0)return null;
            for(int i=active.Count-1;i>=0;i--)if(active[i]==null)active.RemoveAt(i);
            // A skipped effect must never remove a tracer from a still-live projectile.
            if(active.Count>=Catalog.maxEffects)return null;
            var effect=new GameObject("VFX "+kind).AddComponent<BattleVfxEffect>();
            effect.transform.SetParent(transform,false);effect.transform.position=position;
            int order=kind==BattleVfxKind.Smoke?69:kind==BattleVfxKind.Tracer?70:kind==BattleVfxKind.Muzzle?75:kind==BattleVfxKind.GroundFire?80:85;
            effect.Configure(kind,clip,Mathf.Max(.01f,size),order,follow,lifetime,direction);
            active.Add(effect);return effect;
        }
        BattleVfxEffect Particle(GameObject prefab, BattleVfxKind kind, Vector2 position, Vector2 direction,
            float scale, Transform follow=null, bool projectile=false)
        {
            if(prefab==null)return null;
            for(int i=active.Count-1;i>=0;i--)if(active[i]==null)active.RemoveAt(i);
            if(active.Count>=Catalog.maxEffects)return null;
            var effect=new GameObject("Arcade VFX "+kind).AddComponent<BattleVfxEffect>();
            effect.transform.SetParent(transform,false);
            effect.ConfigureParticle(prefab,kind,position,direction,scale,follow,projectile);
            active.Add(effect);return effect;
        }
        public BattleVfxEffect Muzzle(Transform socket,Vector2 direction,bool heavy=false,bool mortar=false)
        {
            if(Catalog==null)return null;
            float size=mortar?Catalog.mortarMuzzleSize:heavy?Catalog.cannonMuzzleSize:Catalog.machineGunMuzzleSize;
            if(Catalog.muzzlePrefab!=null)
                return Particle(Catalog.muzzlePrefab,BattleVfxKind.Muzzle,socket.position,direction,size/1.1f,socket);
            return Play(BattleVfxKind.Muzzle,socket.position,size,direction,socket);
        }
        public bool Projectile(Transform visual,bool heavy,out BattleVfxEffect effect)
        {
            effect=null;
            if(Catalog==null)return false;
            if(Catalog.fireBoltPrefab!=null)
                effect=Particle(Catalog.fireBoltPrefab,heavy?BattleVfxKind.Smoke:BattleVfxKind.Tracer,
                    visual.position,visual.right,heavy?.65f:.3f,visual,true);
            else effect=Play(heavy?BattleVfxKind.Smoke:BattleVfxKind.Tracer,visual.position,
                heavy?Catalog.smokeSize:Catalog.tracerSize,visual.right,visual);
            return effect!=null;
        }
        public void Impact(Vector2 position,bool heavy)
        {
            if(Catalog==null)return;
            var prefab=heavy?Catalog.explosionSmallPrefab:Catalog.hitPrefab;
            if(prefab!=null)Particle(prefab,heavy?BattleVfxKind.Explosion:BattleVfxKind.Impact,position,Vector2.right,
                heavy?Catalog.cannonExplosionSize/2.6f:Catalog.impactSize/.9f);
            else Play(heavy?BattleVfxKind.Explosion:BattleVfxKind.Impact,position,
                heavy?Catalog.cannonExplosionSize:Catalog.impactSize,Vector2.right);
        }
        public void Blast(Vector2 position,bool mortar=false,bool ground=true,float sizeMultiplier=1)
        {
            if(Catalog==null)return;
            float size=(mortar?Catalog.mortarExplosionSize:Catalog.vehicleExplosionSize)*sizeMultiplier;
            if(Catalog.explosionBigPrefab!=null)Particle(Catalog.explosionBigPrefab,BattleVfxKind.Explosion,position,Vector2.right,size/3.8f);
            else Play(BattleVfxKind.Explosion,position,size,Vector2.right);
            if(ground&&!mortar)GroundFire(new Vector2(position.x,BattleGame.GroundHeight),size*.8f);
        }
        public void GroundFire(Vector2 position,float width)
        {
            if(Catalog==null)return;
            float height=Catalog.groundFireHeight;
            int count=Mathf.Clamp(Mathf.CeilToInt(width/height),1,6);
            for(int i=0;i<count;i++)
            {
                float x=count==1?0:Mathf.Lerp(-width*.5f+height*.5f,width*.5f-height*.5f,i/(float)(count-1));
                Play(BattleVfxKind.GroundFire,position+Vector2.right*x,height,Vector2.right,null,Catalog.groundFireSeconds);
            }
        }
        void Release(int index)
        {
            if(active[index]!=null){active[index].gameObject.SetActive(false);Destroy(active[index].gameObject);}
            active.RemoveAt(index);
        }
        public void Clear(){for(int i=active.Count-1;i>=0;i--)Release(i);}
        void LateUpdate()
        {
            if(game==null)return;
            // Let final explosions finish on rewards/victory/defeat, but freeze during pause.
            float delta=game.Paused?0:Time.deltaTime;
            for(int i=active.Count-1;i>=0;i--)
                if(active[i]==null||!active[i].Tick(delta))Release(i);
        }
    }
}
