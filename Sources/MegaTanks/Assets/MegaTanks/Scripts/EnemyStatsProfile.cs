using UnityEngine;
namespace MegaTanks
{
    [DisallowMultipleComponent]
    public sealed class EnemyStatsProfile : MonoBehaviour
    {
        [Tooltip("Battle stats are floor(Base + Difficulty × Grow), except movement Speed, which keeps fractions. All Grow values start at zero. Artwork and identity do not grow.")]
        public EnemyDefinition stats = new EnemyDefinition();
    }
}
