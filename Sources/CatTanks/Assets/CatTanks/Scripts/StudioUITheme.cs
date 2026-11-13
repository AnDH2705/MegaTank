using System;
using System.Collections.Generic;
using UnityEngine;

namespace CatTanks
{
    [CreateAssetMenu(menuName = "CatTanks/Studio UI Theme")]
    public sealed class StudioUITheme : ScriptableObject
    {
        [Serializable] public struct Asset { public string key; public Sprite sprite; }
        public List<Asset> assets = new List<Asset>();
        Dictionary<string, Sprite> lookup;
        public Sprite Get(string key)
        {
            if (lookup == null)
            {
                lookup = new Dictionary<string, Sprite>();
                foreach (var asset in assets) lookup[asset.key] = asset.sprite;
            }
            return lookup.TryGetValue(key, out var sprite) ? sprite : null;
        }
    }
}
