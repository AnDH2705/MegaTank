using System;
using System.Collections.Generic;
using System.Linq;
using MegaTankVFX;
using UnityEngine;

namespace MegaTanks
{
    // Uses the shipped main-branch catalog/prefabs in the HTML world's independent clock/camera.
    // The prefab assets and the legacy BattleVfx/Projectile2D path remain the shared source of art.
    public sealed class HtmlBattleVfx
    {
        sealed class Sample { public Vector3 position; public float age; }
        sealed class Tail
        {
            public LineRenderer line;public float seconds,minDistance,width;public readonly List<Sample> points=new List<Sample>();
            public void Tick(float delta,Vector3 position,bool emitting,float roadShift)
            {
                if(delta<=0)return;
                foreach(var p in points){p.age+=delta;p.position.x-=roadShift;}
                points.RemoveAll(p=>p.age>seconds);
                if(emitting&&(points.Count==0||Vector3.Distance(points[points.Count-1].position,position)>=minDistance))points.Add(new Sample{position=position});
                line.positionCount=points.Count;
                for(int i=0;i<points.Count;i++)line.SetPosition(i,points[points.Count-1-i].position);
            }
        }
        sealed class Instance
        {
            public GameObject root,prefab;public ParticleSystem[] particles;public bool[] emissions;public ParticleSystem.Particle[] buffer;
            public readonly List<Tail> tails=new List<Tail>();public BattleVfxEffect sprite;
            public HtmlBattleSimulation.Bullet bullet;public HtmlBattleSimulation.Effect source;
            public float age,life,scale,roadAt;public bool stopped,road,projectile;
        }
        readonly BattleVfxCatalog catalog;readonly Transform parent;readonly Func<float,float,Vector3> world;
        readonly List<Instance> active=new List<Instance>();
        readonly Dictionary<GameObject,Stack<Instance>> pool=new Dictionary<GameObject,Stack<Instance>>();
        readonly Dictionary<HtmlBattleSimulation.Bullet,Instance> projectiles=new Dictionary<HtmlBattleSimulation.Bullet,Instance>();
        readonly HashSet<HtmlBattleSimulation.Effect> seen=new HashSet<HtmlBattleSimulation.Effect>();
        HtmlBattleSimulation simulation;
        public float Clock { get;private set; }
        public int ActiveCount=>active.Count;
        public int ProjectileCount=>projectiles.Count;
        public int TailPointCount=>active.Sum(e=>e.tails.Sum(t=>t.points.Count));
        public int ParticleCount=>active.Sum(e=>e.particles==null?0:e.particles.Sum(p=>p.particleCount));
        public string DebugState=>string.Join(", ",active.Select(e=>e.root.name+" age="+e.age+" particles="+(e.particles==null?0:e.particles.Sum(p=>p.particleCount))+" stopped="+e.stopped));
        public Vector3[] GroundFirePositions=>active.Where(e=>e.sprite!=null&&e.sprite.Kind==BattleVfxKind.GroundFire).Select(e=>e.root.transform.position).ToArray();
        public bool Available=>catalog!=null&&catalog.fireBoltPrefab!=null;
        public HtmlBattleVfx(Transform owner,Func<float,float,Vector3> coordinate)
        {parent=owner;world=coordinate;catalog=Resources.Load<BattleVfxCatalog>("BattleVfx");}
        public bool HasProjectile(HtmlBattleSimulation.Bullet bullet)=>projectiles.ContainsKey(bullet)&&!bullet.homing;
        public bool Handles(HtmlBattleSimulation.Effect fx)=>Available&&(fx.clip=="Muzzle"||fx.clip=="Hit"||fx.clip=="Explosion"||fx.clip=="GroundFire"||fx.clip=="Smoke");
        public void Clear()
        {
            for(int i=active.Count-1;i>=0;i--)Release(i);
            projectiles.Clear();seen.Clear();simulation=null;Clock=0;
        }
        Instance Particle(GameObject prefab,Vector3 position,Vector2 direction,float scale,float life,bool road)
        {
            if(prefab==null||active.Count>=catalog.maxEffects)return null;
            if(!pool.TryGetValue(prefab,out var stack)){stack=new Stack<Instance>();pool.Add(prefab,stack);}
            var e=stack.Count>0?stack.Pop():Create(prefab);
            e.source=null;e.bullet=null;e.projectile=false;e.stopped=false;e.age=0;e.life=life>0?life:prefab.GetComponent<VfxAutoRelease>()?.Lifetime??1.5f;e.road=road;e.scale=scale;e.roadAt=simulation.road;
            e.root.transform.SetPositionAndRotation(position,Quaternion.Euler(0,0,Mathf.Atan2(direction.y,direction.x)*Mathf.Rad2Deg));e.root.transform.localScale=Vector3.one*scale;e.root.SetActive(true);
            foreach(var t in e.tails){t.points.Clear();t.line.positionCount=0;t.line.widthMultiplier=t.width*scale;}
            for(int i=0;i<e.particles.Length;i++){var ps=e.particles[i];ps.Stop(false,ParticleSystemStopBehavior.StopEmittingAndClear);var emission=ps.emission;emission.enabled=e.emissions[i];ps.Play(false);ps.Pause(false);ps.Simulate(.001f,false,false,true);}
            active.Add(e);return e;
        }
        Instance Create(GameObject prefab)
        {
            var e=new Instance{prefab=prefab,root=UnityEngine.Object.Instantiate(prefab,parent)};e.root.name="Main VFX "+prefab.name;
            // The adapter owns simulation and release; no demo motion or wall-clock expiry.
            foreach(var release in e.root.GetComponentsInChildren<VfxAutoRelease>(true))release.enabled=false;
            foreach(var motion in e.root.GetComponentsInChildren<FireBoltDemoMotion>(true))motion.enabled=false;
            foreach(var item in e.root.GetComponentsInChildren<Transform>(true))item.gameObject.layer=29;
            foreach(var renderer in e.root.GetComponentsInChildren<Renderer>(true))renderer.sortingOrder+=1000;
            e.particles=e.root.GetComponentsInChildren<ParticleSystem>(true);
            e.emissions=e.particles.Select(p=>p.emission.enabled).ToArray();
            e.buffer=new ParticleSystem.Particle[Math.Max(1,e.particles.Select(p=>p.main.maxParticles).DefaultIfEmpty(1).Max())];
            foreach(var trail in e.root.GetComponentsInChildren<TrailRenderer>(true))
            {
                // A native TrailRenderer ages using Unity time even while this battle is paused.
                // Keep its material/gradient/width, but advance sample ages with the battle clock.
                trail.emitting=false;trail.enabled=false;trail.Clear();
                var go=new GameObject("Battle-clock "+trail.name);go.layer=29;go.transform.SetParent(trail.transform,false);
                var line=go.AddComponent<LineRenderer>();line.sharedMaterials=trail.sharedMaterials;line.useWorldSpace=true;
                line.widthCurve=trail.widthCurve;line.colorGradient=trail.colorGradient;line.textureMode=trail.textureMode;line.alignment=trail.alignment;
                line.numCornerVertices=trail.numCornerVertices;line.numCapVertices=trail.numCapVertices;line.sortingOrder=trail.sortingOrder;line.shadowCastingMode=trail.shadowCastingMode;
                e.tails.Add(new Tail{line=line,seconds=trail.time,minDistance=trail.minVertexDistance,width=trail.widthMultiplier});
            }
            return e;
        }
        void Sprite(BattleVfxKind kind,Vector3 position,float size,float life,bool road)
        {
            var clip=catalog.Get(kind);if(clip?.frames==null||clip.frames.Length==0||active.Count>=catalog.maxEffects)return;
            var root=new GameObject("Main VFX "+kind);root.layer=29;root.transform.SetParent(parent,false);root.transform.position=position;
            var sprite=root.AddComponent<BattleVfxEffect>();sprite.Configure(kind,clip,size,kind==BattleVfxKind.GroundFire?160:1090,null,life,Vector2.right);
            active.Add(new Instance{root=root,sprite=sprite,life=life,road=road,roadAt=simulation.road});
        }
        void GroundFire(float x,float y,float width,float life)
        {
            float height=catalog.groundFireHeight;int count=Mathf.Clamp(Mathf.CeilToInt(width/height),1,6);
            for(int i=0;i<count;i++){float offset=count==1?0:Mathf.Lerp(-width*.5f+height*.5f,width*.5f-height*.5f,i/(float)(count-1));Sprite(BattleVfxKind.GroundFire,world(x,y)+Vector3.right*offset,height,life,true);}
        }
        void Spawn(HtmlBattleSimulation.Effect fx)
        {
            var position=world(fx.x,fx.y);var direction=new Vector2(Mathf.Cos(fx.angle),-Mathf.Sin(fx.angle));Instance e=null;
            if(fx.clip=="Muzzle")
            {
                float size=fx.weapon==2?catalog.mortarMuzzleSize:fx.weapon==1?catalog.cannonMuzzleSize:catalog.machineGunMuzzleSize;
                e=Particle(catalog.muzzlePrefab,position,direction,size/1.1f,-1,false);
            }
            else if(fx.clip=="Hit")e=Particle(fx.heavy?catalog.explosionSmallPrefab:catalog.hitPrefab,position,Vector2.right,fx.heavy?catalog.cannonExplosionSize/2.6f:catalog.impactSize/.9f,-1,fx.road);
            else if(fx.clip=="Explosion")
            {
                bool big=fx.road||fx.size>=190;float size=fx.road?catalog.vehicleExplosionSize*(fx.size>=300?1.6f:1):fx.size>=190?catalog.mortarExplosionSize:catalog.cannonExplosionSize;
                e=Particle(big?catalog.explosionBigPrefab:catalog.explosionSmallPrefab,position,Vector2.right,size/(big?3.8f:2.6f),-1,fx.road);
                if(fx.groundFire)GroundFire(fx.x,simulation.config.groundY,size*.8f,catalog.groundFireSeconds);
            }
            else if(fx.clip=="GroundFire")GroundFire(fx.x,fx.y,fx.size/100,catalog.groundFireSeconds);
            else if(fx.clip=="Smoke")Sprite(BattleVfxKind.Smoke,position,fx.size/100,fx.life,true);
            if(e!=null)e.source=fx;
        }
        void Stop(Instance e)
        {
            if(e.stopped)return;e.stopped=true;e.bullet=null;e.projectile=false;
            foreach(var ps in e.particles){var emission=ps.emission;emission.enabled=false;ps.Stop(false,ParticleSystemStopBehavior.StopEmitting);}
        }
        void Shift(Instance e,float shift)
        {
            if(shift==0)return;
            if(!e.projectile)e.root.transform.position-=Vector3.right*shift;
            if(e.particles==null)return;
            foreach(var ps in e.particles)
            {
                if(ps.main.simulationSpace!=ParticleSystemSimulationSpace.World)continue;
                int count=ps.GetParticles(e.buffer);for(int i=0;i<count;i++)e.buffer[i].position-=Vector3.right*shift;ps.SetParticles(e.buffer,count);
            }
        }
        void Release(int index)
        {
            var e=active[index];active.RemoveAt(index);
            if(e.bullet!=null)projectiles.Remove(e.bullet);
            // Scene teardown can destroy children before the owning world's OnDestroy.
            if(e.root==null)return;
            e.root.SetActive(false);
            if(e.prefab==null){UnityEngine.Object.Destroy(e.root);return;}
            foreach(var ps in e.particles)ps.Stop(false,ParticleSystemStopBehavior.StopEmittingAndClear);
            foreach(var t in e.tails){t.points.Clear();t.line.positionCount=0;}
            var stack=pool[e.prefab];if(stack.Count<32)stack.Push(e);else UnityEngine.Object.Destroy(e.root);
        }
        public void Draw(HtmlBattleSimulation s,float delta)
        {
            if(!Available)return;
            if(simulation!=s){Clear();simulation=s;}
            if(s.phase=="choice")delta=0;
            Clock+=delta;
            foreach(var fx in s.effects)if(Handles(fx)&&seen.Add(fx))Spawn(fx);
            seen.RemoveWhere(fx=>!s.effects.Contains(fx));
            foreach(var b in s.bullets)
            {
                if(b.homing||projectiles.ContainsKey(b))continue;
                var direction=new Vector2(b.tx-b.x,-(b.ty-b.y));
                var e=Particle(catalog.fireBoltPrefab,world(b.x,b.y),direction,b.weapon=="mg"?.3f:.65f,float.PositiveInfinity,true);
                if(e==null)continue;e.projectile=true;e.bullet=b;projectiles.Add(b,e);
            }
            foreach(var pair in projectiles.ToArray())
            {
                var b=pair.Key;var e=pair.Value;
                if(!s.bullets.Contains(b))
                {
                    projectiles.Remove(b);e.root.transform.position=world(b.x,b.y);
                    foreach(var tail in e.tails)tail.points.Add(new Sample{position=e.root.transform.position});
                    Stop(e);continue;
                }
                var before=e.root.transform.position;var position=world(b.x,b.y);var direction=position-before;
                if(direction.sqrMagnitude>.000001f)e.root.transform.rotation=Quaternion.Euler(0,0,Mathf.Atan2(direction.y,direction.x)*Mathf.Rad2Deg);
                e.root.transform.position=position;
            }
            for(int i=active.Count-1;i>=0;i--)
            {
                var e=active[i];float shift=delta>0&&e.road?(s.road-e.roadAt)/100:0;e.roadAt=s.road;Shift(e,shift);
                if(e.source?.owner!=null&&!e.source.owner.dead&&!e.stopped&&e.source.clip=="Muzzle")
                {
                    var mouth=s.ShotOrigin(e.source.owner,e.source.weapon,e.source.owner!=s.player);e.root.transform.position=world(mouth.x,mouth.y);
                }
                if(e.sprite!=null){if(!e.sprite.Tick(delta))Release(i);continue;}
                e.age+=delta;if(e.age>=e.life)Stop(e);
                foreach(var t in e.tails)t.Tick(delta,e.root.transform.position,e.projectile&&!e.stopped,shift);
                if(delta>0)foreach(var ps in e.particles)ps.Simulate(delta,false,false,true);
                if(e.stopped&&e.particles.All(p=>p.particleCount==0)&&e.tails.All(t=>t.points.Count==0))Release(i);
            }
        }
    }
}
