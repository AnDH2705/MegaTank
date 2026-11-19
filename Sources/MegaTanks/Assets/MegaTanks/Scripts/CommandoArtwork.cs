using Spine.Unity;
using UnityEngine;
namespace MegaTanks
{
    public sealed class CommandoArtwork : MonoBehaviour
    {
        [SerializeField] SkeletonAnimation animation;
        [SerializeField] string attackAnimation="shovel_attack";
        System.Action pendingImpact;
        Spine.TrackEntry attackEntry;
        public bool IsAttacking=>animation.AnimationState.GetCurrent(0) is Spine.TrackEntry entry&&entry.Animation.Name==attackAnimation&&!entry.IsComplete;
        public SkeletonAnimation Animation=>animation;
        public string CurrentAnimation=>animation.AnimationState.GetCurrent(0)?.Animation.Name;
        public void Configure(SkeletonAnimation visual,string attack="shovel_attack"){animation=visual;attackAnimation=attack;ResetVisual();}
        public void ResetVisual()
        {
            CancelAttack();
            animation.Initialize(false);animation.UpdateTiming=UpdateTiming.ManualUpdate;
            animation.AnimationState.ClearTracks();animation.Skeleton.SetToSetupPose();animation.Skeleton.ScaleX=1;
            animation.AnimationState.SetAnimation(0,"idle",true);Advance(0);
        }
        public void Face(float direction){if(Mathf.Abs(direction)>.0001f)animation.Skeleton.ScaleX=direction<0?-1:1;}
        public void Locomotion(bool moving)
        {
            var current=animation.AnimationState.GetCurrent(0);
            if(current!=null&&current.Animation.Name==attackAnimation&&!current.IsComplete)return;
            string next=moving?"walk":"idle";
            if(current==null||current.Animation.Name!=next)animation.AnimationState.SetAnimation(0,next,true);
        }
        public void CancelAttack(){pendingImpact=null;attackEntry=null;}
        void OnDisable()=>CancelAttack();
        void Impact(Spine.TrackEntry entry)
        {
            if(entry!=attackEntry)return;
            var callback=pendingImpact;pendingImpact=null;callback?.Invoke();
        }
        public void Attack(System.Action onImpact=null)
        {
            CancelAttack();
            attackEntry=animation.AnimationState.SetAnimation(0,attackAnimation,false);pendingImpact=onImpact;
            attackEntry.Event+=(entry,evt)=>{if(evt.Data.Name.IndexOf("hit",System.StringComparison.OrdinalIgnoreCase)>=0)Impact(entry);};
            // A skeleton without hit events still resolves a single impact at the end of its swing.
            attackEntry.Complete+=Impact;
            attackEntry.Interrupt+=entry=>{if(entry==attackEntry)CancelAttack();};
            animation.AnimationState.AddAnimation(0,"idle",true,0);
        }
        public float Die()
        {
            CancelAttack();
            var entry=animation.AnimationState.SetAnimation(0,"death",false);
            return entry.Animation.Duration;
        }
        public void Advance(float seconds){animation.Update(Mathf.Max(0,seconds));animation.LateUpdate();}
    }
}
