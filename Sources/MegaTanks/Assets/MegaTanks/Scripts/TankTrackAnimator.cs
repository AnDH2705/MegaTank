using Spine.Unity;
using UnityEngine;

namespace MegaTanks
{
    public sealed class TankTrackAnimator : MonoBehaviour
    {
        [SerializeField] TrackAnimationData data;
        [SerializeField] SkeletonAnimation animation;
        float distance;
        public TrackAnimationData Data => data;
        public SkeletonAnimation Animation => animation;
        public float TravelledDistance => distance;
        public int LinkCount => animation.Skeleton.PathConstraints.Items[0].Bones.Count;
        public Vector2 FirstLinkPosition => new Vector2(animation.Skeleton.Bones.Items[1].WorldX, animation.Skeleton.Bones.Items[1].WorldY);
        public void Configure(TrackAnimationData definition, SkeletonAnimation visual)
        { data = definition; animation = visual; ResetVisual(); }
        public void ResetVisual()
        {
            if (animation == null || data == null) return;
            distance = 0;
            animation.Initialize(false);
            animation.UpdateTiming = UpdateTiming.ManualUpdate;
            animation.AnimationState.ClearTracks(); animation.Skeleton.SetToSetupPose();
            animation.AnimationState.SetAnimation(0, "running", true);
            animation.Update(0); animation.LateUpdate();
        }
        void Update()
        {
            var game = BattleGame.Instance;
            if (animation == null || data == null || game == null || !game.Running || Mathf.Abs(game.TankVelocity.x) < .00001f) return;
            float travel = game.TankVelocity.x * Time.deltaTime / Mathf.Max(.001f, Mathf.Abs(transform.lossyScale.x));
            distance = Mathf.Repeat(distance + travel, data.Length);
            // Spine owns the path constraint, bone poses, animation state and mesh generation.
            // Drive its original running loop by terrain travel rather than wall-clock time.
            animation.AnimationState.GetCurrent(0).TrackTime = distance / data.Length * data.sourceDuration;
            animation.Update(0);
        }
    }
}
