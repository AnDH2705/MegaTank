using UnityEngine;

namespace MegaTanks
{
    public sealed class TankHitbox : MonoBehaviour
    {
        BoxCollider2D hull;
        public Vector2 AimPoint => transform.TransformPoint(hull.offset);

        public void Configure(Vector2 size)
        {
            hull = gameObject.AddComponent<BoxCollider2D>();
            hull.size = size;
            hull.isTrigger = true;
        }
    }
}
