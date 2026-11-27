using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
namespace MegaTanks
{
    // Stable member IDs keep assignments independent of asset filenames and list ordering.
    public sealed class PlayerCrew
    {
        [Serializable] sealed class SaveData { public string[] memberIds; }
        [Serializable] sealed class Recruits { public int version=1;public List<string> memberIds=new List<string>(),commonCards=new List<string>();public List<PendingRecruit> pending=new List<PendingRecruit>(); }
        [Serializable] public sealed class PendingRecruit { public string crewId,runId;public int stageIndex; }
        readonly List<string> commonCards=new List<string>();readonly List<PendingRecruit> pending=new List<PendingRecruit>();
        public const string RecruitmentSaveKey="MegaTanks.Crew.Recruitment.v1";
        readonly string recruitmentKey;
        readonly HashSet<string> recruited=new HashSet<string>();
        readonly CrewCatalog catalog;
        readonly CrewDefinition[] slots;
        readonly string saveKey;
        public int SlotCount=>slots.Length;
        public int AssignedCount=>Enumerable.Range(0,slots.Length).Count(i=>Get(i)!=null);
        public PlayerCrew(CrewCatalog available,string preferenceKey="MegaTanks.Crew.Loadout")
        {
            catalog=available;saveKey=preferenceKey;slots=new CrewDefinition[catalog!=null?5:0];
            recruitmentKey=preferenceKey=="MegaTanks.Crew.Loadout"?RecruitmentSaveKey:preferenceKey+".Recruitment.v1";
            var rescue=RescueEventConfig.Load();
            foreach(var member in catalog?.members??Array.Empty<CrewDefinition>())if(member!=null&&rescue.ForMember(member.id)==null)recruited.Add(member.id);
            try{var data=JsonUtility.FromJson<Recruits>(PlayerPrefs.GetString(recruitmentKey,"{}"));foreach(string id in data?.memberIds??new List<string>())recruited.Add(id);commonCards.AddRange(data?.commonCards??new List<string>());pending.AddRange(data?.pending??new List<PendingRecruit>());}catch(ArgumentException){}
            if(slots.Length>0&&!PlayerPrefs.HasKey(saveKey))
            {
                foreach(var role in new[]{CrewRole.Driver,CrewRole.Gunner})slots[(int)role]=catalog?.members?.FirstOrDefault(m=>m!=null&&m.role==role);
                return;
            }
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
        public bool IsRecruited(CrewDefinition member)=>member!=null&&recruited.Contains(member.id);
        public CrewDefinition Member(string id)=>catalog?.members?.FirstOrDefault(m=>m!=null&&m.id==id);
        public bool HasRecruitedRole(CrewRole role)=>(catalog?.members??Array.Empty<CrewDefinition>()).Any(m=>m!=null&&m.role==role&&IsRecruited(m));
        public PendingRecruit Pending=>pending.FirstOrDefault(p=>!IsRecruited(Member(p.crewId)));
        public int CommonCardCount(CrewDefinition member)=>member==null?0:commonCards.Count(id=>id==member.id);
        void SaveRecruits(){PlayerPrefs.SetString(recruitmentKey,JsonUtility.ToJson(new Recruits{memberIds=recruited.OrderBy(id=>id).ToList(),commonCards=commonCards.ToList(),pending=pending.ToList()}));}
        public bool QueueCommonRecruit(CrewDefinition member,string runId,int stageIndex)
        {
            if(member==null||IsRecruited(member)||pending.Any(p=>p.crewId==member.id))return false;
            pending.Add(new PendingRecruit{crewId=member.id,runId=runId,stageIndex=stageIndex});SaveRecruits();PlayerPrefs.Save();return true;
        }
        public bool ClaimCommonRecruit(string crewId)
        {
            var reward=pending.FirstOrDefault(p=>p.crewId==crewId);var member=Member(crewId);if(reward==null||member==null||IsRecruited(member))return false;
            commonCards.Add(member.id);pending.Remove(reward);return RecruitAndAssign(member);
        }
        public CrewDefinition Get(int station)=>station>=0&&station<slots.Length&&IsRecruited(slots[station])?slots[station]:null;
        public string StationName(int station)=>station>=0&&station<slots.Length?CrewDefinition.RoleLabel((CrewRole)station):"";
        public CrewDefinition Get(CrewRole role)=>Get((int)role);
        public event Action Changed;
        public bool CanEquip(CrewDefinition member,int station)=>member!=null&&IsRecruited(member)&&station>=0&&station<slots.Length&&(int)member.role==station&&catalog?.members!=null&&catalog.members.Contains(member)&&!string.IsNullOrWhiteSpace(member.id);
        public int FindSlot(CrewDefinition member)=>!IsRecruited(member)?-1:Array.IndexOf(slots,member);
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
        public bool RecruitAndAssign(CrewDefinition member)
        {
            if(member==null||catalog?.members==null||!catalog.members.Contains(member)||IsRecruited(member))return false;
            recruited.Add(member.id);if(Get(member.role)==null)slots[(int)member.role]=member;
            SaveRecruits();
            Save();Changed?.Invoke();return true;
        }
        void Save(){PlayerPrefs.SetString(saveKey,JsonUtility.ToJson(new SaveData{memberIds=slots.Select(m=>m!=null?m.id:"").ToArray()}));PlayerPrefs.Save();}
    }
}
