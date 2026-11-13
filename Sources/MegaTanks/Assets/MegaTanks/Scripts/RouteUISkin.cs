using UnityEngine;
namespace MegaTanks
{
    [CreateAssetMenu(menuName="MegaTanks/UI/Route Skin")]
    public sealed class RouteUISkin : ScriptableObject
    {
        public Sprite background,header,stagePlate,backPlate,legend;
        public Sprite nodeNormal,nodeAvailable,nodeCurrent,nodeLocked;
        public Sprite battle,hardBattle,supply,unknown,repair,backArrow,playerTank;
        public Sprite IconFor(StageNodeType type)
        {
            switch(type)
            {
                case StageNodeType.HardBattle:case StageNodeType.Boss:return hardBattle;
                case StageNodeType.Supply:return supply;
                case StageNodeType.Repair:return repair;
                case StageNodeType.Random:return unknown;
                default:return battle;
            }
        }
    }
}
