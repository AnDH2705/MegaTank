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
            bool heavy=game.TankProfile.ArmorSlots>1;
            Text(v,"Tank Selection/Label",game.TankProfile.displayName.ToUpperInvariant()+"  ›\nCHANGE TANK");
            v.Get<Button>("Tank Selection").interactable=!game.Deployed&&(game.Frontend.tanks?.Length??0)>1;
            Click(v,"Tank Selection",ChangeTank);
            Text(v,"Tank Stats","BASE HP "+game.TankProfile.stats.maxHull.ToString("0.#")+"   •   SPEED "+game.TankProfile.stats.advanceSpeed.ToString("0.##")+
                "\n"+game.TankProfile.ArmorSlots+" ARMOR   •   "+game.TankProfile.AccessorySlots+(game.TankProfile.AccessorySlots==1?" ACCESSORY":" ACCESSORIES"));
            Active(v,"Filter List",false);Slot("Mortar",EquipmentType.Mortar,0);Slot("Accessory 2",EquipmentType.Accessory,0,"Accessory 1");
            Slot("Accessory 3",heavy?EquipmentType.Armor:EquipmentType.Accessory,1,heavy?"Armor 2":"Accessory 2");
            Slot("MG",EquipmentType.MachineGun,0);Slot("Cannon",EquipmentType.Cannon,0);Slot("Armor",EquipmentType.Armor,0,heavy?"Armor 1":"Armor");
            Text(v,"Inventory Filter/Label","FILTER: "+(filter.HasValue?TypeName(filter.Value):"ALL"));Click(v,"Inventory Filter",OpenFilter);
            Text(v,"Sort Direction/Label",descending?"DESC":"ASC");Click(v,"Sort Direction",()=>{descending=!descending;DrawEquipment();});
            Text(v,"Inventory Sort/Label","ORDER BY: "+new[]{"NAME","TYPE","DAMAGE"}[sort]);Click(v,"Inventory Sort",()=>{sort=(sort+1)%3;DrawEquipment();});
            var content=v.Get<RectTransform>("Inventory/Content");
            foreach(Transform child in content){child.gameObject.SetActive(false);Destroy(child.gameObject);}
            var items=(game.Frontend?.inventory??Array.Empty<EquipmentDefinition>()).Where(i=>i!=null&&(!filter.HasValue||i.Type==filter));
            Func<EquipmentDefinition,string> name=i=>string.IsNullOrEmpty(i.displayName)?i.name:i.displayName;
            IOrderedEnumerable<EquipmentDefinition> sorted=sort==1?items.OrderBy(i=>i.Type).ThenBy(name):sort==2?items.OrderBy(i=>PreviewDamage(i)).ThenBy(name):items.OrderBy(name);
            var list=(descending?sorted.Reverse():sorted).ToArray();InventoryVisibleCount=list.Length;
            foreach(var item in list)
            {
                var cell=Instantiate(layout.inventoryItem,content,false);cell.name="Item "+item.name;cell.gameObject.SetActive(true);
                Click(cell,"$root",()=>{selected=item;DrawEquipment();});
                var icon=cell.Get<Image>("Icon");icon.sprite=item.icon;icon.gameObject.SetActive(item.icon!=null);
                ProgressionCell(cell,item);Text(cell,"Item Name",name(item));Active(cell,"Equipped Indicator",game.Equipment.IsEquipped(item));
                cell.Get<Outline>("$root").enabled=selected==item;
            }
            Active(v,"Empty Inventory",list.Length==0);Text(v,"Empty Inventory","No "+(filter.HasValue?TypeName(filter.Value).ToLowerInvariant():"equipment")+" items available yet.");
            DrawItemDetail(name);DrawProgression(v,selected,false);
        }
        void Slot(string title,EquipmentType type,int index,string label=null)
        {
            var v=View(UIScreen.Equipment);string key="Slot "+title;var item=game.Equipment.GetEquipped(type,index);
            v.Get<UIStarStrip>(key+"/Stars").Stars=game.Progression.Supports(item) ? game.Progression.Get(item).Stars : 0;
            bool active=activeSlot==type&&((type!=EquipmentType.Accessory&&type!=EquipmentType.Armor)||index==accessoryIndex);
            // Slot art reflects selection; its geometry stays entirely authored in the prefab.
            v.Get<Image>(key).sprite=active?equipmentSkin.slotSelected:equipmentSkin.slot;
            Click(v,key,()=>{activeSlot=type;accessoryIndex=index;filter=type;selected=null;DrawEquipment();});
            bool filled=item!=null&&item.icon!=null;
            Active(v,key+"/Equipped Item Icon",filled);if(filled)v.Get<Image>(key+"/Equipped Item Icon").sprite=item.icon;
            Active(v,key+"/Empty Slot Icon",!filled);Text(v,key+"/Slot Description",(label??title)+"\n"+(item!=null?item.displayName:"Empty"));
        }
        public void ChangeTank()
        {
            var tanks=game.Frontend.tanks;
            if(tanks==null||tanks.Length<2||game.Deployed)return;
            int index=Array.IndexOf(tanks,game.TankProfile);
            if(!game.SelectTank(tanks[(index+1)%tanks.Length]))return;
            activeSlot=null;accessoryIndex=0;filter=null;selected=null;
            RebuildPortrait();DrawEquipment();
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
            if(combat!=null){damage=PreviewDamage(selected);combat=game.PreviewWeaponStats((WeaponDefinition)selected);}
            if(combat!=null)detail="BASE "+damage.ToString("0.#")+" | BONUS "+combat.damageBonus.ToString("0.#")+"%\nHIT "+(combat.accuracy+game.AccuracyBonus).ToString("0.#")+"% | PEN "+combat.armorPenetration.ToString("0.#")+"%\nCRIT "+combat.criticalChance.ToString("0.#")+"% | CRIT DMG "+combat.criticalDamage.ToString("0.#")+"%\nRANGE "+range.ToString("0.#")+" | AMMO "+ammo+"\nBASE RELOAD TIME "+reload.ToString("0.##")+"s | SPEED "+(combat.reloadSpeed+game.ReloadSpeedBonus).ToString("0.##")+"%\nBATTLE RELOAD "+WeaponCombatStats.CalculateReloadTime(reload,combat.reloadSpeed+game.ReloadSpeedBonus).ToString("0.##")+"s | "+combat.damageType.ToString().ToUpperInvariant();
            var current=game.Equipment.GetEquipped(selected.Type,accessoryIndex);
            if(combat!=null&&current!=null&&current!=selected)
            {
                float oldDamage=0,oldRange=0,oldReload=0;int oldAmmo=0;WeaponCombatStats oldCombat=null;
                if(current is GunDefinition g){oldCombat=g.stats;oldDamage=g.stats.damage;oldRange=g.stats.range;oldReload=g.stats.reloadTime;oldAmmo=g.stats.reserveCapacity;}
                if(current is MortarDefinition m){oldCombat=m.stats;oldDamage=m.stats.damage;oldRange=m.stats.range;oldReload=m.stats.reloadTime;oldAmmo=m.stats.reserveCapacity;}
                if(oldCombat!=null){oldDamage=PreviewDamage(current);oldCombat=game.PreviewWeaponStats((WeaponDefinition)current);}
                if(oldCombat!=null)detail="EQUIPPED → SELECTED (change)\n"+
                    CompareStat("DMG",new DamageAttack(oldDamage,oldCombat).WeaponDamage,new DamageAttack(damage,combat).WeaponDamage)+"\n"+
                    CompareStat("BASE RELOAD TIME",oldReload,reload,"s")+"\n"+
                    CompareStat("RELOAD SPEED",oldCombat.reloadSpeed+game.ReloadSpeedBonus,combat.reloadSpeed+game.ReloadSpeedBonus,"%")+"\n"+
                    CompareStat("BATTLE RELOAD",WeaponCombatStats.CalculateReloadTime(oldReload,oldCombat.reloadSpeed+game.ReloadSpeedBonus),WeaponCombatStats.CalculateReloadTime(reload,combat.reloadSpeed+game.ReloadSpeedBonus),"s")+
                    "\n"+CompareStat("RANGE",oldRange,range)+" | "+CompareStat("AMMO",oldAmmo,ammo)+
                    "\nHIT "+combat.accuracy.ToString("0.#")+"% · PEN "+combat.armorPenetration.ToString("0.#")+"%\nCRIT "+combat.criticalChance.ToString("0.#")+"% / +"+combat.criticalDamage.ToString("0.#")+"% · "+combat.damageType.ToString().ToUpperInvariant();
            }
            if (selected is GunDefinition missile && missile.stats.homingMissile)
                detail += "\n"+(missile.stats.preferredTarget==EnemyKind.Ground?"AT · GROUND ONLY":"AA · AIR ONLY")+" | TURN "+missile.stats.missileTurnRate.ToString("0.#")+"°/s";
            v.Get<Text>("Item Stats").fontSize=selected is GunDefinition guided && guided.stats.homingMissile?16:18;
            if(selected is ArmorDefinition plate) detail=ArmorDetail(plate);
            if(selected is AccessoryDefinition accessory)detail=AccessoryDetail(accessory);
            Text(v,"Item Stats",detail);var slot=activeSlot??selected.Type;bool compatible=selected.Type==slot;
            int lockedSlot=selected.Type==EquipmentType.MachineGun?0:selected.Type==EquipmentType.Cannon?1:selected.Type==EquipmentType.Mortar?2:-1;bool unlocked=lockedSlot<0||new PlayerTankProfile(HtmlBattleConfig.Load()).Level>=HtmlBattleConfig.Load().unlockLevels[lockedSlot];compatible&=unlocked;
            Text(v,"Equip Item/Label",!unlocked?"TANK LEVEL LOCKED":compatible?"EQUIP":"INCOMPATIBLE");v.Get<Button>("Equip Item").interactable=compatible;
            Click(v,"Equip Item",()=>{if(unlocked&&game.Equipment.Equip(selected,slot,accessoryIndex))RefreshEquipmentArtwork();DrawEquipment();});
        }
        static string CompareStat(string label,float before,float after,string unit="")=>label+"  "+before.ToString("0.##")+unit+" → "+after.ToString("0.##")+unit+" ("+(after-before).ToString("+0.##;-0.##;0")+unit+")";
    }
}

