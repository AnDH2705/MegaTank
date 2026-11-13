using System;
using System.Collections.Generic;
using UnityEngine;

namespace CatTanks
{
    [CreateAssetMenu(menuName="CatTanks/Combat VFX Library")]
    public sealed class CombatVfxLibrary:ScriptableObject
    {
        [Serializable] public sealed class Entry
        {
            public string key,source;
            public GameObject prefab;
            public float duration=.6f,sizeMultiplier=2;
            public int maxInstances=8;
        }
        public int maxActive=48;
        public List<Entry> entries=new List<Entry>();
        public Entry Find(string key)=>entries.Find(e=>e.key==key);
        public Entry For(BattleEffect effect)
        {
            string key=effect.visual;
            if(string.IsNullOrEmpty(key))
                key=effect.kind=="flash"?"muzzle":effect.kind=="explosion"?"death":effect.kind=="lightning"?"electric":effect.kind=="impact"?"impact":effect.kind=="ring"?"buff":null;
            return key==null?null:Find(key)??Find("impact");
        }
    }
}
