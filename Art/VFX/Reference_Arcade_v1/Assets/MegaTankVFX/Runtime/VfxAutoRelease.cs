using System.Collections.Generic;
using UnityEngine;

namespace MegaTankVFX
{
    /// <summary>Reuses effect objects and lets their smoke/trails finish before recycling.</summary>
    public sealed class VfxAutoRelease : MonoBehaviour
    {
        public float Lifetime = 1.4f;
        private static readonly Dictionary<int, Stack<VfxAutoRelease>> Pools = new Dictionary<int, Stack<VfxAutoRelease>>();
        private ParticleSystem[] particles;
        private TrailRenderer[] trails;
        private int sourceId;
        private float age;
        private float trailTail;
        private bool stopping;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetPools() { Pools.Clear(); }

        private void Awake() { Cache(); }
        private void Cache()
        {
            particles = GetComponentsInChildren<ParticleSystem>(true);
            trails = GetComponentsInChildren<TrailRenderer>(true);
            trailTail = 0f;
            foreach (var trail in trails) trailTail = Mathf.Max(trailTail, trail.time);
        }
        private void OnEnable() { age = 0f; stopping = false; }

        public static GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation, float scale = 1f)
        {
            if (!prefab) return null;
            int id = prefab.GetInstanceID();
            Stack<VfxAutoRelease> pool;
            if (!Pools.TryGetValue(id, out pool)) { pool = new Stack<VfxAutoRelease>(); Pools[id] = pool; }
            VfxAutoRelease fx = null;
            while (pool.Count > 0 && !fx) fx = pool.Pop();
            if (!fx)
            {
                var created = Instantiate(prefab, position, rotation);
                fx = created.GetComponent<VfxAutoRelease>();
                if (!fx) fx = created.AddComponent<VfxAutoRelease>();
            }
            fx.sourceId = id;
            fx.transform.SetParent(null, false);
            fx.transform.SetPositionAndRotation(position, rotation);
            fx.transform.localScale = Vector3.one * scale;
            fx.Cache();
            fx.gameObject.SetActive(true);
            fx.age = 0f; fx.stopping = false;
            foreach (var trail in fx.trails) { trail.Clear(); trail.emitting = true; }
            foreach (var ps in fx.particles) { ps.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear); ps.Play(false); }
            return fx.gameObject;
        }

        public void StopEmission()
        {
            if (particles == null) Cache();
            foreach (var ps in particles) ps.Stop(false, ParticleSystemStopBehavior.StopEmitting);
            foreach (var trail in trails) trail.emitting = false;
            stopping = true;
        }

        private void Update()
        {
            age += Time.deltaTime;
            if (age < Lifetime) return;
            if (!stopping) StopEmission();
            bool alive = false;
            foreach (var ps in particles) alive |= ps.IsAlive(false);
            if ((alive || age < Lifetime + trailTail) && age < Lifetime + 3f) return;
            if (sourceId == 0) { Destroy(gameObject); return; }
            Stack<VfxAutoRelease> pool;
            if (!Pools.TryGetValue(sourceId, out pool)) { pool = new Stack<VfxAutoRelease>(); Pools[sourceId] = pool; }
            gameObject.SetActive(false);
            if (pool.Count < 32) pool.Push(this); else Destroy(gameObject);
        }

        private void OnDisable()
        {
            if (particles == null) return;
            foreach (var ps in particles) if (ps) ps.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
            foreach (var trail in trails) if (trail) trail.Clear();
        }
    }
}
