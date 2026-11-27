using UnityEngine;
using System.Collections.Generic;

namespace MegaTanks
{
    public enum ProjectileTeam { Player, Enemy }

    public sealed class Projectile2D : MonoBehaviour
    {
        Vector2 velocity, launchOrigin;
        float radius, maxRange, gravity;
        public DamageAttack Attack { get; private set; }
        public Vector2 Velocity => velocity;
        Color color;
        LineRenderer trail;
        Vector3[] trailPoints;
        int trailCount;
        float trailTimer;
        Transform shellVisual;
        BattleVfxEffect projectileEffect;
        float age;
        const float SafetyLifetime = 30f;
        bool released;
        void Release()
        {
            if(released)return;
            released=true;
            if(projectileEffect!=null)projectileEffect.Detach(transform.position);
            projectileEffect=null;
            Destroy(gameObject);
        }
        void OnDisable()
        {
            if(projectileEffect!=null)projectileEffect.Detach(transform.position);
            projectileEffect=null;
        }
        ProjectileTeam team;
        static readonly List<RaycastHit2D> hits = new List<RaycastHit2D>(32);
        public ProjectileTeam Team => team;
        public bool IsMortar { get; private set; }
        public bool IsMissile { get; private set; }
        public EnemyEntity LockedTarget { get; private set; }
        EnemyKind missileTargetKind;
        float turnRate, missileLifetime, lockRange;

        public void InitializeMissile(Vector2 origin, Vector2 forward, GunStats stats, EnemyEntity target)
        {
            IsMissile = true;
            missileTargetKind = stats.preferredTarget;
            turnRate = Mathf.Max(0, stats.missileTurnRate);
            missileLifetime = Mathf.Max(.1f, stats.missileLifetime);
            lockRange = stats.range;
            LockedTarget = target != null && target.Kind == missileTargetKind ? target : null;
            Initialize(origin, forward, stats.projectileSpeed, stats.damage, new Color(.8f,.8f,.75f), true,
                float.PositiveInfinity, 0, ProjectileTeam.Player, stats.projectileSprite, stats.projectileWidth, false, stats);
            shellVisual.localScale = Vector3.Scale(shellVisual.localScale, new Vector3(.7f, 1, 1));
            var motor = new GameObject("Missile exhaust socket").transform;
            motor.SetParent(shellVisual, false);
            motor.localPosition = Vector3.left * shellVisual.GetComponent<SpriteRenderer>().sprite.bounds.extents.x;
            projectileEffect = BattleGame.Instance.Vfx.Play(BattleVfxKind.Smoke, motor.position, .6f,
                -forward, motor, missileLifetime);
        }

        public static Vector2 Steer(Vector2 currentVelocity, Vector2 desiredDirection, float degreesPerSecond, float delta)
        {
            if (desiredDirection.sqrMagnitude < .000001f) return currentVelocity;
            float angle = Mathf.Atan2(currentVelocity.y,currentVelocity.x)*Mathf.Rad2Deg;
            float desired = Mathf.Atan2(desiredDirection.y,desiredDirection.x)*Mathf.Rad2Deg;
            angle = Mathf.MoveTowardsAngle(angle,desired,Mathf.Max(0,degreesPerSecond)*delta)*Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*currentVelocity.magnitude;
        }
        bool detonated;
        bool heavyProjectile;
        float fullDamageRadius, halfDamageRadius;
        public void InitializeMortar(Vector2 origin, Vector2 launchVelocity, float hitDamage, float downwardGravity,
            float fullRadius, float halfRadius, float width, Sprite projectileSprite = null, WeaponCombatStats combatStats = null, ProjectileTeam faction = ProjectileTeam.Player)
        {
            Initialize(origin, launchVelocity.normalized, launchVelocity.magnitude, hitDamage, new Color(1, .8f, .3f), true,
                float.PositiveInfinity, downwardGravity, faction, projectileSprite, width, faction == ProjectileTeam.Player, combatStats);
            IsMortar = true; fullDamageRadius = fullRadius; halfDamageRadius = halfRadius;
            var renderer = shellVisual.GetComponent<SpriteRenderer>();
            shellVisual.localScale = Vector3.one * (width / renderer.sprite.bounds.size.x);
        }
        void Detonate(Vector2 position)
        {
            if (detonated) return;
            detonated = true;
            if (team == ProjectileTeam.Player) BattleGame.Instance.DetonateMortar(position, Attack, fullDamageRadius, halfDamageRadius);
            else
            {
                var game = BattleGame.Instance;
                float distance = Vector2.Distance(position, game.HullHitbox.GetComponent<Collider2D>().ClosestPoint(position));
                if (distance <= halfDamageRadius) game.ReceiveAttack(Attack, distance <= fullDamageRadius ? 1 : .5f);
                foreach (var unit in game.AlliedCrew)
                    unit.ReceiveBlast(position, Attack, fullDamageRadius, halfDamageRadius);
                if(game.Vfx.Catalog!=null)game.Vfx.Blast(position,true,position.y<=BattleGame.GroundHeight+.3f);
                else game.Flash(position, color, Mathf.Max(.3f, halfDamageRadius));
            }
            Release();
        }
        public void Initialize(Vector2 origin, Vector2 forward, float speed, float hitDamage, Color tint, bool heavy, float maxDistance, float downwardGravity = 0,
            ProjectileTeam faction = ProjectileTeam.Player, Sprite projectileSprite = null, float projectileWidth = -1, bool inheritCannonArt = true, WeaponCombatStats combatStats = null)
        {
            transform.position = origin;
            team = faction;
            heavyProjectile=heavy;
            launchOrigin = origin;
            velocity = forward.normalized * speed;
            maxRange = maxDistance;
            gravity = Mathf.Max(0, downwardGravity);
            Attack = new DamageAttack(hitDamage,combatStats??new WeaponCombatStats());
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
            if(!IsMissile && game.Vfx.Projectile(shellVisual,heavy,out projectileEffect))
            {
                trail.enabled=false;
                visual.enabled=false;
            }
        }

