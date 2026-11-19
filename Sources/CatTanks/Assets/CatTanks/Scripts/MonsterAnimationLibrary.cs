using System;
using System.Collections.Generic;
using UnityEngine;

namespace CatTanks
{
    public sealed class MonsterAnimationLibrary : ScriptableObject
    {
        [Serializable] public sealed class Entry
        {
            public string species,source;public int version;
            public Sprite[] idle,run,attack,hit,enterRun;
            public float idleTime=.18f,runTime=.09f,attackTime=.10f,hitTime=.045f,enterTime=.06f;
            public float referenceWidth=240,cellWidth=320;
        }
        public string sourceSignature;
        public int revision;
        public List<Entry> entries=new List<Entry>();
        Dictionary<string,Entry> lookup;
        int cachedRevision=-1;
        public Entry Find(string species)
        {
            if(lookup==null||cachedRevision!=revision)
            {
                lookup=new Dictionary<string,Entry>();foreach(var entry in entries)lookup[entry.species]=entry;
                cachedRevision=revision;
            }
            return lookup.TryGetValue(species,out var result)?result:null;
        }
    }
}
