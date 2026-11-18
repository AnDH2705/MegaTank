using UnityEngine;

namespace MegaTanks
{
    public sealed class TrackAnimationData : ScriptableObject
    {
        public Spine.Unity.SkeletonDataAsset skeletonData;
        public string sourceVersion, sourceAnimation;
        public float sourceDuration;
        public Sprite belt, chainPin;
        public Vector2[] path;
        public float[] distances;
        public float[] linkOffsets;
        public Vector2 linkOffset, linkScale;
        public float linkRotation;
        public float Length => distances[distances.Length - 1];
    }
}
