using Spine.Unity;
using UnityEngine;
namespace MegaTanks
{
    public sealed class CommandoArtwork : MonoBehaviour
    {
        [SerializeField] SkeletonAnimation animation;
        [SerializeField] string attackAnimation="shovel_attack";
        public SkeletonAnimation Animation=>animation;
        public string CurrentAnimation=>animation.AnimationState.GetCurrent(0)?.Animation.Name;
        public void Configure(SkeletonAnimation visual,string attack="shovel_attack"){animation=visual;attackAnimation=attack;ResetVisual();}
        public void ResetVisual()
        {
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
        public void Attack()
        {
            animation.AnimationState.SetAnimation(0,attackAnimation,false);
            animation.AnimationState.AddAnimation(0,"idle",true,0);
        }
        public float Die()
        {
            var entry=animation.AnimationState.SetAnimation(0,"death",false);
            return entry.Animation.Duration;
        }
        public void Advance(float seconds){animation.Update(Mathf.Max(0,seconds));animation.LateUpdate();}
    }
}
