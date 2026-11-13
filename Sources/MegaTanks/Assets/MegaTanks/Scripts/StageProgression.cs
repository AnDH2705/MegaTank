using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MegaTanks
{
    public enum StageMode { Inactive, Combat, WaveBreak, Reward, Map, Reveal, Service, Victory }
    public sealed class StageProgression : MonoBehaviour
    {
        StageDefinition definition;
        CardCatalog catalog;
        BattleGame game;

        float waveBreak;
        public StageDefinition Definition => definition;
        public StageRoute Route { get; private set; }
        public StageNode Current { get; private set; }
        public int CurrentRoundNumber => (Current?.round ?? 0) + 1;
        public int Difficulty => definition.stageDifficulty + CurrentRoundNumber;
        public StageMode Mode { get; private set; }
        public int EncounterWave { get; private set; }
        public int EncounterWaves => Current != null && Current.EncounterType == StageNodeType.HardBattle ? 3 : 1;
        public CardDefinition FreeServiceCard { get; private set; }
        public CardDefinition PaidServiceCard { get; private set; }
        public bool FreeClaimed { get; private set; }
        public bool PaidPurchased { get; private set; }
        public bool HasOverlay => Mode == StageMode.Map || Mode == StageMode.Reveal || Mode == StageMode.Service || Mode == StageMode.Victory || Mode == StageMode.WaveBreak;
        public void Configure(BattleGame battle, StageDefinition data, CardCatalog cards)
        {
            game = battle; definition = data; catalog = cards;

        }
        public void EndRun() { Mode = StageMode.Inactive; Current = null; }
        public void BeginRun()
        {
            int seed = definition.randomizeSeed ? UnityEngine.Random.Range(0, int.MaxValue) : definition.seed;
            Route = StageRoute.Generate(definition, seed);
            Current = null; FreeServiceCard = PaidServiceCard = null;
            Enter(Route.rounds[0][0]);
        }
        public bool CanSelect(int id) => Mode == StageMode.Map && Current != null && Current.completed && Current.next.Contains(id);
        public bool SelectNode(int id)
        {
            if (!CanSelect(id)) return false;
            Enter(Route.Get(id)); return true;
        }
        void Enter(StageNode node)
        {
            Current = node; node.entered = true;
            FreeClaimed = PaidPurchased = false; FreeServiceCard = PaidServiceCard = null;
            game.ClearEncounterTransient();
            if (node.type == StageNodeType.Random)
            {
                // Boss nodes remain exclusive to the final round; random nodes cannot reveal another random node.
                node.revealedType = (StageNodeType)UnityEngine.Random.Range(0, 4);
                Mode = StageMode.Reveal;
            }
            else BeginEncounter();
        }
        public bool ContinueReveal()
        {
            if (Mode != StageMode.Reveal) return false;
            BeginEncounter(); return true;
        }
        void BeginEncounter()
        {
            if (Current.EncounterType == StageNodeType.Supply || Current.EncounterType == StageNodeType.Repair)
            {
                CardEffect effect = Current.EncounterType == StageNodeType.Supply ? CardEffect.Resupply : CardEffect.Repair;
                FreeServiceCard = catalog.cards.First(card => card.effect == effect && card.rarity == CardRarity.Normal);
                PaidServiceCard = RollServiceCard(effect, FreeServiceCard.id);
                Mode = StageMode.Service;
                return;
            }
            EncounterWave = 1; Mode = StageMode.Combat;
            game.BeginStageWave();
        }
        CardDefinition RollServiceCard(CardEffect effect, string excludedId)
        {
            var pools = new List<CardDefinition>[4];
            float[] weights = { .5f, .3f, .15f, .05f };
            float total = 0;
            for (int i = 0; i < pools.Length; i++)
            {
                pools[i] = catalog.cards.Where(card => card.effect == effect && card.id != excludedId && (int)card.rarity == i).ToList();
                if (pools[i].Count > 0) total += weights[i];
            }
            if (total <= 0) throw new InvalidOperationException("A service node requires a distinct paid card for " + effect);
            float roll = UnityEngine.Random.value * total;
            int rarity = 0;
            for (int i = 0; i < pools.Length; i++)
            {
                if (pools[i].Count == 0) continue;
                rarity = i; roll -= weights[i]; if (roll < 0) break;
            }
            return pools[rarity][UnityEngine.Random.Range(0, pools[rarity].Count)];
        }
        public void OnWaveCleared()
        {
            if (Mode != StageMode.Combat) return;
            game.ClearEncounterTransient();
            if (EncounterWave < EncounterWaves)
            {
                waveBreak = Mathf.Max(.1f, definition.waveBreakSeconds); Mode = StageMode.WaveBreak;
            }
            else if (Current.EncounterType == StageNodeType.Boss)
            {
                Current.completed = true; Mode = StageMode.Victory;
            }
            else
            {
                Mode = StageMode.Reward;
                game.Rewards.OpenBattleReward(game.Wave);
            }
        }
        public void OnRewardChosen()
        {
            if (Mode != StageMode.Reward) return;
            Current.completed = true; Mode = StageMode.Map;
            game.ClearEncounterTransient();
        }
        public bool ClaimFreeService()
        {
            if (Mode != StageMode.Service || FreeClaimed) return false;
            game.ApplyCardEffect(FreeServiceCard); FreeClaimed = true; return true;
        }
        public bool BuyService()
        {
            if (Mode != StageMode.Service || PaidPurchased || !game.Rewards.TrySpendScraps(PaidServiceCard.price)) return false;
            game.ApplyCardEffect(PaidServiceCard); PaidPurchased = true; return true;
        }
        public bool LeaveService()
        {
            if (Mode != StageMode.Service) return false;
            Current.completed = true; Mode = StageMode.Map; return true;
        }
        void Update()
        {
            if (Mode != StageMode.WaveBreak || game.Paused || game.Hull <= 0) return;
            waveBreak -= Time.deltaTime;
            if (waveBreak > 0) return;
            EncounterWave++; Mode = StageMode.Combat; game.BeginStageWave();
        }

    }
}
