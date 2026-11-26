using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
namespace MegaTanks
{
    public sealed partial class CanvasGameUI
    {
        int crewStation=-1,crewFilter;
        bool crewDescending;
        CrewDefinition crewSelected;
        public int CrewInventoryVisibleCount{get;private set;}
        void DrawCrew()
        {
            var v=View(UIScreen.Crew);var roster=game.Frontend?.crew;var assigned=game.Crew;
            int targetStation=crewStation>=0?crewStation:crewSelected!=null?(int)crewSelected.role:-1;
            v.Get<RawImage>("Tank Preview/Tank Avatar").texture=portrait;
            var art=v.GetComponent<LoadoutArtPresentation>();
            if(art!=null){v.Get<Button>("Change Tank").interactable=!game.Deployed;Click(v,"Change Tank",()=>{if(!game.Deployed)OpenEquipment();});}
            Text(v,"Crew Summary",assigned.AssignedCount+" / "+assigned.SlotCount+" STATIONS FILLED");
            Text(v,"Crew Instructions",crewStation>=0?"Role filter active. Click a highlighted station to assign; Filter shows all crew.":"Select crew, then click its highlighted station. Select a station to filter.");
            for(int i=0;i<assigned.SlotCount;i++)
            {
                int station=i;string key="Slot "+i,marker="Tank Preview/Tank Slot "+i;
                var member=assigned.Get(i);bool occupied=member!=null;
                v.Get<UIStarStrip>(key+"/Stars").Stars=occupied?game.Progression.Get(member).Stars:0;
                v.Get<UIStarStrip>(marker+"/Stars").Stars=occupied?game.Progression.Get(member).Stars:0;
                Click(v,key,()=>SelectCrewStation(station));Click(v,marker,()=>SelectCrewStation(station));
                Text(v,key+"/Station",assigned.StationName(i).ToUpperInvariant());Text(v,key+"/Name",occupied?member.displayName:"Empty");
                var image=v.Get<Image>(key+"/Portrait");image.sprite=occupied?member.portrait:null;image.gameObject.SetActive(occupied);
                bool matching=assigned.CanEquip(crewSelected,i);
                Active(v,key+"/Empty",!occupied);HighlightCrewStation(v.Get<Outline>(key),crewStation==i,matching);
                var onTank=v.Get<Image>(marker+"/Portrait");onTank.sprite=occupied?(art!=null&&member.equippedSprite!=null?member.equippedSprite:member.portrait):null;onTank.gameObject.SetActive(occupied);
                Active(v,marker+"/Empty",!occupied);Text(v,marker+"/Station Number",(i+1).ToString());HighlightCrewStation(v.Get<Outline>(marker),crewStation==i,matching);
            }
            string roleFilter=crewStation==(int)CrewRole.WeaponSpecialist?"SPECIALIST":assigned.StationName(crewStation).ToUpperInvariant();
            Text(v,"Inventory Filter/Label",crewStation>=0?roleFilter+" / "+new[]{"ALL","AVAILABLE","ASSIGNED"}[crewFilter]:new[]{"ALL CREW","AVAILABLE","ASSIGNED"}[crewFilter]);
            Click(v,"Inventory Filter",()=>{if(crewStation>=0){crewStation=-1;crewFilter=0;}else crewFilter=(crewFilter+1)%3;DrawCrew();});
            Text(v,"Sort Direction/Label",crewDescending?"NAME: Z–A":"NAME: A–Z");Click(v,"Sort Direction",()=>{crewDescending=!crewDescending;DrawCrew();});
            var content=v.Get<ScrollRect>("Inventory").content;
            foreach(Transform child in content){child.gameObject.SetActive(false);Destroy(child.gameObject);}
            var list=(roster?.members??Array.Empty<CrewDefinition>()).Where(m=>m!=null&&(crewStation<0||assigned.CanEquip(m,crewStation))&&(crewFilter==0||(crewFilter==1?!assigned.IsEquipped(m):assigned.IsEquipped(m)))).OrderBy(m=>m.displayName).ToArray();
            if(crewDescending)Array.Reverse(list);CrewInventoryVisibleCount=list.Length;
            foreach(var member in list)
            {
                var cell=Instantiate(layout.crewInventoryItem!=null?layout.crewInventoryItem:layout.inventoryItem,content,false);cell.gameObject.SetActive(true);cell.name="Crew "+member.id;
                Click(cell,"$root",()=>{crewSelected=member;DrawCrew();});
                var icon=cell.Get<Image>("Icon");icon.sprite=member.portrait;icon.gameObject.SetActive(member.portrait!=null);
                ProgressionCell(cell,member);Text(cell,"Item Name",member.displayName);Active(cell,"Equipped Indicator",assigned.IsEquipped(member));cell.Get<Outline>("$root").enabled=crewSelected==member;
                var frame=cell.transform.Find("Selection");if(frame!=null)frame.gameObject.SetActive(crewSelected==member);
            }
            Active(v,"Empty Inventory",list.Length==0);
            Text(v,"Empty Inventory",crewStation>=0?"No "+assigned.StationName(crewStation).ToLowerInvariant()+" crew match this filter.":"No crew match this filter.");
            Text(v,"Detail Name",crewSelected!=null?crewSelected.displayName:"CREW ASSIGNMENT");
            Text(v,"Detail Role",crewSelected!=null?crewSelected.RoleName+" • "+CrewDefinition.AssociatedSlot(crewSelected.role):"");
            Text(v,"Detail Description",crewSelected!=null?crewSelected.description:"Select a crew member. Each role has a matching station.");
            int current=assigned.FindSlot(crewSelected);
            Text(v,"Detail Assignment",crewSelected!=null?(current>=0?"Assigned: "+assigned.StationName(current):"Available")+"\nTarget station: "+assigned.StationName(targetStation):targetStation>=0?"Target station: "+assigned.StationName(targetStation):"Select a crew member or station.");
            var preview=v.Get<Image>("Detail Portrait");preview.sprite=crewSelected!=null?crewSelected.portrait:null;preview.gameObject.SetActive(crewSelected!=null);
            DrawProgression(v,crewSelected,true);
            var equip=v.Get<Button>("Assign Crew");equip.interactable=!game.Deployed&&assigned.CanEquip(crewSelected,targetStation)&&current!=targetStation;
            Text(v,"Assign Crew/Label",current==targetStation&&crewSelected!=null?"ASSIGNED":crewSelected!=null&&!assigned.CanEquip(crewSelected,targetStation)?"ROLE MISMATCH":"ASSIGN");
            Click(v,"Assign Crew",()=>{if(!game.Deployed&&assigned.Equip(crewSelected,targetStation))DrawCrew();});
            v.Get<Button>("Unequip Crew").interactable=!game.Deployed&&current>=0;Click(v,"Unequip Crew",()=>{int from=assigned.FindSlot(crewSelected);if(!game.Deployed&&assigned.Unequip(from))DrawCrew();});
            if(art!=null)art.DrawCrew(v,crewSelected,game.Progression,assigned,crewStation);
        }
        void HighlightCrewStation(Outline outline,bool selectedStation,bool matching)
        {
            outline.enabled=selectedStation||matching;
            outline.effectColor=matching?layout.routeCurrentText:layout.routeAvailableConnection;
            // Dark sprite borders can hide an Outline; tint the visible station art too.
            var button=outline.GetComponent<Button>();var colors=button.colors;
            colors.normalColor=matching?layout.routeCurrentText:selectedStation?layout.routeAvailableConnection:Color.white;
            colors.selectedColor=colors.normalColor;
            colors.highlightedColor=Color.Lerp(colors.normalColor,Color.white,.35f);
            colors.pressedColor=Color.Lerp(colors.normalColor,Color.black,.2f);
            button.colors=colors;
        }
        void SelectCrewStation(int station)
        {
            crewStation=station;
            if(game.Crew.CanEquip(crewSelected,station))
            {
                if(!game.Deployed)game.Crew.Equip(crewSelected,station);
            }
            else crewSelected=game.Crew.Get(station);
            DrawCrew();
        }
    }
}
