using UnityEngine;
using UnityEngine.UI;

namespace MegaTanks
{
    // Visual adapter only. The existing controllers own equips, costs, saves and upgrades.
    public sealed class LoadoutArtPresentation : MonoBehaviour
    {
        public bool crew;
        public Text heading,level,cost,summary,statsSource,statsText,statsCaption;
        public ScrollRect statsScroll;
        public EquipmentStatIcons equipmentStats;
        public Sprite labelNormal,labelSelected,ascendNormal,ascendDisabled;
        public Image fragmentFill;
        public GameObject ribbon,detailFrame;
        public string[] slotKeys;
        public Text[] slotLabels;
        public Image[] skills;
        public Text[] skillLabels,statValues;
        public Button[] skillButtons,statButtons;
        public Sprite[] skillSprites;
        public GameObject info;
        public Text infoText;
        public ScrollRect infoScroll;
        public Button infoClose,portraitInfo;
        Object previous;
        static readonly string[][] SkillNames={new[]{"Steady\nAim","Quick\nReload","Heavy\nShells"},new[]{"Full\nThrottle","Reinforced\nArmor","Crew\nRally"},new[]{"Extended\nBelt","Bullet\nStorm","Stopping\nPower"},new[]{"Pinpoint\nStrike","Extra\nRounds","Explosive\nPayload"},new[]{"Shovel\nSlam","Rapid\nStrikes","Iron\nHide"}};
        static readonly string[] StatNames={"ATK","HP","Accuracy","Defense","Move Speed","Crit Rate"};
        public static string Ascii(string text)=>text.Replace("→",">").Replace("•","|").Replace("·","|").Replace("°"," deg").Replace('\u2013','-').Replace("›",">");
        void Awake(){if(infoClose!=null)infoClose.onClick.AddListener(()=>info.SetActive(false));}
        void Progression(UIView view,Object selected,PlayerProgression progression)
        {
            var state=progression.Get(selected);level.text=selected!=null?"LV."+state.Level:"";
            int next=ProgressionRules.LevelCost(state.Level),star=ProgressionRules.StarCost(state.Stars);
            cost.text=selected==null?"":next>0?next.ToString("N0")+(crew?" FOOD":" PARTS"):"LEVEL COST LIMIT";
            view.Get<Text>("Level Up/Label").text="LEVEL UP!";
            bool ready=selected!=null&&progression.CanStarUp(selected);
            view.Get<Button>("Star Up").image.sprite=ready?ascendNormal:ascendDisabled;
            view.Get<Text>("Star Up/Label").text=star<=0?"MAX":ready?"ASCEND":state.Fragments+"/"+star;
            fragmentFill.fillAmount=star>0?Mathf.Clamp01((float)state.Fragments/star):0;fragmentFill.gameObject.SetActive(!ready&&star>0);
            var fragmentIcon=fragmentFill.transform.parent.Find("Fragment Icon");if(fragmentIcon!=null)fragmentIcon.gameObject.SetActive(!ready&&star>0);
            ribbon.SetActive(selected!=null);detailFrame.SetActive(selected!=null);
            var name=view.Get<Text>("Detail Name");name.resizeTextForBestFit=false;name.fontSize=12;
            while(name.fontSize>7&&name.preferredWidth>name.rectTransform.rect.width)name.fontSize--;
            if(previous!=selected&&statsScroll!=null)statsScroll.verticalNormalizedPosition=1;previous=selected;
        }
        public void DrawEquipment(UIView view,EquipmentDefinition selected,PlayerProgression progression)
        {
            Progression(view,selected,progression);heading.text=selected!=null?selected.Type==EquipmentType.MachineGun?"Machine Gun":selected.Type.ToString():"Equipment";
            statsScroll.gameObject.SetActive(selected!=null);statsText.text=Ascii(statsSource.text);
            float contentHeight;
            if(equipmentStats!=null)contentHeight=equipmentStats.Draw(selected,statsSource.text);
            else{contentHeight=statsText.preferredHeight+8;statsText.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,Mathf.Max(statsScroll.viewport.rect.height,contentHeight));}
            statsCaption.text=contentHeight>statsScroll.viewport.rect.height?"STATS & COMPARISON - SCROLL":"STATS & COMPARISON";
            // The affected stat rows already contain the relevant values/comparisons.
            summary.text="";summary.gameObject.SetActive(false);
            if(selected!=null&&equipmentStats!=null&&equipmentStats.VisibleStatCount==0)statsCaption.text="NO STATS CHANGE WITH LEVEL";
            for(int i=0;i<slotKeys.Length;i++)slotLabels[i].text=view.Get<Text>(slotKeys[i]+"/Slot Description").text.Split('\n')[0].ToUpperInvariant();
            var tank=view.Get<Text>("Tank Selection/Label");tank.text=tank.text.Replace("  ›","");
            var tankStats=view.Get<Text>("Tank Stats");tankStats.text=Ascii(tankStats.text);
            var filter=view.Get<Text>("Inventory Filter/Label");filter.text=filter.text.Replace("FILTER: ","");
            var sort=view.Get<Text>("Inventory Sort/Label");sort.text=sort.text.Replace("ORDER BY: ","");
        }
        public void StyleEquipmentSlot(UIView view,string key,bool selected)
        {
            var root=view.Get<Button>(key).transform;root.Find("Selection").gameObject.SetActive(selected);
            root.Find("Role Plate").GetComponent<Image>().sprite=selected?labelSelected:labelNormal;
            root.Find("Role Plate/Name").GetComponent<Text>().color=selected?Color.black:Color.white;
        }
        public void DrawCrew(UIView view,CrewDefinition selected,PlayerProgression progression,PlayerCrew assigned,int activeSlot)
        {
            Progression(view,selected,progression);var state=progression.Get(selected);int role=selected!=null?(int)selected.role:0;
            heading.text=selected==null?"Select a crew":new[]{"Tank Cannon","Tank Driver","Tank MG","Tank Mortar","Melee Crew"}[role];
            for(int i=0;i<5;i++)
            {
                var root=view.Get<Button>("Tank Preview/Tank Slot "+i).transform;root.Find("Selection").gameObject.SetActive(activeSlot==i||assigned.CanEquip(selected,i));
                root.Find("Role Plate").GetComponent<Image>().sprite=activeSlot==i?labelSelected:labelNormal;slotLabels[i].color=activeSlot==i?Color.black:Color.white;
            }
            for(int i=0;i<6;i++)
            {
                int index=i;var stat=selected!=null?Stat(selected,i):null;bool applies=selected!=null&&(selected.role==CrewRole.Driver?i==1||i==4:selected.role==CrewRole.Commando||i==0||i==2||i==5);
                string suffix=i==2||i==5||i==0&&role!=4?"%":"";
                statValues[i].text=applies?stat.AtLevel(state.Level,state.Stars).ToString("0.##")+suffix:"-";
                statButtons[i].onClick.RemoveAllListeners();statButtons[i].onClick.AddListener(()=>ShowInfo(selected==null?"Select a crew member first.":StatNames[index]+"\n\n"+(applies?"Current: "+stat.AtLevel(state.Level,state.Stars).ToString("0.##")+suffix+"\nNext level: "+stat.AtLevel(state.Level+1,state.Stars).ToString("0.##")+suffix:"This role does not modify this stat.")+"\n\nCrew contribution while equipped."));
            }
            for(int i=0;i<3;i++)
            {
                int index=i;skills[i].sprite=skillSprites[role*3+i];skills[i].color=selected!=null?Color.white:Color.gray;skillLabels[i].text=SkillNames[role][i];
                skillButtons[i].onClick.RemoveAllListeners();skillButtons[i].onClick.AddListener(()=>ShowInfo(selected==null?"Select a crew member first.":SkillInfo(selected,index,state.Level,state.Stars)));
            }
            portraitInfo.onClick.RemoveAllListeners();portraitInfo.onClick.AddListener(()=>ShowInfo(Ascii(statsSource.text)));
            view.Get<Text>("Assign Crew/Label").text=view.Get<Text>("Assign Crew/Label").text.Replace("ASSIGNED","Equipped").Replace("ASSIGN","Equip");
            var sort=view.Get<Text>("Sort Direction/Label");sort.text=Ascii(sort.text);info.SetActive(false);
        }
        static CrewStat Stat(CrewDefinition member,int index)
        {
            var s=member.stats;switch(index){case 0:return member.role==CrewRole.Commando?s.damage:s.damageBonus;case 1:return s.hp;case 2:return s.accuracy;case 3:return s.armor;case 4:return s.speed;default:return s.criticalChance;}
        }
        static string SkillInfo(CrewDefinition member,int index,int level,int stars)
        {
            var s=member.stats;CrewStat stat;string effect;int role=(int)member.role;
            switch(member.role)
            {
                case CrewRole.Gunner:stat=index==0?s.accuracy:index==1?s.reloadSpeed:s.damageBonus;effect=index==0?"Cannon accuracy":index==1?"Cannon reload speed":"Cannon damage bonus";break;
                case CrewRole.Driver:stat=index==0?s.speed:index==1?s.hp:s.evasion;effect=index==0?"Tank movement speed":index==1?"Tank HP":"Tank evasion";break;
                case CrewRole.Commando:stat=index==0?s.damage:index==1?s.reloadSpeed:s.armor;effect=index==0?"Melee damage":index==1?"Melee reload speed":"Melee armor";break;
                default:
                    if(role==2&&index==0||role==3&&index==1)return SkillNames[role][index].Replace('\n',' ')+"\n\nAmmo capacity comes from the equipped weapon. This crew has no ammo-capacity bonus.";
                    stat=index==2?s.damageBonus:role==2?s.reloadSpeed:s.accuracy;effect=index==2?"Weapon damage bonus":role==2?"MG reload speed":"Mortar accuracy";break;
            }
            return SkillNames[role][index].Replace('\n',' ')+"\n\n"+effect+": "+stat.AtLevel(level,stars).ToString("0.##")+"\nNext level: "+stat.AtLevel(level+1,stars).ToString("0.##")+"\n\nApplies while this crew is equipped.";
        }
        void ShowInfo(string text)
        {
            infoText.text=text;infoText.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,Mathf.Max(160,infoText.preferredHeight+8));infoScroll.verticalNormalizedPosition=1;info.SetActive(true);info.transform.SetAsLastSibling();
        }
    }
}