        void AlignVisual()
        {
            if (velocity.sqrMagnitude > .000001f)
                shellVisual.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg);
        }

        void Update() => Tick(Time.deltaTime);

        void Tick(float delta)
        {
            if (released || !BattleGame.Instance.Running) return;
            age+=delta;
            if(IsMissile && age>=missileLifetime){Release();return;}
            if(age>=SafetyLifetime){Release();return;}
            // Contact is at the shell's lower edge, rather than waiting for its center to sink below ground.
            if (transform.position.y - radius <= BattleGame.GroundHeight)
            {
                if (IsMortar) Detonate(new Vector2(transform.position.x, BattleGame.GroundHeight));
                else {BattleGame.Instance.Vfx.Impact(new Vector2(transform.position.x,BattleGame.GroundHeight),heavyProjectile);Release();}
                return;
            }
            // Small exact ballistic steps keep curved sweeps accurate even on slow frames.
            if(!IsMortar)Physics2D.SyncTransforms();
            float remainingTime = delta;
            while (remainingTime > 0)
            {
                float step = Mathf.Min(remainingTime, 1f / 120);
                remainingTime -= step;
                if (IsMissile)
                {
                    if (LockedTarget == null || !LockedTarget.Alive || !LockedTarget.gameObject.activeInHierarchy || LockedTarget.Kind != missileTargetKind)
                        LockedTarget = BattleGame.Instance.FindTarget(transform.position, missileTargetKind, lockRange, true);
                    if (LockedTarget != null)
                        velocity = Steer(velocity, (Vector2)LockedTarget.transform.position-(Vector2)transform.position, turnRate, step);
                }
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
                // Mortar shells pass through every unit; only the ground sweep can detonate them.
                if(!IsMortar)
                {
                    var filter = new ContactFilter2D().NoFilter();
                    Physics2D.CircleCast(transform.position, radius, displacement.normalized, filter, hits, displacement.magnitude);
                    float closest = float.PositiveInfinity;
                    EnemyEntity targetEnemy = null;
                    TankHitbox targetHull = null;
                    CommandoUnit targetCommando = null;
                    Vector2 impact = Vector2.zero;
                    foreach (var hit in hits)
                    {
                        var enemy = hit.collider.GetComponent<EnemyEntity>();
                        var hull = hit.collider.GetComponent<TankHitbox>();
                        var commando=hit.collider.GetComponent<CommandoUnit>();
                        bool valid = team == ProjectileTeam.Player ? enemy != null && enemy.Alive : hull != null||(commando!=null&&commando.Active);
                        if (IsMissile && enemy != null && enemy.Kind != missileTargetKind) valid = false;
                        if (valid && hit.distance < closest)
                        {
                            closest = hit.distance;
                            targetEnemy = enemy;
                            targetHull = hull;
                            targetCommando=commando;
                            impact = hit.point;
                        }
                    }
                    if (targetEnemy != null || targetHull != null || targetCommando!=null)
                    {
                        if (team == ProjectileTeam.Player) targetEnemy.ReceiveAttack(Attack);
                        else if(targetCommando!=null)targetCommando.ReceiveAttack(Attack);
                        else BattleGame.Instance.ReceiveAttack(Attack);
                        if(BattleGame.Instance.Vfx.Catalog!=null)BattleGame.Instance.Vfx.Impact(impact,heavyProjectile);
                        else BattleGame.Instance.Flash(impact, color, .22f);
                        transform.position=impact;
                        Release();
                        return;
                    }
                }
                transform.position += (Vector3)displacement;
                if (hitGround && IsMortar) { Detonate(new Vector2(transform.position.x, BattleGame.GroundHeight)); return; }
                if (expired || hitGround)
                {
                    if(hitGround)BattleGame.Instance.Vfx.Impact(new Vector2(transform.position.x,BattleGame.GroundHeight),heavyProjectile);
                    Release(); return;
                }
            }
            AlignVisual();
            if(!trail.enabled)return;
            trailTimer -= delta;
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
