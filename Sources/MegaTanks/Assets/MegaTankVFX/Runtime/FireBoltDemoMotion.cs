using UnityEngine;

namespace MegaTankVFX
{
    /// <summary>Preview motion only. In game, attach FireBolt VFX to your existing projectile.</summary>
    public sealed class FireBoltDemoMotion : MonoBehaviour
    {
        public float Speed = 18f;
        public float TravelTime = 0.32f;
        public GameObject ImpactPrefab;
        private float age;
        private bool impacted;
        private void OnEnable() { age = 0f; impacted = false; }
        private void Update()
        {
            if (impacted) return;
            float step = Mathf.Min(Time.deltaTime, Mathf.Max(0f, TravelTime - age));
            transform.position += transform.right * Speed * step;
            age += step;
            if (age < TravelTime) return;
            impacted = true;
            if (ImpactPrefab) VfxAutoRelease.Spawn(ImpactPrefab, transform.position, Quaternion.identity);
            var release = GetComponent<VfxAutoRelease>();
            if (release) release.StopEmission();
        }
    }
}
