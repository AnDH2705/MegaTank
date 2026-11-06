using UnityEngine;

namespace MegaTanks
{
    [CreateAssetMenu(menuName = "MegaTanks/Stage Definition")]
    public sealed class StageDefinition : ScriptableObject
    {
        public string stageName = "Stage 1";
        [Min(2)] public int rounds = 10;
        [Header("Intermediate node weights")]
        [Min(0)] public int battleWeight = 45;
        [Min(0)] public int hardBattleWeight = 20;
        [Min(0)] public int supplyWeight = 12;
        [Min(0)] public int repairWeight = 12;
        [Min(0)] public int randomWeight = 11;
        [Min(.1f)] public float waveBreakSeconds = 1.25f;
        public bool randomizeSeed = true;
        public int seed = 10427;
    }
}
