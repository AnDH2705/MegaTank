using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace MegaTanks.Editor
{
    public static class CurrencyAudit
    {
        static void Require(bool value, string message) { if (!value) throw new Exception(message); }
        public static void RunWithUI() { Run(); ItemProgressionAudit.Run(); }
        internal static void CheckCounters(BattleGame game)
        {
            var layout=game.UI.GetComponent<UICanvasLayout>();
            foreach(var screen in new[]{UIScreen.Home,UIScreen.Equipment,UIScreen.Crew})
            {
                var view=layout.View(screen);
                foreach(CurrencyType currency in Enum.GetValues(typeof(CurrencyType)))
                {
                    string key="Currency Balances/"+currency;
                    Require(view.Get<Text>(key+"/Amount").text==game.Wallet.GetBalance(currency).ToString("N0"),"All currency counters update after transactions.");
                    var icon=view.Get<Image>(key+"/Icon");
                    Require(icon.sprite!=null&&!icon.raycastTarget&&icon.preserveAspect,"Currency artwork must not block input.");
                }
            }
        }

        public static void Run()
        {
            string key = "MegaTanks.Wallet.Audit." + Guid.NewGuid();
            try
            {
                var wallet = new PlayerWallet(key);
                int changes = 0;
                wallet.Changed += () => changes++;
                Require(wallet.Parts == 0 && wallet.PetFood == 0 && wallet.Gems == 0, "New wallet starts at zero.");
                foreach (CurrencyType type in Enum.GetValues(typeof(CurrencyType)))
                {
                    Require(wallet.TryAdd(type, 100), "Credit each currency.");
                    Require(wallet.CanAfford(type, 100) && !wallet.CanAfford(type, 101), "Exact affordability.");
                    Require(!wallet.TrySpend(type, 101) && !wallet.TrySpend(type, -1) && !wallet.TrySpend(type, 0), "Reject invalid spending.");
                    Require(!wallet.TryAdd(type, -1) && !wallet.TryAdd(type, 0) && !wallet.TryAdd(type, int.MaxValue), "Reject invalid credits and overflow.");
                    Require(wallet.TrySpend(type, 40) && wallet.GetBalance(type) == 60, "Spend only the chosen balance.");
                }
                Require(changes == 6, "Only successful transactions notify the UI.");
                var loaded = new PlayerWallet(key);
                Require(loaded.Parts == 60 && loaded.PetFood == 60 && loaded.Gems == 60, "All balances survive reload.");
                Require(loaded.TrySpend(CurrencyType.Parts, 60) && loaded.PetFood == 60 && loaded.Gems == 60, "Balances remain independent.");
                Require(loaded.TryAdd(CurrencyType.Parts, int.MaxValue) && !loaded.TryAdd(CurrencyType.Parts, 1), "Maximum balance never wraps.");
                bool invalid = false;
                try { loaded.TryAdd((CurrencyType)99, 1); } catch (ArgumentOutOfRangeException) { invalid = true; }
                Require(invalid, "Unknown currencies are rejected.");
                PlayerPrefs.SetString(key, "{\"parts\":-4,\"petFood\":8,\"gems\":-1}");
                loaded = new PlayerWallet(key);
                Require(loaded.Parts == 0 && loaded.PetFood == 8 && loaded.Gems == 0, "Negative saves are sanitized.");
                PlayerPrefs.SetString(key, "not json");
                Require(new PlayerWallet(key).Parts == 0, "Malformed save falls back safely.");
                PlayerPrefs.SetString(key, "{}");
                Require(new PlayerWallet(key).PetFood == 0, "Missing fields default to zero.");
                CurrencySetup.Install();
                CurrencySetup.Install();
                foreach (string screen in new[] { "Home", "Equipment", "Crew" })
                {
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/MegaTanks/Prefabs/UI/" + screen + ".prefab");
                    var label = prefab.GetComponent<UIView>().Get<Text>("Currency Balances");
                    Require(!label.raycastTarget && label.text.Contains("PET FOOD") && label.text.Contains("GEMS"), "Authored balances must not block input.");
                    Require(!label.enabled,"The legacy aggregate must not render behind the icon counters.");
                    foreach(string name in new[]{"Parts","PetFood","Gems"})
                    {
                        string binding="Currency Balances/"+name;
                        var icon=prefab.GetComponent<UIView>().Get<Image>(binding+"/Icon");
                        Require(icon.sprite==AssetDatabase.LoadAssetAtPath<Sprite>("Assets/MegaTanks/Art/Currencies/"+name+".png"),"Each counter uses its generated icon.");
                        Require(!icon.raycastTarget&&icon.preserveAspect,"Currency icons preserve proportions without blocking input.");
                        int badges=0;foreach(Transform child in label.transform)if(child.name==name)badges++;
                        Require(badges==1,"Repeated migration must not duplicate currency badges.");
                    }

                    int count = 0;
                    foreach (Transform child in prefab.transform) if (child.name == "Currency Balances") count++;
                    Require(count == 1, "Repeated migration must not duplicate balances.");
                }
                Debug.Log("CURRENCY_AUDIT_PASS");
            }
            finally { PlayerPrefs.DeleteKey(key); PlayerPrefs.Save(); }
        }
    }
}
