using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CatTanks
{
    // Particle time is driven by battle time, so pause and replay do not leak real-time simulation.
    public sealed class CombatVfxPlayer
    {
        sealed class Instance
        {
            public GameObject root;public ParticleSystem[] particles;
            public CombatVfxLibrary.Entry entry;public float age;public int eventId;
        }
        readonly Transform parent;
        readonly CombatVfxLibrary library;
        readonly Dictionary<int,Instance> active=new Dictionary<int,Instance>();
        readonly Dictionary<string,Stack<Instance>> free=new Dictionary<string,Stack<Instance>>();
        readonly Dictionary<string,int> counts=new Dictionary<string,int>();
        public int ActiveCount=>active.Count;
        public int CreatedCount{get;private set;}
        public CombatVfxPlayer(Transform parent){this.parent=parent;library=Resources.Load<CombatVfxLibrary>("CombatVfxLibrary");}
        public bool Handles(BattleEffect effect)=>library&&library.For(effect)!=null;
        public void Render(Battle battle)
        {
            if(!library)return;
            // Retire first, allowing busy automatic weapons to reuse their previous bursts.
            var ids=new HashSet<int>(battle.effects.Where(e=>e.kind!="text").Select(e=>e.id));
            foreach(int id in active.Keys.Where(id=>!ids.Contains(id)).ToArray())Release(id);
            foreach(var effect in battle.effects)
            {
                if(effect.kind=="text")continue;
                var entry=library.For(effect);if(entry==null||!entry.prefab)continue;
                if(!active.TryGetValue(effect.id,out var item))
                {
                    if(active.Count>=library.maxActive)continue;
                    item=Acquire(entry);if(item==null)continue;
                    item.eventId=effect.id;item.age=0;active.Add(effect.id,item);
                    foreach(var ps in item.particles){ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);ps.useAutoRandomSeed=false;ps.randomSeed=(uint)(effect.id+1);}
                }
                Vector2 pos=effect.kind=="lightning"?effect.end:effect.position;
                item.root.transform.localPosition=new Vector3(pos.x,pos.y,-1);
                item.root.transform.localScale=Vector3.one*Mathf.Clamp(effect.size*entry.sizeMultiplier,20,320);
                if(effect.kind=="flash")
                {
                    Vector2 direction=effect.position-effect.end;
                    item.root.transform.localRotation=Quaternion.Euler(0,0,Mathf.Atan2(direction.y,direction.x)*Mathf.Rad2Deg);
                }
                else item.root.transform.localRotation=Quaternion.identity;
                float age=Mathf.Clamp01(1-effect.life/Mathf.Max(.001f,effect.maxLife))*entry.duration;
                if(age<item.age)foreach(var ps in item.particles)ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
                float delta=age<item.age?age:age-item.age;
                if(delta>0)foreach(var ps in item.particles){ps.Simulate(delta,true,item.age==0||age<item.age,false);ps.Pause(true);}
                item.age=age;
            }
        }
        Instance Acquire(CombatVfxLibrary.Entry entry)
        {
            if(!free.TryGetValue(entry.key,out var pool)){pool=new Stack<Instance>();free[entry.key]=pool;}
            Instance item;
            if(pool.Count>0)item=pool.Pop();
            else
            {
                int count=counts.TryGetValue(entry.key,out int n)?n:0;if(count>=entry.maxInstances)return null;
                var root=Object.Instantiate(entry.prefab,parent,false);root.name="Combat VFX • "+entry.key;
                var all=root.GetComponentsInChildren<ParticleSystem>(true);
                var systems=all.Where(p=>!all.Any(other=>other!=p&&p.transform.IsChildOf(other.transform))).ToArray();
                item=new Instance{root=root,particles=systems,entry=entry};counts[entry.key]=count+1;CreatedCount++;
            }
            item.root.SetActive(true);return item;
        }
        void Release(int id)
        {
            var item=active[id];foreach(var ps in item.particles)ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            item.root.SetActive(false);free[item.entry.key].Push(item);active.Remove(id);
        }
    }
}
