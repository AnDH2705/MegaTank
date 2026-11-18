using System;
using System.Collections.Generic;
using System.Linq;

namespace MegaTanks
{
    public enum StageNodeType { Battle, HardBattle, Supply, Repair, Random, Boss }
    [Serializable]
    public sealed class StageNode
    {
        public int id, round, lane;
        public float height;
        public StageNodeType type;
        public bool entered, completed;
        public StageNodeType? revealedType;
        public readonly List<int> next = new List<int>();
        public StageNodeType EncounterType => revealedType ?? type;
    }
    public sealed class StageRoute
    {
        public readonly List<List<StageNode>> rounds = new List<List<StageNode>>();
        public int Seed { get; private set; }
        public StageNode Get(int id) => rounds.SelectMany(round => round).First(node => node.id == id);
        public static StageRoute Generate(StageDefinition definition, int seed)
        {
            var random = new Random(seed);
            var route = new StageRoute { Seed = seed };
            int count = Math.Max(2, definition.rounds), id = 0;
            int[] weights = { definition.battleWeight, definition.hardBattleWeight, definition.supplyWeight, definition.repairWeight, definition.randomWeight };
            for (int round = 0; round < count; round++)
            {
                int nodes = round == 0 || round == count - 1 ? 1 : random.Next(2, 5);
                var column = new List<StageNode>();
                for (int lane = 0; lane < nodes; lane++)
                    column.Add(new StageNode { id = id++, round = round, lane = lane,
                        height = nodes == 1 ? .5f : .12f + lane * .76f / (nodes - 1) + (float)(random.NextDouble() - .5) * .04f,
                        type = round == 0 ? StageNodeType.Battle : round == count - 1 ? StageNodeType.Boss : WeightedType(random, weights) });
                route.rounds.Add(column);
            }
            for (int round = 0; round < count - 1; round++)
            {
                var from = route.rounds[round]; var to = route.rounds[round + 1];
                // Sorted nearest-neighbor links preserve order between adjacent columns.
                foreach (var source in from)
                    source.next.Add(to.OrderBy(node => Math.Abs(node.height - source.height)).First().id);
                // Give every next-round node an incoming link so none are decorative dead ends.
                foreach (var destination in to)
                {
                    if (from.Any(source => source.next.Contains(destination.id))) continue;
                    var source = from.OrderBy(node => Math.Abs(node.height - destination.height))
                        .First(node => CanConnect(node, destination, from, to));
                    source.next.Add(destination.id);
                }
                foreach (var source in from)
                    foreach (var destination in to)
                        if (!source.next.Contains(destination.id) && random.NextDouble() < .2 && CanConnect(source, destination, from, to))
                            source.next.Add(destination.id);
            }
            return route;
        }
        static StageNodeType WeightedType(Random random, int[] weights)
        {
            int sum = weights.Sum(weight => Math.Max(0, weight));
            if (sum == 0) return StageNodeType.Battle;
            int roll = random.Next(sum);
            for (int i = 0; i < weights.Length; i++) { roll -= Math.Max(0, weights[i]); if (roll < 0) return (StageNodeType)i; }
            return StageNodeType.Battle;
        }
        static bool CanConnect(StageNode source, StageNode destination, List<StageNode> from, List<StageNode> to)
        {
            foreach (var other in from)
                foreach (int id in other.next)
                {
                    var end = to.First(node => node.id == id);
                    if ((source.lane - other.lane) * (destination.lane - end.lane) < 0) return false;
                }
            return true;
        }
    }
}
