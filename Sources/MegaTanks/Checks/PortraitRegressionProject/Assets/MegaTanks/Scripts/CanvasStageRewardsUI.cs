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
            string firstClearTitle = receipt.firstClear
                ? $"FIRST COMPLETION BONUS\n+{receipt.accountExperience:N0} ACCOUNT XP"
                : "FIRST COMPLETION BONUS • ALREADY COLLECTED";
            Text(view, "First Completion Title", firstClearTitle);
            var account = game.StageRewards.AccountProgress;
            var detail = view.Get<Text>("Result Detail");
            detail.text += $"    ACCOUNT LV {account.Level}  •  {account.ExperienceIntoLevel:N0}/{account.ExperienceRequired:N0} XP";
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
