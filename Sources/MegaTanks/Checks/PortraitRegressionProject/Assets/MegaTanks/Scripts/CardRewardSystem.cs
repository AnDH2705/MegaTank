using System;
using UnityEngine;

namespace MegaTanks
{
    public enum IntermissionMode { None, Reward, Shop }
    public sealed class CardOffer
    {
        public CardDefinition card;
        public bool purchased;
    }

    public sealed class CardRewardSystem : MonoBehaviour
    {
        CardCatalog catalog;
        CardOffer[] offers = new CardOffer[0];
        public CardOffer[] Offers => offers;
        public IntermissionMode Mode { get; private set; }
        public bool IsOpen => Mode != IntermissionMode.None;
        public int Scraps { get; private set; }
        public int CompletedWave { get; private set; }


        public void Configure(CardCatalog data) { catalog = data; catalog.Validate(); }
        public void ResetRun() { Mode = IntermissionMode.None; Scraps = 0; CompletedWave = 0; offers = new CardOffer[0]; }
        public void AddScraps(int value) { Scraps = (int)Math.Min(int.MaxValue, (long)Scraps + Mathf.Max(0, value)); }
        public bool TrySpendScraps(int value)
        {
            if (value < 0 || Scraps < value) return false;
            Scraps -= value; return true;
        }
        public void OpenBattleReward(int wave) { OpenForWave(wave, true); }
        public void OpenForWave(int wave, bool battleReward = false)
        {
            if (IsOpen || wave < 1) return;
            CompletedWave = wave;
            Mode = !battleReward && wave % 5 == 0 ? IntermissionMode.Shop : IntermissionMode.Reward;
            offers = new CardOffer[Mode == IntermissionMode.Shop ? 8 : 3];
            var cards = catalog.RollUniqueCards(offers.Length);
            for (int i = 0; i < offers.Length; i++) offers[i] = new CardOffer { card = cards[i] };
        }

        public bool ChooseReward(int index)
        {
            if (Mode != IntermissionMode.Reward || index < 0 || index >= offers.Length) return false;
            BattleGame.Instance.ApplyCardEffect(offers[index].card);
            Finish();
            return true;
        }

        public bool Buy(int index)
        {
            if (Mode != IntermissionMode.Shop || index < 0 || index >= offers.Length) return false;
            var offer = offers[index];
            if (offer.purchased || Scraps < offer.card.price) return false;
            Scraps -= offer.card.price;
            offer.purchased = true;
            BattleGame.Instance.ApplyCardEffect(offer.card);
            return true;
        }

        public void LeaveShop() { if (Mode == IntermissionMode.Shop) Finish(); }
        void Finish()
        {
            Mode = IntermissionMode.None;
            offers = new CardOffer[0];
            BattleGame.Instance.ContinueAfterRewards();
        }

        public static Color RarityColor(CardRarity rarity)
        {
            switch (rarity)
            {
                case CardRarity.Rare: return new Color(.2f, .6f, 1);
                case CardRarity.Epic: return new Color(.75f, .35f, 1);
                case CardRarity.Legendary: return new Color(1, .58f, .12f);
                default: return new Color(.94f, .96f, 1);
            }
        }

    }
}

