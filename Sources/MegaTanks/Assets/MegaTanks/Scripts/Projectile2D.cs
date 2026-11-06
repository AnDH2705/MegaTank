using UnityEngine;
using System.Collections.Generic;

namespace MegaTanks
{
    public enum ProjectileTeam { Player, Enemy }

    public sealed class Projectile2D : MonoBehaviour
    {
        Vector2 velocity, launchOrigin;
        float damage, radius, maxRange, gravity;
        public Vector2 Velocity => velocity;
        Color color;
        LineRenderer trail;
        Vector3[] trailPoints;
        int trailCount;
        float trailTimer;
        Transform shellVisual;
        ProjectileTeam team;
        static readonly List<RaycastHit2D> hits = new List<RaycastHit2D>(32);
        public ProjectileTeam Team => team;
        public bool IsMortar { get; private set; }
        bool detonated;
        float fullDamageRadius, halfDamageRadius;
        public void InitializeMortar(Vector2 origin, Vector2 launchVelocity, float hitDamage, float downwardGravity,
            float fullRadius, float halfRadius, float width, Sprite projectileSprite = null)
        {
            Initialize(origin, launchVelocity.normalized, launchVelocity.magnitude, hitDamage, new Color(1, .8f, .3f), true,
                float.PositiveInfinity, downwardGravity, ProjectileTeam.Player, projectileSprite, width);
            IsMortar = true; fullDamageRadius = fullRadius; halfDamageRadius = halfRadius;
            var renderer = shellVisual.GetComponent<SpriteRenderer>();
            shellVisual.localScale = Vector3.one * (width / renderer.sprite.bounds.size.x);
        }
        void Detonate(Vector2 position)
        {
            if (detonated) return;
            detonated = true;
            BattleGame.Instance.DetonateMortar(position, damage, fullDamageRadius, halfDamageRadius);
            Destroy(gameObject);
        }
        public void Initialize(Vector2 origin, Vector2 forward, float speed, float hitDamage, Color tint, bool heavy, float maxDistance, float downwardGravity = 0,
            ProjectileTeam faction = ProjectileTeam.Player, Sprite projectileSprite = null, float projectileWidth = -1, bool inheritCannonArt = true)
        {
            transform.position = origin;
            team = faction;
            launchOrigin = origin;
            velocity = forward.normalized * speed;
            maxRange = maxDistance;
            gravity = Mathf.Max(0, downwardGravity);
            damage = hitDamage;
            color = tint;
            radius = heavy ? .14f : .055f;
            SpriteRenderer visual;
            var game = BattleGame.Instance;
            Sprite sprite = projectileSprite != null ? projectileSprite : heavy && inheritCannonArt ? game.CannonProjectileSprite : null;
            if (sprite != null)
            {
                var shell = new GameObject("Cannon shell art");
                shell.transform.SetParent(transform, false);
                visual = shell.AddComponent<SpriteRenderer>();
                visual.sprite = sprite;
                visual.color = Color.white;
                visual.sortingOrder = 70;
                float scale = (projectileWidth > 0 ? projectileWidth : game.CannonProjectileWidth) / visual.sprite.bounds.size.x;
                shell.transform.localScale = Vector3.one * scale;
            }
            else visual = ShapeFactory.Create("Shell", transform, Vector2.zero, Vector2.one * radius * 2, color, true, 70);
            shellVisual = visual.transform;
            AlignVisual();
            trail = gameObject.AddComponent<LineRenderer>();
            trail.sharedMaterial = visual.sharedMaterial;
            trail.useWorldSpace = true;
            trail.sortingOrder = 69;
            trail.startWidth = heavy ? .055f : .022f;
            trail.endWidth = 0;
            trail.startColor = color;
            trail.endColor = new Color(color.r, color.g, color.b, 0);
            trailPoints = new Vector3[heavy ? 24 : 7];
            trailPoints[0] = origin;
            trailCount = 1;
            trail.positionCount = 1;
            trail.SetPosition(0, origin);
        }

