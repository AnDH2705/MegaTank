using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace MegaTanks.Editor
{
    public static class PlaytestResourcesSetup
    {
        [MenuItem("MegaTanks/Playtesting/Top Up Upgrade Resources")]
        public static void Grant()
        {
            var game = EditorApplication.isPlaying ? BattleGame.Instance : null;
            var catalog = game != null ? game.Frontend : AssetDatabase.LoadAssetAtPath<FrontendCatalog>("Assets/MegaTanks/Data/UI/Frontend.asset");
            if (catalog == null) throw new InvalidOperationException("Frontend catalog is required.");
            var wallet = game != null ? game.Wallet : new PlayerWallet();
            var progression = game != null ? game.Progression : new PlayerProgression(catalog, wallet);
            var definitions = catalog.inventory.Where(i => i is WeaponDefinition || i is ArmorDefinition).Cast<UnityEngine.Object>()
                .Concat(catalog.crew.members.Where(m => m != null).Cast<UnityEngine.Object>()).Distinct().ToArray();
            var before = definitions.Select(progression.Get).ToArray();
            PlaytestResources.TopUp(catalog, wallet, progression);
            var loadedWallet = new PlayerWallet();
            var loaded = new PlayerProgression(catalog, loadedWallet);
            foreach (CurrencyType type in Enum.GetValues(typeof(CurrencyType)))
                if (loadedWallet.GetBalance(type) < PlaytestResources.CurrencyAllowance) throw new Exception("Currency grant was not saved.");
            for (int i = 0; i < definitions.Length; i++)
            {
                var state = loaded.Get(definitions[i]);
                if (state.Fragments < PlaytestResources.FragmentAllowance || state.Level != before[i].Level || state.Stars != before[i].Stars)
                    throw new Exception("Material grant must preserve levels and stars.");
            }
            Debug.Log("PLAYTEST_RESOURCES_GRANTED: Parts/Pet Food/Gems >= 100000; Blueprints for " +
                definitions.Count(d => d is WeaponDefinition || d is ArmorDefinition) + " equipment items and Pet Tags for " +
                definitions.OfType<CrewDefinition>().Count() + " crew >= 1000 each. Levels and stars preserved.");
        }
    }
}
