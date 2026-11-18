using UnityEngine;

namespace MegaTanks
{
    public sealed class StageDefinition : ScriptableObject
    {
        public string stageName = "Stage 1";
        [Min(1)] public int stageNumber = 1;
        [Tooltip("Regular enemy difficulty = this value + the current round number (starting at 1). Boss power uses only its template and stage-configured levels/stars.")]
        public int stageDifficulty;
        [Header("Boss encounter (independent of stage difficulty)")]
        public BossEncounterSettings boss = new BossEncounterSettings();
        public static int DefaultDifficulty(int number) => (number - 1) * 10;
        public void InitializeStage(int number)
        {
            stageNumber=Mathf.Max(1,number);stageName="Stage "+stageNumber;
            stageDifficulty=DefaultDifficulty(stageNumber);
        }
        [Header("First completion rewards")]
        [Min(0)] public int firstClearGems = 100;
        [Min(0)] public int firstClearPartsBase = 500;
        [Min(0)] public int firstClearPetFoodBase = 500;
        [Min(0)] public int firstClearResourcesPerStage = 50;
        public CurrencyRewards RepeatCompletionRewards => new CurrencyRewards(RewardValue(100,10),RewardValue(100,10));
        public CurrencyRewards FirstCompletionRewards => new CurrencyRewards(RewardValue(firstClearPartsBase,firstClearResourcesPerStage),RewardValue(firstClearPetFoodBase,firstClearResourcesPerStage),Mathf.Max(0,firstClearGems));
        int RewardValue(int initial,int step) => checked(Mathf.Max(0,initial) + (Mathf.Max(1,stageNumber)-1)*Mathf.Max(0,step));
        [Header("Stage length")]
        [Min(2), Tooltip("Number of rounds in this stage, including the opening battle and final boss. Set independently for each stage; minimum 2.")]
        public int rounds = 5;
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
