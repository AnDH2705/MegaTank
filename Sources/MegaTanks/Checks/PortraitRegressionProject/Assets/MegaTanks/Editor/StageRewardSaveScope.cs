using System;
using System.Collections.Generic;
using UnityEngine;
namespace MegaTanks.Editor
{
    // Completing a stage in an editor audit must not spend the player's first-clear eligibility.
    internal sealed class StageRewardSaveScope : IDisposable
    {
        readonly Dictionary<string,string> values=new Dictionary<string,string>();
        readonly HashSet<string> existed=new HashSet<string>();
        public StageRewardSaveScope()
        {
            foreach(string key in new[]{PlayerWallet.DefaultSaveKey,PlayerProgression.DefaultSaveKey,StageCompletionRewards.DefaultSaveKey,PlaytestResources.SaveKey,PlaytestResources.ArmorSaveKey,PlayerCrew.RecruitmentSaveKey,"MegaTanks.Crew.Loadout"})
            {if(PlayerPrefs.HasKey(key))existed.Add(key);values[key]=PlayerPrefs.GetString(key,"");}
        }
        public void Dispose()
        {
            foreach(var pair in values){if(existed.Contains(pair.Key))PlayerPrefs.SetString(pair.Key,pair.Value);else PlayerPrefs.DeleteKey(pair.Key);}
            PlayerPrefs.Save();
        }
    }
}
