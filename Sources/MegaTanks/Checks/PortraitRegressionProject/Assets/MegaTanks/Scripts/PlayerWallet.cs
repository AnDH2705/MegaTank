using System;
using UnityEngine;

namespace MegaTanks
{
    public enum CurrencyType { Parts, PetFood, Gems }

    // Permanent progression balances, separate from the scraps earned during a run.
    public sealed class PlayerWallet
    {
        public const string DefaultSaveKey = "MegaTanks.Wallet";
        [Serializable] sealed class SaveData { public int parts, petFood, gems; }
        readonly string saveKey;
        SaveData data = new SaveData();
        public event Action Changed;
        public int Parts => data.parts;
        public int PetFood => data.petFood;
        public int Gems => data.gems;

        public PlayerWallet(string preferenceKey = DefaultSaveKey)
        {
            if (string.IsNullOrWhiteSpace(preferenceKey)) throw new ArgumentException("A save key is required.", nameof(preferenceKey));
            saveKey = preferenceKey;
            string json = PlayerPrefs.GetString(saveKey, "");
            if (string.IsNullOrEmpty(json)) return;
            try { data = JsonUtility.FromJson<SaveData>(json) ?? new SaveData(); }
            catch (ArgumentException) { data = new SaveData(); }
            data.parts = Math.Max(0, data.parts);
            data.petFood = Math.Max(0, data.petFood);
            data.gems = Math.Max(0, data.gems);
        }

        public int GetBalance(CurrencyType currency)
        {
            switch (currency)
            {
                case CurrencyType.Parts: return Parts;
                case CurrencyType.PetFood: return PetFood;
                case CurrencyType.Gems: return Gems;
                default: throw new ArgumentOutOfRangeException(nameof(currency));
            }
        }
        public bool CanAfford(CurrencyType currency, int amount)
        {
            int balance = GetBalance(currency);
            return amount >= 0 && balance >= amount;
        }
        public bool TryAdd(CurrencyType currency, int amount)
        {
            int balance = GetBalance(currency);
            if (amount <= 0 || amount > int.MaxValue - balance) return false;
            SetBalance(currency, balance + amount);
            return true;
        }
        public bool TrySpend(CurrencyType currency, int amount)
        {
            if (amount <= 0 || !CanAfford(currency, amount)) return false;
            SetBalance(currency, GetBalance(currency) - amount);
            return true;
        }
        internal bool TryCreditRewards(CurrencyRewards rewards, Action commit)
        {
            if (!rewards.Valid || rewards.parts > int.MaxValue - Parts || rewards.petFood > int.MaxValue - PetFood || rewards.gems > int.MaxValue - Gems) return false;
            data.parts += rewards.parts; data.petFood += rewards.petFood; data.gems += rewards.gems;
            commit();
            PlayerPrefs.SetString(saveKey, JsonUtility.ToJson(data)); PlayerPrefs.Save(); Changed?.Invoke(); return true;
        }
        internal bool TrySpendWithCommit(CurrencyType currency, int amount, Action commit)
        {
            if (amount <= 0 || !CanAfford(currency, amount)) return false;
            commit();
            SetBalance(currency, GetBalance(currency) - amount);
            return true;
        }
        void SetBalance(CurrencyType currency, int balance)
        {
            switch (currency)
            {
                case CurrencyType.Parts: data.parts = balance; break;
                case CurrencyType.PetFood: data.petFood = balance; break;
                case CurrencyType.Gems: data.gems = balance; break;
                default: throw new ArgumentOutOfRangeException(nameof(currency));
            }
            PlayerPrefs.SetString(saveKey, JsonUtility.ToJson(data));
            PlayerPrefs.Save();
            Changed?.Invoke();
        }
    }
}
