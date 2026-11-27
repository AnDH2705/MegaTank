using System;
using UnityEngine;
namespace MegaTanks
{
    [Serializable] public sealed class RescueStoryLine { public string speaker,text; }
    [Serializable] public sealed class RescueStageStory
    {
        public int stageIndex,weaponSlot;public string crewId;public bool removeOpeningMeleeEnemies;
        public RescueStoryLine[] dialogue;
    }
    [Serializable] public sealed class RescueEventConfig
    {
        public int version=1,firstEncounter=0;
        public bool enabled=true,repeatIfSkipped=true;
        public RescueStageStory[] stages=Array.Empty<RescueStageStory>();
        public float stopX=1550,cageSize=300,cageFeetInset=22,prisonerHeight=155,unlockSeconds=.45f,cageLiftSeconds=.6f,walkSpeed=400;
        public static RescueEventConfig Load()
        {
            var file=Resources.Load<TextAsset>("RescueEventConfig");if(file==null)throw new InvalidOperationException("Missing RescueEventConfig.json");
            var c=JsonUtility.FromJson<RescueEventConfig>(file.text);
            if(c.version!=1||c.firstEncounter<0||c.stages==null||c.stages.Length==0||c.cageSize<=0||c.prisonerHeight<=0||c.unlockSeconds<0||c.cageLiftSeconds<=0||c.walkSpeed<=0)throw new InvalidOperationException("Invalid RescueEventConfig.json");
            foreach(var story in c.stages)if(story.stageIndex<0||story.weaponSlot<0||story.weaponSlot>2||string.IsNullOrWhiteSpace(story.crewId)||story.dialogue==null||story.dialogue.Length==0)throw new InvalidOperationException("Invalid rescue stage story.");
            return c;
        }
        public RescueStageStory Stage(int index)=>Array.Find(stages??Array.Empty<RescueStageStory>(),s=>s.stageIndex==index);
        public RescueStageStory ForMember(string id)=>Array.Find(stages??Array.Empty<RescueStageStory>(),s=>s.crewId==id);
    }
}
