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
        Dictionary<string,Entry> lookup;
        int lookupCount=-1;
        public void InvalidateLookup(){lookup=null;}
        void OnEnable()=>InvalidateLookup();
        void OnValidate()=>InvalidateLookup();
        public Entry Find(string key)
        {
            if(key==null)return null;
            if(lookup==null||lookupCount!=entries.Count)
            {
                lookup=new Dictionary<string,Entry>();lookupCount=entries.Count;
                foreach(var entry in entries)if(entry!=null&&entry.key!=null&&!lookup.ContainsKey(entry.key))lookup.Add(entry.key,entry);
            }
            return lookup.TryGetValue(key,out var value)?value:null;
        }
        public Entry For(BattleEffect effect)
        {
            string key=effect.visual;
            if(string.IsNullOrEmpty(key))
                key=effect.kind=="flash"?"muzzle":effect.kind=="explosion"?"death":effect.kind=="lightning"?"electric":effect.kind=="impact"?"impact":effect.kind=="ring"?"buff":null;
            return key==null?null:Find(key)??Find("impact");
        }
    }
}
