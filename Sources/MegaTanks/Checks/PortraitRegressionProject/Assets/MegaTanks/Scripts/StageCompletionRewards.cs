using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MegaTanks
{
    [Serializable]
    public struct CurrencyRewards
    {
        public int parts, petFood, gems;
        public CurrencyRewards(int parts, int petFood, int gems = 0) { this.parts=parts;this.petFood=petFood;this.gems=gems; }
        public bool Valid => parts >= 0 && petFood >= 0 && gems >= 0;
        public static CurrencyRewards operator +(CurrencyRewards a,CurrencyRewards b) =>
            new CurrencyRewards(checked(a.parts+b.parts),checked(a.petFood+b.petFood),checked(a.gems+b.gems));
    }
    [Serializable]
    public sealed class StageRewardReceipt
    {
        public string id;
        public int stageNumber;
        public bool firstClear, paid;
        public CurrencyRewards repeat, bonus;
        public CurrencyRewards Total => repeat + bonus;
    }
    // Saved entitlements and first-clear flags are independent of transient run cards/scraps.
    public sealed class StageCompletionRewards : IDisposable
    {
        public const string DefaultSaveKey="MegaTanks.StageCompletionRewards";
        [Serializable] sealed class SaveData
        {
            public int version=1;
            public List<int> clearedStages=new List<int>();
            public List<StageRewardReceipt> pending=new List<StageRewardReceipt>();
        }
        readonly HashSet<int> cleared=new HashSet<int>();
        readonly List<StageRewardReceipt> pending=new List<StageRewardReceipt>();
        readonly PlayerWallet wallet;readonly string saveKey;
        int currentStage;
        CurrencyRewards repeat,bonus;
        bool active,collecting;
        public StageRewardReceipt LastReceipt { get; private set; }
        public bool HasCleared(int stageNumber)=>cleared.Contains(stageNumber);
        public int PendingCount=>pending.Count;
        public StageCompletionRewards(PlayerWallet wallet,string preferenceKey=DefaultSaveKey)
        {
            this.wallet=wallet??throw new ArgumentNullException(nameof(wallet));
            if(string.IsNullOrWhiteSpace(preferenceKey))throw new ArgumentException("A save key is required.");
            saveKey=preferenceKey;SaveData data=null;
            try{data=JsonUtility.FromJson<SaveData>(PlayerPrefs.GetString(saveKey,"{}"));}catch(ArgumentException){}
            foreach(int number in data?.clearedStages??new List<int>())if(number>0)cleared.Add(number);
            var ids=new HashSet<string>();
            foreach(var receipt in data?.pending??new List<StageRewardReceipt>())
                if(receipt!=null&&!receipt.paid&&receipt.stageNumber>0&&!string.IsNullOrWhiteSpace(receipt.id)&&receipt.repeat.Valid&&receipt.bonus.Valid&&ids.Add(receipt.id))
                {pending.Add(receipt);cleared.Add(receipt.stageNumber);}
            wallet.Changed+=CollectPending;CollectPending();
        }
        public void BeginRun(StageDefinition stage)
        {
            if(stage==null)throw new ArgumentNullException(nameof(stage));
            currentStage=Math.Max(1,stage.stageNumber);
            repeat=stage.RepeatCompletionRewards;bonus=stage.FirstCompletionRewards;
            LastReceipt=null;active=true;
        }
        public StageRewardReceipt CompleteRun()
        {
            if(!active||LastReceipt!=null)return LastReceipt;
            bool first=cleared.Add(currentStage);
            LastReceipt=new StageRewardReceipt{id=Guid.NewGuid().ToString("N"),stageNumber=currentStage,firstClear=first,repeat=repeat,bonus=first?bonus:default};
            pending.Add(LastReceipt);
            // Save the entitlement before trying to credit it. A full wallet or restart cannot lose it.
            SavePending();PlayerPrefs.Save();CollectPending();return LastReceipt;
        }
        public void EndRun(){active=false;}
        public void CollectPending()
        {
            if(collecting)return;collecting=true;
            try
            {
                foreach(var receipt in pending.ToArray())
                    wallet.TryCreditRewards(receipt.Total,()=>{
                        receipt.paid=true;pending.Remove(receipt);SavePending();
                    });
            }
            finally{collecting=false;}
        }
        void SavePending()=>PlayerPrefs.SetString(saveKey,JsonUtility.ToJson(new SaveData{clearedStages=cleared.OrderBy(n=>n).ToList(),pending=pending.ToList()}));
        public void Dispose()=>wallet.Changed-=CollectPending;
    }
}
