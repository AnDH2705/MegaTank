using UnityEngine;

namespace MegaTanks
{
    public sealed class TankHitbox : MonoBehaviour
    {
        BoxCollider2D hull;
        public Vector2 AimPoint => transform.TransformPoint(hull.offset);

        internal Vector3 AnchorPoint(Vector2 normalizedPosition)
        {
            Vector2 localPoint = hull.offset + Vector2.Scale(hull.size, normalizedPosition - Vector2.one * .5f);
            return transform.TransformPoint(localPoint);
        }

        public void Configure(Vector2 size)
        {
            hull = gameObject.AddComponent<BoxCollider2D>();
            hull.size = size;
            hull.isTrigger = true;
        }
    }
}
