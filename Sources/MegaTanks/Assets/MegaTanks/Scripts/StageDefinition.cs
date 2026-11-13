using UnityEngine;

namespace MegaTanks
{
    public sealed class StageDefinition : ScriptableObject
    {
        public string stageName = "Stage 1";
        [Min(1)] public int stageNumber = 1;
        [Tooltip("Battle difficulty = this value + the current round number (starting at 1).")]
        public int stageDifficulty;
        public static int DefaultDifficulty(int number) => (number - 1) * 10;
        public void InitializeStage(int number)
        {
            stageNumber=Mathf.Max(1,number);stageName="Stage "+stageNumber;
            stageDifficulty=DefaultDifficulty(stageNumber);
        }
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
