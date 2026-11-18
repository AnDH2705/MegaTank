using System;
using System.Linq;
using UnityEngine;
namespace MegaTanks
{
    // Stable member IDs keep assignments independent of asset filenames and list ordering.
    public sealed class PlayerCrew
    {
        [Serializable] sealed class SaveData { public string[] memberIds; }
        readonly CrewCatalog catalog;
        readonly CrewDefinition[] slots;
        readonly string saveKey;
        public int SlotCount=>slots.Length;
        public int AssignedCount=>slots.Count(member=>member!=null);
        public PlayerCrew(CrewCatalog available,string preferenceKey="MegaTanks.Crew.Loadout")
        {
            catalog=available;saveKey=preferenceKey;slots=new CrewDefinition[catalog!=null?5:0];
            string json=PlayerPrefs.GetString(saveKey,"");if(string.IsNullOrEmpty(json))return;
            SaveData saved;try{saved=JsonUtility.FromJson<SaveData>(json);}catch(ArgumentException){return;}
            if(saved?.memberIds==null||catalog?.members==null)return;
            for(int i=0;i<Math.Min(slots.Length,saved.memberIds.Length);i++)
            {
                var member=catalog.members.FirstOrDefault(m=>m!=null&&m.id==saved.memberIds[i]);
                // Recover legacy assignments by role, including formerly unrestricted station placements.
                if(member!=null&&!IsEquipped(member)) { int target=(int)member.role;if(target>=0&&target<slots.Length&&slots[target]==null)slots[target]=member; }
            }
        }
        public CrewDefinition Get(int station)=>station>=0&&station<slots.Length?slots[station]:null;
        public string StationName(int station)=>station>=0&&station<slots.Length?CrewDefinition.RoleLabel((CrewRole)station):"";
        public CrewDefinition Get(CrewRole role)=>Get((int)role);
        public event Action Changed;
        public bool CanEquip(CrewDefinition member,int station)=>member!=null&&station>=0&&station<slots.Length&&(int)member.role==station&&catalog?.members!=null&&catalog.members.Contains(member)&&!string.IsNullOrWhiteSpace(member.id);
        public int FindSlot(CrewDefinition member)=>member==null?-1:Array.IndexOf(slots,member);
        public bool IsEquipped(CrewDefinition member)=>FindSlot(member)>=0;
        public bool Equip(CrewDefinition member,int station)
        {
            if(!CanEquip(member,station))return false;
            int previous=FindSlot(member);if(previous==station)return true;
            if(previous>=0)slots[previous]=null;
            slots[station]=member;Save();Changed?.Invoke();return true;
        }
        public bool Unequip(int station)
        {
            if(station<0||station>=slots.Length||slots[station]==null)return false;
            slots[station]=null;Save();Changed?.Invoke();return true;
        }
        void Save(){PlayerPrefs.SetString(saveKey,JsonUtility.ToJson(new SaveData{memberIds=slots.Select(m=>m!=null?m.id:"").ToArray()}));PlayerPrefs.Save();}
    }
}
