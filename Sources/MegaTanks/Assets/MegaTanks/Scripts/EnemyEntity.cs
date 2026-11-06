using UnityEngine;

namespace MegaTanks
{
    public enum EnemyKind { Ground, Flyer }

    public sealed class EnemyEntity : MonoBehaviour
    {
        public EnemyKind Kind { get; private set; }
        public int WaveNumber { get; private set; }
        public EnemyDefinition Data { get; private set; }
        public EnemyWeapon Weapon { get; private set; }
        public EnemyVehicleArtwork Artwork { get; private set; }
        public int ScrapsReward => Data.baseScraps + Mathf.Max(0, WaveNumber - 1) * Data.scrapsPerWave;
        public float Health { get; private set; }
        public float Speed { get; private set; }
        public bool Alive => Health > 0;
        public bool InWeaponRange => Vector2.Distance(transform.position, BattleGame.Instance.TankAimPoint) <= Data.weapon.range + .001f;
        public Vector2 Velocity => Alive && BattleGame.Instance.Running && transform.position.x > StoppingX + .0001f ? Vector2.left * Speed : Vector2.zero;
        public float AdvanceInsideRange { get; private set; }
        public float StoppingX { get; private set; }
        float maxHealth;
        SpriteRenderer body;
        Transform healthFill;
        Color color;
        float hitUntil;
        float healthBarWidth = .9f;

        public void Initialize(EnemyKind kind, int wave)
        {
            Initialize(BattleGame.Instance.EnemyDatabase.Get(kind), wave);
        }

        public void Initialize(EnemyDefinition definition, int wave)
        {
            Data = definition;
            Kind = definition.kind;
            WaveNumber = wave;
            gameObject.name = definition.id;
            maxHealth = definition.baseHealth + wave * definition.healthPerWave;
            Health = maxHealth;
            Speed = definition.baseSpeed + Mathf.Min(wave * definition.speedPerWave, definition.maxSpeedBonus);
            color = definition.color;
            float healthY = .67f;
            if (definition.artworkPrefab != null)
            {
                Artwork = Instantiate(definition.artworkPrefab, transform);
                Artwork.Attach(this, definition.ScaledArtworkWidth);
                var bounds = Artwork.OwnerLocalBounds;
                var collider = gameObject.AddComponent<BoxCollider2D>();
                collider.offset = bounds.center; collider.size = bounds.size; collider.isTrigger = true;
                healthY = bounds.max.y + .16f;
                healthBarWidth = Mathf.Clamp(bounds.size.x * .65f, .9f, 1.8f);
            }
            else
            {
                body = ShapeFactory.Create(Kind.ToString(), transform, Vector2.zero, Vector2.one * .8f, color, Kind == EnemyKind.Ground, 10);
                if (Kind == EnemyKind.Flyer) body.transform.localRotation = Quaternion.Euler(0, 0, 45);
                ShapeFactory.Create("Core", transform, Vector2.zero, Vector2.one * .22f, new Color(.15f, .1f, .18f), true, 11);
                var collider = gameObject.AddComponent<CircleCollider2D>(); collider.radius = .46f; collider.isTrigger = true;
            }
            float minimumAdvance = Mathf.Max(0, Mathf.Min(definition.advanceInsideRange.x, definition.advanceInsideRange.y));
            float maximumAdvance = Mathf.Max(minimumAdvance, Mathf.Max(definition.advanceInsideRange.x, definition.advanceInsideRange.y));
            AdvanceInsideRange = Random.Range(minimumAdvance, maximumAdvance);
            Vector2 target = BattleGame.Instance.TankAimPoint;
            float height = transform.position.y - target.y;
            float rangeEdgeX = target.x + Mathf.Sqrt(Mathf.Max(0, definition.weapon.range * definition.weapon.range - height * height));
            // Choose once per enemy so the destination stays stable while the tank idles.
            StoppingX = Mathf.Max(target.x, rangeEdgeX - AdvanceInsideRange);
            ShapeFactory.Create("Health background", transform, new Vector2(0, healthY), new Vector2(healthBarWidth, .07f), new Color(.12f, .17f, .23f), false, 20);
            var anchor = new GameObject("Health anchor").transform;
            anchor.SetParent(transform, false);
            anchor.localPosition = new Vector3(-healthBarWidth * .5f, healthY, 0);
            healthFill = ShapeFactory.Create("Health", anchor, new Vector2(healthBarWidth * .5f, 0), new Vector2(healthBarWidth, .07f), color, false, 21).transform;
            Weapon = gameObject.AddComponent<EnemyWeapon>();
            Weapon.Configure(this, definition.weapon);
        }

        void Update()
        {
            if (!BattleGame.Instance.Running || !Alive) return;
            if (transform.position.x > StoppingX)
            {
                Vector3 position = transform.position;
                position.x = Mathf.Max(StoppingX, position.x - Speed * Time.deltaTime);
                transform.position = position;
            }
            if (Artwork != null) Artwork.SetHitFlash(Time.time < hitUntil);
            else body.color = Time.time < hitUntil ? Color.white : color;
        }

        public void Damage(float damage)
        {
            if (!Alive || damage <= 0) return;
            Vector2 numberPosition = transform.position;
            numberPosition.y += Artwork != null ? Artwork.OwnerLocalBounds.max.y + .3f : .8f;
            BattleGame.Instance.ShowDamageNumber(numberPosition, Mathf.Min(Health, damage));
            Health = Mathf.Max(0, Health - damage);
            hitUntil = Time.time + .07f;
            healthFill.localScale = new Vector3(healthBarWidth * Health / maxHealth, .07f, 1);
            healthFill.localPosition = new Vector3(healthBarWidth * .5f * Health / maxHealth, 0, 0);
            if (Alive) return;
            GetComponent<Collider2D>().enabled = false;
            BattleGame.Instance.Flash(transform.position, color, .7f);
            BattleGame.Instance.RemoveEnemy(this, true);
            Destroy(gameObject);
        }
    }
}
