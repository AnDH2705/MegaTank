using UnityEngine.UI;
namespace MegaTanks
{
    public sealed partial class CanvasGameUI
    {
        void DrawStageCompletionRewards()
        {
            var view=View(UIScreen.Victory);var receipt=game.StageRewards.LastReceipt;
            if(receipt==null)return;
            Text(view,"Completion Reward Status",receipt.paid?"REWARDS ADDED TO YOUR BALANCE":"REWARDS SAVED • WAITING FOR WALLET SPACE");
            Text(view,"First Completion Title",receipt.firstClear?"FIRST COMPLETION BONUS":"FIRST COMPLETION BONUS • ALREADY COLLECTED");
            foreach(string group in new[]{"Repeat Rewards","First Completion Rewards","Total Rewards"})
            {
                var amounts=group=="Repeat Rewards"?receipt.repeat:group=="First Completion Rewards"?receipt.bonus:receipt.Total;
                Text(view,group+"/Parts/Amount","+"+amounts.parts.ToString("N0"));
                Text(view,group+"/PetFood/Amount","+"+amounts.petFood.ToString("N0"));
                Text(view,group+"/Gems/Amount","+"+amounts.gems.ToString("N0"));
            }
        }
    }
}
