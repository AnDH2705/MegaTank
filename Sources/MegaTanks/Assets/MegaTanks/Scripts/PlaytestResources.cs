using System;
using System.Linq;
using UnityEngine;

namespace MegaTanks
{
    // Temporary play-test allowance. A saved marker prevents refilling spent resources on every launch.
    public static class PlaytestResources
    {
        public const string SaveKey = "MegaTanks.PlaytestResources.UpgradeTesting.v1";
        public const string ArmorSaveKey = "MegaTanks.PlaytestResources.Armor.v1";
        public const int CurrencyAllowance = 100000, FragmentAllowance = 1000;
        public static void ApplyOnce(FrontendCatalog catalog, PlayerWallet wallet, PlayerProgression progression)
        {
            if (!PlayerPrefs.HasKey(SaveKey)) TopUp(catalog, wallet, progression);
            GrantArmorOnce(catalog, progression);
        }
        public static void GrantArmorOnce(FrontendCatalog catalog, PlayerProgression progression)
        {
            if (PlayerPrefs.HasKey(ArmorSaveKey)) return;
            var armors = (catalog?.inventory ?? Array.Empty<EquipmentDefinition>()).OfType<ArmorDefinition>().Distinct().ToArray();
            if (armors.Length == 0) return;
            foreach (var armor in armors)
            {
                int missing = FragmentAllowance - progression.Get(armor).Fragments;
                if (missing > 0 && !progression.TryGrantFragments(armor, missing)) throw new InvalidOperationException("Armor blueprint grant failed.");
            }
            PlayerPrefs.SetInt(ArmorSaveKey, 1); PlayerPrefs.Save();
        }
        public static void TopUp(FrontendCatalog catalog, PlayerWallet wallet, PlayerProgression progression)
        {
            foreach (CurrencyType currency in Enum.GetValues(typeof(CurrencyType)))
            {
                int missing = CurrencyAllowance - wallet.GetBalance(currency);
                if (missing > 0 && !wallet.TryAdd(currency, missing)) throw new InvalidOperationException("Play-test currency grant failed.");
            }
            var weapons = (catalog?.inventory ?? Array.Empty<EquipmentDefinition>()).OfType<WeaponDefinition>().Cast<UnityEngine.Object>();
            var crew = (catalog?.crew?.members ?? Array.Empty<CrewDefinition>()).Where(c => c != null).Cast<UnityEngine.Object>();
            var armors = (catalog?.inventory ?? Array.Empty<EquipmentDefinition>()).OfType<ArmorDefinition>().Cast<UnityEngine.Object>();
            foreach (var definition in weapons.Concat(crew).Concat(armors).Distinct())
            {
                int missing = FragmentAllowance - progression.Get(definition).Fragments;
                if (missing > 0 && !progression.TryGrantFragments(definition, missing)) throw new InvalidOperationException("Play-test material grant failed: " + definition.name);
            }
            PlayerPrefs.SetInt(SaveKey, 1);
            PlayerPrefs.Save();
        }
    }
}