        void AlignVisual()
        {
            if (velocity.sqrMagnitude > .000001f)
                shellVisual.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg);
        }

        void Update()
        {
            if (!BattleGame.Instance.Running) return;
            // Contact is at the shell's lower edge, rather than waiting for its center to sink below ground.
            if (transform.position.y - radius <= BattleGame.GroundHeight)
            {
                if (IsMortar) Detonate(new Vector2(transform.position.x, BattleGame.GroundHeight));
                else Destroy(gameObject);
                return;
            }
            // Small exact ballistic steps keep curved sweeps accurate even on slow frames.
            Physics2D.SyncTransforms();
            float remainingTime = Time.deltaTime;
            while (remainingTime > 0)
            {
                float step = Mathf.Min(remainingTime, 1f / 120);
                remainingTime -= step;
                Vector2 displacement = velocity * step + Vector2.down * (.5f * gravity * step * step);
                velocity += Vector2.down * (gravity * step);
                Vector2 offset = (Vector2)transform.position - launchOrigin;
                bool expired = (offset + displacement).sqrMagnitude >= maxRange * maxRange;
                if (expired && displacement.sqrMagnitude > 0)
                {
                    // Clip to a radius around the launch point, not the longer curved path length.
                    float b = Vector2.Dot(offset, displacement);
                    float c = offset.sqrMagnitude - maxRange * maxRange;
                    float fraction = (-b + Mathf.Sqrt(Mathf.Max(0, b * b - displacement.sqrMagnitude * c))) / displacement.sqrMagnitude;
                    displacement *= Mathf.Clamp01(fraction);
                }
                bool hitGround = displacement.y < 0 && transform.position.y + displacement.y - radius <= BattleGame.GroundHeight;
                if (hitGround)
                {
                    // Clip the sweep at first ground contact so fast rounds cannot tunnel below it
                    // or damage an enemy beyond the ground impact point.
                    float fraction = (transform.position.y - radius - BattleGame.GroundHeight) / -displacement.y;
                    displacement *= Mathf.Clamp01(fraction);
                }
                var filter = new ContactFilter2D().NoFilter();
                Physics2D.CircleCast(transform.position, radius, displacement.normalized, filter, hits, displacement.magnitude);
                float closest = float.PositiveInfinity;
                EnemyEntity targetEnemy = null;
                TankHitbox targetHull = null;
                Vector2 impact = Vector2.zero;
                foreach (var hit in hits)
                {
                    var enemy = hit.collider.GetComponent<EnemyEntity>();
                    var hull = hit.collider.GetComponent<TankHitbox>();
                    bool valid = team == ProjectileTeam.Player ? enemy != null && enemy.Alive : hull != null;
                    if (valid && hit.distance < closest)
                    {
                        closest = hit.distance;
                        targetEnemy = enemy;
                        targetHull = hull;
                        impact = hit.point;
                    }
                }
                if (targetEnemy != null || targetHull != null)
                {
                    if (IsMortar) { Detonate(impact); return; }
                    if (team == ProjectileTeam.Player) targetEnemy.Damage(damage);
                    else BattleGame.Instance.DamageHull(damage);
                    BattleGame.Instance.Flash(impact, color, .22f);
                    Destroy(gameObject);
                    return;
                }
                transform.position += (Vector3)displacement;
                if (hitGround && IsMortar) { Detonate(new Vector2(transform.position.x, BattleGame.GroundHeight)); return; }
                if (expired || hitGround) { Destroy(gameObject); return; }
            }
            AlignVisual();
            trailTimer -= Time.deltaTime;
            if (trailTimer <= 0)
            {
                trailTimer = .02f;
                trailCount = Mathf.Min(trailCount + 1, trailPoints.Length);
                for (int i = trailCount - 1; i > 0; i--) trailPoints[i] = trailPoints[i - 1];
                trailPoints[0] = transform.position;
                trail.positionCount = trailCount;
                for (int i = 0; i < trailCount; i++) trail.SetPosition(i, trailPoints[i]);
            }
        }
    }
}
