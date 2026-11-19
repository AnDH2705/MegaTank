using UnityEngine;

namespace CatTanks
{
    // Sampled by BattleView using simulation time. No independent Update or real-time timer.
    public sealed class MonsterSpriteAnimator : MonoBehaviour
    {
        public MonsterAnimationLibrary library;
        MonsterAnimationLibrary.Entry entry;
        SpriteRenderer spriteRenderer;
        Vector2 previousPosition;
        float previousAge=-1,previousAttack,previousFlash,attackStarted=-100,hitStarted=-100,runStarted;
        bool wasMoving;
        public string Pose{get;private set;}
        public int Frame{get;private set;}
        public bool Sample(Actor actor)
        {
            var current=library?library.Find(actor.asset):null;if(current==null)return false;
            if(current!=entry){entry=current;previousAge=-1;attackStarted=hitStarted=-100;wasMoving=false;}
            if(!spriteRenderer)spriteRenderer=GetComponent<SpriteRenderer>();
            float scale=actor.def.size*entry.cellWidth/Mathf.Max(1,entry.referenceWidth);
            transform.localScale=Vector3.one*scale;transform.localPosition=new Vector3(0,-actor.def.size*.43f,0);transform.localRotation=Quaternion.identity;
            if(Mathf.Approximately(actor.age,previousAge))return true;
            bool moving=previousAge<0?actor.position.x>700:(actor.position-previousPosition).sqrMagnitude>.0001f;
            if(actor.stun>0||actor.dead)moving=false;
            if(actor.flash>previousFlash+.025f)hitStarted=actor.age;
            if(previousAge>=0&&actor.attack>previousAttack+.05f)attackStarted=actor.age;
            if(moving&&!wasMoving)runStarted=actor.age;
            Sprite[] frames;float duration,elapsed;bool loop;
            if(actor.dead||actor.age-hitStarted<entry.hit.Length*entry.hitTime)
            {Pose="hit";frames=entry.hit;duration=entry.hitTime;elapsed=actor.dead?frames.Length*duration:actor.age-hitStarted;loop=false;}
            else if(actor.age-attackStarted<entry.attack.Length*entry.attackTime)
            {Pose="attack";frames=entry.attack;duration=entry.attackTime;elapsed=actor.age-attackStarted;loop=false;}
            else if(moving&&entry.enterRun.Length>0&&actor.age-runStarted<entry.enterRun.Length*entry.enterTime)
            {Pose="enter-run";frames=entry.enterRun;duration=entry.enterTime;elapsed=actor.age-runStarted;loop=false;}
            else if(moving)
            {Pose="run";frames=entry.run;duration=entry.runTime;elapsed=actor.age-runStarted-entry.enterRun.Length*entry.enterTime;loop=true;}
            else
            {Pose="idle";frames=entry.idle;duration=entry.idleTime;elapsed=actor.age+actor.phase*.1f;loop=true;}
            if(frames==null||frames.Length==0)return false;
            int frame=Mathf.Max(0,Mathf.FloorToInt(elapsed/Mathf.Max(.001f,duration)));
            Frame=loop?frame%frames.Length:Mathf.Min(frames.Length-1,frame);spriteRenderer.sprite=frames[Frame];
            previousPosition=actor.position;previousAge=actor.age;previousAttack=actor.attack;previousFlash=actor.flash;wasMoving=moving;
            return true;
        }
    }
}
