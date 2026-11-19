using System;
using UnityEngine;

namespace CatTanks
{
    public sealed class StudioUIRoot : MonoBehaviour
    {
        [Serializable] public struct ComponentPrefab { public string kind; public GameObject prefab; }
        public RectTransform viewport, screens, popups, notifications;
        public StudioUITheme theme;
        public bool mockupKit;
        public ComponentPrefab[] components;
        StudioUIView[] views;
        public StudioUIView[] Views=>views??(views=GetComponentsInChildren<StudioUIView>(true));
        public void InvalidateViews(){views=null;}
        public StudioUIView Find(string key)
        {
            foreach(var view in Views) if(view.key==key)return view;
            return null;
        }
        public GameObject Component(string kind)
        {
            if(components!=null)foreach(var item in components)if(item.kind==kind)return item.prefab;
            return null;
        }
    }
}
