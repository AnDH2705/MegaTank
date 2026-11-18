using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
namespace MegaTanks
{
    public sealed partial class CanvasGameUI
    {
        int crewStation,crewFilter;
        bool crewDescending;
        CrewDefinition crewSelected;
        public int CrewInventoryVisibleCount{get;private set;}
        void DrawCrew()
        {
            var v=View(UIScreen.Crew);var roster=game.Frontend?.crew;var assigned=game.Crew;
            v.Get<RawImage>("Tank Preview/Tank Avatar").texture=portrait;
            Text(v,"Crew Summary",assigned.AssignedCount+" / "+assigned.SlotCount+" STATIONS FILLED");
            Text(v,"Crew Instructions","Select a matching crew station. Scroll the stat panel to view all stats and level gains.");
            for(int i=0;i<assigned.SlotCount;i++)
            {
                int station=i;string key="Slot "+i,marker="Tank Preview/Tank Slot "+i;
                var member=assigned.Get(i);bool occupied=member!=null;
                v.Get<UIStarStrip>(key+"/Stars").Stars=occupied?game.Progression.Get(member).Stars:0;
                v.Get<UIStarStrip>(marker+"/Stars").Stars=occupied?game.Progression.Get(member).Stars:0;
                Click(v,key,()=>SelectCrewStation(station));Click(v,marker,()=>SelectCrewStation(station));
                Text(v,key+"/Station",assigned.StationName(i).ToUpperInvariant());Text(v,key+"/Name",occupied?member.displayName:"Empty");
                var image=v.Get<Image>(key+"/Portrait");image.sprite=occupied?member.portrait:null;image.gameObject.SetActive(occupied);
                Active(v,key+"/Empty",!occupied);v.Get<Outline>(key).enabled=crewStation==i;
                var onTank=v.Get<Image>(marker+"/Portrait");onTank.sprite=occupied?member.portrait:null;onTank.gameObject.SetActive(occupied);
                Active(v,marker+"/Empty",!occupied);Text(v,marker+"/Station Number",(i+1).ToString());v.Get<Outline>(marker).enabled=crewStation==i;
            }
            Text(v,"Inventory Filter/Label",new[]{"ALL CREW","AVAILABLE","ASSIGNED"}[crewFilter]);Click(v,"Inventory Filter",()=>{crewFilter=(crewFilter+1)%3;DrawCrew();});
            Text(v,"Sort Direction/Label",crewDescending?"NAME: Z–A":"NAME: A–Z");Click(v,"Sort Direction",()=>{crewDescending=!crewDescending;DrawCrew();});
            var content=v.Get<ScrollRect>("Inventory").content;
            foreach(Transform child in content){child.gameObject.SetActive(false);Destroy(child.gameObject);}
            var list=(roster?.members??Array.Empty<CrewDefinition>()).Where(m=>m!=null&&(crewFilter==0||(crewFilter==1?!assigned.IsEquipped(m):assigned.IsEquipped(m)))).OrderBy(m=>m.displayName).ToArray();
            if(crewDescending)Array.Reverse(list);CrewInventoryVisibleCount=list.Length;
            foreach(var member in list)
            {
                var cell=Instantiate(layout.inventoryItem,content,false);cell.gameObject.SetActive(true);cell.name="Crew "+member.id;
                Click(cell,"$root",()=>{crewSelected=member;DrawCrew();});
                var icon=cell.Get<Image>("Icon");icon.sprite=member.portrait;icon.gameObject.SetActive(member.portrait!=null);
                ProgressionCell(cell,member);Text(cell,"Item Name",member.displayName);Active(cell,"Equipped Indicator",assigned.IsEquipped(member));cell.Get<Outline>("$root").enabled=crewSelected==member;
            }
            Active(v,"Empty Inventory",list.Length==0);
            Text(v,"Detail Name",crewSelected!=null?crewSelected.displayName:"CREW ASSIGNMENT");
            Text(v,"Detail Role",crewSelected!=null?crewSelected.RoleName+" • "+CrewDefinition.AssociatedSlot(crewSelected.role):"");
            Text(v,"Detail Description",crewSelected!=null?crewSelected.description:"Select a crew member. Each role has a matching station.");
            int current=assigned.FindSlot(crewSelected);
            Text(v,"Detail Assignment",crewSelected!=null?(current>=0?"Assigned: "+assigned.StationName(current):"Available")+"\nTarget station: "+assigned.StationName(crewStation):"Target station: "+assigned.StationName(crewStation));
            var preview=v.Get<Image>("Detail Portrait");preview.sprite=crewSelected!=null?crewSelected.portrait:null;preview.gameObject.SetActive(crewSelected!=null);
            DrawProgression(v,crewSelected,true);
            var equip=v.Get<Button>("Assign Crew");equip.interactable=!game.Deployed&&assigned.CanEquip(crewSelected,crewStation)&&current!=crewStation;
            Text(v,"Assign Crew/Label",current==crewStation&&crewSelected!=null?"ASSIGNED":crewSelected!=null&&!assigned.CanEquip(crewSelected,crewStation)?"ROLE MISMATCH":"ASSIGN");
            Click(v,"Assign Crew",()=>{if(!game.Deployed&&assigned.Equip(crewSelected,crewStation))DrawCrew();});
            v.Get<Button>("Unequip Crew").interactable=!game.Deployed&&current>=0;Click(v,"Unequip Crew",()=>{int from=assigned.FindSlot(crewSelected);if(!game.Deployed&&assigned.Unequip(from))DrawCrew();});
        }
        void SelectCrewStation(int station){crewStation=station;crewSelected=game.Crew.Get(station);DrawCrew();}
    }
}
