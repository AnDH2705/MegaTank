using UnityEngine;
using UnityEngine.UI;
namespace MegaTanks
{
    public sealed partial class HtmlBattleScreen
    {
        CrewBitmapText crewNotice;
        void UpdateCrewNotice()
        {
            if(crewNotice==null)crewNotice=ComicLabel(hud,"Unstaffed Weapon Notice","",75,280,515,70,27);
            int empty=-1;for(int i=0;i<3;i++)if(Simulation.SlotUnlocked(i)&&Simulation.player.weapons[i]&&!Simulation.HasAssignedOperator(i)){empty=i;break;}
            crewNotice.gameObject.SetActive(empty>=0&&!Simulation.Terminal&&Simulation.phase!="choice"&&Simulation.phase!="upgrade"&&Simulation.phase!="rescue_release");
            if(empty>=0)crewNotice.Value=config.weapons[empty].id.ToUpperInvariant()+" UNLOCKED\nGUNNER NEEDED";
        }
        void RescueDesign()
        {
            var s=Simulation;var member=game.Crew.Member(s.RescueStory.crewId);string role=s.RescueStory.weaponSlot==0?"MG GUNNER":"MORTAR GUNNER";
            var eyebrow=ComicLabel(modal,"Event Eyebrow","PRISONER FOUND",380,150,1160,30,24);eyebrow.color=new Color(1,.86f,.4f);
            ComicLabel(modal,"Event Title","RESCUE OR MOVE ON",380,185,1160,60,49);
            Label(modal,"Event Subtitle","A crewmate needs your help.",380,256,1160,35,24,Color.white,TextAnchor.MiddleCenter);
            for(int i=0;i<2;i++)
            {
                bool rescue=i==0;string choice=rescue?"rescue":"skip",rarity=rescue?"rare":"common";float width=344;
                var root=Rect(modal,"Card "+choice,570+i*400,310,width,580);
                var bg=StaticImage(root,"Base","card_base_"+rarity,15,0,width,580);bg.gameObject.AddComponent<ComicSkew>().slope=.0524f;bg.raycastTarget=true;
                var button=root.gameObject.AddComponent<Button>();button.targetGraphic=bg;button.onClick.AddListener(()=>ChooseDesign(choice));var motion=root.gameObject.AddComponent<HtmlChoiceCardMotion>();motion.Initialize(button,bg);
                Label(root,"Rarity",rarity.ToUpperInvariant(),30,17,width-60,24,16,new Color(.16f,.13f,.2f));
                StaticImage(root,"Info Badge","card_info_badge",width-22,-16,53,53);Label(root,"Info","i",width-22,-17,53,53,34,Color.white,TextAnchor.MiddleCenter).fontStyle=FontStyle.BoldAndItalic;
                if(rescue)
                {
                    var image=StaticImage(root,"Crew Portrait","",30,65,width-60,220);image.sprite=RescueEventArt.CrewPortrait;image.preserveAspect=true;
                    var icon=Rect(root,"Captive Lock",245,202,48,58).gameObject.AddComponent<UILockIcon>();icon.color=new Color(1,.86f,.4f);icon.raycastTarget=false;
                }
                else{TankPreview(root,s.player.skin,s.player.weapons,20,85,width-40,190);Label(root,"Move Arrow",">",238,211,75,60,62,new Color(1,.85f,.25f),TextAnchor.MiddleCenter);}
                ComicLabel(root,"Name",rescue?"RESCUE CAT":"MOVE ON",12,295,width-24,70,40);
                Label(root,"Tag",rescue?role:"KEEP MOVING",12,374,width-24,34,20,new Color(.2f,.35f,.47f),TextAnchor.MiddleCenter).fontStyle=FontStyle.BoldAndItalic;
                Label(root,"Description",rescue?"Free "+member.displayName+" and invite them aboard for this stage.":"Leave the cage and continue your journey.",25,424,width-50,67,22,new Color(.14f,.12f,.18f),TextAnchor.MiddleCenter);
                var action=StaticImage(root,"Action Plate","card_action_plate",32,515,width-64,48);action.type=Image.Type.Sliced;
                Label(root,"Action",rescue?"RESCUE":"CONTINUE",37,516,width-74,43,23,Color.white,TextAnchor.MiddleCenter).fontStyle=FontStyle.BoldAndItalic;
                Label(root,"Action Arrow",">",width-72,516,32,43,32,new Color(.2f,.65f,1),TextAnchor.MiddleCenter);
                button.interactable=!rescue||s.RescueReady;
            }
        }
    }
}
