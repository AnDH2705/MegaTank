namespace MegaTanks
{
    public sealed partial class CanvasGameUI
    {
        void BindCurrencies()
        {
            game.Wallet.Changed += RefreshCurrencies;
            RefreshCurrencies();
        }
        void RefreshCurrencies()
        {
            string balances = "PARTS  " + game.Wallet.Parts.ToString("N0") +
                "     PET FOOD  " + game.Wallet.PetFood.ToString("N0") +
                "     GEMS  " + game.Wallet.Gems.ToString("N0");
            foreach (var screen in new[] { UIScreen.Home, UIScreen.Equipment, UIScreen.Crew })
            {
                var view = View(screen);
                Text(view, "Currency Balances", balances);
                Text(view, "Currency Balances/Parts/Amount", game.Wallet.Parts.ToString("N0"));
                Text(view, "Currency Balances/PetFood/Amount", game.Wallet.PetFood.ToString("N0"));
                Text(view, "Currency Balances/Gems/Amount", game.Wallet.Gems.ToString("N0"));
            }
        }
    }
}
