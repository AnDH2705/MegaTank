using UnityEngine;

namespace MegaTanks
{
    public sealed class EnemyVehicleArtwork : MonoBehaviour
    {
        [SerializeField] Transform muzzle;
        [SerializeField] Transform[] wheels;
        [SerializeField] SpriteRenderer propeller;
        [SerializeField] Sprite[] propellerFrames;
        [SerializeField] bool groundVehicle;
        SpriteRenderer[] renderers;
        EnemyEntity owner;
        float propellerTime;
        bool flashing;
        public Transform Muzzle => muzzle;
        public Bounds OwnerLocalBounds { get; private set; }

        public void Configure(Transform gunTip, Transform[] rotatingWheels, SpriteRenderer blades, Sprite[] frames, bool car)
        { muzzle = gunTip; wheels = rotatingWheels; propeller = blades; propellerFrames = frames; groundVehicle = car; }

        Bounds WorldBounds()
        {
            Bounds bounds = renderers[0].bounds;
            foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
            return bounds;
        }

        public void Attach(EnemyEntity enemy, float width)
        {
            owner = enemy;
            renderers = GetComponentsInChildren<SpriteRenderer>();
            float scale = Mathf.Max(.1f, width) / WorldBounds().size.x;
            transform.localScale = new Vector3(-scale, scale, scale);
            Bounds bounds = WorldBounds();
            Vector3 offset = enemy.transform.position - bounds.center;
            if (groundVehicle) offset.y = BattleGame.GroundHeight - bounds.min.y;
            transform.position += offset;
            bounds = WorldBounds();
            if (groundVehicle)
            {
                // Keep the logical aim point centered on a grounded car, even if its spawn lane had a Y offset.
                Vector3 artPosition = transform.position;
                Vector3 entityPosition = enemy.transform.position;
                entityPosition.y = bounds.center.y;
                enemy.transform.position = entityPosition;
                transform.position = artPosition;
                bounds = WorldBounds();
            }
            OwnerLocalBounds = new Bounds(enemy.transform.InverseTransformPoint(bounds.center), bounds.size);
        }

        public void SetHitFlash(bool value)
        {
            if (flashing == value) return;
            flashing = value;
            foreach (var renderer in renderers) renderer.color = value ? new Color(1, .45f, .45f) : Color.white;
        }

        void Update()
        {
            if (owner == null || !owner.Alive || !BattleGame.Instance.Running) return;
            if (groundVehicle && owner.Velocity.sqrMagnitude > .0001f)
                foreach (Transform wheel in wheels)
                {
                    float radius = wheel.GetComponent<SpriteRenderer>().sprite.bounds.size.y * Mathf.Abs(wheel.lossyScale.y) * .5f;
                    wheel.Rotate(0, 0, -owner.Velocity.magnitude / Mathf.Max(.01f, radius) * Mathf.Rad2Deg * Time.deltaTime);
                }
            if (propeller != null && propellerFrames != null && propellerFrames.Length > 0)
            {
                propellerTime += Time.deltaTime;
                propeller.sprite = propellerFrames[Mathf.FloorToInt(propellerTime / .04f) % propellerFrames.Length];
            }
        }
    }
}
