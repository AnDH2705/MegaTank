using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
namespace MegaTanks
{
    public sealed partial class CanvasGameUI
    {
        void DrawEquipment()
        {
            var v=View(UIScreen.Equipment);v.Get<RawImage>("Tank Avatar").texture=portrait;
            Active(v,"Filter List",false);Slot("Mortar",EquipmentType.Mortar,0);Slot("Accessory 2",EquipmentType.Accessory,0);Slot("Accessory 3",EquipmentType.Accessory,1);
            Slot("MG",EquipmentType.MachineGun,0);Slot("Cannon",EquipmentType.Cannon,0);Slot("Armor",EquipmentType.Armor,0);
            Text(v,"Inventory Filter/Label","FILTER: "+(filter.HasValue?TypeName(filter.Value):"ALL"));Click(v,"Inventory Filter",OpenFilter);
            Text(v,"Sort Direction/Label",descending?"DESC":"ASC");Click(v,"Sort Direction",()=>{descending=!descending;DrawEquipment();});
            Text(v,"Inventory Sort/Label","ORDER BY: "+new[]{"NAME","TYPE","DAMAGE"}[sort]);Click(v,"Inventory Sort",()=>{sort=(sort+1)%3;DrawEquipment();});
            var content=v.Get<RectTransform>("Inventory/Content");
            foreach(Transform child in content){child.gameObject.SetActive(false);Destroy(child.gameObject);}
            var items=(game.Frontend?.inventory??Array.Empty<EquipmentDefinition>()).Where(i=>i!=null&&(!filter.HasValue||i.Type==filter));
            Func<EquipmentDefinition,string> name=i=>string.IsNullOrEmpty(i.displayName)?i.name:i.displayName;
            IOrderedEnumerable<EquipmentDefinition> sorted=sort==1?items.OrderBy(i=>i.Type).ThenBy(name):sort==2?items.OrderBy(i=>i is GunDefinition g?g.stats.damage:i is MortarDefinition m?m.stats.damage:0).ThenBy(name):items.OrderBy(name);
            var list=(descending?sorted.Reverse():sorted).ToArray();InventoryVisibleCount=list.Length;
            foreach(var item in list)
            {
                var cell=Instantiate(layout.inventoryItem,content,false);cell.name="Item "+item.name;cell.gameObject.SetActive(true);
                Click(cell,"$root",()=>{selected=item;DrawEquipment();});
                var icon=cell.Get<Image>("Icon");icon.sprite=item.icon;icon.gameObject.SetActive(item.icon!=null);
                Text(cell,"Item Name",name(item));Active(cell,"Equipped Indicator",game.Equipment.IsEquipped(item));
                cell.Get<Outline>("$root").enabled=selected==item;
            }
            Active(v,"Empty Inventory",list.Length==0);Text(v,"Empty Inventory","No "+(filter.HasValue?TypeName(filter.Value).ToLowerInvariant():"equipment")+" items available yet.");
            DrawItemDetail(name);
        }
        void Slot(string title,EquipmentType type,int index)
        {
            var v=View(UIScreen.Equipment);string key="Slot "+title;var item=game.Equipment.GetEquipped(type,index);
            bool active=activeSlot==type&&(type!=EquipmentType.Accessory||index==accessoryIndex);
            // Slot art reflects selection; its geometry stays entirely authored in the prefab.
            v.Get<Image>(key).sprite=active?equipmentSkin.slotSelected:equipmentSkin.slot;
            Click(v,key,()=>{activeSlot=type;accessoryIndex=index;filter=type;selected=null;DrawEquipment();});
            bool filled=item!=null&&item.icon!=null;
            Active(v,key+"/Equipped Item Icon",filled);if(filled)v.Get<Image>(key+"/Equipped Item Icon").sprite=item.icon;
            Active(v,key+"/Empty Slot Icon",!filled);Text(v,key+"/Slot Description",title+"\n"+(item!=null?item.displayName:"Empty"));
        }
        void OpenFilter()
        {
            var v=View(UIScreen.Equipment);Active(v,"Filter List",!v.Get<RectTransform>("Filter List").gameObject.activeSelf);
            foreach(var option in new EquipmentType?[]{null,EquipmentType.Cannon,EquipmentType.MachineGun,EquipmentType.Mortar,EquipmentType.Armor,EquipmentType.Accessory})
            {
                var value=option;Click(v,"Filter List/Filter "+(value?.ToString()??"All"),()=>{filter=value;selected=null;DrawEquipment();});
            }
        }
        void DrawItemDetail(Func<EquipmentDefinition,string> name)
        {
            var v=View(UIScreen.Equipment);bool visible=selected!=null;
            Active(v,"Detail Prompt",!visible);
            foreach(string key in new[]{"Detail Item Card","Detail Item Icon","Detail Card Name","Detail Name","Item Stats","Equip Item"})Active(v,key,visible);
            bool equipped=visible&&game.Equipment.IsEquipped(selected);Active(v,"Equipped Detail Tick",equipped);Active(v,"Equipped Detail Label",equipped);
            if(!visible)return;
            var icon=v.Get<Image>("Detail Item Icon");icon.sprite=selected.icon;icon.gameObject.SetActive(selected.icon!=null);
            Text(v,"Detail Card Name",name(selected));Text(v,"Detail Name",name(selected));string detail=TypeName(selected.Type);
            WeaponCombatStats combat=null;float damage=0,range=0,reload=0;int ammo=0;
            if(selected is GunDefinition gun){combat=gun.stats;damage=gun.stats.damage;range=gun.stats.range;reload=gun.stats.reloadTime;ammo=gun.stats.reserveCapacity;}
            if(selected is MortarDefinition mortar){combat=mortar.stats;damage=mortar.stats.damage;range=mortar.stats.range;reload=mortar.stats.reloadTime;ammo=mortar.stats.reserveCapacity;}
            if(combat!=null)detail="BASE "+damage.ToString("0.#")+" | BONUS "+combat.damageBonus.ToString("0.#")+"%\nHIT "+combat.accuracy.ToString("0.#")+"% | PEN "+combat.armorPenetration.ToString("0.#")+"%\nCRIT "+combat.criticalChance.ToString("0.#")+"% | CRIT DMG "+combat.criticalDamage.ToString("0.#")+"%\nRANGE "+range.ToString("0.#")+" | AMMO "+ammo+"\n"+combat.damageType.ToString().ToUpperInvariant()+" | RELOAD "+reload.ToString("0.##")+"s";
            Text(v,"Item Stats",detail);var slot=activeSlot??selected.Type;bool compatible=selected.Type==slot;
            Text(v,"Equip Item/Label",compatible?"EQUIP":"INCOMPATIBLE");v.Get<Button>("Equip Item").interactable=compatible;
            Click(v,"Equip Item",()=>{if(game.Equipment.Equip(selected,slot,accessoryIndex))RefreshEquipmentArtwork();DrawEquipment();});
        }
    }
}
