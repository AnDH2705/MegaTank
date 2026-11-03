using UnityEngine;

namespace MegaTanks
{
    public enum EnemyKind { Ground, Flyer }

    public sealed class EnemyEntity : MonoBehaviour
    {
        public EnemyKind Kind { get; private set; }
        public int WaveNumber { get; private set; }
        public float Health { get; private set; }
        public float Speed { get; private set; }
        public bool Alive => Health > 0;
        public Vector2 Velocity => Vector2.left * Speed;
        float maxHealth;
        SpriteRenderer body;
        Transform healthFill;
        Color color;
        float hitUntil;

        public void Initialize(EnemyKind kind, int wave)
        {
            Kind = kind;
            WaveNumber = wave;
            maxHealth = kind == EnemyKind.Flyer ? 26 + wave * 4 : 70 + wave * 10;
            Health = maxHealth;
            Speed = (kind == EnemyKind.Flyer ? .72f : .42f) + Mathf.Min(wave * .035f, .5f);
            color = kind == EnemyKind.Flyer ? new Color(1, .63f, .25f) : new Color(.96f, .37f, .4f);
            body = ShapeFactory.Create(kind.ToString(), transform, Vector2.zero, Vector2.one * .8f, color, kind == EnemyKind.Ground, 10);
            if (kind == EnemyKind.Flyer) body.transform.localRotation = Quaternion.Euler(0, 0, 45);
            ShapeFactory.Create("Core", transform, Vector2.zero, Vector2.one * .22f, new Color(.15f, .1f, .18f), true, 11);
            var collider = gameObject.AddComponent<CircleCollider2D>();
            collider.radius = .46f;
            collider.isTrigger = true;
            ShapeFactory.Create("Health background", transform, new Vector2(0, .67f), new Vector2(.9f, .07f), new Color(.12f, .17f, .23f), false, 12);
            var anchor = new GameObject("Health anchor").transform;
            anchor.SetParent(transform, false);
            anchor.localPosition = new Vector3(-.45f, .67f, 0);
            healthFill = ShapeFactory.Create("Health", anchor, new Vector2(.45f, 0), new Vector2(.9f, .07f), color, false, 13).transform;
        }

        void Update()
        {
            if (!BattleGame.Instance.Running || !Alive) return;
            transform.position += (Vector3)(Velocity * Time.deltaTime);
            body.color = Time.time < hitUntil ? Color.white : color;
            if (transform.position.x < -3.85f)
            {
                Health = 0;
                BattleGame.Instance.DamageHull(Kind == EnemyKind.Flyer ? 8 : 15);
                BattleGame.Instance.RemoveEnemy(this, false);
                Destroy(gameObject);
            }
        }

        public void Damage(float damage)
        {
            if (!Alive) return;
            Health = Mathf.Max(0, Health - damage);
            hitUntil = Time.time + .07f;
            healthFill.localScale = new Vector3(.9f * Health / maxHealth, .07f, 1);
            healthFill.localPosition = new Vector3(.45f * Health / maxHealth, 0, 0);
            if (Alive) return;
            GetComponent<Collider2D>().enabled = false;
            BattleGame.Instance.Flash(transform.position, color, .7f);
            BattleGame.Instance.RemoveEnemy(this, true);
            Destroy(gameObject);
        }
    }
}
