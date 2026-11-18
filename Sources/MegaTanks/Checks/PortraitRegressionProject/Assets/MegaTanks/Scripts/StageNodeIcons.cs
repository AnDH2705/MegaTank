using UnityEngine;

namespace MegaTanks
{
    // Shared node names and colors; CanvasGameUI renders the uGUI symbols.
    public static class StageNodeIcons
    {

        public static string Name(StageNodeType type)
        {
            switch (type)
            {
                case StageNodeType.HardBattle: return "Hard battle";
                case StageNodeType.Supply: return "Supply";
                case StageNodeType.Repair: return "Repair";
                case StageNodeType.Random: return "Unknown";
                case StageNodeType.Boss: return "Boss";
                default: return "Battle";
            }
        }
        public static Color ColorFor(StageNodeType type)
        {
            switch (type)
            {
                case StageNodeType.HardBattle: return new Color(1, .35f, .25f);
                case StageNodeType.Supply: return new Color(1, .78f, .3f);
                case StageNodeType.Repair: return new Color(.3f, .9f, .65f);
                case StageNodeType.Random: return new Color(.77f, .5f, 1);
                case StageNodeType.Boss: return new Color(1, .28f, .5f);
                default: return new Color(.4f, .78f, 1);
            }
        }
    }
}

