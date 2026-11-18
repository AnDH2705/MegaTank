using System;
using System.Collections.Generic;
using UnityEngine;

namespace MegaTanks
{
    public enum CardRarity { Normal, Rare, Epic, Legendary }
    public enum CardEffect { CannonDamage, MachineGunDamage, MaxHull, Resupply, Repair }

    [Serializable]
    public sealed class CardDefinition
    {
        public string id, title, description;
        public CardRarity rarity;
        public CardEffect effect;
        [Min(0)] public float fraction;
        [Min(0)] public int price;
        public Sprite art;
        public string EffectText => effect == CardEffect.Resupply ? "Restore " + Percent + " of all ammo reserves"
            : effect == CardEffect.Repair ? "Restore " + Percent + " of maximum HP"
            : effect == CardEffect.MaxHull ? "+" + Percent + " maximum tank HP"
            : "+" + Percent + (effect == CardEffect.CannonDamage ? " cannon damage" : " MG damage");
        string Percent => Mathf.RoundToInt(fraction * 100) + "%";
    }

    [CreateAssetMenu(menuName = "MegaTanks/Card Catalog")]
    public sealed class CardCatalog : ScriptableObject
    {
        public const string SourceUrl = "https://docs.google.com/spreadsheets/d/1JoIspgth7KeGzcHgBdwluRhmUyCh7FG83wH2ZbsP9iw/edit?gid=0#gid=0";
        public string sourceUrl = SourceUrl;
        public CardDefinition[] cards;

        public static CardRarity RollRarity(float roll)
        {
            if (roll < .05f) return CardRarity.Legendary;
            if (roll < .20f) return CardRarity.Epic;
            if (roll < .50f) return CardRarity.Rare;
            return CardRarity.Normal;
        }

        public void Validate()
        {
            var ids = new HashSet<string>();
            foreach (var rarity in (CardRarity[])Enum.GetValues(typeof(CardRarity)))
            {
                int count = 0;
                if (cards != null) foreach (var card in cards)
                {
                    if (card == null || string.IsNullOrEmpty(card.id) || card.price < 0 || card.fraction < 0)
                        throw new InvalidOperationException("Invalid card definition.");
                    if (card.rarity == rarity) { count++; if (!ids.Add(card.id)) throw new InvalidOperationException("Duplicate card ID: " + card.id); }
                }
                if (count == 0) throw new InvalidOperationException("Card pool is empty: " + rarity);
            }
        }

        public CardDefinition RollCard()
        {
            var rarity = RollRarity(UnityEngine.Random.value);
            var pool = new List<CardDefinition>();
            foreach (var card in cards) if (card.rarity == rarity) pool.Add(card);
            if (pool.Count == 0) throw new InvalidOperationException("No cards for rarity " + rarity);
            return pool[UnityEngine.Random.Range(0, pool.Count)];
        }

        public CardDefinition[] RollUniqueCards(int count)
        {
            Validate();
            if (count < 0 || count > cards.Length)
                throw new ArgumentOutOfRangeException(nameof(count), "Not enough distinct cards for this offer.");
            var pools = new List<CardDefinition>[4];
            for (int i = 0; i < pools.Length; i++) pools[i] = new List<CardDefinition>();
            foreach (var card in cards) pools[(int)card.rarity].Add(card);
            var result = new CardDefinition[count];
            float[] weights = { .50f, .30f, .15f, .05f };
            for (int slot = 0; slot < count; slot++)
            {
                // Keep rarity-first rolls; redistribute odds only when a rarity has no unused cards left.
                float total = 0;
                for (int i = 0; i < pools.Length; i++) if (pools[i].Count > 0) total += weights[i];
                float roll = UnityEngine.Random.value * total;
                int rarity = -1;
                for (int i = 0; i < pools.Length; i++)
                {
                    if (pools[i].Count == 0) continue;
                    rarity = i;
                    roll -= weights[i];
                    if (roll < 0) break;
                }
                var pool = pools[rarity];
                int index = UnityEngine.Random.Range(0, pool.Count);
                result[slot] = pool[index];
                pool.RemoveAt(index);
            }
            return result;
        }
    }
}
