using System;
using System.Collections.Generic;
using UnityEngine;

namespace MegaTanks
{
    // Keys are stable binding IDs. The object can be renamed or moved in the editor.
    public sealed class UIView : MonoBehaviour
    {
        [Serializable] public struct Binding { public string key; public RectTransform target; }
        [SerializeField] Binding[] bindings = Array.Empty<Binding>();
        Dictionary<string, RectTransform> index;
        public T Get<T>(string key) where T : Component
        {
            if(index == null) { index = new Dictionary<string,RectTransform>(); foreach(var b in bindings) index[b.key] = b.target; }
            if(!index.TryGetValue(key, out var rect) || rect == null) throw new InvalidOperationException(name + ": missing UI binding " + key);
            var result = rect.GetComponent<T>();
            if(result == null) throw new InvalidOperationException(name + ": " + key + " needs " + typeof(T).Name);
            return result;
        }
        public void CaptureBindings()
        {
            var list = new List<Binding>();
            foreach(var rect in GetComponentsInChildren<RectTransform>(true))
            {
                string key = rect == transform ? "$root" : rect.name;
                var parent = rect.parent;
                while(parent != null && parent != transform && rect != transform) { key = parent.name + "/" + key; parent = parent.parent; }
                list.Add(new Binding { key = key, target = rect });
            }
            bindings = list.ToArray(); index = null;
        }
    }
}
