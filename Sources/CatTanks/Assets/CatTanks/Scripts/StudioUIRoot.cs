using System;
using UnityEngine;

namespace CatTanks
{
    public sealed class StudioUIRoot : MonoBehaviour
    {
        [Serializable] public struct ComponentPrefab { public string kind; public GameObject prefab; }
        public RectTransform viewport, screens, popups, notifications;
        public ComponentPrefab[] components;
        public StudioUIView Find(string key)
        {
            foreach(var view in GetComponentsInChildren<StudioUIView>(true)) if(view.key==key)return view;
            return null;
        }
        public GameObject Component(string kind)
        {
            if(components!=null)foreach(var item in components)if(item.kind==kind)return item.prefab;
            return null;
        }
    }
}
