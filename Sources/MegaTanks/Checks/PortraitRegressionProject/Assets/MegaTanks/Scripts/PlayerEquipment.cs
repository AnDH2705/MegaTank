using System;
using System.Linq;
using UnityEngine;

namespace MegaTanks
{
    // Loadout choices are independent of mutable battle ammo/upgrades and saved definition assets.
    public sealed class PlayerEquipment
    {
        public GunDefinition MachineGun { get; private set; }
        public GunDefinition Cannon { get; private set; }
        public MortarDefinition Mortar { get; private set; }
        public GunDefinition MissilePack { get; private set; }
        public WheelsDefinition Wheels { get; private set; }
        public WeaponDefinition MortarSlot => MissilePack != null ? (WeaponDefinition)MissilePack : Mortar;
        public ArmorDefinition Armor { get; private set; }
        public ArmorDefinition SecondArmor { get; private set; }
        public int ArmorSlotCount { get; private set; } = 1;
        public int AccessorySlotCount => 3-ArmorSlotCount;
        public readonly AccessoryDefinition[] Accessories = new AccessoryDefinition[2];
        readonly FrontendCatalog catalog;
        public PlayerEquipment(WeaponLoadout defaults, FrontendCatalog available)
        {
            catalog = available; MachineGun = defaults.machineGun; Cannon = defaults.cannon; Mortar = defaults.mortar;
            if (catalog == null || catalog.inventory == null) return;
            bool migrated = false;
            string oldCannon=PlayerPrefs.GetString("MegaTanks.Equipment.Cannon","");
            var oldMissile=catalog.inventory.OfType<GunDefinition>().FirstOrDefault(g=>g.stats.homingMissile&&(g.id==oldCannon||g.name==oldCannon));
            if(oldMissile!=null)
            {
                if(!PlayerPrefs.HasKey("MegaTanks.Equipment.Mortar"))PlayerPrefs.SetString("MegaTanks.Equipment.Mortar",oldMissile.id);
                PlayerPrefs.DeleteKey("MegaTanks.Equipment.Cannon");migrated=true;
            }
            foreach (var item in catalog.inventory.Where(i => i != null))
            {
                string savedId = PlayerPrefs.GetString("MegaTanks.Equipment." + item.Type, "");
                if (savedId != (item is WeaponDefinition w ? w.id : item is ArmorDefinition a ? a.id : item.name) && savedId != item.name) continue;
                if (item is WeaponDefinition legacy && savedId == item.name) { PlayerPrefs.SetString("MegaTanks.Equipment." + item.Type, legacy.id); migrated = true; }
                if (item is ArmorDefinition legacyArmor && savedId == item.name) { PlayerPrefs.SetString("MegaTanks.Equipment." + item.Type, legacyArmor.id); migrated = true; }
                if (item is GunDefinition gun) { if (gun.Type == EquipmentType.Mortar) MissilePack=gun; else if (gun.Type == EquipmentType.MachineGun) MachineGun = gun; else Cannon = gun; }
                if (item is ArmorDefinition armor) Armor = armor;
                if (item is MortarDefinition mortar) Mortar = mortar;
            }
            Wheels=catalog.inventory.OfType<WheelsDefinition>().FirstOrDefault(w=>w.id==PlayerPrefs.GetString("MegaTanks.Equipment.Wheels",""));
            for (int i = 0; i < Accessories.Length; i++)
            {
                // Keep the saved identities of the remaining slots 2 and 3; former slot 1 is retired.
                string key="MegaTanks.Accessory."+(i+1),savedId=PlayerPrefs.GetString(key,"");
                Accessories[i]=catalog.inventory.OfType<AccessoryDefinition>().FirstOrDefault(a=>a.id==savedId||a.name==savedId);
                if(Accessories[i]!=null&&savedId!=Accessories[i].id){PlayerPrefs.SetString(key,Accessories[i].id);migrated=true;}
            }
            if(Accessories[1]!=null&&Accessories[1]==Accessories[0]){Accessories[1]=null;PlayerPrefs.SetString("MegaTanks.Accessory.2","");migrated=true;}
            string secondId=PlayerPrefs.GetString("MegaTanks.Equipment.Armor.1","");
            SecondArmor=catalog.inventory.OfType<ArmorDefinition>().FirstOrDefault(a=>a.id==secondId||a.name==secondId);
            if(SecondArmor==Armor)SecondArmor=null;
            if (migrated) PlayerPrefs.Save();
        }
        public void ConfigureTank(TankDefinition tank)
        {
            ArmorSlotCount=tank!=null?tank.ArmorSlots:1;
            if(Wheels==null)Wheels=catalog?.inventory?.OfType<WheelsDefinition>().FirstOrDefault(w=>w.skin=="standard");
        }
        public float ReloadSpeedBonus(PlayerProgression progression,AccessoryDefinition replacement=null,int slot=-1)
            =>AccessoryBonus(AccessoryBonusType.ReloadSpeed,progression,replacement,slot);
        public float AccessoryBonus(AccessoryBonusType type,PlayerProgression progression,AccessoryDefinition replacement=null,int slot=-1)
        {
            float total=0;
            for(int i=0;i<AccessorySlotCount;i++)
            {
                var accessory=Accessories[i];
                if(slot>=0&&slot<AccessorySlotCount)
                {
                    if(i==slot)accessory=replacement;
                    else if(replacement!=null&&accessory==replacement)accessory=Accessories[slot];
                }
                if(accessory!=null&&accessory.bonusType==type)total+=accessory.Bonus(progression!=null?progression.Get(accessory).Level:1);
            }
            return total;
        }
        public EquipmentDefinition GetEquipped(EquipmentType slot, int accessoryIndex = 0)
        {
            switch(slot)
            {
                case EquipmentType.MachineGun: return MachineGun;
                case EquipmentType.Cannon: return Cannon;
                case EquipmentType.Mortar: return MortarSlot;
                case EquipmentType.Wheels: return Wheels;
                case EquipmentType.Armor: return accessoryIndex==0?Armor:accessoryIndex==1&&ArmorSlotCount>1?SecondArmor:null;
                case EquipmentType.Accessory: return accessoryIndex >= 0 && accessoryIndex < AccessorySlotCount ? Accessories[accessoryIndex] : null;
                default: return null;
            }
        }
        public bool IsEquipped(EquipmentDefinition item) => item != null &&
            (item == MachineGun || item == Cannon || item == MortarSlot || item == Wheels || item == Armor || (ArmorSlotCount>1&&item==SecondArmor) || Accessories.Take(AccessorySlotCount).Contains(item));
        public bool Equip(EquipmentDefinition item, EquipmentType slot, int accessoryIndex = 0)
        {
            if (item == null || item.Type != slot || catalog == null || !catalog.inventory.Contains(item)) return false;
            if (item is GunDefinition gun) { if (slot == EquipmentType.Mortar) MissilePack=gun; else if (slot == EquipmentType.MachineGun) MachineGun = gun; else Cannon = gun; }
            else if (item is ArmorDefinition armor)
            {
                if(accessoryIndex<0||accessoryIndex>=ArmorSlotCount)return false;
                // Moving an equipped plate between armor slots swaps it instead of duplicating it.
                if(accessoryIndex==1){if(Armor==armor)Armor=SecondArmor;SecondArmor=armor;}
                else {if(SecondArmor==armor)SecondArmor=Armor;Armor=armor;}
                PlayerPrefs.SetString("MegaTanks.Equipment.Armor",Armor!=null?Armor.id:"");
                PlayerPrefs.SetString("MegaTanks.Equipment.Armor.1",SecondArmor!=null?SecondArmor.id:"");
                PlayerPrefs.Save();return true;
            }
            else if (item is MortarDefinition mortar) { Mortar = mortar; MissilePack=null; }
            else if (item is WheelsDefinition wheels) { Wheels=wheels;PlayerPrefs.SetString("MegaTanks.Equipment.Wheels",wheels.id);PlayerPrefs.Save();return true; }
            else if (item is AccessoryDefinition accessory)
            {
                if (accessoryIndex < 0 || accessoryIndex >= AccessorySlotCount) return false;
                for(int i=0;i<Accessories.Length;i++)
                    if(i!=accessoryIndex&&Accessories[i]==accessory)Accessories[i]=Accessories[accessoryIndex];
                Accessories[accessoryIndex] = accessory;
                for(int i=0;i<Accessories.Length;i++)PlayerPrefs.SetString("MegaTanks.Accessory."+(i+1),Accessories[i]!=null?Accessories[i].id:"");
                PlayerPrefs.Save(); return true;
            }
            else return false;
            PlayerPrefs.SetString("MegaTanks.Equipment." + slot, item is WeaponDefinition weapon ? weapon.id : item is ArmorDefinition plate ? plate.id : item.name); PlayerPrefs.Save(); return true;
        }
    }
}
