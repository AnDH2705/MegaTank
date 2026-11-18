using System;
using UnityEngine;

namespace MegaTanks
{
    [Serializable]
    public sealed class BattleBackgroundLayer
    {
        public Sprite sprite;
        [Min(0)] public float parallaxFactor;
        public bool repeatHorizontally = true;
    }
    [Serializable]
    public sealed class BattleBackgroundPreset
    {
        public string id;
        public BattleBackgroundLayer[] layers;
    }
    [CreateAssetMenu(menuName = "MegaTanks/Battle Background Catalog")]
    public sealed class BattleBackgroundCatalog : ScriptableObject
    {
        [Range(.1f,1f), Tooltip("Scenery size relative to the full-screen backdrop. Sky remains full-screen.")]
        public float sceneryScale=.5f;
        public BattleBackgroundPreset[] presets;
    }
}
