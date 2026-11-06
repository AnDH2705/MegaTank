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
        public ArmorDefinition Armor { get; private set; }
        public readonly AccessoryDefinition[] Accessories = new AccessoryDefinition[2];
        readonly FrontendCatalog catalog;
        public PlayerEquipment(WeaponLoadout defaults, FrontendCatalog available)
        {
            catalog = available; MachineGun = defaults.machineGun; Cannon = defaults.cannon; Mortar = defaults.mortar;
            if (catalog == null || catalog.inventory == null) return;
            foreach (var item in catalog.inventory.Where(i => i != null))
            {
                if (PlayerPrefs.GetString("MegaTanks.Equipment." + item.Type, "") != item.name) continue;
                if (item is GunDefinition gun) { if (gun.Type == EquipmentType.MachineGun) MachineGun = gun; else Cannon = gun; }
                if (item is ArmorDefinition armor) Armor = armor;
                if (item is MortarDefinition mortar) Mortar = mortar;
            }
            for (int i = 0; i < Accessories.Length; i++)
                // Keep the saved identities of the remaining slots 2 and 3; former slot 1 is retired.
                Accessories[i] = catalog.inventory.OfType<AccessoryDefinition>().FirstOrDefault(a => a.name == PlayerPrefs.GetString("MegaTanks.Accessory." + (i + 1), ""));
        }
        public EquipmentDefinition GetEquipped(EquipmentType slot, int accessoryIndex = 0)
        {
            switch(slot)
            {
                case EquipmentType.MachineGun: return MachineGun;
                case EquipmentType.Cannon: return Cannon;
                case EquipmentType.Mortar: return Mortar;
                case EquipmentType.Armor: return Armor;
                case EquipmentType.Accessory: return accessoryIndex >= 0 && accessoryIndex < Accessories.Length ? Accessories[accessoryIndex] : null;
                default: return null;
            }
        }
        public bool IsEquipped(EquipmentDefinition item) => item != null &&
            (item == MachineGun || item == Cannon || item == Mortar || item == Armor || Accessories.Contains(item));
        public bool Equip(EquipmentDefinition item, EquipmentType slot, int accessoryIndex = 0)
        {
            if (item == null || item.Type != slot || catalog == null || !catalog.inventory.Contains(item)) return false;
            if (item is GunDefinition gun) { if (slot == EquipmentType.MachineGun) MachineGun = gun; else Cannon = gun; }
            else if (item is ArmorDefinition armor) Armor = armor;
            else if (item is MortarDefinition mortar) Mortar = mortar;
            else if (item is AccessoryDefinition accessory)
            {
                if (accessoryIndex < 0 || accessoryIndex >= Accessories.Length) return false;
                Accessories[accessoryIndex] = accessory;
                PlayerPrefs.SetString("MegaTanks.Accessory." + (accessoryIndex + 1), item.name); PlayerPrefs.Save(); return true;
            }
            else return false;
            PlayerPrefs.SetString("MegaTanks.Equipment." + slot, item.name); PlayerPrefs.Save(); return true;
        }
    }
}
