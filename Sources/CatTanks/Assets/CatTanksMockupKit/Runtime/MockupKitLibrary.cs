using System;
using System.Collections.Generic;
using UnityEngine;

namespace CatTanks.MockupKit
{
    public sealed class MockupKitLibrary : ScriptableObject
    {
        [Serializable] public sealed class Entry { public string key; public Sprite sprite; }
        public List<Entry> entries = new List<Entry>();
        public Sprite Get(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;
            foreach (var entry in entries) if (entry.key == key) return entry.sprite;
            return null;
        }
    }
}
