using System;
using System.Collections.Generic;
using UnityEngine;

namespace MegaTanks
{
    // Tank progression has its own save key; crew levels and run upgrades never write here.
    public sealed class PlayerTankProfile
    {
        public const string SaveKey="MegaTanks.TankProfile.v1";
        [Serializable] sealed class Data { public int version=1,totalXP;public string avatarId="driver";public List<string> awards=new List<string>(); }
        readonly string key;readonly HtmlBattleConfig config;Data data;
        public PlayerTankProfile(HtmlBattleConfig config,string saveKey=SaveKey)
        {
            this.config=config;key=saveKey;
            try{data=JsonUtility.FromJson<Data>(PlayerPrefs.GetString(key,"{}"));}catch(ArgumentException){}
            if(data==null)data=new Data();if(data.awards==null)data.awards=new List<string>();data.totalXP=Mathf.Max(0,data.totalXP);
        }
        public int TotalXP=>data.totalXP;
        public string AvatarId=>string.IsNullOrEmpty(data.avatarId)?"driver":data.avatarId;
        public int Level { get { int level=1,xp=data.totalXP;while(level<config.maxTankLevel&&xp>=Cost(level)){xp-=Cost(level);level++;}return level; } }
        public int IntoLevel { get { int xp=data.totalXP;for(int l=1;l<Level;l++)xp-=Cost(l);return xp;} }
        public int Cost(int level)=>(int)Math.Min(int.MaxValue,Math.Ceiling(config.tankXPBase*Math.Pow(config.tankXPGrowth,Math.Max(0,level-1))));
        public bool Award(string runId,int xp)
        {
            if(string.IsNullOrEmpty(runId)||xp<=0||data.awards.Contains(runId))return false;
            data.awards.Add(runId);data.totalXP=(int)Math.Min(int.MaxValue,(long)data.totalXP+xp);Save();return true;
        }
        public void SetAvatar(string id){if(string.IsNullOrWhiteSpace(id))return;data.avatarId=id;Save();}
        void Save(){PlayerPrefs.SetString(key,JsonUtility.ToJson(data));PlayerPrefs.Save();}
    }
    public readonly struct ResolvedCrewStats
    {
        public readonly float attack,hp,accuracy,defense,speed,crit;
        public ResolvedCrewStats(CrewDefinition member,ProgressionState p,HtmlBattleConfig config)
        {
            var s=member.stats;
            attack=s.damage.value>0?s.damage.AtLevel(p.Level,p.Stars):config.meleeDamage;
            hp=s.hp.value>0?s.hp.AtLevel(p.Level,p.Stars):config.meleeHP;
            accuracy=s.accuracy.value>0?s.accuracy.AtLevel(p.Level,p.Stars):config.crewDefaultAccuracy;
            defense=s.armor.AtLevel(p.Level,p.Stars);speed=s.speed.value>0?s.speed.AtLevel(p.Level,p.Stars):config.meleeSpeed;
            crit=s.criticalChance.AtLevel(p.Level,p.Stars);
        }
    }
}
